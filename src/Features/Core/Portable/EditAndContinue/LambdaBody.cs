// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal abstract class LambdaBody : DeclarationBody
{
    public abstract SyntaxNode GetLambda();

    /// <summary>
    /// Determines if the bodies are syntactically same disregarding trivia differences.
    /// </summary>
    public abstract bool IsSyntaxEquivalentTo(LambdaBody other);

    public abstract LambdaBody? TryGetPartnerLambdaBody(SyntaxNode newLambda);
}
