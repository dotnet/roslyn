// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal interface ISpeculationAnalyzer
{
    /// <summary>
    /// The original expression that is being replaced by <see cref="ReplacedExpression"/>.  This will be in the
    /// <see cref="SemanticModel.SyntaxTree"/> that <see cref="OriginalSemanticModel"/> points at.
    /// </summary>
    SyntaxNode OriginalExpression { get; }

    /// <summary>
    /// The new node that <see cref="OriginalExpression"/>n was replaced with.  This will be in the <see
    /// cref="SemanticModel.SyntaxTree"/> that <see cref="SpeculativeSemanticModel"/> points at.
    /// </summary>
    SyntaxNode ReplacedExpression { get; }

    /// <summary>
    /// The original semantic model that <see cref="OriginalExpression"/> was contained in.
    /// </summary>
    SemanticModel OriginalSemanticModel { get; }

    /// <summary>
    /// A forked semantic model off of <see cref="OriginalSemanticModel"/>.  In that model <see
    /// cref="OriginalExpression"/> will have been replaced with <see cref="ReplacedExpression"/>.
    /// </summary>
    SemanticModel SpeculativeSemanticModel { get; }
}
