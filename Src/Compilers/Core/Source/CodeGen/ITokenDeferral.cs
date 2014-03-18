// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal interface ITokenDeferral
    {
        uint GetFakeStringTokenForIL(string value);
        uint GetFakeSymbolTokenForIL(Microsoft.Cci.IReference value, SyntaxNode syntaxNode, DiagnosticBag diagnostics);

        Microsoft.Cci.IFieldReference GetFieldForData(byte[] data, SyntaxNode syntaxNode, DiagnosticBag diagnostics);
        Microsoft.Cci.IMethodReference GetInitArrayHelper();

        string GetStringFromToken(uint token);
        Microsoft.Cci.IReference GetReferenceFromToken(uint token);

        ArrayMethods ArrayMethods { get; }
    }
}
