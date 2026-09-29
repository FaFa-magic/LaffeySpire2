# Patchwork 白鹰芯片装配台（第二版）

## 场景与美术
游戏入口为 `LaffeySpire2/scenes/ui/patchwork_screen.tscn`，顶部栏通过 PackedScene 实例化并注入玩家。交互预览为 `patchwork_preview.tscn`，使用独立内存样例，不操作玩家存档或真实奖励；PreviewLanguage 支持 zhs / eng。独立芯片图鉴为 `patchwork_chip_gallery.tscn`，一次展示全部 34 个编号。

背景使用 `images/patchwork/workbench-v2.png`：俯视芯片装配台、示波器、焊接工具、零件盘、防静电垫及白鹰风格装饰。中央使用 `chip-socket-v2.png`：四周真实金属引脚、银白机框、紧固螺钉与青蓝状态灯。独立芯片位于 `images/patchwork/chips/chip_00.png` 至 `chip_33.png`。每张是一整块异形封装的 PNG 模型插画，具有连续线路、金属边框和不同角色头像，不是每格重复贴图，也不是 3D 网格模型。画布按逻辑格数和同一个正方形像素单元导出，缺失格透明，绘制只使用等比缩放；不按 alpha 边界裁边后补偿拉伸。早期背景和元件图集保留，但新场景不再引用。2026-09-27 的逐芯片比例、重绘与旋转回归详见 [芯片方格比例修正](chip-proportions.md)。

工作区设计尺寸 1680×1000，随视口等比缩放，主板水平中心与视口中心一致。左侧为规则、奖励效果和获得状态；右侧为滚动库存、选中整块预览、旋转/翻转、确认和关闭。图片不烘焙文字。实际 Godot 1920×1080 截图保存在本目录，以 `patchwork_v2_` 为前缀，包括中英文预览、安装后状态与芯片图鉴。

按住左键可以将库存芯片拖入主板，也可以拖动待放预览和已安装芯片。抓取板内芯片时保持抓取格偏移，跨格吸附；松手只保留预览，按确认才提交。拖到板外松手、拖动时按 Esc 或窗口失去焦点，会取消本次拖动。旋转、翻转和方向键仍可使用。调整已安装芯片位置不会增减库存，已领取的奖励永久保留且不重复发放。

入口和提交动作共用 `PatchworkAccess.CanUse`，按 CombatManager 的 IsInProgress / IsStarting 判断；战斗结束后的奖励页面虽然仍在 CombatRoom，也能显示入口和装配。进入下一场战斗时关闭已经打开的装配台。网格线在所有格底色之后统一绘制，避免相邻格覆盖边界。

`PatchworkVisuals.DrawChip` 一次绘制完整 PNG，通过一个缩放标量和旋转/镜像基向量变换整块芯片，视觉变换与放置判定一致。仅裁切主板外围，不逐格切割金属边框和透明缺口；安装后的轮廓由原画金属框提供。非法预览红色，合法预览半透明青蓝色，提示轮廓的凸角采用倒角。2026-09-28 的 17 张芯片边框、长门与塔什干头像修订，以及最新导出与渲染规则见 [修订记录](rim-refresh.md)。

## 接线与扩展
### 商店芯片栏位

`patchwork_merchant_slot.tscn` 是独立的 1×1 拉菲芯片商品，位于删牌服务右侧，与遗物行对齐，随官方商店地毯一起移动。图像缩小至本地 128×128，并增加灰色边框；点击区域也相应收窄，避开删牌服务向左、向上延伸的真实点击区域，双方悬停放大时仍不重叠。使用已有 `chip_00.png` 整块芯片图片、官方金币图标和价格字体；基础价格固定 50，不参与随机浮动，官方商店折扣生效。悬停说明支持中英文，金币不足显示红色价格，售出后与官方遗物一样隐藏商品。

`PatchworkMerchantSlot` 继承官方 `NMerchantSlot`，保留原生鼠标/手柄输入、悬停缩放、商人指示手、购买失败动画。通过 RitsuLib 的 `IPatchMethod` 注册将商品并入 `MerchantInventory.AllEntries` 和 `NMerchantInventory.GetAllSlots`，并在原生导航更新后连接额外栏位。没有将芯片伪装成遗物或药水，也不占用它们原有的商品数量。

