using BenchmarkDotNet.Attributes;

namespace PerformanceDemo.Levenshtein
{
    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class LevenshteinDistance
    {
        //private string _source = " Kitten";
        //private string _target = "Miten ";
        private string _source = " Levenshtein";
        private string _target = "Mwilwnstein ";

        [Benchmark]
        public void LevenshteinDistance_WithHeapArray()
        {
            CalculateLevenshteinDistance(_source, _target);
        }

        [Benchmark]
        public void LevenshteinDistance_WithSpanTrim()
        {
            CalculateLevenshteinDistance_WithSpanTrim(_source, _target);
        }

        [Benchmark]
        public void LevenshteinDistance_With2RowSpan()
        {
            CalculateLevenshteinDistance_With2RowSpan(_source, _target);
        }

        [Benchmark]
        public void LevenshteinDistance_With2RowPointer()
        {
            CalculateLevenshteinDistance_With2RowPointer(_source, _target);
        }

        [Benchmark]
        public void LevenshteinDistance_WithPointer2Row_EarlyExit_CacheCharSwap()
        {
            CalculateLevenshteinDistance_With2RowPointerAndEarlyExit(_source, _target, 1);
        }

        static int CalculateLevenshteinDistance(string source, string target)
        {
            source = source.Trim();
            target = target.Trim();
            int sourceLength = source.Length;
            int targetLength = target.Length;
            int[,] distanceMatrix = new int[sourceLength + 1, targetLength + 1];

            // Initialize the matrix's first column and first row.  
            for (int i = 0; i <= sourceLength; distanceMatrix[i, 0] = i++) { }
            for (int j = 0; j <= targetLength; distanceMatrix[0, j] = j++) { }

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    // Calculate the cost (0 if the characters are the same, 1 otherwise).  
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

                    // Find the minimum between insertion, deletion, and substitution.  
                    distanceMatrix[i, j] = Math.Min(
                        Math.Min(distanceMatrix[i - 1, j] + 1, distanceMatrix[i, j - 1] + 1),
                        distanceMatrix[i - 1, j - 1] + cost);
                }
            }

