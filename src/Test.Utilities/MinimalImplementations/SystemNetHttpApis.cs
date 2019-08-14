// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class SystemNetHttpApis
    {
        public const string CSharp = @"
namespace System.Net.Http
{
    namespace Unix
    {
        public class CurlHandler : HttpMessageHandler
        {
            public bool CheckCertificateRevocationList { get; set; }
        }
    }

    public class HttpClient
    {
        public HttpClient ()
        {
        }

        public HttpClient(HttpMessageHandler handler)
        {
        }

        public HttpClient (HttpMessageHandler handler, bool disposeHandler)
        {
        }
    }

    public abstract class HttpMessageHandler
    {
    }

    public class WinHttpHandler : HttpMessageHandler
    {
        public bool CheckCertificateRevocationList { get; set; }
    }

    public class HttpClientHandler : HttpMessageHandler
    {
        public bool CheckCertificateRevocationList { get; set; }
    }
}";
    }
}
