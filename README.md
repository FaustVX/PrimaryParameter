# Primary Parameter
[![NuGet version (FaustVX.PrimaryParameter.SG)](https://img.shields.io/nuget/v/FaustVX.PrimaryParameter.SG.svg)](https://www.nuget.org/packages/FaustVX.PrimaryParameter.SG/)
[![Update NuGet](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml/badge.svg)](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml)

Using a `Field` attributes on parameters
Automaticaly generate `private readonly` fields
Forbid the use of primary constructor's parameters

## Usage

```cs
partial class C([Field]int i) // type must be partial, but can be class / struct / record
{
    public void M0()
    {
        i++;                    // error on usage of i
        Console.WriteLine(i);   // error on usage of i
    }
    public void M1()
    {
        var i = 0;
        i++;                    // don't error on usage of locals
        Console.WriteLine(_i);  // automaticaly created readonly field
    }
}
```

## Versions
|Version|Date|Comments|
|-------|----|--------|
|v0.1.0|19/04/2023|Initial release|