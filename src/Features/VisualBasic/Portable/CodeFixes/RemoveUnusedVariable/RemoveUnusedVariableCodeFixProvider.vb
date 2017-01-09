' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.RemoveUnusedVariable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.RemoveUnusedVariable

    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast), [Shared]>
    Friend Class RemoveUnusedVariableCodeFixProvider
        Inherits AbstractRemoveUnusedVariableCodeFixProvider(Of
            LocalDeclarationStatementSyntax, ModifiedIdentifierSyntax, VariableDeclaratorSyntax)

        Friend Const BC42024 As String = "BC42024"
        Friend Shared ReadOnly Ids As ImmutableArray(Of String) = ImmutableArray.Create(BC42024)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return Ids
            End Get
        End Property
    End Class

End Namespace
