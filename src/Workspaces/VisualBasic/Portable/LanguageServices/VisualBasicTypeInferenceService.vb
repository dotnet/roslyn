' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
