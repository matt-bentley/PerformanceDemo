# PerformanceDemo
Some tricks for C# performance optimization and benchmarks. This repository focuses on optimizing an implementation of the Levenshtein Distance.

| Method                                                      | Mean      | Error     | StdDev   | Gen0   | Allocated |
|------------------------------------------------------------ |----------:|----------:|---------:|-------:|----------:|
| LevenshteinDistance_WithPointer2Row_EarlyExit_CacheCharSwap |  67.35 ns |  1.303 ns | 1.219 ns |      - |         - |
| LevenshteinDistance_With2RowPointer                         | 214.78 ns |  4.034 ns | 3.150 ns |      - |         - |
| LevenshteinDistance_With2RowSpan                            | 296.81 ns |  5.769 ns | 5.397 ns |      - |         - |
| LevenshteinDistance_WithSpanTrim                            | 650.13 ns |  7.496 ns | 6.260 ns | 0.0734 |     616 B |
| LevenshteinDistance_WithHeapArray                           | 696.50 ns | 11.713 ns | 9.781 ns | 0.0849 |     712 B |