﻿namespace ConsoleApp1
{
    partial class C
    {
        private string _a = i.ToString();
        private readonly int b = i;
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
