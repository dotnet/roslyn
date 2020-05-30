﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateContextAsync(
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
