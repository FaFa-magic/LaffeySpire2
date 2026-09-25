using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Scaffolding.Content;

namespace LaffeySpire2.LaffeySpire2Code.Powers;

public sealed class AwakenedFirepowerPower : LaffeyPowerModel
{
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerAssetProfile AssetProfile => new(
		IconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png",
		BigIconPath: "res://JanusSpire2/images/powers/big/AngelPrayerPower.png"
	);

	protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
		HoverTipFactory.FromPower<StrengthPower>()
	];

	public override CardLocation ModifyCardPlayResultLocation(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		CardLocation location)
	{
		if (card.Owner.Creature != Owner || card.Type != CardType.Attack || card.IsDupe || FirstAttackThisTurn() != null)
			return location;
		location.pileType = PileType.Exhaust;
		return location;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != Owner || cardPlay.Card.Type != CardType.Attack ||
			!cardPlay.IsFirstInSeries || FirstAttackThisTurn()?.CardPlay != cardPlay)
			return;

		Flash();
		await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
	}

	private CardPlayStartedEntry? FirstAttackThisTurn() =>
		CombatManager.Instance.History.CardPlaysStarted.FirstOrDefault(entry =>
			entry.HappenedThisTurn(CombatState) &&
			entry.CardPlay.Player == Owner.Player &&
			entry.CardPlay.Card.Type == CardType.Attack &&
			entry.CardPlay.IsFirstInSeries);
}
