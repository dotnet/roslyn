' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Structure
Imports Microsoft.CodeAnalysis.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class OverallStructureTests
        Inherits AbstractSyntaxStructureProviderTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Async Function GetBlockSpansWorkerAsync(document As Document, options As BlockStructureOptions, position As Integer) As Task(Of ImmutableArray(Of BlockSpan))
            Dim outliningService = document.GetLanguageService(Of BlockStructureService)()

            Return (Await outliningService.GetBlockStructureAsync(document, options, CancellationToken.None)).Spans
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function DirectivesAtEndOfFile() As Task
            Const code = "
$${|span1:Class C
End Class|}

{|span2:#Region ""Something""
#End Region|}
"

            Await VerifyBlockSpansAsync(code,
                Region("span2", "Something", autoCollapse:=False, isDefaultCollapsed:=True),
                Region("span1", "Class C ...", autoCollapse:=False))
        End Function
    End Class
End Namespace
