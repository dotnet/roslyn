// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Linq;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An attribute which identifies the method which an <see cref="IMethodHandler"/> implements.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false)]
#if BINARY_COMPAT // TODO - Remove with https://github.com/dotnet/roslyn/issues/72251
public class LanguageServerEndpointAttribute : Attribute
#else
internal class LanguageServerEndpointAttribute : Attribute
#endif
{
    /// <summary>
    /// Contains the method that this <see cref="IMethodHandler"/> implements.
    /// </summary>
    public string Method { get; }

    /// <summary>
    /// Contains the language name(s) supported by this <see cref="IMethodHandler"/>.
    /// </summary>
    public string[] Languages { get; }

    [Obsolete("Use the constructor that takes a language instead.", error: false)]
    public LanguageServerEndpointAttribute(string method)
        : this(method, LanguageServerConstants.DefaultLanguageName)
    {
    }

    /// <summary>
    /// Specifies the method that this <see cref="IMethodHandler"/> implements and the language(s) supported by it.
    /// </summary>
    /// <param name="method">The request handler method name.</param>
    /// <param name="language">The language name supported by this <see cref="IMethodHandler"/>. For example, <see cref="LanguageServerConstants.DefaultLanguageName"/>, 'C#', etc.</param>
    /// <param name="additionalLanguages">Additional language names supported by this <see cref="IMethodHandler"/>.</param>
    public LanguageServerEndpointAttribute(string method, string language, params string[] additionalLanguages)
    {
        Method = method;
        Languages = new[] { language }.Concat(additionalLanguages).ToArray();
    }
}
