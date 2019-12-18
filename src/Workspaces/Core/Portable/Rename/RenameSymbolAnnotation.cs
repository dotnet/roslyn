// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Provides a way to produce and consume annotations for rename
    /// that contain the previous symbol serialized as a <see cref="SymbolKey" />.
    /// This annotations is used by <see cref="Workspace.TryApplyChanges(Solution)" /> 
    /// in some cases to notify the workspace host of refactorings.
    /// </summary>
    internal static class RenameSymbolAnnotation
    {
        public const string RenameSymbolKind = nameof(RenameSymbolAnnotation);

        public static SyntaxAnnotation Create(ISymbol oldSymbol)
            => new SyntaxAnnotation(RenameSymbolKind, SerializeData(oldSymbol));

        internal static ISymbol ResolveSymbol(SyntaxAnnotation annotation, Compilation oldCompilation)
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

        private static string SerializeData(ISymbol oldSymbol)
            => oldSymbol.GetSymbolKey().ToString();
    }
}
