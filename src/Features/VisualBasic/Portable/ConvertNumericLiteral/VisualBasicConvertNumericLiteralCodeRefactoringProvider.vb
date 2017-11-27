' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertNumericLiteral

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertNumericLiteral
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertNumericLiteralCodeRefactoringProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicConvertNumericLiteralCodeRefactoringProvider
        Inherits AbstractConvertNumericLiteralCodeRefactoringProvider

        Public Sub New()
            MyBase.New(hexPrefix:="&H", binaryPrefix:="&B")
        End Sub

        Protected Overrides Function SupportLeadingUnderscore(options As ParseOptions) As Boolean
            Return DirectCast(options, VisualBasicParseOptions).LanguageVersion >= LanguageVersion.VisualBasic15_5
        End Function
    End Class
End Namespace
