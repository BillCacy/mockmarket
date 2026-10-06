using BenchmarkDotNet.Running;

BenchmarkSwitcher
    .FromAssembly(typeof(MarketDataBenchmarks).Assembly)
    .Run(args);
