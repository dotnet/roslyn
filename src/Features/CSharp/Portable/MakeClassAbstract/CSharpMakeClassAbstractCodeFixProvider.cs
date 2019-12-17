// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeClassAbstract;

namespace Microsoft.CodeAnalysis.CSharp.MakeClassAbstract
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpMakeClassAbstractCodeFixProvider)), Shared]
    internal sealed class CSharpMakeClassAbstractCodeFixProvider : AbstractMakeClassAbstractCodeFixProvider<ClassDeclarationSyntax>
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
               ImmutableArray.Create(
                   "CS0513" // 'C.M()' is abstract but it is contained in non-abstract class 'C'
               );

        protected override bool IsValidMemberNode(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    var method = (MethodDeclarationSyntax)node;
                    if (method.Body != null || method.ExpressionBody != null)
                    {
                        return false;
                    }
                    break;

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    var accessor = (AccessorDeclarationSyntax)node;
                    if (accessor.Body != null || accessor.ExpressionBody != null)
                    {
                        return false;
                    }
                    break;

                default:
                    return false;
            }

            return node.FirstAncestorOrSelf<ClassDeclarationSyntax>() != null;
        }
    }
}
