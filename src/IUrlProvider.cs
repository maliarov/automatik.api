using System;

namespace Automatik.Api
{
    public interface IUrlProvider
    {
        string GetUrl(Type apiType);
    }

}