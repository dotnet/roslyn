// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ChangeSignature;

internal interface IChangeSignatureOptionsService : IWorkspaceService
{
    /// <summary>
    /// Gets options and produces a <see cref="SignatureChange"/> if successful.
    /// </summary>
    /// <param name="document">the context document</param>
    /// <param name="positionForTypeBinding">the position in the document with 
    /// the signature of the method, used for binding types (e.g. for added
    /// parameters)</param>
    /// <param name="symbol">the symbol for changing the signature</param>
    /// <param name="parameters">existing parameters of the symbol</param>
    /// <returns></returns>
    ChangeSignatureOptionsResult? GetChangeSignatureOptions(
        Document document,
        int positionForTypeBinding,
        ISymbol symbol,
        ParameterConfiguration parameters);
}
