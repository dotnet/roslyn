// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.SignatureHelp;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal readonly struct PythiaSignatureHelpItemWrapper
    {
        internal readonly SignatureHelpItem UnderlyingObject;

        public PythiaSignatureHelpItemWrapper(SignatureHelpItem underlyingObject)
        {
            UnderlyingObject = underlyingObject;
        }

        public static SymbolDisplayPart CreateTextDisplayPart(string text)
            => new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text);

        public static PythiaSignatureHelpItemWrapper CreateFromMethodGroupMethod(
            Document document,
            IMethodSymbol method,
            int position,
            SemanticModel semanticModel,
            IList<SymbolDisplayPart> descriptionParts)
        => new PythiaSignatureHelpItemWrapper(AbstractOrdinaryMethodSignatureHelpProvider.ConvertMethodGroupMethod(document, method, position, semanticModel, descriptionParts));
    }
}