`PatchworkMerchantEntry` 继承官方 `MerchantEntry`。购买由 RitsuLib 托管同步动作执行，调用官方购买包装方法以保留 `AfterItemPurchased`、购买完成/失败事件和其他商品刷新。由于会员卡等折扣依赖 `LocalContext`，价格由商品所属玩家计算，再通过官方 `PlayerChoiceSynchronizer` 同步；各端扣除相同金额，并且不再额外发送金币奖励同步以避免重复扣款。动作携带商店标识，拒绝跨店的过期请求。每店一次、总计最多 5 块（包括已安装芯片）的既有规则保留，信使不会补货。

本地 Godot 验证覆盖原生栏位/商品注册、鼠标购买、手柄导航、等待同步时的重复点击、50 金币扣款、金币不足、售罄、重复购买、玩家独立库存、折扣金额、下一商店和 5 块上限。使用官方删牌服务的实际点击区域偏移，检查双方普通/悬停缩放组合，并通过真实视口鼠标事件分别点击删牌和芯片。购买后通过 RitsuLib 的 JSON 保存/恢复验证芯片库存与售罄标识仍保留。验证使用当前安装的游戏与 RitsuLib 程序集，在简化商店场景中运行；尚未进行双机实机联机测试。

`LaffeyPillowRelic.CardsPlayedThisTurn` 的存档 setter 使用 `protected`，使官方 `SavedProperties.FillInternal` 从具体抱枕遗物类型反射时能够找到 setter。原属性名和存档格式保留，旧存档无需迁移。原生 `RelicModel.ToSerializable` → JSON → `FromSerializable` 验证覆盖两个抱枕遗物的零计数、激活前计数、触发阈值、状态恢复与旧存档显式零值。

`PatchworkGeometry.LargestSquare(bool[,], minimumSize)` 动态规划返回实际完整方阵的 X、Y、Size。并列时取最上方、再最左方。至少 3×3 的最大已安装方阵以青蓝外发光框展示，将完成新奖励的预览方阵以金色框展示。

`PatchworkBoardView.RewardPort(size)` 定义插座输出点。`PatchworkWiringView` 根据控件实际变换将插座接到左侧 LED，适配缩放与翻译引起的行高变化。主板内部也从完成方阵接线到输出点。已获得奖励显示青蓝线路和灯，待确认奖励显示金色线路和灯，未获得显示灰色；亮起的线路带移动光点。

`PatchworkBoard.NewSquares` 返回从 FirstRewardSize 到最大方阵之间的全部未领取档位，一次跨档也会全部解锁。AvailablePieces、Placements、ClaimedSquares、PurchasedShops 保持不变，不需迁移存档。生产界面通过现有 PatchworkActions 联机动作提交 Placement；同步期间阻止重复确认。真实奖励继续由 PatchworkActions / PatchworkHooks 发放。

`PatchworkBoard.TryApply` 是安装和移动共用的确定性提交入口。移动忽略该芯片原先占用的格子，但仍检查与其他芯片的重叠和越界。移动动作携带 PlacementIndex 与完整 OriginalPlacement，执行前核对当前记录；过期、重复、原地移动请求不生效。新增动作 Kind=2 使用已有托管同步队列，与安装和购买一样由动作所属玩家的 NetId 决定存档归属，载荷不能指定其他玩家。个人库存、摆放、奖励与商店进度继续保存在 RegisterPerPlayer 槽位。

| 方阵 | 保留的实际奖励 |
| --- | --- |
| 3×3 | 战斗开始时 3 活力 |
| 4×4 | 战斗开始时 6 格挡 |
| 5×5 | 战斗开始时 1 力量 |
| 6×6 | 战斗开始时 1 敏捷 |
| 7×7 | 获得稀有遗物 |
| 8×8 | 古老牙齿 / 欧罗巴斯之触，沿用原选择与重复处理 |
| 9×9 | 每回合多抽 2 张牌 |
| 10×10 | 每回合额外 1 能量 |

