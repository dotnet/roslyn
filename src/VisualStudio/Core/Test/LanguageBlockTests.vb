' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Roslyn.Test.Utilities

Namespace Tests
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.VsLanguageBlock)>
    Public Class LanguageBlockTests
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_NotInImports_VB()
            VerifyNoBlock("
I$$mports System

Module Program
    Sub M()

    End Sub
End Module
", LanguageNames.VisualBasic)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_NotLeadingTriviaOfRootClass_VB()
            VerifyNoBlock("
Imports System

$$

Module Program
    Sub M()

    End Sub
End Module
", LanguageNames.VisualBasic)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InNamespace_VB()
            VerifyBlock("
[|Namespace N
$$
    Module Program
        Sub M()

        End Sub
    End Module
End Namespace|]
", LanguageNames.VisualBasic, "N")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InModule_VB()
            VerifyBlock("
Namespace N
    [|Module Program
        $$
        Sub M()

        End Sub
    End Module|]
End Namespace
", LanguageNames.VisualBasic, "Program")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InSub()
            VerifyBlock("
Namespace N
    Module Program
        [|Sub M()
            $$
        End Sub|]
    End Module
End Namespace
", LanguageNames.VisualBasic, "Sub Program.M()")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InFunction()
            VerifyBlock("
Namespace N
    Module Program
        [|Function F() As Integer
            $$
        End Function|]
    End Module
End Namespace
", LanguageNames.VisualBasic, "Function Program.F() As Integer")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InProperty_VB()
            VerifyBlock("
Namespace N
    Module Program
        [|ReadOnly Property P() As Integer
            Get
                $$
            End Get
        End Property|]
    End Module
End Namespace
", LanguageNames.VisualBasic, "Property Program.P() As Integer")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_NotInUsings_CS()
            VerifyNoBlock("
u$$sing System;

class Program
{
    void M() { }
}
", LanguageNames.CSharp)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_NotLeadingTriviaOfRootClass_CS()
            VerifyNoBlock("
using System;

$$

class Program
{
    void M() { }
}
", LanguageNames.CSharp)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InNamespace_CS()
            VerifyBlock("
[|namespace N
{
$$
    class Program
    {
        void M() { }
    }
}|]
", LanguageNames.CSharp, "N")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InClass_CS()
            VerifyBlock("
namespace N
{
    [|class Program
    {
        $$
        void M() { }
    }|]
}
", LanguageNames.CSharp, "Program")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InMethod()
            VerifyBlock("
namespace N
{
    class Program
    {
        [|void M()
        {
            $$
        }|]
    }
}
", LanguageNames.CSharp, "void Program.M()")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_InProperty_CS()
            VerifyBlock("
namespace N
{
    class Program
    {
        [|public int P
        {
            get
            {
                $$
            }
        }|]
    }
}
", LanguageNames.CSharp, "int Program.P")
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_DocumentDoesNotSupportSyntax()
            ' NoCompilation is the special Language-Name we use to indicate that a language does not
            ' support SyntaxTrees/SemanticModels.  This test validates that we do not crash in that
            ' case and we gracefully bail out with 'false' for VsLanguageBlock.GetCurrentBlock.
            VerifyNoBlock("$$", languageName:="NoCompilation")
        End Sub

        <Fact>
        Public Sub TestGetCurrentBlock_NotInGlobalCode_CS()
            VerifyNoBlock("
var message = ""Hello"";
System.Console$$.WriteLine(message);
", LanguageNames.CSharp, SourceCodeKind.Script)

            VerifyNoBlock("
var message = ""Hello"";
System.Console$$.WriteLine(message);
", LanguageNames.CSharp, SourceCodeKind.Regular)
        End Sub

        <Fact>
        Public Sub TestGetCurrentBlock_NotInGlobalCode_VB()
            VerifyNoBlock("
Dim message = ""Hello""
System.Console$$.WriteLine(message)
", LanguageNames.VisualBasic, SourceCodeKind.Script)

            VerifyNoBlock("
Dim message = ""Hello""
System.Console$$.WriteLine(message)
", LanguageNames.VisualBasic, SourceCodeKind.Regular)
        End Sub

        Private Shared Sub VerifyNoBlock(markup As String, languageName As String, Optional sourceCodeKind As SourceCodeKind = SourceCodeKind.Regular)
            Dim xml = <Workspace>
                          <Project Language=<%= languageName %> CommonReferences="True">
                              <Document>
                                  <ParseOptions Kind=<%= sourceCodeKind %>/>
                                  <%= markup %>
                              </Document>
                          </Project>
                      </Workspace>

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeDefinitions),
                GetType(NoCompilationContentTypeLanguageService))

            Using workspace = EditorTestWorkspace.Create(xml, composition:=composition)
                Dim hostDocument = workspace.Documents.Single()

                Assert.Null(VsLanguageBlock.GetCurrentBlock(
                    hostDocument.GetTextBuffer().CurrentSnapshot,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None))
            End Using
        End Sub

        Private Shared Sub VerifyBlock(markup As String, languageName As String, expectedDescription As String)
            Dim xml = <Workspace>
                          <Project Language=<%= languageName %> CommonReferences="True">
                              <Document>
                                  <%= markup %>
                              </Document>
                          </Project>
                      </Workspace>
            Using workspace = EditorTestWorkspace.Create(xml)
                Dim hostDocument = workspace.Documents.Single()

                Dim tuple = VsLanguageBlock.GetCurrentBlock(
                    hostDocument.GetTextBuffer().CurrentSnapshot,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None)

                Assert.Equal(expectedDescription, tuple.Value.description)
                Assert.Equal(hostDocument.SelectedSpans.Single(), tuple.Value.span)
            End Using
        End Sub
    End Class
End Namespace
