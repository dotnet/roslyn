' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class OverallOutliningTests
        Inherits AbstractSyntaxOutlinerTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Async Function GetRegionsAsync(document As Document, position As Integer) As Task(Of OutliningSpan())
            Dim outliningService = document.Project.LanguageServices.GetService(Of IOutliningService)()

            Return (Await outliningService.GetOutliningSpansAsync(document, CancellationToken.None)) _
                .WhereNotNull() _
                .ToArray()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function DirectivesAtEndOfFile() As Task
            Const code = "
$${|span1:Class C
End Class|}

{|span2:#Region ""Something""
#End Region|}
"

            Await VerifyRegionsAsync(code,
                Region("span1", "Class C ...", autoCollapse:=False),
                Region("span2", "Something", autoCollapse:=False, isDefaultCollapsed:=True))
        End Function

    End Class
End Namespace
