' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.RemoveUnusedVariable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable

    <ExportCodeFixProviderAttribute(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.RemoveUnusedVariable), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddImport)>
    Friend Class VisualBasicRemoveUnusedVariableCodeFixProvider
        Inherits AbstractRemoveUnusedVariableCodeFixProvider(Of
            LocalDeclarationStatementSyntax, ModifiedIdentifierSyntax, VariableDeclaratorSyntax)

        Private Const BC42024 As String = "BC42024"

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            ImmutableArray.Create(BC42024)
    End Class
End Namespace