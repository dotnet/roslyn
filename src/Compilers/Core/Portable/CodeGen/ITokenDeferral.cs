// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal interface ITokenDeferral
    {
        uint GetFakeStringTokenForIL(string value);
        uint GetFakeSymbolTokenForIL(Cci.IReference value, SyntaxNode syntaxNode, DiagnosticBag diagnostics);
        uint GetSourceDocumentIndexForIL(Cci.DebugSourceDocument document);

        Cci.IFieldReference GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics);
        Cci.IMethodReference GetInitArrayHelper();

        string GetStringFromToken(uint token);
        Cci.IReference GetReferenceFromToken(uint token);

        ArrayMethods ArrayMethods { get; }
    }
}
