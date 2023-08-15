﻿//HintName: RefFieldAttribute.g.cs
using global::System;
namespace PrimaryParameter.SG
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    sealed class RefFieldAttribute : Attribute
    {
        public string Name { get; init; }
        public string Scope { get; init; }
        public bool IsReadonlyRef { get; init; }
        public bool IsRefReadonly { get; init; }
    }
}
