// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Common base for ordinary methods synthesized by compiler for records.
    /// </summary>
    internal abstract class SynthesizedRecordOrdinaryMethod : SynthesizedSourceOrdinaryMethodSymbol
    {
        private readonly int _memberOffset;

        protected SynthesizedRecordOrdinaryMethod(SourceMemberContainerTypeSymbol containingType, string name, int memberOffset, DeclarationModifiers declarationModifiers)
            : base(containingType, name, containingType.GetFirstLocation(), (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(),
                   (declarationModifiers, MakeFlags(
                                                    MethodKind.Ordinary, RefKind.None, declarationModifiers, returnsVoid: false, returnsVoidIsSet: false,
                                                    isExpressionBodied: false, isExtensionMethod: false, isNullableAnalysisEnabled: false, isVarArg: false,
                                                    isExplicitInterfaceImplementation: false, hasThisInitializer: false)))
        {
            _memberOffset = memberOffset;
        }

        internal sealed override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);
    }
}
