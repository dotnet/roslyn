' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Copilot
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Copilot
    <ExportLanguageService(GetType(ICopilotProposalAdjusterService), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicCopilotProposalAdjusterService
        Inherits AbstractCopilotProposalAdjusterService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(globalOptions As IGlobalOptionService)
            MyBase.New(globalOptions)
        End Sub

        Protected Overrides Function AddMissingTokensIfAppropriateAsync(originalDocument As Document, forkedDocument As Document, cancellationToken As CancellationToken) As Task(Of (changed As Boolean, document As Document))
            Return Task.FromResult((False, forkedDocument))
        End Function
    End Class
End Namespace
