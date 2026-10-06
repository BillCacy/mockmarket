using System.Buffers.Binary;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MockMarket.Core;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class MarketDataBenchmarks
{
    private const int BatchSize = 4_096;
    private const int BatchIndexMask = BatchSize - 1;
    private const int SymbolCapacity = 256;

    private byte[] _frames = [];
    private MarketUpdate[] _updates = [];
    private MarketDataProcessor _combinedProcessor = null!;
    private MarketDataProcessor _processOnlyProcessor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _frames = CreateQuoteFrames();
        _updates = new MarketUpdate[BatchSize];
        _combinedProcessor = new MarketDataProcessor(SymbolCapacity);
        _processOnlyProcessor = new MarketDataProcessor(SymbolCapacity);

        for (var i = 0; i < BatchSize; i++)
        {
            var frame = _frames.AsSpan(i * MarketUpdateParser.FrameLength, MarketUpdateParser.FrameLength);
            if (!MarketUpdateParser.TryParse(frame, out _updates[i]))
            {
                throw new InvalidOperationException($"Could not parse setup frame at index {i}.");
            }
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public long ParseAndProcessBatch()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            var frameOffset = (i & BatchIndexMask) * MarketUpdateParser.FrameLength;
            if (!MarketUpdateParser.TryParse(
                    _frames.AsSpan(frameOffset, MarketUpdateParser.FrameLength),
                    out var update) ||
                !_combinedProcessor.TryProcess(in update))
            {
                throw new InvalidOperationException($"Could not process benchmark frame at index {i}.");
            }
        }

        return _combinedProcessor.TryGetSnapshot(0, out var snapshot)
            ? snapshot.QuoteCount
            : 0;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public long ParseOnlyBatch()
    {
        long checksum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var frameOffset = (i & BatchIndexMask) * MarketUpdateParser.FrameLength;
            if (!MarketUpdateParser.TryParse(
                    _frames.AsSpan(frameOffset, MarketUpdateParser.FrameLength),
                    out var update))
            {
                throw new InvalidOperationException($"Could not parse benchmark frame at index {i}.");
            }

            checksum += update.SymbolId + update.TimestampNanoseconds;
        }

        return checksum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public long ProcessOnlyBatch()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            if (!_processOnlyProcessor.TryProcess(in _updates[i]))
            {
                throw new InvalidOperationException($"Could not process benchmark update at index {i}.");
            }
        }

        return _processOnlyProcessor.TryGetSnapshot(0, out var snapshot)
            ? snapshot.QuoteCount
            : 0;
    }

    private static byte[] CreateQuoteFrames()
    {
        var frames = new byte[BatchSize * MarketUpdateParser.FrameLength];
        for (var i = 0; i < BatchSize; i++)
        {
            var frame = frames.AsSpan(i * MarketUpdateParser.FrameLength, MarketUpdateParser.FrameLength);
            var bidPrice = 10_000 + i % 100;
            frame[0] = (byte)MarketUpdateKind.Quote;
            BinaryPrimitives.WriteUInt32LittleEndian(frame[1..], (uint)(i % SymbolCapacity));
            BinaryPrimitives.WriteInt64LittleEndian(frame[5..], 1_728_000_000_000_000_000 + i);
            BinaryPrimitives.WriteInt64LittleEndian(frame[13..], bidPrice);
            BinaryPrimitives.WriteInt64LittleEndian(frame[21..], bidPrice + 5);
            BinaryPrimitives.WriteInt32LittleEndian(frame[29..], 100 + i % 400);
            BinaryPrimitives.WriteInt32LittleEndian(frame[33..], 100 + (i * 3) % 400);
        }

        return frames;
    }
}
