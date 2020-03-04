using System;

namespace Automatik.Api
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class ApiAttribute : Attribute
    {
        public string Url {get;set;}
        public Type UrlProvider {get;set;}

        public ApiAttribute()
        {
        }
    }
}