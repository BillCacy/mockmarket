using System.Buffers.Binary;
using MockMarket.Core;

namespace MockMarket.Tests;

public sealed class MarketUpdateParserTests
{
    [Fact]
    public void TryParse_QuoteFrame_ReturnsQuote()
    {
        var frame = CreateFrame(MarketUpdateKind.Quote);
        var result = MarketUpdateParser.TryParse(frame, out var update);

        Assert.True(result);
        Assert.Equal(MarketUpdateKind.Quote, update.Kind);
        Assert.Equal(7u, update.SymbolId);
        Assert.Equal(1_728_000_000_000_000_000, update.TimestampNanoseconds);
        Assert.Equal(10_025, update.BidPriceTicks);
        Assert.Equal(10_030, update.AskPriceTicks);
        Assert.Equal(500, update.BidSize);
        Assert.Equal(450, update.AskSize);
    }

    [Fact]
    public void TryParse_TradeFrame_ReturnsTrade()
    {
        var frame = CreateFrame(MarketUpdateKind.Trade);
        var result = MarketUpdateParser.TryParse(frame, out var update);

        Assert.True(result);
        Assert.Equal(MarketUpdateKind.Trade, update.Kind);
        Assert.Equal(10_025, update.LastPriceTicks);
        Assert.Equal(500, update.TradeSize);
    }

    [Fact]
    public void TryParse_InvalidLengthKindReservedBytesOrNegativeValues_ReturnsFalse()
    {
        var frame = CreateFrame(MarketUpdateKind.Quote);

        Assert.False(MarketUpdateParser.TryParse(frame.AsSpan(1), out _));

        frame[0] = byte.MaxValue;
        Assert.False(MarketUpdateParser.TryParse(frame, out _));

        frame = CreateFrame(MarketUpdateKind.Quote);
        frame[39] = 1;
        Assert.False(MarketUpdateParser.TryParse(frame, out _));

        frame = CreateFrame(MarketUpdateKind.Trade);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(29), -1);
        Assert.False(MarketUpdateParser.TryParse(frame, out _));
    }

    [Fact]
    public void TryParse_ValidFrames_DoesNotAllocate()
    {
        var frame = CreateFrame(MarketUpdateKind.Quote);
        var processor = new MarketDataProcessor(symbolCapacity: 8);

        for (var i = 0; i < 10_000; i++)
        {
            Assert.True(MarketUpdateParser.TryParse(frame, out var update));
            Assert.True(processor.TryProcess(in update));
        }

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            MarketUpdateParser.TryParse(frame, out var update);
            processor.TryProcess(in update);
        }
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(0, allocatedBytes);
    }

    private static byte[] CreateFrame(MarketUpdateKind kind)
    {
        var frame = new byte[MarketUpdateParser.FrameLength];
        frame[0] = (byte)kind;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(1), 7);
        BinaryPrimitives.WriteInt64LittleEndian(frame.AsSpan(5), 1_728_000_000_000_000_000);
        BinaryPrimitives.WriteInt64LittleEndian(frame.AsSpan(13), 10_025);
        BinaryPrimitives.WriteInt64LittleEndian(frame.AsSpan(21), 10_030);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(29), 500);
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(33), 450);
        return frame;
    }
}
