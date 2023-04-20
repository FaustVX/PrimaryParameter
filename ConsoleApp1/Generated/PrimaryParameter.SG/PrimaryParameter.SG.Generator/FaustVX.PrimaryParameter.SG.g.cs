namespace ConsoleApp1
{
    partial class C
    {
        private readonly int _a = i;
        private readonly int b = i;
        private readonly int _i = i;
    }
}
namespace ConsoleApp1
{
    partial class C
    {
        private readonly string _s = s;
    }
}
namespace ConsoleApp1
{
    partial record struct R
    {
        partial class C<T>
        {
            private readonly T _s = s;
        }
    }
}