            // The distance is found in the opposite corner from the starting point.  
            return distanceMatrix[sourceLength, targetLength];
        }

        static int CalculateLevenshteinDistance_WithSpanTrim(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
        {
            var cleansedSource = source.Trim();
            var cleansedTarget = target.Trim();

            int sourceLength = cleansedSource.Length;
            int targetLength = cleansedTarget.Length;

            int[,] distanceMatrix = new int[sourceLength + 1, targetLength + 1];

            // Initialize the matrix's first column and first row.  
            for (int i = 0; i <= sourceLength; distanceMatrix[i, 0] = i++) { }
            for (int j = 0; j <= targetLength; distanceMatrix[0, j] = j++) { }

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    // Calculate the cost (0 if the characters are the same, 1 otherwise).  
                    int cost = (cleansedTarget[j - 1] == cleansedSource[i - 1]) ? 0 : 1;

                    // Find the minimum between insertion, deletion, and substitution.  
                    distanceMatrix[i, j] = Math.Min(
                        Math.Min(distanceMatrix[i - 1, j] + 1, distanceMatrix[i, j - 1] + 1),
                        distanceMatrix[i - 1, j - 1] + cost);
                }
            }

            // The distance is found in the opposite corner from the starting point.  
            return distanceMatrix[sourceLength, targetLength];
        }

        static int CalculateLevenshteinDistance_With2RowSpan(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
        {
            var cleansedSource = source.Trim();
            var cleansedTarget = target.Trim();

            int sourceLength = cleansedSource.Length;
            int targetLength = cleansedTarget.Length;

            // Allocate space for two rows on the stack.  
            Span<int> previousRow = stackalloc int[sourceLength + 1];
            Span<int> currentRow = stackalloc int[sourceLength + 1];

            for (int j = 1; j <= targetLength; j++)
            {
                currentRow[0] = j;

                for (int i = 1; i <= sourceLength; i++)
                {
                    // Calculate the cost (0 if the characters are the same, 1 otherwise).  
                    int cost = (cleansedTarget[j - 1] == cleansedSource[i - 1]) ? 0 : 1;

                    // Find the minimum between insertion, deletion, and substitution.  
                    currentRow[i] = Math.Min(
                        Math.Min(currentRow[i - 1] + 1, previousRow[i] + 1),
                        previousRow[i - 1] + cost);
                }

                // Swap the spans for the next iteration  
                var temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            // The result is in the previous row because we swapped the spans  
            return previousRow[sourceLength];
        }

        static unsafe int CalculateLevenshteinDistance_With2RowPointer(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
        {
            var cleansedSource = source.Trim();
            var cleansedTarget = target.Trim();

            int sourceLength = cleansedSource.Length;
            int targetLength = cleansedTarget.Length;

            // Allocate space for two rows on the stack.  
            int* previousRow = stackalloc int[sourceLength + 1];
            int* currentRow = stackalloc int[sourceLength + 1];

            for (int j = 1; j <= targetLength; j++)
            {
                currentRow[0] = j;

                for (int i = 1; i <= sourceLength; i++)
                {
                    // Calculate the cost (0 if the characters are the same, 1 otherwise).  
                    int cost = (cleansedTarget[j - 1] == cleansedSource[i - 1]) ? 0 : 1;

                    // Find the minimum between insertion, deletion, and substitution.  
                    currentRow[i] = Math.Min(
                        Math.Min(currentRow[i - 1] + 1, previousRow[i] + 1),
                        previousRow[i - 1] + cost);
                }

                // Swap the rows for the next iteration  
                int* temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            // The result is in the previous row because we swapped the rows  
            return previousRow[sourceLength];
        }

        static unsafe int CalculateLevenshteinDistance2RowPointer_EarlyExit(ReadOnlySpan<char> source, ReadOnlySpan<char> target, int maxDistance)
        {
            var cleansedSource = source.Trim();
            var cleansedTarget = target.Trim();

            int sourceLength = cleansedSource.Length;
            int targetLength = cleansedTarget.Length;

            // Allocate space for two rows on the stack.  
            int* previousRow = stackalloc int[sourceLength + 1];
            int* currentRow = stackalloc int[sourceLength + 1];

            for (int j = 1; j <= targetLength; j++)
            {
                currentRow[0] = j;

                for (int i = 1; i <= sourceLength; i++)
                {
                    // Calculate the cost (0 if the characters are the same, 1 otherwise).  
                    int cost = (cleansedTarget[j - 1] == cleansedSource[i - 1]) ? 0 : 1;

                    // Find the minimum between insertion, deletion, and substitution.  
                    currentRow[i] = Math.Min(
                        Math.Min(currentRow[i - 1] + 1, previousRow[i] + 1),
                        previousRow[i - 1] + cost);
                }

                if (currentRow[j] > maxDistance)
                {
                    return currentRow[j];
                }

                // Swap the rows for the next iteration  
                int* temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            // The result is in the previous row because we swapped the rows  
            return previousRow[sourceLength];
        }

        static unsafe int CalculateLevenshteinDistance_With2RowPointerAndEarlyExit(ReadOnlySpan<char> source, ReadOnlySpan<char> target, int maxDistance)
        {
            var cleansedSource = source.Trim();
            var cleansedTarget = target.Trim();

            int sourceLength = cleansedSource.Length;
            int targetLength = cleansedTarget.Length;

            // Ensure the source is the shorter string to use less memory.  
            if (sourceLength > targetLength)
            {
                ReadOnlySpan<char> temp = cleansedTarget;
                cleansedTarget = cleansedSource;
                cleansedSource = temp;
                (sourceLength, targetLength) = (targetLength, sourceLength);
            }

            // Allocate space for two rows on the stack.  
            int* previousRow = stackalloc int[sourceLength + 1];
            int* currentRow = stackalloc int[sourceLength + 1];

            for (int j = 1; j <= targetLength; j++)
            {
                currentRow[0] = j;

                // cache value for inner loop to avoid index lookup and bonds checking, profiled this is quicker
                char targetChar = cleansedTarget[j - 1];

                for (int i = 1; i <= sourceLength; i++)
                {
                    // Calculate the cost (0 if the characters are the same, 1 otherwise).  
                    int cost = (targetChar == cleansedSource[i - 1]) ? 0 : 1;

                    // Find the minimum between insertion, deletion, and substitution.  
                    currentRow[i] = Math.Min(
                        Math.Min(currentRow[i - 1] + 1, previousRow[i] + 1),
                        previousRow[i - 1] + cost);
                }

                if (currentRow[j] > maxDistance)
                {
                    return currentRow[j];
                }

                // Swap the rows for the next iteration  
                int* temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            // The result is in the previous row because we swapped the rows  
            return previousRow[sourceLength];
        }
    }
}