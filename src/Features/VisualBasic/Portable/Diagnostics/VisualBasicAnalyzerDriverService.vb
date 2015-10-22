' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Diagnostics
    <ExportLanguageService(GetType(IAnalyzerDriverService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicAnalyzerDriverService
        Implements IAnalyzerDriverService

        Public Sub ComputeDeclarationsInSpan(model As SemanticModel, span As TextSpan, getSymbol As Boolean, builder As List(Of DeclarationInfo), cancellationToken As CancellationToken) Implements IAnalyzerDriverService.ComputeDeclarationsInSpan
            VisualBasicDeclarationComputer.ComputeDeclarationsInSpan(model, span, getSymbol, builder, cancellationToken)
        End Sub
    End Class
End Namespace
