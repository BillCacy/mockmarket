namespace MockMarket.Core;

public readonly record struct MarketSnapshot(
    uint SymbolId,
    long QuoteTimestampNanoseconds,
    long BidPriceTicks,
    int BidSize,
    long AskPriceTicks,
    int AskSize,
    long TradeTimestampNanoseconds,
    long LastPriceTicks,
    int LastTradeSize,
    long QuoteCount,
    long TradeCount);

public sealed class MarketDataProcessor
{
    private readonly SymbolState[] _symbols;

    public MarketDataProcessor(int symbolCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(symbolCapacity);
        _symbols = new SymbolState[symbolCapacity];
    }

    public int SymbolCapacity => _symbols.Length;

    public bool TryProcess(in MarketUpdate update)
    {
        if (update.SymbolId >= (uint)_symbols.Length)
        {
            return false;
        }

        ref var state = ref _symbols[(int)update.SymbolId];

        switch (update.Kind)
        {
            case MarketUpdateKind.Quote:
                state.QuoteTimestampNanoseconds = update.TimestampNanoseconds;
                state.BidPriceTicks = update.BidPriceTicks;
                state.BidSize = update.BidSize;
                state.AskPriceTicks = update.AskPriceTicks;
                state.AskSize = update.AskSize;
                state.QuoteCount++;
                return true;
            case MarketUpdateKind.Trade:
                state.TradeTimestampNanoseconds = update.TimestampNanoseconds;
                state.LastPriceTicks = update.LastPriceTicks;
                state.LastTradeSize = update.TradeSize;
                state.TradeCount++;
                return true;
            default:
                return false;
        }
    }

    public bool TryGetSnapshot(uint symbolId, out MarketSnapshot snapshot)
    {
        if (symbolId >= (uint)_symbols.Length)
        {
            snapshot = default;
            return false;
        }

        ref readonly var state = ref _symbols[(int)symbolId];
        snapshot = new MarketSnapshot(
            symbolId,
            state.QuoteTimestampNanoseconds,
            state.BidPriceTicks,
            state.BidSize,
            state.AskPriceTicks,
            state.AskSize,
            state.TradeTimestampNanoseconds,
            state.LastPriceTicks,
            state.LastTradeSize,
            state.QuoteCount,
            state.TradeCount);
        return true;
    }

    private struct SymbolState
    {
        public long QuoteTimestampNanoseconds;
        public long BidPriceTicks;
        public int BidSize;
        public long AskPriceTicks;
        public int AskSize;
        public long TradeTimestampNanoseconds;
        public long LastPriceTicks;
        public int LastTradeSize;
        public long QuoteCount;
        public long TradeCount;
    }
}
