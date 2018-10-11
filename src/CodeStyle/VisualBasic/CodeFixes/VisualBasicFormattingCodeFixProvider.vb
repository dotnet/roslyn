' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.CodingConventions

Namespace Microsoft.CodeAnalysis.CodeStyle
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FixFormatting)>
    <[Shared]>
    Friend Class VisualBasicFormattingCodeFixProvider
        Inherits AbstractFormattingCodeFixProvider

        Protected Overrides Function ApplyFormattingOptions(optionSet As OptionSet, codingConventionContext As ICodingConventionContext) As OptionSet
            Return optionSet
        End Function
    End Class
End Namespace

