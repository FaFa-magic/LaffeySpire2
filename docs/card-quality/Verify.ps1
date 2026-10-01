param(
    [string]$GameDataDir = 'D:/STEAM/steamapps/common/Slay the Spire 2/data_sts2_windows_x86_64',
    [string]$RitsuPath = 'C:/Users/15832/.nuget/packages/sts2.ritsulib/0.5.20/lib/net9.0/STS2-RitsuLib.dll'
)
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$sdkDir = Join-Path 'C:/Program Files/dotnet/sdk' ((& dotnet --version).Trim())
Add-Type -Path (Join-Path $sdkDir 'Roslyn/bincore/Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $sdkDir 'Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll')
$parseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::Preview)
$trees = [System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
$trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText('global using System; global using System.Collections.Generic; global using System.Linq; global using System.Threading.Tasks;', $parseOptions))
foreach ($file in Get-ChildItem (Join-Path $projectRoot 'LaffeySpire2Code') -Recurse -Filter '*.cs') {
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file.FullName), $parseOptions, $file.FullName))
}
$references = [System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$refRoot = Get-ChildItem 'C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref' -Directory | Where-Object Name -Like '9.*' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
foreach ($file in Get-ChildItem (Join-Path $refRoot.FullName 'ref/net9.0') -Filter '*.dll') {
    $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($file.FullName))
}
foreach ($path in @((Join-Path $GameDataDir 'sts2.dll'), (Join-Path $GameDataDir '0Harmony.dll'), (Join-Path $GameDataDir 'GodotSharp.dll'), $RitsuPath)) {
    $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path))
}
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary).WithMetadataImportOptions([Microsoft.CodeAnalysis.MetadataImportOptions]::All)
$compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create('LaffeyQualityStaticVerification', $trees, $references, $options)
$errors = @($compilation.GetDiagnostics() | Where-Object Severity -EQ 'Error')
if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Output $_.ToString() }
    throw "Static compilation failed: $($errors.Count) errors."
}
Write-Output "PASS: $($trees.Count - 1) production C# files; no DLL or PCK emitted."
$inspector = $compilation.GetTypeByMetadataName('MegaCrit.Sts2.Core.Nodes.Screens.NInspectCardScreen')
$memberLookup = [Microsoft.CodeAnalysis.INamespaceOrTypeSymbol].GetMethod('GetMembers', [type[]]@([string]))
foreach ($field in @('_cards', '_index', '_card', '_leftButton', '_rightButton', '_hoverTipRect', '_upgradeTickbox')) {
    if (@($memberLookup.Invoke($inspector, @($field))).Count -ne 1) { throw "Missing inspector injection field: $field" }
}
foreach ($method in @('Open', 'Close', 'UpdateCardDisplay')) {
    if (@($memberLookup.Invoke($inspector, @($method))).Count -ne 1) { throw "Missing or ambiguous inspector patch target: $method" }
}
$library = $compilation.GetTypeByMetadataName('MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary.NCardLibrary')
if (@($memberLookup.Invoke($library, @('ShowCardDetail'))).Count -ne 1) { throw 'Missing library patch target.' }
Write-Output 'PASS: Harmony targets and private inspector fields in the installed sts2.dll.'
foreach ($language in @('zhs', 'eng')) {
    foreach ($table in @('cards', 'powers', 'static_hover_tips', 'rest_site_ui')) {
        $path = Join-Path $projectRoot "LaffeySpire2/localization/$language/$table.json"
        if (!(Test-Path -LiteralPath $path)) {
            if ($language -eq 'zhs') { throw "Missing Chinese localization: $table" }
            continue
        }
        $json = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable
        if ($table -eq 'cards' -and !$json.ContainsKey('LAFFEY_SPIRE2_CARD_RIGGING_REFIT.description')) { throw "Missing Refit translation: $language" }
        if ($table -eq 'rest_site_ui' -and !$json.ContainsKey('OPTION_LAFFEY_REFIT.prompt')) { throw "Missing rest site prompt: $language" }
    }
}
Write-Output 'PASS: Available localization JSON.'
$rarityRanks = @{ Basic=0; Common=1; Uncommon=2; Rare=3; Ancient=4 }
$cardRanks = @{}
foreach ($file in Get-ChildItem (Join-Path $projectRoot 'LaffeySpire2Code/Cards') -Recurse -Filter '*.cs') {
    $cardSource = [IO.File]::ReadAllText($file.FullName)
    if ($cardSource -match 'CardRarity\.(\w+)') {
        $rank = if ($rarityRanks.ContainsKey($Matches[1])) { $rarityRanks[$Matches[1]] } else { 5 }
        $stem = 'LAFFEY_SPIRE2_CARD_' + ([regex]::Replace($file.BaseName, '(?<=[a-z0-9])(?=[A-Z])', '_')).ToUpperInvariant()
        $cardRanks[$stem] = $rank
    }
}
foreach ($language in @('zhs', 'eng')) {
    $path = Join-Path $projectRoot "LaffeySpire2/localization/$language/cards.json"
    if (!(Test-Path -LiteralPath $path)) { continue }
    $json = Get-Content $path -Raw | ConvertFrom-Json -AsHashtable
    $previousRank = -1
    $previousStem = ''
    foreach ($key in $json.Keys) {
        $stem = $key.Split('.')[0]
        if ($stem -eq $previousStem) { continue }
        if (!$cardRanks.ContainsKey($stem)) { throw "Unknown localization card: $stem" }
        $rank = $cardRanks[$stem]
        if ($rank -lt $previousRank -or $rank -eq $previousRank -and [string]::CompareOrdinal($previousStem, $stem) -gt 0) { throw "Card localization order is incorrect: $stem" }
        $previousRank = $rank
        $previousStem = $stem
    }
}
Write-Output 'PASS: Native-rarity and alphabetical card localization order.'
$source = [IO.File]::ReadAllText((Join-Path $projectRoot 'LaffeySpire2Code/Cards/LaffeyCardModel.cs'))
if ($source -notmatch '\[SavedProperty\]\s+public int QualityRank') { throw 'Quality must use an official saved property.' }
if ($source -match 'override IEnumerable<IHoverTip> ExtraHoverTips') { throw 'Use AdditionalHoverTips.' }
$refitSource = [IO.File]::ReadAllText((Join-Path $projectRoot 'LaffeySpire2Code/Cards/Common/RiggingRefit.cs'))
if ($refitSource -notmatch 'new BlockVar\(5M' -or $refitSource -notmatch 'CreatureCmd\.GainBlock') { throw 'Rigging Refit must gain five Block.' }
$previewSource = [IO.File]::ReadAllText((Join-Path $projectRoot 'LaffeySpire2Code/CardQuality/QualityRestSiteOption.cs'))
if ($previewSource -notmatch 'card\.MutableClone\(' -or $previewSource -notmatch 'preview\.QualityRank' -or $previewSource -notmatch 'new CardTransformation\(card, preview\)' -or $previewSource -notmatch 'NDeckTransformSelectScreen\.ShowScreen' -or $previewSource -notmatch 'SyncLocalChoice') { throw 'Rest preview must use the native transform preview and synchronize selection.' }
$qualitySources = Get-ChildItem (Join-Path $projectRoot 'LaffeySpire2Code/CardQuality') -Filter '*.cs'
$qualitySources += Get-Item (Join-Path $projectRoot 'LaffeySpire2Code/Patches/LaffeyQualityPatches.cs')
$inspectorPatchSource = [IO.File]::ReadAllText((Join-Path $projectRoot 'LaffeySpire2Code/Patches/LaffeyQualityPatches.cs'))
if ($inspectorPatchSource -notmatch 'source\.Duplicate\(' -or $inspectorPatchSource -notmatch 'NGoldArrowButton') { throw 'Quality arrows must reuse native gold arrows.' }
if ($inspectorPatchSource -match 'LaffeyQualityLabel|LAFFEY_QUALITY_\{state\.Rank\}\.title') { throw 'Do not overlay quality text on the card.' }
foreach ($file in $qualitySources) {
    if ([IO.File]::ReadAllText($file.FullName) -match 'HarmonyTranspiler|CodeInstruction') { throw 'Quality system must not use a Transpiler.' }
}
Write-Output 'PASS: SavedProperty persistence, native quality arrows and no quality-system Transpiler.'
