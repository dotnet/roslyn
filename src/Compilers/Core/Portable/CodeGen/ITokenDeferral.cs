// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal interface ITokenDeferral
    {
        uint GetFakeStringTokenForIL(string value);
        uint GetFakeSymbolTokenForIL(Cci.IReference value, SyntaxNode syntaxNode, DiagnosticBag diagnostics);
        uint GetFakeSymbolTokenForIL(Cci.ISignature value, SyntaxNode syntaxNode, DiagnosticBag diagnostics);
        uint GetSourceDocumentIndexForIL(Cci.DebugSourceDocument document);

        Cci.IFieldReference GetFieldForData(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics);
        Cci.IMethodReference GetInitArrayHelper();

        string GetStringFromToken(uint token);
        /// <summary>
        /// Gets the <see cref="Cci.IReference"/> or <see cref="Cci.ISignature"/> corresponding to this token.
        /// </summary>
        object GetReferenceFromToken(uint token);

        ArrayMethods ArrayMethods { get; }
    }
}
