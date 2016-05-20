// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// IDE-only document based diagnostic analyzer.
    /// </summary>
    internal abstract class DocumentDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        // REVIEW: why DocumentDiagnosticAnalyzer doesn't have span based analysis?
        // TODO: Make abstract once TypeScript and F# move over to the overloads above
        public async virtual Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<Diagnostic>.GetInstance();
            await AnalyzeSyntaxAsync(document, builder.Add, cancellationToken).ConfigureAwait(false);
            return builder.ToImmutableAndFree();
        }

        // TODO: Make abstract once TypeScript and F# move over to the overloads above
        public async virtual Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<Diagnostic>.GetInstance();
            await AnalyzeSemanticsAsync(document, builder.Add, cancellationToken).ConfigureAwait(false);
            return builder.ToImmutableAndFree();
        }

        // TODO: Remove once TypeScript and F# move over to the overloads above
        public virtual Task AnalyzeSyntaxAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            throw ExceptionUtilities.Unreachable;
        }

        // TODO: Remove once TypeScript and F# move over to the overloads above
        public virtual Task AnalyzeSemanticsAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// it is not allowed one to implement both DocumentDiagnosticAnalzyer and DiagnosticAnalyzer
        /// </summary>
        public sealed override void Initialize(AnalysisContext context)
        {
        }
    }
}
