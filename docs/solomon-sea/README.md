# 所罗门海

单人使用拉菲，或多人队伍至少包含一名拉菲（包括皮肤变体）时，追加第四幕。其他角色对局保持原流程；已经由其他模组提供第四幕的对局不会被替换。

路线为宝箱 → 商店 → 火堆 → 精英 → 精英 → 火堆 → Boss，共七个房间，单人和多人一致。没有先古房。暂时复用 Glory 的地图、背景、火堆、宝箱、音乐，两场精英依次使用官方 `KnightsElite` 和 `MechaKnightElite`，Boss 使用官方 `QueenBoss`；后续替换 `SolomonSea.InitialEliteEncounters`、`SolomonSea.PlaceholderBoss` 和幕资源即可接入正式内容。第四幕 Boss 后恢复官方建筑师结局流程。

## 接入依据

- RitsuLib 的 `RegisterAct`、`ModActTemplate` 负责模型登记、资源覆盖和网络模型 ID；`AllowInRandomActList` 默认关闭，不改变官方前三幕的随机列表。
- RitsuLib 的 `RegisterActEnterForce` 只能替换已有索引，无法创建原本不存在的第四幕。沿用 RitsuLib 修改 `RunState.Acts` 的方式，在官方 `GenerateRooms` 完成后追加可变幕实例；读档的 `InitializeSavedRun` 完成后兼容尚未包含第四幕的旧存档。
- 官方房间生成先完成，因此第三幕先古分配、遭遇、随机数和 A10 双 Boss 不会被第四幕改变。追加后的 `Acts.Count` 让第三幕 Boss 奖励、最终战计时和 `EnterNextAct` 自动按非最终幕处理，无需改写异步切幕流程。
- 在可变幕实例的官方 `RoomSet.eliteEncounters` 中填入两场精英，沿用 `PullNextEncounter`、`MarkRoomVisited` 及原生访问计数读写，不额外消耗随机数。官方通用 `GenerateRooms` 依赖先古与普通遭遇池，不用于这个无先古、无普通战斗的固定幕。
- `ActModel.CreateMap`、房间数和楼层数不是虚方法，使用仅作用于 `SolomonSea` 的 Prefix。地图采用官方 `ActMap`、`MapPoint`、`AddChildPoint`，遵循起点、网格和 Boss 分开存储的布局。
- 官方 `TreasureRoom(int)` 只接受 0～2，但不存储参数；第四幕宝箱仅在 `CreateRoom` 中使用 `TreasureRoom(2)` 通过检查。宝箱实际资源、遗物和奖励仍读取第四幕的 `runState`。
- 官方 `NRestSiteCharacter._Ready` 的动画索引只支持前三幕。为混合角色联机队伍添加线程隔离的显示上下文，仅在该同步初始化方法中将索引读取为 Glory；Finalizer 始终清理上下文，不调用幕索引 setter，不改变地图位置或实际幕状态。
- 官方 `SerializableMapPoint` 的 JSON 默认忽略 false，而 `CanBeModified` 初始化为 true。因此读档地图进入 `ModifyGeneratedMapLate` 前恢复固定节点标记，不重建地图或清除已访问坐标。
- 官方 `SerializableRoomSet` 会在 JSON 中省略空的事件、普通遭遇和精英遭遇列表，但反序列化不会将缺失字段初始化为空列表；`RoomSet.FromSave` 又直接对这些字段调用 `Select`。所罗门海的事件与普通遭遇列表为空，旧版第四幕的精英列表也为空。通过 `ActModel.FromSave` 的限定 Prefix 在进入官方房间读档前补齐空列表，同时在 `SerializableActModel.Serialize` 前做同样的处理，兼容已有存档及多人存档传输。若精英列表为空，且地图尚未生成或保存地图已有精英节点，则补入两场精英；保留已有非空集合、访问计数、Boss、地图和其他幕。

已经生成并保存的旧四节点地图保持原路线，不改变已访问坐标或进度；尚未生成第四幕地图的旧存档使用新七节点路线，并补齐精英列表。

所有新增补丁均为 Prefix、Postfix 或 Finalizer，无 Transpiler；未修改官方和 RitsuLib 代码。

## 多人及验证

资格判断使用所有玩家的角色，不依赖本地玩家、设置或存档解锁。所有端执行相同追加逻辑；固定地图与精英顺序不消耗额外随机数。继续使用官方 `ActChangeSynchronizer`、`MapSelectionSynchronizer`、宝箱遗物投票、商店和火堆同步，以及 `SerializableActModel` / `SerializableActMap` 存档和网络格式。

运行 `docs/solomon-sea/Verify.ps1` 可在内存中编译并检验项目绑定的游戏和 RitsuLib DLL：七节点连线与坐标、补丁目标解析、混合队伍与皮肤资格、幂等追加、第三幕双 Boss 保留、幕和地图的官方 JSON / 网络往返、三种房间集合缺失或为空的八种组合、两场精英的原生取用与访问计数、首场精英后读档和网络恢复、旧四节点存档兼容、Boss 进度与已有集合保留、宝箱创建、火堆显示上下文及多语言幕名。网络测试在独立进程中设置测试模型 ID 映射后调用官方编码器，核对两个路径得到的字节一致。不写出 DLL 或 PCK。

该验证直接调用生产补丁逻辑，不注入正在运行的游戏。当前 PowerShell 的 .NET 10 运行时不受游戏自带 Harmony 支持；尚未执行 Godot 场景渲染、双端联机、断线重连或完整通关实机验证。
