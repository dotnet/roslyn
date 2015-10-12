' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.LanguageServices
    Public Class SyntaxFactsServiceTests

        <WpfFact>
        Public Sub CSharp_TestGetMemberBodySpanForSpeculativeBinding1()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub CSharp_TestGetMemberBodySpanForSpeculativeBinding2()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub CSharp_TestGetMemberBodySpanForSpeculativeBinding3()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub CSharp_TestGetMemberBodySpanForSpeculativeBinding4()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub CSharp_TestGetMemberBodySpanForSpeculativeBinding5()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub VB_TestGetMemberBodySpanForSpeculativeBinding1()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub VB_TestGetMemberBodySpanForSpeculativeBinding2()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub VB_TestGetMemberBodySpanForSpeculativeBinding3()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub VB_TestGetMemberBodySpanForSpeculativeBinding4()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        <WpfFact>
        Public Sub VB_TestGetMemberBodySpanForSpeculativeBinding5()
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

            VerifyGetMemberBodySpanForSpeculativeBinding(definition)
        End Sub

        Private Sub VerifyGetMemberBodySpanForSpeculativeBinding(workspaceDefinition As XElement)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceDefinition)
                Dim cursorDocument = workspace.DocumentWithCursor
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Dim root = document.GetSyntaxRootAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)
                Dim node = root.FindNode(New TextSpan(cursorPosition, 0))
                Dim syntaxFactsService = document.GetLanguageService(Of ISyntaxFactsService)()

                Dim expected = If(cursorDocument.AnnotatedSpans.ContainsKey("MemberBodySpan"), cursorDocument.AnnotatedSpans!MemberBodySpan.Single(), Nothing)
                Dim actual = syntaxFactsService.GetMemberBodySpanForSpeculativeBinding(node)

                Assert.Equal(expected, actual)
            End Using
        End Sub

    End Class
End Namespace