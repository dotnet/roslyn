// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal interface ITokenDeferral
    {
        uint GetFakeStringTokenForIL(string value);
        uint GetFakeSymbolTokenForIL(Cci.IReference value, SyntaxNode? syntaxNode, DiagnosticBag diagnostics);
        uint GetFakeSymbolTokenForIL(Cci.ISignature value, SyntaxNode? syntaxNode, DiagnosticBag diagnostics);
        uint GetSourceDocumentIndexForIL(Cci.DebugSourceDocument document);

        Cci.IFieldReference GetFieldForData(ImmutableArray<byte> data, ushort alignment, SyntaxNode syntaxNode, DiagnosticBag diagnostics);

        /// <summary>Gets a field that may be used to lazily cache an array created to store the specified data.</summary>
        /// <remarks>This is used to cache an array created with the data passed to <see cref="GetFieldForData"/>.</remarks>
        Cci.IFieldReference GetArrayCachingFieldForData(ImmutableArray<byte> data, Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics);

        Cci.IFieldReference GetArrayCachingFieldForConstants(ImmutableArray<ConstantValue> constants, Cci.IArrayTypeReference arrayType, SyntaxNode syntaxNode, DiagnosticBag diagnostics);

        Cci.IMethodReference GetInitArrayHelper();

        string GetStringFromToken(uint token);
        /// <summary>
        /// Gets the <see cref="Cci.IReference"/> or <see cref="Cci.ISignature"/> corresponding to this token.
        /// </summary>
        object GetReferenceFromToken(uint token);

        ArrayMethods ArrayMethods { get; }
    }
}
