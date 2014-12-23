// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticAnalysisContextHelpers
    {
        internal static void VerifyArguments<TContext>(Action<TContext> action)
        {
            VerifyAction(action);
        }

        internal static void VerifyArguments<TContext>(Action<TContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            VerifyAction(action);
            VerifySymbolKinds(symbolKinds);
        }

        internal static void VerifyArguments<TContext, TLanguageKindEnum>(Action<TContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
            where TLanguageKindEnum : struct
        {
            VerifyAction(action);
            VerifySyntaxKinds(syntaxKinds);
        }

        internal static void VerifyArguments(Diagnostic diagnostic)
        {
            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }
        }

        private static void VerifyAction<TContext>(Action<TContext> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
        }

        private static void VerifySymbolKinds(ImmutableArray<SymbolKind> symbolKinds)
        {
            if (symbolKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(symbolKinds));
            }

            if (symbolKinds.IsEmpty)
            {
                throw new ArgumentException(AnalyzerDriverResources.ArgumentCannotBeEmpty, nameof(symbolKinds));
            }
        }

        private static void VerifySyntaxKinds<TLanguageKindEnum>(ImmutableArray<TLanguageKindEnum> syntaxKinds)
            where TLanguageKindEnum : struct
        {
            if (syntaxKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(syntaxKinds));
            }

            if (syntaxKinds.IsEmpty)
            {
                throw new ArgumentException(AnalyzerDriverResources.ArgumentCannotBeEmpty, nameof(syntaxKinds));
            }
        }
    }
}
