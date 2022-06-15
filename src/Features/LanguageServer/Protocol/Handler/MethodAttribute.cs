// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class MethodAttribute : Attribute
    {
        /// <summary>
        /// Contains the method that this <see cref="IRequestHandler{RequestContextType}"/> implements.
        /// </summary>
        public string Method { get; }

        public MethodAttribute(string method)
        {
            Method = method;
        }
    }
}
