' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
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

        Friend Overrides Function GetRegions(document As Document, position As Integer) As OutliningSpan()
            Dim outliningService = document.Project.LanguageServices.GetService(Of IOutliningService)()

            Return outliningService _
                .GetOutliningSpansAsync(document, CancellationToken.None) _
                .WaitAndGetResult(CancellationToken.None) _
                .WhereNotNull() _
                .ToArray()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Sub DirectivesAtEndOfFile()
            Const code = "
$${|span1:Class C
End Class|}

{|span2:#Region ""Something""
#End Region|}
"

            Regions(code,
                Region("span1", "Class C ...", autoCollapse:=False),
                Region("span2", "Something", autoCollapse:=False, isDefaultCollapsed:=True))
        End Sub

    End Class
End Namespace
