// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeActions
{
    internal static class OmniSharpCodeFixContextFactory
    {
        public static CodeFixContext Create(
            Document document,
            TextSpan span,
            ImmutableArray<Diagnostic> diagnostics,
            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix,
            OmniSharpImplementTypeOptions implementTypeOptions,
            CancellationToken cancellationToken)
            => new(document, span, diagnostics, registerCodeFix, GetCodeActionOptions(implementTypeOptions), cancellationToken);

        private static CodeActionOptions GetCodeActionOptions(OmniSharpImplementTypeOptions implementTypeOptions)
            => new(
                SymbolSearchOptions.Default,
                new ImplementTypeOptions(
                    InsertionBehavior: (ImplementTypeInsertionBehavior)implementTypeOptions.InsertionBehavior,
                    PropertyGenerationBehavior: (ImplementTypePropertyGenerationBehavior)implementTypeOptions.PropertyGenerationBehavior));
    }
}
