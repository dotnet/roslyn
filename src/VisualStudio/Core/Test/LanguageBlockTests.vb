' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Roslyn.Test.Utilities

Namespace Tests
    <[UseExportProvider]>
    Public Class LanguageBlockTests
        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_NotInImports_VB()
            VerifyNoBlock("
I$$mports System

Module Program
    Sub M()

    End Sub
End Module
", LanguageNames.VisualBasic)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_NotInUsings_CS()
            VerifyNoBlock("
u$$sing System;

class Program
{
    void M() { }
}
", LanguageNames.CSharp)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043580")>
        Public Sub TestGetCurrentBlock_DocumentDoesNotSupportSyntax()
            ' NoCompilation is the special Language-Name we use to indicate that a language does not
            ' support SyntaxTrees/SemanticModels.  This test validates that we do not crash in that
            ' case and we gracefully bail out with 'false' for VsLanguageBlock.GetCurrentBlock.
            VerifyNoBlock("$$", languageName:="NoCompilation")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock)>
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

        Private Sub VerifyNoBlock(markup As String, languageName As String, Optional sourceCodeKind As SourceCodeKind = SourceCodeKind.Regular)
            Dim xml = <Workspace>
                          <Project Language=<%= languageName %> CommonReferences="True">
                              <Document>
                                  <ParseOptions Kind=<%= sourceCodeKind %>/>
                                  <%= markup %>
                              </Document>
                          </Project>
                      </Workspace>
            Using workspace = TestWorkspace.Create(xml)
                Dim hostDocument = workspace.Documents.Single()

                Assert.Null(VsLanguageBlock.GetCurrentBlock(
                    hostDocument.TextBuffer.CurrentSnapshot,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None))
            End Using
        End Sub

        Private Sub VerifyBlock(markup As String, languageName As String, expectedDescription As String)
            Dim xml = <Workspace>
                          <Project Language=<%= languageName %> CommonReferences="True">
                              <Document>
                                  <%= markup %>
                              </Document>
                          </Project>
                      </Workspace>
            Using workspace = TestWorkspace.Create(xml)
                Dim hostDocument = workspace.Documents.Single()

                Dim tuple = VsLanguageBlock.GetCurrentBlock(
                    hostDocument.TextBuffer.CurrentSnapshot,
                    hostDocument.CursorPosition.Value,
                    CancellationToken.None)

                Assert.Equal(expectedDescription, tuple.Value.description)
                Assert.Equal(hostDocument.SelectedSpans.Single(), tuple.Value.span)
            End Using
        End Sub
    End Class
End Namespace