修改奖励数值只需更新 `LaffeySpire2Code/Patchwork/PatchworkBalance.cs` 中的命名常量；动作、战斗钩子和 `REWARD_n` 的 `{Amount}` 翻译共用这些数值。商店基础价格与持有上限也在此文件，分别绑定 `{BasePrice}`、`{MaxChips}`。例如将 `CombatStartVigor` 改为 4，中英文奖励说明和实际获得活力都会变为 4，无需修改 JSON。改变奖励类型时仍需同步修改逻辑和翻译。新增芯片需扩展原 Shapes、独立图片、CHIP_nn 翻译及 PatchworkVisuals 缓存大小。

文字使用 `gameplay_ui` 的 `LAFFEY_PATCHWORK_*` 键，包含 RULES_TITLE、RULES_TEXT、CHIP_00…CHIP_33、REWARD_3…REWARD_10 与状态文案。Count、Type、Id、Cells、X、Y、Size、MinimumSize、Amount、BasePrice、MaxChips 参数在中英文中保持一致。`PatchworkVisuals.LocalizedText` 为生产界面和商店悬浮提示统一注入参数，独立预览读取相同配置；底板尺寸和最低奖励尺寸来自 `PatchworkBoard` 现有常量。奖励档位和存档键保持不变。生产环境使用 LocString，古代遗物使用游戏本地化标题。其他语言提供同名键即可；独立预览暂支持现有两种语言。

卡牌平衡值继续使用官方 `CanonicalVars` / `DynamicVars`：决战时刻的最早回合使用 `EarliestTurn`，美梦的负面层数上限使用 `DebuffCap`，梦游战术与火力觉醒使用已有 PowerVar。战意的每层伤害增长统一在 `FightingSpiritPower.GrowthPerStack`，供卡牌和能力共用。能力描述中的叠层数使用官方 `{Amount}`。调整这些值后，重新编译并导出资源即可；无需逐语言改数值。

## 验证与运行

```powershell
dotnet run --no-restore --project tests/PatchworkLogic/PatchworkLogic.csproj
./tests/PatchworkUi/Verify.ps1
```

UI 检查支持 GodotPath / GameDataDir 参数。脚本编译全部生产 Patchwork 源码，仅以测试替身代替角色身份类型和模组入口。输出保存在忽略的 `.verification/patchwork/`，不部署到真实游戏 mods。独立项目位于 `.verification/patchwork/preview/project.godot`，可以使用 Godot Mono 打开；默认运行交互预览，图鉴也可单独运行。

`tests/` 为本地验证辅助目录，按项目约定在 .gitignore 中忽略，不是游戏运行依赖。新增检查包括真实鼠标事件驱动的库存拖入、板内拖动、板外取消、已安装芯片移动、已获奖励保留和截图中央竖线像素检查。运行时检查使用当前安装的真实 RitsuLib 按玩家存储实现，以两名玩家和两份运行状态验证 JSON 动作重放一致性、库存/奖励/商店独立以及该槽位的 JSON 保存/重进快照恢复。仅为持久化测试创建轻量玩家/运行身份，不模拟完整战斗或网络连接。这些检查不能代替两台设备的实机联机测试。

通过：2,000 个随机棋盘与穷举对照、272 种旋转/翻转、越界/重叠/库存/跨档奖励/去重检查；Patchwork 模块零错误零警告编译；实际 Godot 中英文选择、旋转、翻转、非法位置、奖励预览和确认安装；4×4→5×5 展示更新；全部 34 张图片加载和渲染。

当前整项目构建已通过（0 错误，Barrage.cs 保留 1 个既有空引用警告），Patchwork 模块单独编译零警告。尚未做两台设备的真实战斗/联机验证，也未导出整包。预览灯不等于真实奖励发放测试。

## 图片生成记录
使用内置 imagegen，每个资产独立生成后复制入项目。实际调用提示词记录在 `imagegen-v2-prompts.json`，包含工作台、插座、34 种芯片及轮廓修订。风格统一为正交俯视、白鹰海军科技、清晰金属边框与金色线路，不同彩色封装区分芯片。
