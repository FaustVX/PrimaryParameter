# Primary Parameter
[![NuGet version (FaustVX.PrimaryParameter.SG)](https://img.shields.io/nuget/v/FaustVX.PrimaryParameter.SG.svg)](https://www.nuget.org/packages/FaustVX.PrimaryParameter.SG/)
[![Update NuGet](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml/badge.svg)](https://github.com/FaustVX/PrimaryParameter/actions/workflows/pushToNuget.yaml)

## Description
Using a `Field` or `RefField` or `Property` attribute on parameters.

Automaticaly generate `private readonly` fields or `private readonly ref readonly` fields or `public` properties.

Forbid the use of primary constructor's parameters.

## Usage

```cs
partial class C([Field(Name = "_a", AssignFormat = "{0}.ToString()", Type = typeof(string)), Field(Name = nameof(C._b)), Field, Property(WithInit = true)]int i) // type must be partial, but can be class / struct
{
# region Generated members
    // private readonly string _a = i.ToString();   // generated field (with type and formated assignment)
    // private readonly int _b = i;                 // generated field (with computed name)
    // private readonly int _i = i;                 // generated field
    // private int { get; init; } = i;              // generated Property
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

ref partial struct Ref([RefField(IsReadonlyRef = false, IsRefReadonly = false), RefField(Name = nameof(Ref.I), Scope = "public")]int i)
{
# region Generated members
    private ref int _i = ref Unsafe.AsRef(in i);                 // Bug in the compiler (https://github.com/dotnet/roslyn/issues/67371)
    public readonly ref readonly int I = ref Unsafe.AsRef(in i); // Bug in the compiler (https://github.com/dotnet/roslyn/issues/67371)
# endregion
}
```

To enable the feaure, type `[Field]` or `[RefField]` or `[Property]` before the primary parameter you want.

You can type as many attributes as you want on a single parameter.

## Attribute Properties
|Attribute|Property|Comments|Default value|
|---------|--------|--------|-------------|
|`Field`|`Name`|Property to modify the generated field name|`_i` (for a parameter named `i`)|
||`IsReadnoly`|To generate the `readonly` modifier|`true`|
||`Scope`|To change the scope of the generated property|`private`|
||`AssignFormat`|To change the assignment for that field|`{0}`|
||`Type`|To change the type for that field|same type as parameter|
|`RefField`|`Name`|Property to modify the generated field name|`_i` (for a parameter named `i`)|
||`IsReadnolyRef`|To generate the `readonly ref` modifier|`true`|
||`IsRefReadnoly`|To generate the `ref readonly` modifier|`true`|
||`Scope`|To change the scope of the generated property|`private`|
|`Property`|`Name`|Property to modify the generated field name|`I` (for a parameter named `i`)|
||`WithInit`|To generate the `init` accessor along the `get`|`false`|
||`Scope`|To change the scope of the generated property|`public`|
||`AssignFormat`|To change the assignment for that property|`{0}`|
||`Type`|To change the type for that property|same type as parameter|

## Reported Diagnostics
|Code|Title|Message|Severity|
|----|-----|-------|--------|
|`PC01`|Accessing a Primary Parameter|Can't access a primary parameter ('{0}') with a [Field] or [RefField] or [Property] attribute, use {1}|`Error`|
|`PC02`|Attribute generate nothing|Use this attributes only on primary parameter|`Warning`|
|`PC03`|Attribute generate nothing|This member's name ('{0}') is already used|`Warning`</br>`Error` when a member's name is already used in the type|
|`PC04`|RefField in non ref struct|Can't apply [RefField] in non ref struct '{0}'|`Error`|
|`PC05`|RefField on non ref parameter|Can't apply [RefField] on non ref parameter '{0}'|`Error`|

## Versions
|Version|Date|Comments|
|-------|----|--------|
|v1.0.0|01/08/2023|Added code-fixes|
|v0.4.7|16/07/2023|Don't error on `nameof` access or inside the same argument list usage|
|v0.4.6.1|16/07/2023|Fix typos in Readme.md|
|v0.4.6|16/07/2023|Added `RefField` attribute</br>Curently uses `Unsafe.AsRef()` due to a compiler bug [dotnet/roslyn#67371](https://github.com/dotnet/roslyn/issues/67371)|
|v0.4.5|18/05/2023|Added `Scope` and `IsReadonly` properties on `Field`</br>`Scope` property on `Property` defaulted to `public`|
|v0.4.4|28/04/2023|Added `AssignFormat` and `Type` properties|
|v0.4.3|27/04/2023|`Field` and `Property` now have `using global::System`|
|v0.4.2|26/04/2023|Bug-fix with previous update|
|v0.4.1|25/04/2023|More precise warnings/errors location|
|v0.4.0|22/04/2023|Added `PC02` and `PC03` warnings/errors|
|v0.3.1|21/04/2023|Added `Scope` property on `Property` attribute</br>Attributes are `internal`|
|v0.3.0|20/04/2023|Added `Property` attribute|
|v0.2.0|20/04/2023|Support for `Name` fields and multiple `Field`|
|v0.1.0|19/04/2023|Initial release|
