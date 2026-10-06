using MockMarket.Core;

namespace MockMarket.Tests;

public sealed class MarketDataProcessorTests
{
    [Fact]
    public void TryProcess_QuoteAndTrade_UpdatesSymbolSnapshot()
    {
        var processor = new MarketDataProcessor(symbolCapacity: 4);
        var quote = MarketUpdate.CreateQuote(2, 100, 10_025, 500, 10_030, 450);
        var trade = MarketUpdate.CreateTrade(2, 110, 10_028, 25);

        Assert.True(processor.TryProcess(in quote));
        Assert.True(processor.TryProcess(in trade));
        Assert.True(processor.TryGetSnapshot(2, out var snapshot));

        Assert.Equal(2u, snapshot.SymbolId);
        Assert.Equal(100, snapshot.QuoteTimestampNanoseconds);
        Assert.Equal(10_025, snapshot.BidPriceTicks);
        Assert.Equal(500, snapshot.BidSize);
        Assert.Equal(10_030, snapshot.AskPriceTicks);
        Assert.Equal(450, snapshot.AskSize);
        Assert.Equal(110, snapshot.TradeTimestampNanoseconds);
        Assert.Equal(10_028, snapshot.LastPriceTicks);
        Assert.Equal(25, snapshot.LastTradeSize);
        Assert.Equal(1, snapshot.QuoteCount);
        Assert.Equal(1, snapshot.TradeCount);
    }

    [Fact]
    public void TryProcess_AndTryGetSnapshot_OutOfRangeSymbol_ReturnFalse()
    {
        var processor = new MarketDataProcessor(symbolCapacity: 2);
        var update = MarketUpdate.CreateTrade(2, 1, 100, 1);

        Assert.False(processor.TryProcess(in update));
        Assert.False(processor.TryGetSnapshot(2, out _));
        Assert.False(processor.TryGetSnapshot(uint.MaxValue, out _));
    }

    [Fact]
    public void Constructor_RequiresPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarketDataProcessor(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarketDataProcessor(-1));
    }
}
