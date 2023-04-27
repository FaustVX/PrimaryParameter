using global::System;
namespace PrimaryParameter.SG
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    sealed class FieldAttribute : Attribute
    {
        public string Name { get; init; }
    }
}