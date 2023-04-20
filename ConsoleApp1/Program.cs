namespace ConsoleApp1
{
    using PrimaryParameter.SG;

    static class Program
    {
        static void Main()
        { }
    }

    public partial class C([Field(Name = "_a"), Field(Name = "b"), Field] int i, [Property(WithInit = true, Scope = "public"), Field(Name = "s")] string s)
    {
        public void M0()
        {
            var i = 0;
            i++;
            Console.WriteLine(i);
            Console.WriteLine(_a);
            Console.WriteLine(b);
            Console.WriteLine(_i);
            Console.WriteLine(S);
            Console.WriteLine(s);
        }
        public void M1()
        {
            i++;
            Console.WriteLine(i);
        }
    }

    public partial record struct R
    {
        partial class C<T>([Field] T s)
            where T : struct
        {

            public void M1()
            {
                Console.WriteLine(s);
                Console.WriteLine(_s);
            }
        }
        partial class C<T>
        {
            public void M()
                => Console.WriteLine(_s);
        }
    }
}