' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
