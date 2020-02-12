﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    <Export(GetType(IExtractMethodService)), ExportLanguageService(GetType(IExtractMethodService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicExtractMethodService
        Inherits AbstractExtractMethodService(Of VisualBasicSelectionValidator, VisualBasicMethodExtractor, VisualBasicSelectionResult)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateSelectionValidator(document As SemanticDocument,
                                                              textSpan As TextSpan,
                                                              options As OptionSet) As VisualBasicSelectionValidator
            Return New VisualBasicSelectionValidator(document, textSpan, options)
        End Function

        Protected Overrides Function CreateMethodExtractor(selectionResult As VisualBasicSelectionResult, localFunction As Boolean) As VisualBasicMethodExtractor
            Return New VisualBasicMethodExtractor(selectionResult)
        End Function
    End Class
End Namespace
