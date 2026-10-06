# 拉菲入梦转场

使用内置 image_gen 扩展用户提供的睡觉拉菲图片，保留粗轮廓、简洁阴影和柔软的 Q 版画风。睡觉画面和梦境画面均为 1920×1080；生成源图按单一比例缩放，只有不足一像素的外围裁边，没有横向或纵向拉伸。提示词保存于 `art_sources/transitions/laffey_dream/prompts.txt`。

转场共 4.5 秒，缩短开场和静止展示，推进与入梦阶段重叠衔接，呼吸、气泡、星光保留原来的柔缓速度：

- 0–0.45 秒：睡觉场景渐入。
- 0.45–1.05 秒：轻微呼吸、睡眠气泡缓缓上浮。
- 1.05–2.95 秒：镜头等比推进到拉菲脸旁，气泡随镜头移动。
- 2.35–3.5 秒：画面逐渐柔化，梦境光晕从中心扩展开，进入兔子云朵和胡萝卜云海。
- 3.5–3.8 秒：梦境完整展示，云海推进、云朵轻晃、星光闪烁。
- 3.8–4.5 秒：继续梦境动画并完全淡黑。

`LaffeyCharacter.AssetProfile.Ui.CharacterSelectTransitionPath` 绑定 `res://LaffeySpire2/materials/laffey_dream_transition_mat.tres`。材质仍由官方 `NTransition.FadeOut` 的 `threshold` 参数驱动。独立场景 `LaffeySpire2/scenes/transitions/laffey_dream_transition.tscn` 使用相同材质和时长，可在编辑器播放预览。

RitsuLib 注册的补丁只针对本材质将有效正数转场时长设为 4.5 秒，并暂时隐藏官方的额外淡黑子节点，避免提前盖住插画；官方异步任务结束、取消、异常时恢复原可见性。保留官方加载、输入控制和 Instant 模式；不改存档、随机数或网络状态，其他角色的转场不受影响。

验证：生产项目编译 0 警告、0 错误；Godot 实际渲染十个阶段，检查透明开场、完全黑色末帧、官方超出屏幕的转场矩形；检查 16:10 和 21:9 画面覆盖。隔离运行真实 Harmony 补丁后的官方 `NTransition`，验证正常完成、取消、Instant、无效时长、其他材质、输入拦截和异常恢复，偏好设置只使用内存模拟。

动画预览 `laffey-dream-preview.gif` 来自同一场景的 90 张实际渲染帧，20fps，共 4.5 秒。验证日志保存于 `verification-laffey-dream.log`。隔离渲染环境的系统证书库读取错误不影响本地画面；未出现着色器或脚本错误。
