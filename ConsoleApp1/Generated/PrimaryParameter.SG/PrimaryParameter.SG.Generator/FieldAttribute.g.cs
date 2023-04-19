namespace PrimaryParameter.SG
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed class FieldAttribute : Attribute
    {
        public string Name { get; init; }
    }
}