' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.VisualStudio.CodingConventions
Imports optionSet = Microsoft.CodeAnalysis.Options.OptionSet

Namespace Microsoft.CodeAnalysis.CodeStyle
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FixFormatting)>
    <[Shared]>
    Friend Class VisualBasicFormattingCodeFixProvider
        Inherits AbstractFormattingCodeFixProvider

        Protected Overrides ReadOnly Property SyntaxFormattingService As ISyntaxFormattingService
            Get
                Return New VisualBasicSyntaxFormattingService()
            End Get
        End Property

        Protected Overrides Function ApplyFormattingOptions(optionSet As OptionSet, codingConventionContext As ICodingConventionContext) As OptionSet
            Return optionSet
        End Function
    End Class
End Namespace

