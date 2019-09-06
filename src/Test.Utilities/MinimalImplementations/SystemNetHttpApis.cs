// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class SystemNetHttpApis
    {
        public const string CSharp = @"
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
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

    public class HttpRequestMessage
    {
    }

    public class WinHttpHandler : HttpMessageHandler
    {
        public bool CheckCertificateRevocationList { get; set; }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors,bool> ServerCertificateValidationCallback { get; set; }
    }

    public class HttpClientHandler : HttpMessageHandler
    {
        public bool CheckCertificateRevocationList { get; set; }

        public Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }
    }

    public class CurlHandler : HttpMessageHandler
    {
        public bool CheckCertificateRevocationList { get; set; }

        internal Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback { get; set; }
    }
}";
    }
}
