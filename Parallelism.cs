using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace PerformanceDemo
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class Parallelism
    {
        private const int _maxDegreeOfParallelism = 1000;
        private static readonly ParallelOptions _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _maxDegreeOfParallelism };
        private const int _count = 10000;
        private static readonly List<int> _tasks = Enumerable.Range(0, _count).ToList();

        private async Task LongRunningTaskAsync()
        {
            await Task.Delay(1);
        }

        [Benchmark]
        public async Task Parallel_ForEachAsync()
        {
            await Parallel.ForEachAsync(_tasks, _parallelOptions, async (i, token) =>
            {
                await LongRunningTaskAsync();
            });
        }

        [Benchmark]
        public void Parallel_ForEach()
        {
            Parallel.ForEach(_tasks, _parallelOptions, (i, token) =>
            {
                LongRunningTaskAsync().GetAwaiter().GetResult();
            });
        }

        [Benchmark]
        public async Task Iterator_WhenAny()
        {
            var iterator = new AsyncIterator_WhenAny(_maxDegreeOfParallelism);
            await iterator.IterateAsync(_tasks, CancellationToken.None, async (i) =>
            {
                await Task.Delay(1);
            });
        }

        public class AsyncIterator_WhenAny
        {
            private readonly int _concurrency;

            public AsyncIterator_WhenAny(int concurrency)
            {
                _concurrency = concurrency;
            }

            public async Task IterateAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken, Func<T, Task> processor)
            {
                var exceptions = new Queue<Exception>();
                var nextIndex = 0;
                var tasks = new List<Task>();
                var itemList = items.ToList();

                // populate task list with number of concurrent tasks
                while (nextIndex < _concurrency && nextIndex < itemList.Count)
                {
                    tasks.Add(ProcessItemAsync(itemList[nextIndex], processor, exceptions));
                    nextIndex++;
                }

                while (tasks.Count > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var task = await Task.WhenAny(tasks);
                    tasks.Remove(task);

                    // add another item if there are any left
                    if (nextIndex < itemList.Count)
                    {
                        tasks.Add(ProcessItemAsync(itemList[nextIndex], processor, exceptions));
                        nextIndex++;
                    }
                }

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
            }

            private async Task ProcessItemAsync<T>(T item, Func<T, Task> processor, Queue<Exception> exceptions)
            {
                try
                {
                    await processor(item);
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(new Exception($"Error while processing matches for {item}", ex));
                }
            }
        }

        [Benchmark]
        public async Task Iterator_SemaphoreSlim()
        {
            var iterator = new AsyncIterator_SemaphoreSlim(_maxDegreeOfParallelism);
            await iterator.IterateAsync(_tasks, CancellationToken.None, async (i) =>
            {
                await LongRunningTaskAsync();
            });
        }

        public class AsyncIterator_SemaphoreSlim
        {
            private readonly SemaphoreSlim _semaphore;
            private readonly int _concurrency;

            public AsyncIterator_SemaphoreSlim(int concurrency)
            {
                _concurrency = concurrency;
                _semaphore = new SemaphoreSlim(concurrency, concurrency);
            }

            public async Task IterateAsync<T>(List<T> items, CancellationToken cancellationToken, Func<T, Task> processor)
            {
                var exceptions = new ConcurrentQueue<Exception>();

                foreach (var item in items)
                {
                    // Respect cancellation  
                    cancellationToken.ThrowIfCancellationRequested();

                    // Wait until there is room to start a new task  
                    await _semaphore.WaitAsync(cancellationToken);

                    // Start a new task  
                    var task = processor(item);

                    // Add the task to the list of running tasks  
                    _ = task.ContinueWith(t =>
                    {
                        // Release on task completion  
                        _semaphore.Release();

                        // Record the exception if one occurred  
                        if (t.Exception != null)
                        {
                            exceptions.Append(t.Exception);
                        }
                    }, cancellationToken);
                }

                // Loop until the semaphore's current count is equal to the maximum count  
                while (_semaphore.CurrentCount < _concurrency)
                {
                    // Check if cancellation is requested  
                    cancellationToken.ThrowIfCancellationRequested();
                    await _semaphore.WaitAsync(cancellationToken);
                    _semaphore.Release();
                }

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
            }
        }
    }
}
