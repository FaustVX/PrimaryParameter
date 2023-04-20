﻿namespace ConsoleApp1;

using PrimaryParameter.SG;

static class Program
{
    static void Main()
    { }
}

public partial class C([Field(Name = "_a"), Field(Name = "b"), Field]int i)
{
    public void M0()
    {
        var i = 0;
        i++;
        Console.WriteLine(i);
        Console.WriteLine(_a);
        Console.WriteLine(b);
        Console.WriteLine(_i);
    }
    public void M1()
    {
        i++;
        Console.WriteLine(i);
        Console.WriteLine(_s);
    }
}

public partial record struct R([Field]int i)
{
    void M()
    {
        Console.WriteLine(_i);
    }

    partial class C([Field]string s)
    {

        public void M1()
        {
            Console.WriteLine(s);
            Console.WriteLine(_s);
        }
    }
}
