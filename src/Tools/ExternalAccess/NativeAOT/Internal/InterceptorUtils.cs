// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class InterceptorUtils
{
    /// <summary>
    /// If the call denoted by <paramref name="invocation"/> is intercepted in <paramref name="compilation"/>, returns the method symbol for the interceptor.
    /// Otherwise, returns null.
    /// </summary>
    /// <remarks>
    /// Calling this method forces completion of attributes on all source symbols.
    /// If using this API in an analyzer, you may want to only access it through
    /// the callback to 'RegisterCompilationEndAction', which will avoid potential negative perf impact in the IDE.
    /// </remarks>
    internal static MethodSymbol? GetInterceptor(InvocationExpressionSyntax invocation, CSharpCompilation compilation, CancellationToken cancellationToken)
    {
        // Force binding of all source attributes on methods.
        // This ensures that the backing data structure for 'TryGetInterceptor' is fully populated.
        compilation.GlobalNamespace.ForceComplete(locationOpt: null, cancellationToken);

        var location = invocation.GetInterceptableLocation();
        if (compilation.TryGetInterceptor(location) is var (_, interceptor))
        {
            return interceptor;
        };

        return null;
    }
}
