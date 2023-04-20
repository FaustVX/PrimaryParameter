namespace PrimaryParameter.SG
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed class PropertyAttribute : Attribute
    {
        public string Name { get; init; }
        public bool WithInit { get; init; }
    }
}