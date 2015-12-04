' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration1() As Task
            Dim markup = <a>Class C
    $$
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration2() As Task
            Dim markup = <a>Class C
    Public $$
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration3() As Task
            Dim markup = <a>Module M
    Public $$
End Module</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration4() As Task
            Dim markup = <a>Structure S
    Public $$
End Structure</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration5() As Task
            Dim markup = <a>Class C
    WithEvents $$
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFieldDeclaration6() As Task
            Dim markup = <a>Class C
    Protected Friend $$
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration1() As Task
            Dim markup = <a>Class C
    Public Sub Bar($$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration2() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional foo as Integer, $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration3() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional $$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration4() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional x $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration5() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional x As $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration6() As Task
            Dim markup = <a>Class C
    Public Sub Bar(Optional x As Integer $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration7() As Task
            Dim markup = <a>Class C
    Public Sub Bar(ByVal $$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration8() As Task
            Dim markup = <a>Class C
    Public Sub Bar(ByVal x $$
    End Sub
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration9() As Task
            Dim markup = <a>Class C
    Sub Foo $$
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterDeclaration10() As Task
            Dim markup = <a>Class C
    Public Property SomeProp $$
End Class</a>

            Await VerifyNotBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectClause1() As Task
            Dim markup = <a>Class z
    Sub bar()
        Dim a = New Integer(1, 2, 3) {}
        Dim foo = From z In a
                  Select $$

    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectClause2() As Task
            Dim markup = <a>Class z
    Sub bar()
        Dim a = New Integer(1, 2, 3) {}
        Dim foo = From z In a
                  Select 1, $$

    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement1() As Task
            Dim markup = <a>Class z
    Sub bar()
        For $$
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestForStatement2() As Task
            Dim markup = <a>Class z
    Sub bar()
        For $$ = 1 To 10
        Next
    End Sub
End Class</a>

            Await VerifyBuilderAsync(markup)
        End Function

        <WorkItem(545351)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WorkItem(546659)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WorkItem(734596)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WorkItem(734596)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WorkItem(960416)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <WorkItem(1044441)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BuilderInDebugger() As Task
            Dim markup = <a> 
Class C1
    Sub Foo()
        Dim __o = $$
    End Sub
End Class
</a>
            Await VerifyBuilderAsync(markup, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), useDebuggerOptions:=True)
        End Function

        Private Function VerifyNotBuilderAsync(markup As XElement, Optional triggerInfo As CompletionTriggerInfo? = Nothing, Optional useDebuggerOptions As Boolean = False) As Task
            Return VerifySuggestionModeWorkerAsync(markup, isBuilder:=False, triggerInfo:=triggerInfo, useDebuggerOptions:=useDebuggerOptions)
        End Function

        Private Function VerifyBuilderAsync(markup As XElement, Optional triggerInfo As CompletionTriggerInfo? = Nothing, Optional useDebuggerOptions As Boolean = False) As Task
            Return VerifySuggestionModeWorkerAsync(markup, isBuilder:=True, triggerInfo:=triggerInfo, useDebuggerOptions:=useDebuggerOptions)
        End Function

        Private Async Function VerifySuggestionModeWorkerAsync(markup As XElement, isBuilder As Boolean, triggerInfo As CompletionTriggerInfo?, Optional useDebuggerOptions As Boolean = False) As Task
            Dim code As String = Nothing
            Dim position As Integer = 0
            MarkupTestFile.GetPosition(markup.NormalizedValue, code, position)

            Using workspaceFixture = New VisualBasicTestWorkspaceFixture()
                Dim options = If(useDebuggerOptions,
                                 (Await workspaceFixture.GetWorkspaceAsync()).Options.WithDebuggerCompletionOptions(),
                                 (Await workspaceFixture.GetWorkspaceAsync()).Options)

                Dim document1 = Await workspaceFixture.UpdateDocumentAsync(code, SourceCodeKind.Regular)
                CheckResults(document1, position, isBuilder, triggerInfo, options)

                If CanUseSpeculativeSemanticModel(document1, position) Then
                    Dim document2 = Await workspaceFixture.UpdateDocumentAsync(code, SourceCodeKind.Regular, cleanBeforeUpdate:=False)
                    CheckResults(document2, position, isBuilder, triggerInfo, options)
                End If
            End Using

        End Function

        Private Sub CheckResults(document As Document, position As Integer, isBuilder As Boolean, triggerInfo As CompletionTriggerInfo?, options As OptionSet)
            triggerInfo = If(triggerInfo, CompletionTriggerInfo.CreateTypeCharTriggerInfo("a"c))

            Dim completionList = GetCompletionList(document, position, triggerInfo.Value, options)

            If isBuilder Then
                Assert.NotNull(completionList)
                Assert.NotNull(completionList.Builder)
            Else
                If completionList IsNot Nothing Then
                    Assert.True(completionList.Builder Is Nothing, "group.Builder = " & If(completionList.Builder IsNot Nothing, completionList.Builder.DisplayText, "null"))
                End If
            End If
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New VisualBasicSuggestionModeCompletionProvider()
        End Function
    End Class
End Namespace
