' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Roslyn.Test.Utilities

Namespace Tests
    Public Class LanguageBlockTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_NotInImports_VB() As Task
            Await VerifyNoBlockAsync("
I$$mports System

Module Program
    Sub M()

    End Sub
End Module
", LanguageNames.VisualBasic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_NotLeadingTriviaOfRootClass_VB() As Task
            Await VerifyNoBlockAsync("
Imports System

$$

Module Program
    Sub M()

    End Sub
End Module
", LanguageNames.VisualBasic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InNamespace_VB() As Task
            Await VerifyBlockAsync("
[|Namespace N
$$
    Module Program
        Sub M()

        End Sub
    End Module
End Namespace|]
", LanguageNames.VisualBasic, "N")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InModule_VB() As Task
            Await VerifyBlockAsync("
Namespace N
    [|Module Program
        $$
        Sub M()

        End Sub
    End Module|]
End Namespace
", LanguageNames.VisualBasic, "Program")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InSub() As Task
            Await VerifyBlockAsync("
Namespace N
    Module Program
        [|Sub M()
            $$
        End Sub|]
    End Module
End Namespace
", LanguageNames.VisualBasic, "Sub Program.M()")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InFunction() As Task
            Await VerifyBlockAsync("
Namespace N
    Module Program
        [|Function F() As Integer
            $$
        End Function|]
    End Module
End Namespace
", LanguageNames.VisualBasic, "Function Program.F() As Integer")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InProperty_VB() As Task
            Await VerifyBlockAsync("
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_NotInUsings_CS() As Task
            Await VerifyNoBlockAsync("
u$$sing System;

class Program
{
    void M() { }
}
", LanguageNames.CSharp)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_NotLeadingTriviaOfRootClass_CS() As Task
            Await VerifyNoBlockAsync("
using System;

$$

class Program
{
    void M() { }
}
", LanguageNames.CSharp)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InNamespace_CS() As Task
            Await VerifyBlockAsync("
[|namespace N
{
$$
    class Program
    {
        void M() { }
    }
}|]
", LanguageNames.CSharp, "N")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InClass_CS() As Task
            Await VerifyBlockAsync("
namespace N
{
    [|class Program
    {
        $$
        void M() { }
    }|]
}
", LanguageNames.CSharp, "Program")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InMethod() As Task
            Await VerifyBlockAsync("
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_InProperty_CS() As Task
            Await VerifyBlockAsync("
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
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock), WorkItem(1043580)>
        Public Async Function TestGetCurrentBlock_DocumentDoesNotSupportSyntax() As Task
            ' NoCompilation is the special Language-Name we use to indicate that a language does not
            ' support SyntaxTrees/SemanticModels.  This test validates that we do not crash in that
            ' case and we gracefully bail out with 'false' for VsLanguageBlock.GetCurrentBlock.
            Await VerifyNoBlockAsync("$$", languageName:="NoCompilation")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock)>
        Public Async Function TestGetCurrentBlock_NotInGlobalCode_CS() As Task
            Await VerifyNoBlockAsync("
var message = ""Hello"";
System.Console$$.WriteLine(message);
", LanguageNames.CSharp, SourceCodeKind.Script)

            Await VerifyNoBlockAsync("
var message = ""Hello"";
System.Console$$.WriteLine(message);
", LanguageNames.CSharp, SourceCodeKind.Regular)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsLanguageBlock)>
        Public Async Function TestGetCurrentBlock_NotInGlobalCode_VB() As Task
            Await VerifyNoBlockAsync("
Dim message = ""Hello""
System.Console$$.WriteLine(message)
", LanguageNames.VisualBasic, SourceCodeKind.Script)

            Await VerifyNoBlockAsync("
Dim message = ""Hello""
System.Console$$.WriteLine(message)
", LanguageNames.VisualBasic, SourceCodeKind.Regular)
        End Function

        Private Async Function VerifyNoBlockAsync(markup As String, languageName As String, Optional sourceCodeKind As SourceCodeKind = SourceCodeKind.Regular) As Tasks.Task
            Dim xml = <Workspace>
                          <Project Language=<%= languageName %> CommonReferences="True">
                              <Document>
                                  <ParseOptions Kind=<%= sourceCodeKind %>/>
                                  <%= markup %>
                              </Document>
                          </Project>
                      </Workspace>
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(xml)
                Dim hostDocument = workspace.Documents.Single()

                Assert.Null(Await VsLanguageBlock.GetCurrentBlockAsync(
                         hostDocument.TextBuffer.CurrentSnapshot,
                         hostDocument.CursorPosition.Value,
                         CancellationToken.None))
            End Using
        End Function

        Private Async Function VerifyBlockAsync(markup As String, languageName As String, expectedDescription As String) As Tasks.Task
            Dim xml = <Workspace>
                          <Project Language=<%= languageName %> CommonReferences="True">
                              <Document>
                                  <%= markup %>
                              </Document>
                          </Project>
                      </Workspace>
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(xml)
                Dim hostDocument = workspace.Documents.Single()

                Dim tuple = Await VsLanguageBlock.GetCurrentBlockAsync(
                             hostDocument.TextBuffer.CurrentSnapshot,
                             hostDocument.CursorPosition.Value,
                             CancellationToken.None)

                Assert.Equal(expectedDescription, tuple.Item1)
                Assert.Equal(hostDocument.SelectedSpans.Single(), tuple.Item2)
            End Using
        End Function
    End Class
End Namespace