using System;

namespace Automatik.Api
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ApiMethodHeaderAttribute : Attribute
    {
        public readonly string Name;
        public readonly string Value;

        public ApiMethodHeaderAttribute(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
    }
}