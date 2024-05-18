// #define SHOW_ERRORS
using PrimaryParameter.SG;
using System.Runtime.InteropServices;

namespace ConsoleApp1
{
    static class Program
    {
        static void Main([Field, Property] string[] args)
        {
            var c = new C(5, " hello");
            c.M0();
            new Ref(ref c.B, 3).ChangeAbc(3);
            c.M0();
        }
    }
#if SHOW_ERRORS
    partial class C
    {
        private readonly int _a;
    }
#endif

    public partial class C([Field(Name = "_" + "a", AssignFormat = "{0}.ToString()", Type = typeof(string), IsReadonly = false), Field(Name = nameof(C.B), Scope = "public", IsReadonly = false), Field, Field] int i, [Property(Setter = "init", AssignFormat = "{0}.Trim()", Summary = "Documentation"), Field(Name = nameof(C.s))] string s)
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

    public class D([DoNotUse]int i) : C(i, "");

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

    [StructLayout(LayoutKind.Auto)]
    public readonly ref partial struct Ref([RefField(IsRefReadonly = false, Name = nameof(Ref.Abc))] ref int i, [Field] int a)
    {
        public readonly void ChangeAbc(int a) => Abc = a;
        private void Test()
            => Console.WriteLine(nameof(a));
    }

#if SHOW_ERRORS
    [StructLayout(LayoutKind.Auto)]
    public readonly partial struct S([RefField(IsRefReadonly = false, Name = nameof(S.Abc))]ref int i, [RefField]int a)
    { }
#endif

    public partial class ParamNameOf([Field(AssignFormat = $$"""{0}.{{nameof(dateTime.Day)}}""", Type = typeof(int), Name = nameof(ParamNameOf.Day))] DateTime dateTime)
    { }
}

public partial struct C([Field] int i)
{
    int a = 0;
}
