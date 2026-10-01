# 卡牌品质系统

Common、Uncommon、Rare 原生拉菲牌拥有三档品质。Basic 等其他牌不参与。原生品质来自构造函数，默认卡池与图鉴排序不变；当前品质由 `Rarity` 返回。现在所有档位沿用原有效果，不额外注册同名卡牌模型。

普通升级和品质互相独立。品质变化不修改 `CurrentUpgradeLevel`，普通升级或降级不清除品质。图鉴在卡牌下方、“查看升级”两侧复用缩小的原版金色箭头，不在卡面添加品质文字。新增箭头只修改展示副本，切换另一张牌后恢复该牌的原生品质；原有升级预览继续有效。

## 调用入口

- `LaffeyQuality.CanIncrease(card)`：原生三档牌且当前为白卡或蓝卡。
- `LaffeyQuality.TryIncreaseInCombat(card)`：仅作用于战斗牌堆中的牌，不触碰 `DeckVersion`。战斗复制牌继承当前品质，原永久牌组不变。
- `LaffeyQuality.TryIncreasePermanently(card)`：仅作用于永久牌组中的牌。
- `LaffeyQuality.IncreaseHoverTip`：提升品质说明，可在 `AdditionalHoverTips` 中使用。
- `LaffeyCardModel.NativeRarity`：该牌的原生品质。
- `LaffeyCardModel.QualityRank`：官方 `[SavedProperty]` 字段，0 表示原生品质，1/2/3 表示 Common/Uncommon/Rare。设置时必须是可变实例。

战斗选牌采用官方 `CardSelectCmd.FromHand`。火堆选牌复用官方 `NDeckTransformSelectScreen` 与 `CardTransformation` 的左右预览，按官方 `CardSelectCmd.FromDeckForTransformation` 的选牌同步流程选择拉菲牌，不 patch 预览界面。品质随官方 `SerializableCard.Props` 保存和传输，由 RitsuLib 注册继承的存档字段。多人游戏需要所有玩家使用相同版本的模组和 RitsuLib。

## 逐张配置费用与数值

在卡牌类中覆盖 `GetQualityConfiguration(CardRarity quality, bool upgraded)`。配置是相对原有效果的增量，不是最终数值：费用增量作用于基础费用，数值增量按 `DynamicVars` 的名称应用。原生品质配置应返回 `Empty`。普通升级逻辑仍写在原有 `OnUpgrade` 中；框架负责在升级、降级、品质改变及存档恢复后协调配置。重复刷新不会重复累计增量，复制保留已应用的配置。

```csharp
protected override LaffeyQualityConfiguration GetQualityConfiguration(CardRarity quality, bool upgraded)
{
    return quality switch
    {
        CardRarity.Uncommon => new(values: new Dictionary<string, decimal> { ["Block"] = upgraded ? 5M : 4M }),
        CardRarity.Rare => new(-1, new Dictionary<string, decimal> { ["Block"] = upgraded ? 9M : 8M }),
        _ => LaffeyQualityConfiguration.Empty
    };
}
```

以上为原生 Common 牌的示例，没有应用到现有牌。`EnergyCostAdjustment` 面向固定费用卡牌，不用于将固定费用与 X 费用相互转换。设置数值需使用该牌已经声明的变量名。

## 逐张配置机制

`QualityValue(common, uncommon, rare)` 用于按当前品质选择机制参数，在 `OnPlay` 或钩子里读取即可，也可以直接按 `Rarity` 分支。

`ConfigureQualityMechanics()` 用于按品质、`IsUpgraded` 调整固有关键词等状态。此方法会反复调用，必须幂等；反序列化时可能还没有 Owner，不能在这里访问玩家、应用能力、抽牌或消耗随机数。

`OnQualityChanged(previous, current)` 用于实际品质变化的附加处理；存档恢复和图鉴预览也会触发，不能在这里执行战斗奖励或其他局内副作用。游戏效果放到正常打牌逻辑或官方钩子中。

## 示例及验证

舰装改造（Rigging Refit）：1 费白卡技能，获得 5 点格挡并提升一张拉菲手牌的品质；升级后提升所有可提升手牌，格挡不变。火堆的“改造”只对拉菲及其皮肤角色开放，选择后使用官方变换选牌界面的确认页展示左侧原牌、右侧品质提升后的副本；确认后才永久改造，取消不消耗火堆行动。品质为金卡、任务牌及不可变换的牌不出现在改造候选中。

`./docs/card-quality/Verify.ps1` 对生产代码做 Roslyn 静态编译和安装版游戏 API 检查，并验证本地化、存档字段及无 Transpiler。不会生成 DLL 或 PCK。

尚需游戏内回归：图鉴三档切换及升级预览、切牌和关闭重开、火堆取消与确认、改造后存档重载、临时改造后战斗结束、战斗复制牌，以及双人战斗选牌和火堆同步。
