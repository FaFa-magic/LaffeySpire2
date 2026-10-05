# 芯片悬浮提示

战后奖励文字为 `电子元件 {Piece}`，仍以原 PieceId 格式化。

`PatchworkVisuals.PieceHoverTip` 是统一的提示定义。标题包括角色芯片名、编号和格数；描述使用 `PIECE_DESCRIPTION` 本地化，方阵下限从 `PatchworkBoard.FirstRewardSize` 注入。00–33 共 34 种芯片均适用。

战后奖励通过 `PatchworkPieceReward.HoverTips` 提供提示，保留 `base.HoverTips`，由官方 `NRewardButton` 处理鼠标、手柄焦点、按下及取消提示。无需修改官方奖励节点或奖励同步逻辑。

待装配列表和选中芯片预览通过 `PatchworkPieceHoverTip` 使用同一提示定义，以官方 `NHoverTipSet` 显示在控件左侧。移除列表原先的 Godot TooltipText；鼠标移开、拖动、等待同步、禁用、打开图鉴及退出场景时清理提示。普通鼠标选中后保留的按钮焦点不会独立触发提示，手柄和纯键盘模式的聚焦仍然适用。

删除未被代码或场景引用的 `LAFFEY_PATCHWORK_TYPE_0` 至 `TYPE_5` 六条旧元件类型文本。检查包含完整键、短键和 `CHIP_00..33` / `REWARD_3..10` 动态键；其余 71 条均有引用。保留用户已有的其他文案调整。

隔离预览只读加载官方资源包，使用真实 `NRewardButton`、`NHoverTipSet` 和提示框场景验证显示及清理，查看前后装配数据不变。存档及玩家同步检查、拖动装配检查和整项目编译均通过。日志：`verification-chip-piece-hover-tips.log`。
