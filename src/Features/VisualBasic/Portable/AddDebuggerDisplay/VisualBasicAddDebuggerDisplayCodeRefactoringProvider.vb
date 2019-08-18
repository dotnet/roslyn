' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddDebuggerDisplay
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay

    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddDebuggerDisplayCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicAddDebuggerDisplayCodeRefactoringProvider
        Inherits AbstractAddDebuggerDisplayCodeRefactoringProvider(Of TypeBlockSyntax)

        Protected Overrides Function IsDebuggerDisplayAttribute(attribute As SyntaxNode) As Boolean
            ' Purposely bails for efficiency if anything called "DebuggerDisplay" is already applied, regardless of
            ' whether it's the "real" one.

            Dim name = DirectCast(attribute, AttributeSyntax).Name

            While True
                Dim qualified = TryCast(name, QualifiedNameSyntax)
                If qualified Is Nothing Then Exit While
                name = qualified.Right
            End While

            Dim identifier = TryCast(name, IdentifierNameSyntax)

            Return identifier IsNot Nothing AndAlso IsDebuggerDisplayAttributeIdentifier(identifier.Identifier)
        End Function

        Protected Overrides Function DeclaresToStringOverride(typeDeclaration As TypeBlockSyntax) As Boolean
            Return typeDeclaration.Members.Any(AddressOf IsToStringOverride)
        End Function

        Private Shared Function IsToStringOverride(memberDeclaration As StatementSyntax) As Boolean
            ' Purposely bails for efficiency if no "ToString" override is in the same syntax tree, regardless of whether
            ' it's declared in another partial class file. Since the DebuggerDisplay attribute will refer to it, it's
            ' nicer to have them both in the same file anyway.

            Dim method = TryCast(memberDeclaration, MethodBlockSyntax)

            If method Is Nothing Then Return False
            If method.SubOrFunctionStatement.GetArity <> 0 Then Return False
            If method.SubOrFunctionStatement.ParameterList?.Parameters.Any Then Return False
            If Not method.SubOrFunctionStatement.Modifiers.Any(SyntaxKind.OverridesKeyword) Then Return False

            Return True
        End Function
    End Class
End Namespace
