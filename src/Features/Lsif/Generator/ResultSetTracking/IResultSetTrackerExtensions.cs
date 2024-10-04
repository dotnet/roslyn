// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Roslyn.LanguageServer.Protocol;
using Moniker = Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph.Moniker;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.ResultSetTracking
{
    internal static class IResultSetTrackerExtensions
    {
        /// <summary>
        /// Returns the ID of the <see cref="ReferenceResult"/> for a <see cref="ResultSet"/>.
        /// </summary>
        public static Id<ReferenceResult> GetResultSetReferenceResultId(this IResultSetTracker tracker, ISymbol symbol)
            => tracker.GetResultIdForSymbol(symbol, Methods.TextDocumentReferencesName, static idFactory => new ReferenceResult(idFactory));

        /// <summary>
        /// Fetches the moniker node for a symbol; this should only be called on symbols where <see cref="SymbolMoniker.HasMoniker"/> returns true.
        /// </summary>
        public static Id<Moniker> GetMoniker(this IResultSetTracker tracker, ISymbol symbol, Compilation sourceCompilation)
        {
            return tracker.GetResultIdForSymbol(symbol, "moniker", idFactory =>
            {
                var moniker = SymbolMoniker.Create(symbol);

                string? kind;

                if (symbol.Kind == SymbolKind.Namespace)
                {
                    kind = null;
                }
                else if (symbol.ContainingAssembly.Equals(sourceCompilation.Assembly))
                {
                    kind = "export";
                }
                else
                {
                    kind = "import";
                }

                // Since we fully qualify everything, all monitors are unique within the scheme
                return new Moniker(moniker.Scheme, moniker.Identifier, kind, unique: "scheme", idFactory);
            });
        }
    }
}
