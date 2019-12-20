' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.MakeClassAbstract
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeClassAbstract
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicMakeClassAbstractCodeFixProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicMakeClassAbstractCodeFixProvider
        Inherits AbstractMakeClassAbstractCodeFixProvider(Of ClassStatementSyntax)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(
                "BC31411"
            )

        Protected Overrides Function IsValidRefactoringContext(node As SyntaxNode, ByRef classDeclaration As ClassStatementSyntax) As Boolean
            If node Is Nothing OrElse Not (node.IsKind(SyntaxKind.ClassStatement)) Then
                Return False
            End If

            classDeclaration = CType(node, ClassStatementSyntax)

            Return Not (classDeclaration.Modifiers.Any(SyntaxKind.MustInheritKeyword) OrElse classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
        End Function
    End Class
End Namespace
