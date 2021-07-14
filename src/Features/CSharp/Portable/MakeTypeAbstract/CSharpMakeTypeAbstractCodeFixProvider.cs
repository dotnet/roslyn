// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeTypeAbstract;

namespace Microsoft.CodeAnalysis.CSharp.MakeTypeAbstract
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeTypeAbstract), Shared]
    internal sealed class CSharpMakeTypeAbstractCodeFixProvider : AbstractMakeTypeAbstractCodeFixProvider<TypeDeclarationSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpMakeTypeAbstractCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
               ImmutableArray.Create(
                   "CS0513" // 'C.M()' is abstract but it is contained in non-abstract type 'C'
               );

        protected override bool IsValidRefactoringContext(SyntaxNode? node, [NotNullWhen(true)] out TypeDeclarationSyntax? typeDeclaration)
        {
            switch (node)
            {
                case MethodDeclarationSyntax method:
                    if (method.Body != null || method.ExpressionBody != null)
                    {
                        typeDeclaration = null;
                        return false;
                    }

                    break;

                case AccessorDeclarationSyntax accessor:
                    if (accessor.Body != null || accessor.ExpressionBody != null)
                    {
                        typeDeclaration = null;
                        return false;
                    }

                    break;

                default:
                    typeDeclaration = null;
                    return false;
            }

            var enclosingType = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if ((enclosingType.IsKind(SyntaxKind.ClassDeclaration) || enclosingType.IsKind(SyntaxKind.RecordDeclaration)) &&
                !enclosingType.Modifiers.Any(SyntaxKind.AbstractKeyword) && !enclosingType.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                typeDeclaration = enclosingType;
                return true;
            }

            typeDeclaration = null;
            return false;
        }
    }
}
