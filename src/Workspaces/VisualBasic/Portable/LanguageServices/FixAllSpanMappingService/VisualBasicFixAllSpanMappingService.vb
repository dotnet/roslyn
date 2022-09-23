' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeFixesAndRefactorings
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixesAndRefactorings
    <ExportLanguageService(GetType(IFixAllSpanMappingService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicFixAllSpanMappingService
        Inherits AbstractFixAllSpanMappingService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetFixAllSpansIfWithinGlobalStatementAsync(document As Document, diagnosticSpan As TextSpan, cancellationToken As CancellationToken) As Task(Of ImmutableDictionary(Of Document, ImmutableArray(Of TextSpan)))
            ' VB does not support global statements
            Return Task.FromResult(ImmutableDictionary(Of Document, ImmutableArray(Of TextSpan)).Empty)
        End Function
    End Class
End Namespace
