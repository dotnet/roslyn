' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.LanguageServices
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

        Private Async Function VerifyGetMemberBodySpanForSpeculativeBindingAsync(workspaceDefinition As XElement) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(workspaceDefinition)
                Dim cursorDocument = workspace.DocumentWithCursor
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Dim root = Await document.GetSyntaxRootAsync()
                Dim node = root.FindNode(New TextSpan(cursorPosition, 0))
                Dim syntaxFactsService = document.GetLanguageService(Of ISyntaxFactsService)()

                Dim expected = If(cursorDocument.AnnotatedSpans.ContainsKey("MemberBodySpan"), cursorDocument.AnnotatedSpans!MemberBodySpan.Single(), Nothing)
                Dim actual = syntaxFactsService.GetMemberBodySpanForSpeculativeBinding(node)

                Assert.Equal(expected, actual)
            End Using
        End Function

    End Class
End Namespace