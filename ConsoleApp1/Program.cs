namespace ConsoleApp1;

using PrimaryParameter.SG;

static class Program
{
    static void Main()
        => new C(5).M();
}

public partial class C([Field]int i)
{
    public void M()
    {
        Console.WriteLine(i);
        i++;
        Console.WriteLine(_i);
    }
}
