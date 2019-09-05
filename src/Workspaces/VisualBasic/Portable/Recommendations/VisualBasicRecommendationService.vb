' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Recommendations
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Recommendations
    <ExportLanguageService(GetType(IRecommendationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicRecommendationService
        Inherits AbstractRecommendationService(Of VisualBasicSyntaxContext)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateContext(
                                                  workspace As Workspace,
                                                  semanticModel As SemanticModel,
                                                  position As Integer,
                                                  cancellationToken As CancellationToken) As Task(Of VisualBasicSyntaxContext)
            Return VisualBasicSyntaxContext.CreateContextAsync(workspace, semanticModel, position, cancellationToken)
        End Function

        Protected Overrides Function CreateRunner(
                                                 context As VisualBasicSyntaxContext,
                                                 filterOutOfScopeLocals As Boolean,
                                                 cancellationToken As CancellationToken) As AbstractRecommendationServiceRunner(Of VisualBasicSyntaxContext)
            Return New VisualBasicRecommendationServiceRunner(context, filterOutOfScopeLocals, cancellationToken)
        End Function
    End Class
End Namespace
