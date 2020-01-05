' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.VisualStudio.CodingConventions

Namespace Microsoft.CodeAnalysis.CodeStyle
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicFormattingAnalyzer
        Inherits AbstractFormattingAnalyzer

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
