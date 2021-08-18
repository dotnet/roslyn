' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.AddAnonymousTypeMemberName
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddAnonymousTypeMemberName
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddAnonymousTypeMemberName), [Shared]>
    Friend Class VisualBasicAddAnonymousTypeMemberNameCodeFixProvider
        Inherits AbstractAddAnonymousTypeMemberNameCodeFixProvider(Of
            ExpressionSyntax,
            ObjectMemberInitializerSyntax,
            FieldInitializerSyntax)

        Private Const BC36556 As String = NameOf(BC36556) ' Anonymous type member name can be inferred only from a simple or qualified name with no arguments.

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
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
