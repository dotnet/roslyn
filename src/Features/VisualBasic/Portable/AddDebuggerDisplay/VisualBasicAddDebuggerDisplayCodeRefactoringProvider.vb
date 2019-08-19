' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.AddDebuggerDisplay
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddDebuggerDisplay

    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicAddDebuggerDisplayCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicAddDebuggerDisplayCodeRefactoringProvider
        Inherits AbstractAddDebuggerDisplayCodeRefactoringProvider(Of TypeBlockSyntax, MethodBlockSyntax)

        Protected Overrides Function HasDebuggerDisplayAttribute(typeDeclaration As TypeBlockSyntax) As Boolean
            Return (
                From list In typeDeclaration.BlockStatement.AttributeLists
                From attribute In list.Attributes
                Where IsDebuggerDisplayAttribute(attribute)).Any()
        End Function

        Private Function IsDebuggerDisplayAttribute(attribute As AttributeSyntax) As Boolean
            ' Purposely bails for efficiency if anything called "DebuggerDisplay" is already applied, regardless of
            ' whether it's the "real" one.

            Dim name = attribute.Name

            While True
                Dim qualified = TryCast(name, QualifiedNameSyntax)
                If qualified Is Nothing Then Exit While
                name = qualified.Right
            End While

            Dim identifier = TryCast(name, IdentifierNameSyntax)

            Return identifier IsNot Nothing AndAlso IsDebuggerDisplayAttributeIdentifier(identifier.Identifier)
        End Function

        Protected Overrides Function IsToStringOverride(methodDeclaration As MethodBlockSyntax) As Boolean
            ' Purposely bails for efficiency if no "ToString" override is in the same syntax tree, regardless of whether
            ' it's declared in another partial class file. Since the DebuggerDisplay attribute will refer to it, it's
            ' nicer to have them both in the same file anyway.

            If methodDeclaration Is Nothing Then Return False
            If methodDeclaration.SubOrFunctionStatement.GetArity <> 0 Then Return False
            If methodDeclaration.SubOrFunctionStatement.ParameterList?.Parameters.Any Then Return False
            If Not methodDeclaration.SubOrFunctionStatement.Modifiers.Any(SyntaxKind.OverridesKeyword) Then Return False

            Return True
        End Function
    End Class
End Namespace
