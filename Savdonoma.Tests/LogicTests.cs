using Savdonoma.Core.Logic;

namespace Savdonoma.Tests;

public class LogicTests
{
    [Theory]
    [InlineData(1500, 1.5, 2250)]
    [InlineData(999, 0.333, 333)]   // 332.667 -> 333
    [InlineData(1000, 0.0005, 1)]   // 0.5 -> 1 (AwayFromZero)
    [InlineData(12000, 3, 36000)]
    public void RowSum_rounds_to_whole_sum(long price, double quantity, long expected)
        => Assert.Equal(expected, SaleCalculator.RowSum(price, (decimal)quantity));

    [Theory]
    [InlineData("  coca   COLA  ", "Coca Cola")]
    [InlineData("o'rik qoqi", "Orik Qoqi")]
    [InlineData("G‘isht", "Gisht")]
    [InlineData("", "")]
    public void NameNormalizer_normalizes(string input, string expected)
        => Assert.Equal(expected, NameNormalizer.Normalize(input));
}
