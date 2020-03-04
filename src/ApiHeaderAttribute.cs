using System;

namespace Automatik.Api
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class ApiHeaderAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Value;

        public ApiHeaderAttribute(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

    }
}