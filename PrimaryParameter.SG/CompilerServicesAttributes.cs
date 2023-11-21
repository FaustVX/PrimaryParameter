namespace System.Runtime.CompilerServices;

sealed class IsExternalInit();

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple=false, Inherited=false)]
sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
    public string ParameterName { get; } = parameterName;
}
