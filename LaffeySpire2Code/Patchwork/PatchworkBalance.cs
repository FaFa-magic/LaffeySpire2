namespace LaffeySpire2.LaffeySpire2Code.Patchwork;

/// <summary>Balance values shared by gameplay and all localized Patchwork descriptions.</summary>
public static class PatchworkBalance
{
	public const int CombatStartVigor = 3;
	public const int CombatStartBlock = 6;
	public const int CombatStartStrength = 1;
	public const int CombatStartDexterity = 1;
	public const int RareRelicCount = 1;
	public const int ExtraCardsPerTurn = 2;
	public const int ExtraEnergyPerTurn = 1;
	public const int ShopBasePrice = 40;
	public const int ShopMaxChips = 10;

	public static (string Name, object Value)[] TextValues(string key) => key switch
	{
		"REWARD_3" => [("Amount", CombatStartVigor)],
		"REWARD_4" => [("Amount", CombatStartBlock)],
		"REWARD_5" => [("Amount", CombatStartStrength)],
		"REWARD_6" => [("Amount", CombatStartDexterity)],
		"REWARD_7" => [("Amount", RareRelicCount)],
		"REWARD_9" => [("Amount", ExtraCardsPerTurn)],
		"REWARD_10" => [("Amount", ExtraEnergyPerTurn)],
		"BOARD" => [("Size", PatchworkBoard.BoardSize)],
		"RULES_TEXT" => [("MinimumSize", PatchworkBoard.FirstRewardSize)],
		"PIECE_DESCRIPTION" => [("MinimumSize", PatchworkBoard.FirstRewardSize)],
		"SHOP_DESCRIPTION" => [("MaxChips", ShopMaxChips), ("BasePrice", ShopBasePrice)],
		_ => []
	};
}
