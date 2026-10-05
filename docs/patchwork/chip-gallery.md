# 芯片图鉴

装配台主板右上方的书形按钮打开全芯片图鉴。按钮使用透明底书本素材，并继承官方 NTopBarButton，复用悬停转动、放大、按下变暗及移开回弹动画。普通 Button 的蓝色边框和默认 TooltipText 已移除。悬停或手柄聚焦时，使用官方 NHoverTipSet 显示本地化标题及图鉴说明；打开图鉴、移开光标或退出场景时清除提示框。图鉴复用现有 00–33 芯片图片和 PatchworkPieceView，保持每块芯片的整体形状和等比缩放。

图鉴作为装配台的本地全屏子层，拦截鼠标操作并把焦点留在关闭按钮。右上角 × 或 Escape 关闭图鉴；使用官方方向导航模式（手柄或纯键盘）时将焦点还给书本，鼠标模式不强制聚焦，避免光标离开后提示框仍然残留。底板选择和装配数据保留。查看不调用安装、奖励、存档或联机同步命令。运行时名称、标题和关闭提示使用官方 LocString；独立预览仍读取预览语言 JSON。

顶部保留游戏状态栏和遗物栏空间。书本启用 mipmaps，降低小尺寸显示时细线闪烁。

图鉴入口沿用官方顶部按钮的默认 Arrow 光标形状，取消 Godot 的 PointingHand 覆盖。图鉴不加载或替换光标图片，也不调用 OverrideCursor / StopOverridingCursor；游戏光标及其他模组提供的替换由官方 NCursorManager 继续管理。选中芯片后在底板内使用扳手的逻辑保持原样。

光标回归检查覆盖书本、图鉴、关闭按钮的 Arrow 类型，模拟其他模组通过官方管理器替换光标后的保留，以及选中芯片后扳手光标的使用与返回。日志：`verification-chip-gallery-cursor.log`。

未曾获得的芯片去色为灰阶，并保留 82% 的亮度及原透明边缘；已获得芯片显示原色。获得状态从当前玩家已同步的待安装芯片与主板芯片取并集，安装和移动不会失去图鉴颜色。现有获取途径不会丢弃芯片，因此无需增加重复的历史存档字段。图鉴打开期间每 0.15 秒检查该玩家的最新状态。存档仍遵循官方检查点：未存档的获取随继续游戏回退，实时联机重连保留本局的最新获取状态。旧存档直接适用。

灰阶材质直接使用 fragment 输入的 COLOR，避免重复采样相乘而过度变暗：[Godot 4.5 CanvasItem shader 文档](https://docs.godotengine.org/en/4.5/tutorials/shaders/shader_reference/canvas_item_shader.html#color-and-texture)。

素材：`LaffeySpire2/images/patchwork/chip_catalog_book.png`。生成方式和完整提示词：`book-button-prompt.txt`。

验证：生产 Patchwork 模块在隔离 Godot 预览中通过书本悬停/点击、34 块芯片显示、底板点击及右键隔离、官方鼠标恢复、关闭/重复打开/Escape，以及查看前后装配数据不变检查。检查日志：`verification-chip-gallery.log`；界面截图：`chip-gallery-entry-preview.png`、`chip-gallery-open-preview.png`。

原生按钮验证在隔离预览中只读加载游戏资源包，使用实际 NTopBarButton 动画和实际 NHoverTipSet、HoverTip、MegaLabel 场景渲染，确认悬停提示显示、移开时移除与回弹、打开图鉴时清除提示以及退出后的清理。截图：`chip-gallery-native-hover-preview.png`；日志：`verification-chip-gallery-native-hover.log`。

关闭后的焦点回归检查覆盖鼠标点击关闭、鼠标模式 Escape、再次悬停、手柄模式返回书本及提示清理；全项目编译通过（0 警告、0 错误）。日志：`verification-chip-gallery-focus.log`。
