using System.Buffers.Binary;

namespace MockMarket.Core;

public static class MarketUpdateParser
{
    public const int FrameLength = 40;

    private const int KindOffset = 0;
    private const int SymbolIdOffset = 1;
    private const int TimestampOffset = 5;
    private const int PrimaryPriceOffset = 13;
    private const int SecondaryPriceOffset = 21;
    private const int PrimarySizeOffset = 29;
    private const int SecondarySizeOffset = 33;
    private const int ReservedOffset = 37;

    public static bool TryParse(ReadOnlySpan<byte> frame, out MarketUpdate update)
    {
        update = default;

        if (frame.Length != FrameLength ||
            frame[ReservedOffset] != 0 ||
            frame[ReservedOffset + 1] != 0 ||
            frame[ReservedOffset + 2] != 0)
        {
            return false;
        }

        var symbolId = BinaryPrimitives.ReadUInt32LittleEndian(frame[SymbolIdOffset..]);
        var timestamp = BinaryPrimitives.ReadInt64LittleEndian(frame[TimestampOffset..]);
        var primaryPrice = BinaryPrimitives.ReadInt64LittleEndian(frame[PrimaryPriceOffset..]);
        var primarySize = BinaryPrimitives.ReadInt32LittleEndian(frame[PrimarySizeOffset..]);

        switch ((MarketUpdateKind)frame[KindOffset])
        {
            case MarketUpdateKind.Quote:
            {
                var askPrice = BinaryPrimitives.ReadInt64LittleEndian(frame[SecondaryPriceOffset..]);
                var askSize = BinaryPrimitives.ReadInt32LittleEndian(frame[SecondarySizeOffset..]);

                if (primaryPrice < 0 || askPrice < 0 || primarySize < 0 || askSize < 0)
                {
                    return false;
                }

                update = MarketUpdate.CreateQuote(
                    symbolId,
                    timestamp,
                    primaryPrice,
                    primarySize,
                    askPrice,
                    askSize);
                return true;
            }
            case MarketUpdateKind.Trade:
                if (primaryPrice < 0 || primarySize < 0)
                {
                    return false;
                }

                update = MarketUpdate.CreateTrade(symbolId, timestamp, primaryPrice, primarySize);
                return true;
            default:
                return false;
        }
    }
}
