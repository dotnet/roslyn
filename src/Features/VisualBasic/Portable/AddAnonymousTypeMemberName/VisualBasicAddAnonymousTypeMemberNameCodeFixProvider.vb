' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddAnonymousTypeMemberName
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAnonymousTypeMemberName
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicAddAnonymousTypeMemberNameCodeFixProvider
        Inherits AbstractAddAnonymousTypeMemberNameCodeFixProvider(Of
            ExpressionSyntax,
            ObjectMemberInitializerSyntax,
            FieldInitializerSyntax)

        Private Const BC36556 As String = NameOf(BC36556) ' Anonymous type member name can be inferred only from a simple or qualified name with no arguments.

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC36556)

        Protected Overrides Function HasName(declarator As FieldInitializerSyntax) As Boolean
            Return Not TypeOf declarator Is InferredFieldInitializerSyntax
        End Function

        Protected Overrides Function GetExpression(declarator As FieldInitializerSyntax) As ExpressionSyntax
            Return DirectCast(declarator, InferredFieldInitializerSyntax).Expression
        End Function

        Protected Overrides Function WithName(declarator As FieldInitializerSyntax, nameToken As SyntaxToken) As FieldInitializerSyntax
            Dim inferredField = DirectCast(declarator, InferredFieldInitializerSyntax)
            Return SyntaxFactory.NamedFieldInitializer(
                inferredField.KeyKeyword,
                SyntaxFactory.Token(SyntaxKind.DotToken),
                SyntaxFactory.IdentifierName(nameToken),
                SyntaxFactory.Token(SyntaxKind.EqualsToken),
                inferredField.Expression).WithTriviaFrom(declarator)
        End Function

        Protected Overrides Function GetAnonymousObjectMemberNames(initializer As ObjectMemberInitializerSyntax) As IEnumerable(Of String)
            Return initializer.Initializers.OfType(Of NamedFieldInitializerSyntax).
                                            Select(Function(i) i.Name.Identifier.ValueText)
        End Function
    End Class
End Namespace
