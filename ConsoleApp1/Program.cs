//#define SHOW_ERRORS

namespace ConsoleApp1
{
    using PrimaryParameter.SG;

    static class Program
    {
        static void Main([Field] string[] args)
        {
            var c = new C(5, " hello");
            c.M0();
            new Ref(ref c.B).ChangeAbc(3);
            c.M0();
        }
    }
#if SHOW_ERRORS
    partial class C
    {
        private readonly int _a;
    }
#endif

    public partial class C([Field(Name = "_" + "a", AssignFormat = "{0}.ToString()", Type = typeof(string), IsReadonly = false), Field(Name = nameof(C.B), Scope = "public", IsReadonly = false), Field, Field] int i, [Property(WithInit = true, AssignFormat = "{0}.Trim()"), Field(Name = nameof(C.s))] string s)
    {
        public void M0()
        {
            var i = _i;
            i++;
            Console.WriteLine(i);
            Console.WriteLine(_a);
            Console.WriteLine(B);
            Console.WriteLine(_i);
            Console.WriteLine(S);
            Console.WriteLine(s);
            _a = "";
        }
#if SHOW_ERRORS
        public void M1()
        {
            i++;
            Console.WriteLine(i);
        }
#endif
    }

    public partial record struct R
    {
        partial class C<T>([Field] T s)
            where T : struct
        {

            public void M1()
            {
#if SHOW_ERRORS
                Console.WriteLine(s);
#endif
                Console.WriteLine(_s);
            }
        }
        partial class C<T>
        {
            public void M()
                => Console.WriteLine(_s);
        }
    }

    public ref partial struct Ref([RefField(IsRefReadonly = false, Name = nameof(Ref.Abc))]ref int i)
    {
        public void ChangeAbc(int a) => Abc = a;
    }
}
