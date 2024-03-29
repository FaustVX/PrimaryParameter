﻿//HintName: FieldAttribute.g.cs
// <auto-generated/>
using global::System;
namespace PrimaryParameter.SG
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    sealed class FieldAttribute : Attribute
    {
        public string Name { get; init; }
        public string AssignFormat { get; init; }
        public Type Type { get; init; }
        public bool IsReadonly { get; init; }
        public string Scope { get; init; }
    }
}
