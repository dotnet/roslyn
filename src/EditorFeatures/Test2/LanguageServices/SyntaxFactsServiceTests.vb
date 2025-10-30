' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.LanguageServices
    <[UseExportProvider]>
    Public Class SyntaxFactsServiceTests

        <Fact>
        Public Async Function TestCSharp_TestGetMemberBodySpanForSpeculativeBinding1() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    $$void M()
    {{|MemberBodySpan:
        var x = 42;
    |}}
}
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestCSharp_TestGetMemberBodySpanForSpeculativeBinding2() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void $$M()
    {{|MemberBodySpan:
        var x = 42;
    |}}
}
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestCSharp_TestGetMemberBodySpanForSpeculativeBinding3() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()$$
    {{|MemberBodySpan:
        var x = 42;
    |}}
}
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestCSharp_TestGetMemberBodySpanForSpeculativeBinding4() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {{|MemberBodySpan:
        var $$x = 42;
    |}}
}
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestCSharp_TestGetMemberBodySpanForSpeculativeBinding5() As Task
            Dim definition =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class C
{
    void M()
    {
        var x = 42;
    }
$$}
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestVB_TestGetMemberBodySpanForSpeculativeBinding1() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    $$Sub M()
{|MemberBodySpan:        Dim x = 42
    |}End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestVB_TestGetMemberBodySpanForSpeculativeBinding2() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub $$M()
{|MemberBodySpan:        Dim x = 42
    |}End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestVB_TestGetMemberBodySpanForSpeculativeBinding3() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()$$
{|MemberBodySpan:        Dim x = 42
    |}End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestVB_TestGetMemberBodySpanForSpeculativeBinding4() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
{|MemberBodySpan:        Dim $$x = 42
    |}End Sub
End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        <Fact>
        Public Async Function TestVB_TestGetMemberBodySpanForSpeculativeBinding5() As Task
            Dim definition =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
Class C
    Sub M()
        Dim x = 42
    End Sub
$$End Class
        </Document>
    </Project>
</Workspace>

            Await VerifyGetMemberBodySpanForSpeculativeBindingAsync(definition)
        End Function

        Private Shared Async Function VerifyGetMemberBodySpanForSpeculativeBindingAsync(workspaceDefinition As XElement) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(workspaceDefinition)
                Dim cursorDocument = workspace.DocumentWithCursor
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Dim root = Await document.GetSyntaxRootAsync()
                Dim node = root.FindNode(New TextSpan(cursorPosition, 0))
                Dim syntaxFactsService = document.GetLanguageService(Of ISyntaxFactsService)()

                Dim spans As ImmutableArray(Of TextSpan) = Nothing
                Dim expected = If(cursorDocument.AnnotatedSpans.TryGetValue("MemberBodySpan", spans), spans.Single(), Nothing)
                Dim actual = syntaxFactsService.GetMemberBodySpanForSpeculativeBinding(node)

                Assert.Equal(expected, actual)
            End Using
        End Function

    End Class
End Namespace
