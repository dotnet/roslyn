// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class MethodAttribute : Attribute
    {
        /// <summary>
        /// Contains the method and concrete type of the request handler that implements <see cref="IRequestHandler{RequestType, ResponseType}"/>.
        /// The type and method are passed to StreamJsonRpc to create RPC targets without actually instantiating the handler.
        /// </summary>
        public string Method { get; }

        public MethodAttribute(string method)
        {
            Method = method;
        }
    }
}
