using Xunit;

namespace GildedRose.Tests;

/// <summary>
/// Characterization tests for <see cref="Solution.UpdateQuality"/>.
/// Expected values document the current behavior of the legacy implementation — not ideal domain rules.
/// </summary>
public class SolutionCharacterizationTests
{
    private readonly Solution _sut = new();

    private static class Names
    {
        public const string Normal = "+5 Dexterity Vest";
        public const string AgedBrie = "Aged Brie";
        public const string Sulfuras = "Sulfuras, Hand of Ragnaros";
        public const string Backstage =
            "Backstage passes to a TAFKAL80ETC concert";
        public const string Conjured = "Conjured Mana Cake";
    }

    private (int SellIn, int Quality) Act(string name, int sellIn, int quality)
    {
        var result = _sut.UpdateQuality(name, sellIn, quality);
        Assert.Equal(2, result.Length);
        return (result[0], result[1]);
    }

    private static void AssertResult(
        (int SellIn, int Quality) actual,
        int expectedSellIn,
        int expectedQuality)
    {
        Assert.Equal(expectedSellIn, actual.SellIn);
        Assert.Equal(expectedQuality, actual.Quality);
    }

    // --- Return shape ---

    [Fact]
    public void UpdateQuality_ReturnsTwoElementArray_SellInThenQuality()
    {
        var result = _sut.UpdateQuality(Names.Normal, 10, 20);
        Assert.Equal(2, result.Length);
        Assert.Equal(9, result[0]);
        Assert.Equal(19, result[1]);
    }

    // --- Block 1 + 2: "Normal" items (not Brie, Backstage, or Sulfuras) ---

    [Fact]
    public void NormalItem_BeforeSellByDate_DecrementsQualityAndSellIn()
    {
        var actual = Act(Names.Normal, 10, 20);
        AssertResult(actual, 9, 19);
    }

