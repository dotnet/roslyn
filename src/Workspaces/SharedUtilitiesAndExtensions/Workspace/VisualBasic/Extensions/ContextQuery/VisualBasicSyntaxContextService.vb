' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
    <ExportLanguageService(GetType(ISyntaxContextService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxContextService
        Implements ISyntaxContextService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateContext(document As Document, semanticModel As SemanticModel, position As Integer, cancellationToken As CancellationToken) As SyntaxContext Implements ISyntaxContextService.CreateContext
            Return VisualBasicSyntaxContext.CreateContext(document, semanticModel, position, cancellationToken)
        End Function
    End Class
End Namespace
