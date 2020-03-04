using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
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

                var headers = this.driver.headers.ToDictionary(kv => kv.Key, kv => kv.Value);
                var request = new RestRequest(apiMethodAttr.Path, apiMethodAttr.Method);

                var headerAttrs = targetMethod.GetCustomAttributes<ApiMethodHeaderAttribute>();
                if (headerAttrs.Any()) {
                    var headersFromAttrs = headerAttrs.ToDictionary(attr => attr.Name, attr => attr.Value);
                    request.AddHeaders(headersFromAttrs);
                    headers.Union(headersFromAttrs).ToDictionary(kv => kv.Key, kv => kv.Value);
                }

                var formParams = new Dictionary<string, string>();

                foreach (var parameterInfo in targetMethod.GetParameters())
                {
                    var ApiMethodParamAttr = parameterInfo.GetCustomAttribute<ApiMethodParamAttribute>();
                    if (ApiMethodParamAttr == null)
                        continue;

                    switch (ApiMethodParamAttr.Type)
                    {
                        case ApiMethodParamType.Header:
                            headers.Add(ApiMethodParamAttr.Name.ToLower(), args[parameterInfo.Position]?.ToString() ?? "");
                            request.AddHeader(ApiMethodParamAttr.Name, args[parameterInfo.Position]?.ToString() ?? "");
                            break;

                        case ApiMethodParamType.BodyParam:
                            formParams.Add(ApiMethodParamAttr.Name, args[parameterInfo.Position]?.ToString() ?? "");
                            break;

                        case ApiMethodParamType.File:

                            if (args[parameterInfo.Position] != null)
                            {
                                if (args[parameterInfo.Position].GetType() == typeof(string))
                                    request.AddFile(ApiMethodParamAttr.Name, (string)args[parameterInfo.Position]);
                            }

                            break;
                    }
                }

                if (formParams.Any())
                {
                    if (headers.TryGetValue("content-type", out var contentType))
                        request.AddParameter(
                            contentType,
                            string.Join("&", formParams.Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(kv.Value)}")), 
                            ParameterType.RequestBody
                        );
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
        private Dictionary<string, string> headers = new Dictionary<string, string>();


        public readonly TApi Api;


        public struct ApiDriverOptions
        {
            public Dictionary<string, string> Headers;
        }

        public ApiDriver(ApiDriverOptions? options = null)
        {
            var apiAttr = typeof(TApi).GetCustomAttribute<ApiAttribute>();

            var url = apiAttr.Url;
            if (url == null)
            {
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
            if (headerAttrs.Any()) {
                var headersFromAttrs = headerAttrs.ToDictionary(attr => attr.Name.ToLower(), attr => attr.Value);
                client.AddDefaultHeaders(headersFromAttrs);
                headers = headers.Union(headersFromAttrs).ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            if (options?.Headers != null) {
                var headersFromOpts = options?.Headers.ToDictionary(kv => kv.Key.ToLower(), kv => kv.Value);
                client.AddDefaultHeaders(headersFromOpts);
                headers = headers.Union(headersFromOpts).ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            Api = DispatchProxy.Create<TApi, ApiProxy>();
            ((ApiProxy)(Api as object)).Init(this);
        }


        public void AddHeader(string name, string value) =>
            client.AddDefaultHeader(name, value);

    }
}
