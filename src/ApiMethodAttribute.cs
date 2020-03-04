using System;
using System.Net;
using RestSharp;

namespace Automatik.Api
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ApiMethodAttribute : Attribute
    {
        public string Path { get; set; }

        public Method Method { get; set; }

        public ApiMethodAttribute()
        {
        }
    }

}