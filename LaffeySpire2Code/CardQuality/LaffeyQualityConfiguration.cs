namespace LaffeySpire2.LaffeySpire2Code.CardQuality;

public sealed class LaffeyQualityConfiguration
{
	public static LaffeyQualityConfiguration Empty { get; } = new();
	public int EnergyCostAdjustment { get; }
	public IReadOnlyDictionary<string, decimal> Values { get; }

	public LaffeyQualityConfiguration(int energyCostAdjustment = 0, IReadOnlyDictionary<string, decimal>? values = null)
	{
		EnergyCostAdjustment = energyCostAdjustment;
		Values = new System.Collections.ObjectModel.ReadOnlyDictionary<string, decimal>(
			values?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new Dictionary<string, decimal>());
	}

	public decimal ValueAdjustment(string name) => Values.TryGetValue(name, out decimal value) ? value : 0M;
}
