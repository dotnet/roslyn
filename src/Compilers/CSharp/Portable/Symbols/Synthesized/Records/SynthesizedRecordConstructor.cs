// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordConstructor : SourceConstructorSymbolBase
    {
        public SynthesizedRecordConstructor(
             SourceMemberContainerTypeSymbol containingType,
             TypeDeclarationSyntax syntax,
             DiagnosticBag diagnostics) :
             base(containingType, GetParameterList(syntax).GetLocation(), syntax, MethodKind.Constructor, diagnostics)
        {
            Debug.Assert(syntax.IsKind(SyntaxKind.ClassDeclaration) || syntax.IsKind(SyntaxKind.StructDeclaration));
            this.MakeFlags(MethodKind.Constructor, DeclarationModifiers.Public, returnsVoid: true, isExtensionMethod: false);
        }

        internal TypeDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (TypeDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override ParameterListSyntax GetParameterList()
        {
            return GetParameterList(GetSyntax());
        }

        private static ParameterListSyntax GetParameterList(TypeDeclarationSyntax syntax)
        {
            switch (syntax)
            {
                case ClassDeclarationSyntax classDecl:
                    return classDecl.ParameterList!;
                case StructDeclarationSyntax structDecl:
                    return structDecl.ParameterList!;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        protected override CSharpSyntaxNode? GetInitializer()
        {
            var baseTypeSyntax = GetSyntax().BaseList?.Types.FirstOrDefault() as SimpleBaseTypeSyntax;

            if (baseTypeSyntax?.ArgumentList is object)
            {
                return baseTypeSyntax;
            }

            return null;
        }

        internal override bool IsExpressionBodied
        {
            get
            {
                return false;
            }
        }

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
