' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <ExportLanguageService(GetType(IAnalyzerDriverService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicAnalyzerDriverService
        Implements IAnalyzerDriverService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Sub ComputeDeclarationsInSpan(model As SemanticModel,
                                             span As TextSpan,
                                             getSymbol As Boolean,
                                             builder As ArrayBuilder(Of DeclarationInfo),
                                             cancellationToken As CancellationToken) Implements IAnalyzerDriverService.ComputeDeclarationsInSpan
            VisualBasicDeclarationComputer.ComputeDeclarationsInSpan(model, span, getSymbol, builder, cancellationToken)
        End Sub
    End Class
End Namespace
