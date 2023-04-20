namespace ConsoleApp1;

using PrimaryParameter.SG;

static class Program
{
    static void Main()
    { }
}

public partial class C([Field(Name = "_a"), Field(Name = "b")]int i)
{
    public void M0()
    {
        var i = 0;
        i++;
        Console.WriteLine(i);
        Console.WriteLine(_a);
        Console.WriteLine(b);
    }
    public void M1()
    {
        i++;
        Console.WriteLine(i);
    }
}
