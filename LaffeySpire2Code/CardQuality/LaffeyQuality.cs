using LaffeySpire2.LaffeySpire2Code.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace LaffeySpire2.LaffeySpire2Code.CardQuality;

public static class LaffeyQuality
{
	public static bool CanIncrease(CardModel card) => card is LaffeyCardModel { HasQualityVersions: true }
		&& card.Rarity is CardRarity.Common or CardRarity.Uncommon;

	public static CardRarity RarityForRank(int rank) => rank switch
	{
		1 => CardRarity.Common,
		2 => CardRarity.Uncommon,
		3 => CardRarity.Rare,
		_ => throw new ArgumentOutOfRangeException(nameof(rank))
	};

	public static int RankForRarity(CardRarity rarity) => rarity switch
	{
		CardRarity.Common => 1,
		CardRarity.Uncommon => 2,
		CardRarity.Rare => 3,
		_ => throw new ArgumentOutOfRangeException(nameof(rarity))
	};

	public static bool TryIncreaseInCombat(CardModel card)
	{
		if (card.IsCanonical || !CanIncrease(card) || card.CombatState == null || card.Pile?.Type is not
			(PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust or PileType.Play))
			return false;
		return TryIncrease(card);
	}

	public static bool TryIncreasePermanently(CardModel card)
	{
		if (card.IsCanonical || card.Pile?.Type != PileType.Deck)
			return false;
		return TryIncrease(card);
	}

	private static bool TryIncrease(CardModel card)
	{
		if (!CanIncrease(card))
			return false;
		((LaffeyCardModel)card).QualityRank = RankForRarity(card.Rarity) + 1;
		NCard? node = NCard.FindOnTable(card);
		if (node != null)
		{
			node.Model = null;
			node.Model = card;
			node.UpdateVisuals(node.DisplayingPile, CardPreviewMode.Normal);
		}
		return true;
	}

	public static IHoverTip IncreaseHoverTip => new HoverTip(
		new LocString("static_hover_tips", "LAFFEY_QUALITY_INCREASE.title"),
		new LocString("static_hover_tips", "LAFFEY_QUALITY_INCREASE.description"));
}