    [Fact]
    public void NormalItem_QualityZero_DoesNotGoNegative_BeforeExpiry()
    {
        var actual = Act(Names.Normal, 5, 0);
        AssertResult(actual, 4, 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void NormalItem_PositiveSellIn_AlwaysDecrementsSellInByOne(int sellIn)
    {
        var actual = Act(Names.Normal, sellIn, 25);
        AssertResult(actual, sellIn - 1, 24);
    }

    [Fact]
    public void NormalItem_OnSellByDate_DoubleDegradesQuality()
    {
        var actual = Act(Names.Normal, 0, 10);
        AssertResult(actual, -1, 8);
    }

    [Fact]
    public void NormalItem_AfterSellByDate_StopsAtZeroQuality()
    {
        var actual = Act(Names.Normal, 0, 1);
        AssertResult(actual, -1, 0);
    }

    [Fact]
    public void NormalItem_AfterSellByDate_AlreadyZeroQuality_StaysZero()
    {
        var actual = Act(Names.Normal, 0, 0);
        AssertResult(actual, -1, 0);
    }

    [Fact]
    public void NormalItem_AfterSellByDate_HighQuality_DegradesTwiceInOneDay()
    {
        var actual = Act(Names.Normal, 0, 50);
        AssertResult(actual, -1, 48);
    }

    [Fact]
    public void NormalItem_AlreadyExpired_ContinuesDoubleDegradationWhileAboveZero()
    {
        var actual = Act(Names.Normal, -5, 10);
        AssertResult(actual, -6, 8);
    }

    [Fact]
    public void ConjuredItem_IsTreatedAsNormalItem_NotSpecialCased()
    {
        var actual = Act(Names.Conjured, 3, 6);
        AssertResult(actual, 2, 5);
    }

    [Fact]
    public void UnknownItemName_BehavesLikeNormalItem()
    {
        var actual = Act("Some random loot", 7, 15);
        AssertResult(actual, 6, 14);
    }

    // --- Sulfuras ---

    [Fact]
    public void Sulfuras_NeverChangesQualityOrSellIn_WhenBeforeExpiry()
    {
        var actual = Act(Names.Sulfuras, 5, 40);
        AssertResult(actual, 5, 40);
    }

    [Fact]
    public void Sulfuras_NeverChangesQualityOrSellIn_WhenOnSellByDate()
    {
        var actual = Act(Names.Sulfuras, 0, 80);
        AssertResult(actual, 0, 80);
    }

    [Fact]
    public void Sulfuras_NeverChangesQualityOrSellIn_WhenAlreadyExpired()
    {
        var actual = Act(Names.Sulfuras, -1, 80);
        AssertResult(actual, -1, 80);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 80)]
    [InlineData(-10, 50)]
    public void Sulfuras_AlwaysReturnsInputSellInAndQuality(int sellIn, int quality)
    {
        var actual = Act(Names.Sulfuras, sellIn, quality);
        AssertResult(actual, sellIn, quality);
    }

    // --- Aged Brie ---

    [Fact]
    public void AgedBrie_BeforeExpiry_IncreasesQualityAndDecrementsSellIn()
    {
        var actual = Act(Names.AgedBrie, 2, 0);
        AssertResult(actual, 1, 1);
    }

    [Fact]
    public void AgedBrie_OnSellByDate_IncreasesOnceThenDoesNotIncreaseAgainAfterExpiry()
    {
        var actual = Act(Names.AgedBrie, 0, 49);
        AssertResult(actual, -1, 50);
    }

    [Fact]
    public void AgedBrie_QualityFifty_NeverIncreases()
    {
        var actual = Act(Names.AgedBrie, 5, 50);
        AssertResult(actual, 4, 50);
    }

    [Fact]
    public void AgedBrie_AlreadyExpired_StillIncreasesUntilFifty()
    {
        var actual = Act(Names.AgedBrie, -1, 48);
        AssertResult(actual, -2, 50);
    }

    [Fact]
    public void AgedBrie_AlreadyExpired_AtFortyNine_IncreasesOnceToFifty()
    {
        var actual = Act(Names.AgedBrie, -1, 49);
        AssertResult(actual, -2, 50);
    }

    [Fact]
    public void AgedBrie_AlreadyExpired_AtFifty_StaysFifty()
    {
        var actual = Act(Names.AgedBrie, -1, 50);
        AssertResult(actual, -2, 50);
    }

    // --- Backstage passes: before expiry ---

    [Fact]
    public void Backstage_MoreThanTenDays_IncreasesQualityByOne()
    {
        var actual = Act(Names.Backstage, 15, 20);
        AssertResult(actual, 14, 21);
    }

    [Fact]
    public void Backstage_ExactlyElevenDays_IncreasesQualityByOne_NotTwo()
    {
        var actual = Act(Names.Backstage, 11, 20);
        AssertResult(actual, 10, 21);
    }

    [Fact]
    public void Backstage_TenDaysOrLess_IncreasesQualityByTwo()
    {
        var actual = Act(Names.Backstage, 10, 20);
        AssertResult(actual, 9, 22);
    }

    [Fact]
    public void Backstage_ExactlySixDays_IncreasesQualityByTwo_NotThree()
    {
        var actual = Act(Names.Backstage, 6, 20);
        AssertResult(actual, 5, 22);
    }

    [Fact]
    public void Backstage_FiveDaysOrLess_IncreasesQualityByThree()
    {
        var actual = Act(Names.Backstage, 5, 20);
        AssertResult(actual, 4, 23);
    }

    [Theory]
    [InlineData(1, 48, 0, 50)]
    [InlineData(1, 49, 0, 50)]
    [InlineData(5, 47, 4, 50)]
    [InlineData(5, 48, 4, 50)]
    [InlineData(5, 49, 4, 50)]
    public void Backstage_BeforeExpiry_QualityNeverExceedsFifty(
        int sellIn,
        int quality,
        int expectedSellIn,
        int expectedQuality)
    {
        var actual = Act(Names.Backstage, sellIn, quality);
        AssertResult(actual, expectedSellIn, expectedQuality);
    }

    [Fact]
    public void Backstage_QualityFifty_BeforeExpiry_DoesNotIncrease()
    {
        var actual = Act(Names.Backstage, 10, 50);
        AssertResult(actual, 9, 50);
    }

    // --- Backstage passes: on or after sell-by (sellIn becomes negative after update) ---

    [Fact]
    public void Backstage_OnSellByDate_AfterUpdate_QualityDropsToZero()
    {
        var actual = Act(Names.Backstage, 0, 20);
        AssertResult(actual, -1, 0);
    }

    [Fact]
    public void Backstage_OnSellByDate_EvenWhenQualityWouldHaveBeenFifty_DropsToZero()
    {
        var actual = Act(Names.Backstage, 0, 49);
        AssertResult(actual, -1, 0);
    }

    [Fact]
    public void Backstage_AlreadyExpired_StillRunsPreExpiryIncreasesThenZerosQuality()
    {
        var actual = Act(Names.Backstage, -1, 30);
        AssertResult(actual, -2, 0);
    }

    // --- Branch interaction matrix (golden-style table) ---

    public static IEnumerable<object[]> GoldenScenarios =>
        new List<object[]>
        {
            new object[] { Names.Normal, 10, 20, 9, 19 },
            new object[] { Names.Normal, 0, 10, -1, 8 },
            new object[] { Names.Normal, 0, 0, -1, 0 },
            new object[] { Names.AgedBrie, 2, 0, 1, 1 },
            new object[] { Names.AgedBrie, 0, 49, -1, 50 },
            new object[] { Names.Sulfuras, 0, 80, 0, 80 },
            new object[] { Names.Sulfuras, -1, 80, -1, 80 },
            new object[] { Names.Backstage, 15, 20, 14, 21 },
            new object[] { Names.Backstage, 10, 20, 9, 22 },
            new object[] { Names.Backstage, 5, 20, 4, 23 },
            new object[] { Names.Backstage, 0, 20, -1, 0 },
            new object[] { Names.Conjured, 3, 6, 2, 5 },
        };

    [Theory]
    [MemberData(nameof(GoldenScenarios))]
    public void UpdateQuality_GoldenScenarios_MatchCurrentLegacyBehavior(
        string name,
        int sellIn,
        int quality,
        int expectedSellIn,
        int expectedQuality)
    {
        var actual = Act(name, sellIn, quality);
        AssertResult(actual, expectedSellIn, expectedQuality);
    }

    // --- Edge cases on numeric boundaries ---

    [Fact]
    public void NormalItem_QualityOne_OnSellByDate_BecomesZero()
    {
        var actual = Act(Names.Normal, 0, 1);
        AssertResult(actual, -1, 0);
    }

    [Fact]
    public void AgedBrie_SellInOne_QualityFortyNine_BecomesFiftyBeforeExpiry()
    {
        var actual = Act(Names.AgedBrie, 1, 49);
        AssertResult(actual, 0, 50);
    }

    [Fact]
    public void Backstage_SellInOne_QualityTwenty_IncreasesByThree()
    {
        var actual = Act(Names.Backstage, 1, 20);
        AssertResult(actual, 0, 23);
    }

    [Fact]
    public void Backstage_SellInZero_QualityFortySeven_CapsThenZeros()
    {
        var actual = Act(Names.Backstage, 0, 47);
        AssertResult(actual, -1, 0);
    }
}
