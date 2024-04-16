using BenchmarkDotNet.Attributes;

namespace PerformanceDemo
{
    [Flags]
    public enum UserPermissions : byte
    {                      //   Binary
        None = 0,          //   00000000
        Read = 1,          //   00000001
        Write = 2,         //   00000010
        Delete = 4         //   00000100
    }

    [MemoryDiagnoser]
    [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
    public class Bitmask
    {
        [Benchmark]
        public void BitMask_WithEnumArray()
        {
            var userPermissions = new UserPermissions[] { UserPermissions.Read, UserPermissions.Write };
            var canWrite = userPermissions.Contains(UserPermissions.Write);
        }

        [Benchmark]
        public void BitMask_WithBitmask()
        {
            var userPermissions = 1 | 2;               //  00000011
            var canWrite = (2 & userPermissions) == 2; //  true
        }

        [Benchmark]
        public void BitMask_WithFlags()
        {
            var userPermissions = UserPermissions.Read | UserPermissions.Write; //   00000011
            var canWrite = userPermissions.HasFlag(UserPermissions.Write);      //   true
        }
    }
}
