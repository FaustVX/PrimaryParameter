# Primary Parameter
[![NuGet version (FaustVX.PrimaryParameter.SG)](https://img.shields.io/nuget/v/FaustVX.PrimaryParameter.SG.svg)](https://www.nuget.org/packages/FaustVX.PrimaryParameter.SG/)
[![Update NuGet](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml/badge.svg)](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml)

## Description
Using a `Field` or `Property` attribute on parameters.

Automaticaly generate `private readonly` fields or `private` properties.

Forbid the use of primary constructor's parameters.

## Usage

```cs
partial class C([Field(Name = "_a", AssignFormat = "{0} + 1"), [Field(Name = nameof(C._b))], Field, [Property(WithInit = true)]]int i) // type must be partial, but can be class / struct
{
# region Generated members
    // private readonly int _a = i + 1; // generated field (with formated assignment)
    // private readonly int _b = i;     // generated field (with computed name)
    // private readonly int _i = i;     // generated field
    // private int { get; init; } = i;  // generated Property
# endregion

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
        Console.WriteLine(_a);  // automaticaly created readonly field based on Name property
        Console.WriteLine(I);   // automaticaly created readonly property
    }
}
```

To enable the feaure, type `[Field]` or `[Property]` before the primary parameter you want.

You can type as many attributes as you want on a single parameter.

## Attribute Properties
|Attribute|Property|Comments|Default value|
|---------|--------|--------|-------------|
|`Field`|`Name`|Property to modify the generated field name|`_i` (for a parameter named `i`)|
||`AssignFormat`|To change the assignment for that field|`{0}`|
|`Property`|`Name`|Property to modify the generated field name|`I` (for a parameter named `i`)|
||`WithInit`|To generate the `init` accessor along the `get`|`false`|
||`Scope`|To change the scope of the generated property|`private`|
||`AssignFormat`|To change the assignment for that property|`{0}`|

## Reported Diagnostics
|Code|Title|Message|Severity|
|----|-----|-------|--------|
|`PC01`|Accessing a Primary Parameter|Can't access a primary parameter ('{0}') with a [Field] or [Property] attribute, use {1}|`Error`|
|`PC02`|Attribute generate nothing|Use this attributes only on primary parameter|`Warning`|
|`PC03`|Attribute generate nothing|This member's name ('{0}') is already used|`Warning`</br>`Error` when a member's name is already used in the type|

## Versions
|Version|Date|Comments|
|-------|----|--------|
|v0.4.3|27/04/2023|`Field` and `Property` now have `using global::System`|
|v0.4.2|26/04/2023|Bug-fix with previous update|
|v0.4.1|25/04/2023|More precise warnings/errors location|
|v0.4.0|22/04/2023|Added `PC02` and `PC03` warnings/errors|
|v0.3.1|21/04/2023|Added `Scope` property on `Property` attribute</br>Attributes are `internal`|
|v0.3.0|20/04/2023|Added `Property` attribute|
|v0.2.0|20/04/2023|Support for `Name` fields and multiple `Field`|
|v0.1.0|19/04/2023|Initial release|
