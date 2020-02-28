' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.PopulateSwitch

Namespace Microsoft.CodeAnalysis.VisualBasic.PopulateSwitch
    <ExportCodeFixProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeFixProviderNames.PopulateSwitch), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementInterface)>
    Friend Class VisualBasicPopulateSwitchStatementCodeFixProvider
        Inherits AbstractPopulateSwitchStatementCodeFixProvider(Of
            SelectBlockSyntax, CaseBlockSyntax, MemberAccessExpressionSyntax)
    End Class
End Namespace
