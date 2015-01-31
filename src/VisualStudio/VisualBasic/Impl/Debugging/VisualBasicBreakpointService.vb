' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging

    <ExportLanguageService(GetType(IBreakpointResolutionService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicBreakpointResolutionService
        Implements IBreakpointResolutionService

        Public Function ResolveBreakpointAsync(document As Document, textSpan As TextSpan, Optional cancellationToken As CancellationToken = Nothing) As Task(Of BreakpointResolutionResult) Implements IBreakpointResolutionService.ResolveBreakpointAsync
            Return BreakpointGetter.GetBreakpointAsync(document, textSpan.Start, textSpan.Length, cancellationToken)
        End Function

        Public Function ResolveBreakpointsAsync(solution As Solution,
                                           name As String,
                                           Optional cancellationToken As CancellationToken = Nothing) As Task(Of IEnumerable(Of BreakpointResolutionResult)) Implements IBreakpointResolutionService.ResolveBreakpointsAsync
            Return New BreakpointResolver(solution, name).DoAsync(cancellationToken)
        End Function
    End Class
End Namespace
