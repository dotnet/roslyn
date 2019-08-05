// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class ASPNetCoreApis
    {
        public const string CSharp = @"
using System;
using System.Threading.Tasks;

class MyValidateAntiForgeryAttribute : Attribute
{
}

namespace Microsoft.AspNetCore
{
    namespace Antiforgery
    {
        using Microsoft.AspNetCore.Http;

        public interface IAntiforgery
        {
            Task ValidateRequestAsync (HttpContext httpContext);
        }

        namespace Internal
        {
            using Microsoft.AspNetCore.Http;

            public class DefaultAntiforgery : IAntiforgery
            {
                public Task ValidateRequestAsync (HttpContext httpContext)
                {
                    return null;
                }
            }
        }
    }

    namespace Mvc
    {
        public class AcceptedAtActionResult
        {
        }

        public abstract class ControllerBase
        {
            public virtual AcceptedAtActionResult AcceptedAtAction (string actionName)
            {
                return null;
            }
        }

        public abstract class Controller : ControllerBase
        {
        }

        public class HttpPostAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
        {
        }

        public class HttpPutAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
        {
        }

        public class HttpDeleteAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
        {
        }

        public class HttpPatchAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
        {
        }

        public class HttpGetAttribute : Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute
        {
        }

        public sealed class NonActionAttribute : Attribute
        {
        }

        namespace Filters
        {
            public class FilterCollection : System.Collections.ObjectModel.Collection<IFilterMetadata>
            {
                public FilterCollection ()
                {
                }

                public IFilterMetadata Add<TFilterType> () where TFilterType : IFilterMetadata
                {
                    return null;
                }

                public IFilterMetadata Add (Type filterType)
                {
                    return null;
                }
            }

            public interface IFilterMetadata
            {
            }

            public class AuthorizationFilterContext
            {
            }

            public interface IAsyncAuthorizationFilter : IFilterMetadata
            {
                Task OnAuthorizationAsync (AuthorizationFilterContext context);
            }

            public interface IAuthorizationFilter : IFilterMetadata
            {
                Task OnAuthorization (AuthorizationFilterContext context);
            }
        }
        
        namespace Routing
        {
            public class HttpMethodAttribute : Attribute
            {
            }
        }
    }

    namespace Http
    {
        public abstract class HttpContext
        {
        }

        public interface IResponseCookies
        {
            void Append(string key, string value);

            void Append(string key, string value, CookieOptions options);
        }

        public class CookieOptions
        {
            public CookieOptions()
            {
            }

            public bool Secure { get; set; }
        }

        namespace Internal
        {
            public class ResponseCookies : IResponseCookies
            {
                public ResponseCookies()
                {
                }

                public void Append(string key, string value)
                {
                }

                public void Append(string key, string value, CookieOptions options)
                {
                }
            }
        }
    }
}";
    }
}
