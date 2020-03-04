using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RestSharp;

namespace Automatik.Api
{
    public class ApiDriver<TApi> 
        where TApi : class
    {   
        public class ApiProxy : DispatchProxy
        {
            private ApiDriver<TApi> driver;

            public void Init(ApiDriver<TApi> driver)
            {
                this.driver = driver;
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                var apiMethodAttr = targetMethod.GetCustomAttribute<ApiMethodAttribute>();

                var request = new RestRequest(apiMethodAttr.Path, apiMethodAttr.Method);
                
                var headerAttrs = targetMethod.GetCustomAttributes<ApiMethodHeaderAttribute>();
                if (headerAttrs.Any())
                    request.AddHeaders(headerAttrs.ToDictionary(attr => attr.Name, attr => attr.Value));

                foreach (var parameterInfo in targetMethod.GetParameters())
                {
                    var ApiMethodParamAttr = parameterInfo .GetCustomAttribute<ApiMethodParamAttribute>();
                    if (ApiMethodParamAttr == null)
                        continue;

                    switch (ApiMethodParamAttr.Type)
                    {
                        case ApiMethodParamType.BodyParam:
                            request.AddParameter(ApiMethodParamAttr.Name, args[parameterInfo.Position], ParameterType.RequestBody);
                            break;

                        case ApiMethodParamType.File:

                            if (args[parameterInfo.Position] != null) {
                                if (args[parameterInfo.Position].GetType() == typeof(string))
                                    request.AddFile(ApiMethodParamAttr.Name, (string)args[parameterInfo.Position]);
                            }

                            break;
                    }
                }

                var response = driver.client.Execute(request);

                if (
                    targetMethod.ReturnType == typeof(RestResponse) || 
                    targetMethod.ReturnType == typeof(IRestResponse)
                )
                    return response;

                return null;
            }
        }


        private RestClient client;


        public readonly TApi Api;


        public struct ApiDriverOptions 
        {
            public Dictionary<string, string> Headers;
        }

        public ApiDriver(ApiDriverOptions? options = null)
        {
            var apiAttr = typeof(TApi).GetCustomAttribute<ApiAttribute>();

            var url = apiAttr.Url;
            if (url == null) {
                if (apiAttr.UrlProvider == null)
                    throw new Exception($"Provider [{nameof(ApiAttribute.Url)}] or [{nameof(ApiAttribute.UrlProvider)}] propertires for [{typeof(ApiAttribute).FullName}] attribute.");

                if (!typeof(IUrlProvider).IsAssignableFrom(apiAttr.UrlProvider))
                    throw new Exception($"[{nameof(ApiAttribute.UrlProvider)}] type in [{typeof(ApiAttribute).FullName}] attribute must implements [{typeof(IUrlProvider).FullName}] interface.");

                url = ((IUrlProvider)Activator.CreateInstance(apiAttr.UrlProvider)).GetUrl(typeof(TApi));
            }

            if (url == null)
                throw new Exception($"Neither [{nameof(ApiAttribute.Url)}] nor [{nameof(ApiAttribute.UrlProvider)}] provided not null string.");

            client = new RestClient();

            var headerAttrs = typeof(TApi).GetCustomAttributes<ApiHeaderAttribute>();
            if (headerAttrs.Any())
                client.AddDefaultHeaders(headerAttrs.ToDictionary(attr => attr.Name, attr => attr.Value));

            if (options?.Headers != null) 
                client.AddDefaultHeaders(options?.Headers);

            Api = DispatchProxy.Create<TApi, ApiProxy>();
            ((ApiProxy)(Api as object)).Init(this);
        }


        public void AddHeader(string name, string value) =>
            client.AddDefaultHeader(name, value);

    }
}
