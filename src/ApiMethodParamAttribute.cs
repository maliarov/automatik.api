using System;

namespace Automatik.Api
{
    public enum ApiMethodParamType
    {
        Header,
        Cookie,
        UrlParam,
        QueryParam,
        BodyParam,
        File
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public class ApiMethodParamAttribute : Attribute
    {
        public ApiMethodParamType Type {get;set;}
        public string Name {get;set;}

        public ApiMethodParamAttribute()
        {}

    }
}
