namespace MockMarket.Core;

public enum MarketUpdateKind : byte
{
    Quote = 1,
    Trade = 2
}

public readonly record struct MarketUpdate(
    MarketUpdateKind Kind,
    uint SymbolId,
    long TimestampNanoseconds,
    long BidPriceTicks,
    long AskPriceTicks,
    long LastPriceTicks,
    int BidSize,
    int AskSize,
    int TradeSize)
{
    public static MarketUpdate CreateQuote(
        uint symbolId,
        long timestampNanoseconds,
        long bidPriceTicks,
        int bidSize,
        long askPriceTicks,
        int askSize) =>
        new(
            MarketUpdateKind.Quote,
            symbolId,
            timestampNanoseconds,
            bidPriceTicks,
            askPriceTicks,
            0,
            bidSize,
            askSize,
            0);

    public static MarketUpdate CreateTrade(
        uint symbolId,
        long timestampNanoseconds,
        long lastPriceTicks,
        int tradeSize) =>
        new(
            MarketUpdateKind.Trade,
            symbolId,
            timestampNanoseconds,
            0,
            0,
            lastPriceTicks,
            0,
            0,
            tradeSize);
}
