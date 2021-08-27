using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CallstackExplorer;
using Microsoft.CodeAnalysis.Text;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal class ParsedLine
    {
        public ParsedLine(string originalLine, TextSpan symbolSpan)
        {
            OriginalLine = originalLine;
            SymbolSpan = symbolSpan;
        }

        public string OriginalLine { get; }
        public TextSpan SymbolSpan { get; }

        protected (string methodName, string arguments) GetMethodSignatureParts()
        {
            var signature = OriginalLine.Substring(SymbolSpan.Start, SymbolSpan.Length);

            var openingBrace = signature.IndexOf('(');
            var closingBrace = signature.LastIndexOf(')');

            var methodName = signature.Substring(0, openingBrace);

            var length = closingBrace - (openingBrace + 1);
            var arguments = length == 0
                ? string.Empty 
                : signature.Substring(openingBrace + 1, length);

            return (methodName, arguments);
        }

        public virtual async Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
        {
            var (methodName, _) = GetMethodSignatureParts();

            foreach (var project in solution.Projects)
            {
                var foundSymbols = await FindSymbols.DeclarationFinder.FindSourceDeclarationsWithPatternAsync(
                    project,
                    methodName,
                    SymbolFilter.Member,
                    cancellationToken).ConfigureAwait(false);

                var foundSymbol = foundSymbols.Length == 1
                    ? foundSymbols[0]
                    : null;

                if (foundSymbol is not null)
                {
                    return foundSymbol;
                }
            }

            return null;
        }
    }
}
