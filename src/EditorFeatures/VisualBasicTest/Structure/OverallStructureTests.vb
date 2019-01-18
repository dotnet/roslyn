' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Friend Overrides Async Function GetBlockSpansWorkerAsync(document As Document, position As Integer) As Task(Of ImmutableArray(Of BlockSpan))
            Dim outliningService = document.GetLanguageService(Of BlockStructureService)()

            Return (Await outliningService.GetBlockStructureAsync(document, CancellationToken.None)).Spans
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
                Region("span1", "Class C ...", autoCollapse:=False),
                Region("span2", "Something", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function
    End Class
End Namespace
