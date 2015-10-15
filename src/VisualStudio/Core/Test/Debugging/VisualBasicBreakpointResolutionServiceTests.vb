' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.Debugging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging
    Public Class VisualBasicBreakpointResolutionServiceTests

        Public Sub TestSpanWithLength(markup As XElement, length As Integer)
            Dim position As Integer? = Nothing
            Dim expectedSpan As TextSpan? = Nothing
            Dim source As String = Nothing
            MarkupTestFile.GetPositionAndSpan(markup.NormalizedValue, source, position, expectedSpan)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(source)
                Dim document = workspace.CurrentSolution.Projects.First.Documents.First
                Dim result As BreakpointResolutionResult = VisualBasicBreakpointResolutionService.GetBreakpointAsync(document, position.Value, length, CancellationToken.None).WaitAndGetResult(CancellationToken.None)
                Assert.True(expectedSpan.Value = result.TextSpan,
                                String.Format(vbCrLf & "Expected: {0} ""{1}""" & vbCrLf & "Actual: {2} ""{3}""",
                                                expectedSpan.Value,
                                                source.Substring(expectedSpan.Value.Start, expectedSpan.Value.Length),
                                                result.TextSpan,
                                                source.Substring(result.TextSpan.Start, result.TextSpan.Length)))
            End Using
        End Sub

        <WorkItem(876520)>
        <WpfFact>
        Public Sub TestBreakpointSpansMultipleMethods()
            ' Normal case: debugger passing BP spans "sub Foo() end sub"
            TestSpanWithLength(<text>
class C
  [|$$sub Foo()|]
  end sub

  sub Bar()
  end sub
end class</text>, 20)

            ' Rare case: debugger passing BP spans "sub Foo() end sub sub Bar() end sub"
            TestSpanWithLength(<text>
class C
  $$sub Foo()
  end sub

  [|sub Bar()|]
  end sub
end class</text>, 35)
        End Sub
    End Class
End Namespace
