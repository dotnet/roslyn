// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MakeClassAbstract;

namespace Microsoft.CodeAnalysis.CSharp.MakeClassAbstract
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpMakeClassAbstractCodeFixProvider)), Shared]
    internal sealed class CSharpMakeClassAbstractCodeFixProvider : AbstractMakeClassAbstractCodeFixProvider<ClassDeclarationSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpMakeClassAbstractCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
               ImmutableArray.Create(
                   "CS0513" // 'C.M()' is abstract but it is contained in non-abstract class 'C'
               );

        protected override bool IsValidRefactoringContext(SyntaxNode? node, [NotNullWhen(true)] out ClassDeclarationSyntax? classDeclaration)
        {
            classDeclaration = null;

            switch (node?.Kind())
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

            var enclosingType = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (!enclosingType.IsKind(SyntaxKind.ClassDeclaration))
            {
                return false;
            }

            classDeclaration = (ClassDeclarationSyntax)enclosingType;

            return !classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) && !classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
        }
    }
}
