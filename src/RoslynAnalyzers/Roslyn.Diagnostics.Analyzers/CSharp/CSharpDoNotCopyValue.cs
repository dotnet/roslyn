// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpDoNotCopyValue : AbstractDoNotCopyValue
    {
        protected override NonCopyableWalker CreateWalker(OperationBlockAnalysisContext context, NonCopyableTypesCache cache)
            => new CSharpNonCopyableWalker(context, cache);

        protected override NonCopyableSymbolWalker CreateSymbolWalker(SymbolAnalysisContext context, NonCopyableTypesCache cache)
            => new CSharpNonCopyableSymbolWalker(context, cache);

        private sealed class CSharpNonCopyableWalker : NonCopyableWalker
        {
            public CSharpNonCopyableWalker(OperationBlockAnalysisContext context, NonCopyableTypesCache cache)
                : base(context, cache)
            {
            }

            protected override bool CheckForEachGetEnumerator(IForEachLoopOperation operation, [DisallowNull] ref IConversionOperation? conversion, [DisallowNull] ref IOperation? instance)
            {
                if (operation.Syntax is CommonForEachStatementSyntax syntax
                    && operation.SemanticModel.GetForEachStatementInfo(syntax).GetEnumeratorMethod is { } getEnumeratorMethod)
                {
                    CheckMethodSymbolInUnsupportedContext(operation, getEnumeratorMethod);

                    if (instance is not null
                        && Cache.IsNonCopyableType(getEnumeratorMethod.ReceiverType)
                        && !getEnumeratorMethod.IsReadOnly
                        && Acquire(instance) == RefKind.In)
                    {
                        // mark the instance as not checked by this method
                        instance = null;
                    }

                    return true;
                }

                return false;
            }
        }

        private sealed class CSharpNonCopyableSymbolWalker : NonCopyableSymbolWalker
        {
            public CSharpNonCopyableSymbolWalker(SymbolAnalysisContext context, NonCopyableTypesCache cache)
                : base(context, cache)
            {
            }
        }
    }
}
