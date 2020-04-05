// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    internal static class MakeLocalFunctionStaticHelper
    {
        public static bool IsStaticLocalFunctionSupported(SyntaxTree tree)
            => tree.Options is CSharpParseOptions csharpOption && csharpOption.LanguageVersion >= LanguageVersion.CSharp8;

        public static bool TryGetCaputuredSymbols(LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel, out ImmutableArray<ISymbol> captures)
        {
            var dataFlow = semanticModel.AnalyzeDataFlow(localFunction);
            if (dataFlow is null)
            {
                captures = default;
                return false;
            }

            captures = dataFlow.CapturedInside;
            return dataFlow.Succeeded;
        }

        public static bool TryGetCaputuredSymbolsAndCheckApplicability(LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel, out ImmutableArray<ISymbol> captures)
            => TryGetCaputuredSymbols(localFunction, semanticModel, out captures) && CanMakeLocalFunctionStatic(captures);

        private static bool CanMakeLocalFunctionStatic(ImmutableArray<ISymbol> captures)
            => captures.Length > 0 && !captures.Any(s => s.IsThisParameter());
    }
}
