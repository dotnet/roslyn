' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Roslyn.Test.Utilities

Public Class LanguageBlockTests
    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_NotInImports_VB()
        VerifyNoBlock("
I$$mports System

Module Program
    Sub M()

    End Sub
End Module
", LanguageNames.VisualBasic)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_NotLeadingTriviaOfRootClass_VB()
        VerifyNoBlock("
Imports System

$$

Module Program
    Sub M()

    End Sub
End Module
", LanguageNames.VisualBasic)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InNamespace_VB()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InModule_VB()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InSub()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InFunction()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InProperty_VB()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_NotInUsings_CS()
        VerifyNoBlock("
u$$sing System;

class Program
{
    void M() { }
}
", LanguageNames.CSharp)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_NotLeadingTriviaOfRootClass_CS()
        VerifyNoBlock("
using System;

$$

class Program
{
    void M() { }
}
", LanguageNames.CSharp)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InNamespace_CS()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InClass_CS()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InMethod()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_InProperty_CS()
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

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
    Public Sub GetCurrentBlock_DocumentDoesNotSupportSyntax()
        ' NoCompilation is the special Language-Name we use to indicate that a language does not
        ' support SyntaxTrees/SemanticModels.  This test validates that we do not crash in that
        ' case and we gracefully bail out with 'false' for VsLanguageBlock.GetCurrentBlock.
        VerifyNoBlock("$$", languageName:="NoCompilation")
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock)>
    Public Sub GetCurrentBlock_NotInGlobalCode_CS()
        VerifyNoBlock("
var message = ""Hello"";
System.Console$$.WriteLine(message);
", LanguageNames.CSharp, SourceCodeKind.Script)

        VerifyNoBlock("
var message = ""Hello"";
System.Console$$.WriteLine(message);
", LanguageNames.CSharp, SourceCodeKind.Regular)
    End Sub

    <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock)>
    Public Sub GetCurrentBlock_NotInGlobalCode_VB()
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
        Using workspace = TestWorkspaceFactory.CreateWorkspace(xml)
            Dim hostDocument = workspace.Documents.Single()
            Dim description As String = Nothing
            Dim span As TextSpan

            Assert.False(VsLanguageBlock.GetCurrentBlock(
                         hostDocument.TextBuffer.CurrentSnapshot,
                         hostDocument.CursorPosition.Value,
                         CancellationToken.None,
                         description,
                         span))
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
        Using workspace = TestWorkspaceFactory.CreateWorkspace(xml)
            Dim hostDocument = workspace.Documents.Single()
            Dim description As String = Nothing
            Dim span As TextSpan

            Assert.True(VsLanguageBlock.GetCurrentBlock(
                             hostDocument.TextBuffer.CurrentSnapshot,
                             hostDocument.CursorPosition.Value,
                             CancellationToken.None,
                             description,
                             span))

            Assert.Equal(expectedDescription, description)
            Assert.Equal(hostDocument.SelectedSpans.Single(), span)
        End Using
    End Sub
End Class
