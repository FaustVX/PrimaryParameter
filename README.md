# Primary Parameter
[![NuGet version (FaustVX.PrimaryParameter.SG)](https://img.shields.io/nuget/v/FaustVX.PrimaryParameter.SG.svg)](https://www.nuget.org/packages/FaustVX.PrimaryParameter.SG/)
[![Update NuGet](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml/badge.svg)](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml)

Using a `Field` or `Property` attributes on parameters.

Automaticaly generate `private readonly` fields or `private` property.

Forbid the use of primary constructor's parameters.

## Usage

```cs
partial class C([Field(Name = "_a"), Field, [Property(WithInit = true)]]int i) // type must be partial, but can be class / struct
{
	// private readonly int _a = i; // generated field
	// private readonly int _i = i; // generated field
	// private int { get; init; } = i; // generated Property
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
        Console.WriteLine(_a);	// automaticaly created readonly field based on Name property
        Console.WriteLine(I);  // automaticaly created readonly property
    }
}
```

To eneble the feaure, type `[Field]` or `[Property]` before the primary parameter you want.

You can type as many attributes as you want on a single parameter.

- `Field`: attribute have
	- `Name`: property to modify the generated field name
- `Property`: attribute have
	- `Name`: property to modify the generated property name
	- `WithInit`: to generate the `init` accessor along the `get`

## Versions
|Version|Date|Comments|
|-------|----|--------|
|v0.3.0|20/04/2023|Added `Property` attribute|
|v0.2.0|20/04/2023|Support for `Name` fields and multiple `Field`|
|v0.1.0|19/04/2023|Initial release|
