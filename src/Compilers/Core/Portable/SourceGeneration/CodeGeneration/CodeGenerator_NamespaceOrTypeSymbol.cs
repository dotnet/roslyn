// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static INamespaceOrTypeSymbol With(
            this INamespaceOrTypeSymbol namespaceOrType,
            Optional<ISymbol> containingSymbol = default)
        {
            if (namespaceOrType is INamespaceSymbol nsSymbol)
                return nsSymbol.With(containingSymbol: containingSymbol);
            else if (namespaceOrType is INamedTypeSymbol namedType)
                return namedType.With(containingSymbol: containingSymbol);

            throw ExceptionUtilities.UnexpectedValue(namespaceOrType.Kind);
        }

        private abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbol
        {
            #region default implementation

            public bool IsNamespace => throw new System.NotImplementedException();
            public bool IsType => throw new System.NotImplementedException();
            public virtual ImmutableArray<ISymbol> GetMembers() => throw new System.NotImplementedException();
            public ImmutableArray<ISymbol> GetMembers(string name) => throw new System.NotImplementedException();
            public ImmutableArray<INamedTypeSymbol> GetTypeMembers() => throw new System.NotImplementedException();
            public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name) => throw new System.NotImplementedException();
            public ImmutableArray<INamedTypeSymbol> GetTypeMembers(string name, int arity) => throw new System.NotImplementedException();

            #endregion
        }
    }
}
