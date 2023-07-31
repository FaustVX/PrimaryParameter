namespace ConsoleApp1
{
    partial class C
    {
        private string _a = i.ToString();
        public int B = i;
        private readonly int _i = i;
    }
}
namespace ConsoleApp1
{
    partial class C
    {
        public string S { get; init; } = s.Trim();
        private readonly string s = s;
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
namespace ConsoleApp1
{
    partial struct Ref
    {
        private readonly ref int Abc = ref global::System.Runtime.CompilerServices.Unsafe.AsRef(in i);
    }
}
namespace ConsoleApp1
{
    partial struct Ref
    {
        private readonly int _a = a;
    }
}
namespace ConsoleApp1
{
    partial class ParamNameOf
    {
        private readonly int Day = dateTime.Day;
    }
}
