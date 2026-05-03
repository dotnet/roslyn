' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <ExportLanguageService(GetType(IAnalyzerDriverService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicAnalyzerDriverService
        Inherits AbstractAnalyzerDriverService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Sub ComputeDeclarationsInSpan(
                model As SemanticModel,
                span As TextSpan,
                infos As ArrayBuilder(Of DeclarationInfo),
                cancellationToken As CancellationToken)
            VisualBasicDeclarationComputer.ComputeDeclarationsInSpan(model, span, getSymbol:=True, infos, cancellationToken)
        End Sub
    End Class
End Namespace
