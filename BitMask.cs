using BenchmarkDotNet.Attributes;

namespace PerformanceDemo.Levenshtein
{
    [Flags]
    public enum UserPermissions : int
    {
        None = 0,
        Read = 1 << 1, //2
        Write = 1 << 2, //4
        Delete = 1 << 3 // 8,
    }

    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class BitMask
    {
        [Benchmark]
        public void BitMask_WithByteArray()
        {
            var userPermissions = new UserPermissions[] { UserPermissions.Read, UserPermissions.Write };
            var canWrite = userPermissions.Contains(UserPermissions.Read);
        }

        [Benchmark]
        public void BitMask_WithFlags()
        {
            var userPermissions = UserPermissions.Read | UserPermissions.Write; // 0,1,1,0
            var canWrite = userPermissions.HasFlag(UserPermissions.Write);
        }

        [Benchmark]
        public void BitMask_WithBitMask()
        {
            var userPermissions = 2 | 4; // 0,1,1,0
            var canWrite = (4 & userPermissions) == 4;
        }
    }
}
