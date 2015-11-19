' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public Sub FieldDeclaration1()
            Dim markup = <a>Class C
    $$
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldDeclaration2()
            Dim markup = <a>Class C
    Public $$
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldDeclaration3()
            Dim markup = <a>Module M
    Public $$
End Module</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldDeclaration4()
            Dim markup = <a>Structure S
    Public $$
End Structure</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldDeclaration5()
            Dim markup = <a>Class C
    WithEvents $$
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FieldDeclaration6()
            Dim markup = <a>Class C
    Protected Friend $$
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration1()
            Dim markup = <a>Class C
    Public Sub Bar($$
    End Sub
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration2()
            Dim markup = <a>Class C
    Public Sub Bar(Optional foo as Integer, $$
    End Sub
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration3()
            Dim markup = <a>Class C
    Public Sub Bar(Optional $$
    End Sub
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration4()
            Dim markup = <a>Class C
    Public Sub Bar(Optional x $$
    End Sub
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration5()
            Dim markup = <a>Class C
    Public Sub Bar(Optional x As $$
    End Sub
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration6()
            Dim markup = <a>Class C
    Public Sub Bar(Optional x As Integer $$
    End Sub
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration7()
            Dim markup = <a>Class C
    Public Sub Bar(ByVal $$
    End Sub
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration8()
            Dim markup = <a>Class C
    Public Sub Bar(ByVal x $$
    End Sub
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration9()
            Dim markup = <a>Class C
    Sub Foo $$
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterDeclaration10()
            Dim markup = <a>Class C
    Public Property SomeProp $$
End Class</a>

            VerifyNotBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SelectClause1()
            Dim markup = <a>Class z
    Sub bar()
        Dim a = New Integer(1, 2, 3) {}
        Dim foo = From z In a
                  Select $$

    End Sub
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SelectClause2()
            Dim markup = <a>Class z
    Sub bar()
        Dim a = New Integer(1, 2, 3) {}
        Dim foo = From z In a
                  Select 1, $$

    End Sub
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStatement1()
            Dim markup = <a>Class z
    Sub bar()
        For $$
    End Sub
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ForStatement2()
            Dim markup = <a>Class z
    Sub bar()
        For $$ = 1 To 10
        Next
    End Sub
End Class</a>

            VerifyBuilder(markup)
        End Sub

        <WorkItem(545351)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BuilderWhenOptionExplicitOff()
            Dim markup = <a>Option Explicit Off
 
Class C1
    Sub M()
        Console.WriteLine($$
    End Sub
End Class
</a>

            VerifyBuilder(markup)
        End Sub

        <WorkItem(546659)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub UsingStatement()
            Dim markup = <a> 
Class C1
    Sub M()
        Using $$
    End Sub
End Class
</a>
            VerifyBuilder(markup)
        End Sub

        <WorkItem(734596)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OptionExplicitOffStatementLevel1()
            Dim markup = <a> 
Option Explicit Off
Class C1
    Sub M()
        $$
    End Sub
End Class
</a>
            VerifyBuilder(markup)
        End Sub

        <WorkItem(734596)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OptionExplicitOffStatementLevel2()
            Dim markup = <a> 
Option Explicit Off
Class C1
    Sub M()
        a = $$
    End Sub
End Class
</a>
            VerifyBuilder(markup)
        End Sub

        <WorkItem(960416)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ReadonlyField()
            Dim markup = <a> 
Class C1
    Readonly $$
    Sub M()
    End Sub
End Class
</a>
            VerifyBuilder(markup)
        End Sub

        <WorkItem(1044441)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BuilderInDebugger
            Dim markup = <a> 
Class C1
    Sub Foo()
        Dim __o = $$
    End Sub
End Class
</a>
            VerifyBuilder(markup, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), useDebuggerOptions:=True)
        End Sub

        Private Sub VerifyNotBuilder(markup As XElement, Optional triggerInfo As CompletionTriggerInfo? = Nothing, Optional useDebuggerOptions As Boolean = False)
            VerifySuggestionModeWorker(markup, isBuilder:=False, triggerInfo:=triggerInfo, useDebuggerOptions:=useDebuggerOptions)
        End Sub

        Private Sub VerifyBuilder(markup As XElement, Optional triggerInfo As CompletionTriggerInfo? = Nothing, Optional useDebuggerOptions As Boolean = False)
            VerifySuggestionModeWorker(markup, isBuilder:=True, triggerInfo:=triggerInfo, useDebuggerOptions:=useDebuggerOptions)
        End Sub

        Private Sub VerifySuggestionModeWorker(markup As XElement, isBuilder As Boolean, triggerInfo As CompletionTriggerInfo?, Optional useDebuggerOptions As Boolean = False)
            Dim code As String = Nothing
            Dim position As Integer = 0
            MarkupTestFile.GetPosition(markup.NormalizedValue, code, position)

            Using workspaceFixture = New VisualBasicTestWorkspaceFixture()
                Dim options = If(useDebuggerOptions,
                                 workspaceFixture.Workspace.Options.WithDebuggerCompletionOptions(),
                                 workspaceFixture.Workspace.Options)

                Dim document1 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular)
                CheckResults(document1, position, isBuilder, triggerInfo, options)

                If CanUseSpeculativeSemanticModel(document1, position) Then
                    Dim document2 = workspaceFixture.UpdateDocument(code, SourceCodeKind.Regular, cleanBeforeUpdate:=False)
                    CheckResults(document2, position, isBuilder, triggerInfo, options)
                End If
            End Using

        End Sub

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
