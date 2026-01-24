// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

internal readonly struct PythiaSignatureHelpItemWrapper(SignatureHelpItem underlyingObject)
{
    internal readonly SignatureHelpItem UnderlyingObject = underlyingObject;

    public static SymbolDisplayPart CreateTextDisplayPart(string text)
        => new(SymbolDisplayPartKind.Text, null, text);

    public static PythiaSignatureHelpItemWrapper CreateFromMethodGroupMethod(
        Document document,
        IMethodSymbol method,
        int position,
        SemanticModel semanticModel,
        IList<SymbolDisplayPart> descriptionParts)
    => new(AbstractOrdinaryMethodSignatureHelpProvider.ConvertMethodGroupMethod(document, method, position, semanticModel, descriptionParts));
}
