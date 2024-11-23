// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.ChangeSignature;

internal abstract class ChangeSignatureAnalyzedContext
{
}

internal sealed class ChangeSignatureAnalysisSucceededContext(
    Document document, int positionForTypeBinding, ISymbol symbol, ParameterConfiguration parameterConfiguration) : ChangeSignatureAnalyzedContext
{
    public readonly Document Document = document;
    public readonly ISymbol Symbol = symbol;
    public readonly ParameterConfiguration ParameterConfiguration = parameterConfiguration;
    public readonly int PositionForTypeBinding = positionForTypeBinding;

    public Solution Solution => Document.Project.Solution;
}

internal sealed class CannotChangeSignatureAnalyzedContext(ChangeSignatureFailureKind reason) : ChangeSignatureAnalyzedContext
{
    public readonly ChangeSignatureFailureKind CannotChangeSignatureReason = reason;
}
