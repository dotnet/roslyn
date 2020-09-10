// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Provides a way to produce and consume annotations for rename
    /// that contain the previous symbol serialized as a <see cref="SymbolKey" />.
    /// This annotations is used by <see cref="Workspace.TryApplyChanges(Solution)" /> 
    /// in some cases to notify the workspace host of refactorings.
    /// </summary>
    /// <remarks>
    /// This annotation is applied to the declaring syntax of a symbol that has been renamed. 
    /// When Workspace.TryApplyChanges happens in Visual Studio, we raise rename events for that symbol.
    /// </remarks>
    internal static class RenameSymbolAnnotation
    {
        public const string RenameSymbolKind = nameof(RenameSymbolAnnotation);

        /// <summary>
        /// Returns true if there are no existing rename annotations
        /// and the symbol is a candidate for annotation. Out parameter
        /// is the node with the annotation on it
        /// </summary>
        public static bool TryAnnotateNode(SyntaxNode syntaxNode, ISymbol? symbol, [NotNullWhen(returnValue: true)] out SyntaxNode? annotatedNode)
        {
            annotatedNode = null;
            if (!ShouldAnnotateSymbol(symbol))
            {
                return false;
            }

            // If the node is already annotated, assume the original annotation is 
            // more correct for representing the original symbol
            if (syntaxNode.GetAnnotations(RenameSymbolKind).Any())
            {
                return false;
            }

            var annotation = new SyntaxAnnotation(RenameSymbolKind, SerializeData(symbol));
            annotatedNode = syntaxNode.WithAdditionalAnnotations(annotation);
            return true;
        }

        public static bool ShouldAnnotateSymbol([NotNullWhen(returnValue: true)] ISymbol? symbol)
            => symbol is null
            ? false
            : symbol.DeclaringSyntaxReferences.Any();

        public static ISymbol? ResolveSymbol(this SyntaxAnnotation annotation, Compilation oldCompilation)
        {
            if (annotation.Kind != RenameSymbolKind)
            {
                throw new InvalidOperationException($"'{annotation}' is not of kind {RenameSymbolKind}");
            }

            if (string.IsNullOrEmpty(annotation.Data))
            {
                throw new InvalidOperationException($"'{annotation}' has no data");
            }

            var oldSymbolKey = SymbolKey.ResolveString(annotation.Data, oldCompilation);

            return oldSymbolKey.Symbol;
        }

        private static string SerializeData(ISymbol symbol)
            => symbol.GetSymbolKey().ToString();
    }
}
