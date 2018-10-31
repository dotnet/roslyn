' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.CodingConventions

Namespace Microsoft.CodeAnalysis.CodeStyle
    Friend Class VisualBasicFormattingAnalyzerImpl
        Inherits AbstractFormattingAnalyzerImpl

        Public Sub New(descriptor As DiagnosticDescriptor)
            MyBase.New(descriptor)
        End Sub

        Protected Overrides Function ApplyFormattingOptions(optionSet As OptionSet, codingConventionContext As ICodingConventionContext) As OptionSet
            Return optionSet
        End Function
    End Class
End Namespace
