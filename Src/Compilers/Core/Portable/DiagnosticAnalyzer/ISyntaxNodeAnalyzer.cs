// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// An analyzer that is invoked on each syntax node whose Kind is among the kinds return from SyntaxKindsOfInterest.
    /// </summary>
    /// <typeparam name="TSyntaxKind">either Microsoft.CodeAnalysis.CSharp.SyntaxKind (for C# syntax nodes)
    /// or Microsoft.CodeAnalysis.VisualBasic.SyntaxKind (for VB syntax nodes)</typeparam>
    public interface ISyntaxNodeAnalyzer<TSyntaxKind> : IDiagnosticAnalyzer
    {
        /// <summary>
        /// Returns the syntax kinds of syntax nodes for which AnalyzeNode should be called.
        /// </summary>
        ImmutableArray<TSyntaxKind> SyntaxKindsOfInterest { get; }
        /// <summary>
        /// Called for each whose language-specific kind is an element of SyntaxKindsOfInterest.
        /// </summary>
        /// <param name="node">A node of a kind of interest</param>
        /// <param name="semanticModel">A SemanticModel for the compilation unit</param>
        /// <param name="addDiagnostic">A delegate to be used to emit diagnostics</param>
        /// <param name="options">A set of options passed in from the host.</param>
        /// <param name="cancellationToken">A token for cancelling the computation</param>
        void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken);
    }
}
