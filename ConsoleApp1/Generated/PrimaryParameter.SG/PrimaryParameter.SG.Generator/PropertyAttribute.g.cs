﻿using global::System;
namespace PrimaryParameter.SG
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    sealed class PropertyAttribute : Attribute
    {
        public string Name { get; init; }
        public string AssignFormat { get; init; }
        public bool WithInit { get; init; }
        public string Scope { get; init; }
    }
}