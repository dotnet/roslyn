// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SourceGeneration;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration;

internal static partial class SyntaxValueProviderExtensions
{
    /// <inheritdoc cref="Microsoft.CodeAnalysis.SyntaxValueProviderExtensions.CreateSyntaxProviderForAttribute{T}"/>
    internal static IncrementalValuesProvider<T> CreateSyntaxProviderForAttribute<T>(this SyntaxValueProvider provider, string simpleName)
        where T : SyntaxNode
    {
        return provider.CreateSyntaxProviderForAttribute<T>(simpleName, CSharpSyntaxHelper.Instance, compilationGlobalAliases: null);
    }
}
