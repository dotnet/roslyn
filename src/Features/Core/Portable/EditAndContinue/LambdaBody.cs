// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal abstract class LambdaBody : DeclarationBody
{
    public abstract SyntaxNode GetLambda();

    /// <summary>
    /// Determines if the bodies are syntactically same disregarding trivia differences.
    /// </summary>
    public abstract bool IsSyntaxEquivalentTo(LambdaBody other);

    public abstract LambdaBody? TryGetPartnerLambdaBody(SyntaxNode newLambda);

    /// <summary>
    /// Returns all nodes of the body.
    /// </summary>
    /// <remarks>
    /// Note that VB lambda bodies are represented by a lambda header and that some lambda bodies share 
    /// their parent nodes with other bodies (e.g. join clause expressions).
    /// </remarks>
    public abstract IEnumerable<SyntaxNode> GetExpressionsAndStatements();
}
