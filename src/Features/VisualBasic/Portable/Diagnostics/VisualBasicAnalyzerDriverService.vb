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

        Public Function GetDeclarationsInSpan(model As SemanticModel, span As TextSpan, getSymbol As Boolean, cancellationToken As CancellationToken) As ImmutableArray(Of DeclarationInfo) Implements IAnalyzerDriverService.GetDeclarationsInSpan
            Return VisualBasicDeclarationComputer.GetDeclarationsInSpan(model, span, getSymbol, cancellationToken)
        End Function
    End Class
End Namespace
