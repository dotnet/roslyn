// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordConstructor : SourceConstructorSymbolBase
    {
        public SynthesizedRecordConstructor(
             SourceMemberContainerTypeSymbol containingType,
             RecordDeclarationSyntax syntax,
             DiagnosticBag diagnostics) :
             base(containingType, syntax.ParameterList!.GetLocation(), syntax)
        {
            this.MakeFlags(MethodKind.Constructor, containingType.IsAbstract ? DeclarationModifiers.Protected : DeclarationModifiers.Public, returnsVoid: true, isExtensionMethod: false);
        }

        internal RecordDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (RecordDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override ParameterListSyntax GetParameterList() => GetSyntax().ParameterList!;

        protected override CSharpSyntaxNode? GetInitializer()
        {
            return GetSyntax().PrimaryConstructorBaseType;
        }

        protected override bool AllowRefOrOut => false;

        internal override bool IsExpressionBodied => false;

        protected override bool IsWithinExpressionOrBlockBody(int position, out int offset)
        {
            offset = -1;
            return false;
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            TypeDeclarationSyntax typeDecl = GetSyntax();
            InMethodBinder result = (binderFactoryOpt ?? this.DeclaringCompilation.GetBinderFactory(typeDecl.SyntaxTree)).GetRecordConstructorInMethodBinder(this);
            return new ExecutableCodeBinder(SyntaxNode, this, result.WithAdditionalFlags(ignoreAccessibility ? BinderFlags.IgnoreAccessibility : BinderFlags.None));
        }
    }
}
