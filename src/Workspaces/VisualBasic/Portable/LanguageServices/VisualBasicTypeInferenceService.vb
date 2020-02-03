' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.LanguageServices.TypeInferenceService

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ITypeInferenceService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicTypeInferenceService
        Inherits AbstractTypeInferenceService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateTypeInferrer(semanticModel As SemanticModel, cancellationToken As CancellationToken) As AbstractTypeInferrer
            Return New TypeInferrer(semanticModel, cancellationToken)
        End Function
    End Class
End Namespace
