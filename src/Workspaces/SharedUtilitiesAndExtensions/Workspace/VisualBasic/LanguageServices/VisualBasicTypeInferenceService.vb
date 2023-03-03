' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.LanguageService.TypeInferenceService

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ITypeInferenceService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicTypeInferenceService
        Inherits AbstractTypeInferenceService

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateTypeInferrer(semanticModel As SemanticModel, cancellationToken As CancellationToken) As AbstractTypeInferrer
            Return New TypeInferrer(semanticModel, cancellationToken)
        End Function
    End Class
End Namespace
