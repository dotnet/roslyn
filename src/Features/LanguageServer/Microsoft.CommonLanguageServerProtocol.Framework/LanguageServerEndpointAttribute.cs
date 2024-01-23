// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An attribute which identifies the method which an <see cref="IMethodHandler"/> implements.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
public class LanguageServerEndpointAttribute : Attribute
{
    /// <summary>
    /// Contains the method that this <see cref="IMethodHandler"/> implements.
    /// </summary>
    public string Method { get; }

    public LanguageServerEndpointAttribute(string method)
    {
        Method = method;
    }
}
