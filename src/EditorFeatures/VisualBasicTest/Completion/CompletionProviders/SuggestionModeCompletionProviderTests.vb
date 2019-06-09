' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.SuggestionMode

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class SuggestionModeCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration1() As Task
            Dim markup = <a>Class C
    $$
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration2() As Task
            Dim markup = <a>Class C
    Public $$
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration3() As Task
            Dim markup = <a>Module M
    Public $$
End Module</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration4() As Task
            Dim markup = <a>Structure S
    Public $$
End Structure</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration5() As Task
            Dim markup = <a>Class C
    WithEvents $$
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration6() As Task
            Dim markup = <a>Class C
    Protected Friend $$
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration1() As Task
            Dim markup = <a>Class C
    Public Sub Bar($$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration2() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional goo as Integer, $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration3() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional $$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration4() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional x $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration5() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional x As $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration6() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional x As Integer $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration7() As Task
            Dim markup = <a>Class C
    Public Sub Bar(ByVal $$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration8() As Task
            Dim markup = <a>Class C
    Public Sub Bar(ByVal x $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration9() As Task
            Dim markup = <a>Class C
    Sub Goo $$
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration10() As Task
            Dim markup = <a>Class C
    Public Property SomeProp $$
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectClause1() As Task
            Dim markup = <a>Class z
    Sub bar()
        Dim a = New Integer(1, 2, 3) {}
        Dim goo = From z In a
                  Select $$

    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectClause2() As Task
            Dim markup = <a>Class z
    Sub bar()
        Dim a = New Integer(1, 2, 3) {}
        Dim goo = From z In a
                  Select 1, $$

    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement1() As Task
            Dim markup = <a>Class z
    Sub bar()
        For $$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement2() As Task
            Dim markup = <a>Class z
    Sub bar()
        For $$ = 1 To 10
        Next
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WorkItem(545351, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545351")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBuilderWhenOptionExplicitOff() As Task
            Dim markup = <a>Option Explicit Off
 
Class C1
    Sub M()
        Console.WriteLine($$
    End Sub
End Class
</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WorkItem(546659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546659")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUsingStatement() As Task
            Dim markup = <a> 
Class C1
    Sub M()
        Using $$
    End Sub
End Class
</a>
            Await VerifyBuilderAsync(markup)
        End Function

        <WorkItem(734596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734596")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOptionExplicitOffStatementLevel1() As Task
            Dim markup = <a> 
Option Explicit Off
Class C1
    Sub M()
        $$
    End Sub
End Class
</a>
            Await VerifyBuilderAsync(markup)
        End Function

        <WorkItem(734596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/734596")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOptionExplicitOffStatementLevel2() As Task
            Dim markup = <a> 
Option Explicit Off
Class C1
    Sub M()
        a = $$
    End Sub
End Class
</a>
            Await VerifyBuilderAsync(markup)
        End Function

        <WorkItem(960416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960416")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReadonlyField() As Task
            Dim markup = <a> 
Class C1
    Readonly $$
    Sub M()
    End Sub
End Class
</a>
            Await VerifyBuilderAsync(markup)
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceDeclarationName_Unqualified() As Task
            Dim markup = <a> 
Namespace $$
End Namespace
</a>
            Await VerifyBuilderAsync(markup, CompletionTrigger.Invoke)
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NamespaceDeclarationName_Qualified() As Task
            Dim markup = <a> 
Namespace A.$$
End Namespace
</a>
            Await VerifyBuilderAsync(markup, CompletionTrigger.Invoke)
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PartialClassName() As Task
            Dim markup = <a>Partial Class $$</a>
            Await VerifyBuilderAsync(markup, CompletionTrigger.Invoke)
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PartialStructureName() As Task
            Dim markup = <a>Partial Structure $$</a>
            Await VerifyBuilderAsync(markup, CompletionTrigger.Invoke)
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PartialInterfaceName() As Task
            Dim markup = <a>Partial Interface $$</a>
            Await VerifyBuilderAsync(markup, CompletionTrigger.Invoke)
        End Function

        <WorkItem(7213, "https://github.com/dotnet/roslyn/issues/7213")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PartialModuleName() As Task
            Dim markup = <a>Partial Module $$</a>
            Await VerifyBuilderAsync(markup, CompletionTrigger.Invoke)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TupleType() As Task
            Dim markup = <a>
Class C
    Sub M()
        Dim t As (a$$, b)
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TupleTypeAfterComma() As Task
            Dim markup = <a>
Class C
    Sub M()
        Dim t As (a, b$$)
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        Private Function VerifyNotBuilderAsync(markup As XElement, Optional triggerInfo As CompletionTrigger? = Nothing, Optional useDebuggerOptions As Boolean = False) As Task
            Return VerifySuggestionModeWorkerAsync(markup, isBuilder:=False, triggerInfo:=triggerInfo, useDebuggerOptions:=useDebuggerOptions)
        End Function

        Private Function VerifyBuilderAsync(markup As XElement, Optional triggerInfo As CompletionTrigger? = Nothing, Optional useDebuggerOptions As Boolean = False) As Task
            Return VerifySuggestionModeWorkerAsync(markup, isBuilder:=True, triggerInfo:=triggerInfo, useDebuggerOptions:=useDebuggerOptions)
        End Function

        Private Async Function VerifySuggestionModeWorkerAsync(markup As XElement, isBuilder As Boolean, triggerInfo As CompletionTrigger?, Optional useDebuggerOptions As Boolean = False) As Task
            Dim code As String = Nothing
            Dim position As Integer = 0
            MarkupTestFile.GetPosition(markup.NormalizedValue, code, position)

            Using workspaceFixture = New VisualBasicTestWorkspaceFixture()
                Dim options = If(useDebuggerOptions,
                                 (workspaceFixture.GetWorkspace()).Options.WithDebuggerCompletionOptions(),
                                 (workspaceFixture.GetWorkspace()).Options)

                Dim document1 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular)
                Await CheckResultsAsync(document1, position, isBuilder, triggerInfo, options)

                If Await CanUseSpeculativeSemanticModelAsync(document1, position) Then
                    Dim document2 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular, cleanBeforeUpdate:=False)
                    Await CheckResultsAsync(document2, position, isBuilder, triggerInfo, options)
                End If

                workspaceFixture.DisposeAfterTest()
            End Using
        End Function

        Private Overloads Async Function CheckResultsAsync(document As Document, position As Integer, isBuilder As Boolean, triggerInfo As CompletionTrigger?, options As OptionSet) As Task
            triggerInfo = If(triggerInfo, CompletionTrigger.CreateInsertionTrigger("a"c))

            Dim service = GetCompletionService(document.Project.Solution.Workspace)
            Dim context = Await service.GetTestAccessor().GetContextAsync(
                service.GetTestAccessor().ExclusiveProviders?(0), document, position, triggerInfo.Value, options, CancellationToken.None)

            If isBuilder Then
                Assert.NotNull(context)
                Assert.NotNull(context.SuggestionModeItem)
            Else
                If context IsNot Nothing Then
                    Assert.True(context.SuggestionModeItem Is Nothing, "group.Builder = " & If(context.SuggestionModeItem IsNot Nothing, context.SuggestionModeItem.DisplayText, "null"))
                End If
            End If
        End Function

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New VisualBasicSuggestionModeCompletionProvider()
        End Function
    End Class
End Namespace
