using BenchmarkDotNet.Running;
using PerformanceDemo;

//await new Parallelism().Parallel_ForEach();
//new Reflection().CompiledCachedReflection();
//BenchmarkRunner.Run<Bitmask>();
//BenchmarkRunner.Run<LevenshteinDistance>();
BenchmarkRunner.Run<Reflection>();
//BenchmarkRunner.Run<Parallelism>();
