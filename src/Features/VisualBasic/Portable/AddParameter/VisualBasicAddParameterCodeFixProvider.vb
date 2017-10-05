' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddParameter
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.GenerateConstructor
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.AddParameter
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddParameter), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeFixProviderNames.GenerateConstructor)>
    Friend Class VisualBasicAddParameterCodeFixProvider
        Inherits AbstractAddParameterCodeFixProvider(Of
        ArgumentSyntax,
        ArgumentSyntax,
        ArgumentListSyntax,
        ArgumentListSyntax,
        InvocationExpressionSyntax,
        ObjectCreationExpressionSyntax,
        TypeSyntax)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) =
            GenerateConstructorDiagnosticIds.AllDiagnosticIds

        Protected Overrides ReadOnly Property TooManyArgumentsDiagnosticIds As ImmutableArray(Of String) =
            GenerateConstructorDiagnosticIds.TooManyArgumentsDiagnosticIds

        Protected Overrides ReadOnly Property CannotConvertDiagnosticIds As ImmutableArray(Of String) =
            GenerateConstructorDiagnosticIds.CannotConvertDiagnosticIds

    End Class
End Namespace
