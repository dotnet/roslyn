' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class PartialTypeCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New PartialTypeCompletionProvider()
        End Function

        <WorkItem(578224, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578224")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRecommendTypesWithoutPartial() As Task
            Dim text = <text>Class C
End Class

Partial Class $$</text>

            Await VerifyItemExistsAsync(text.Value, "C")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialClass1() As Task
            Dim text = <text>Partial Class C
End Class

Partial Class $$</text>

            Await VerifyItemExistsAsync(text.Value, "C")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialGenericClass1() As Task
            Dim text = <text>Class Bar
End Class
                           
Partial Class C(Of Bar)
End Class

Partial Class $$</text>

            Await VerifyItemExistsAsync(text.Value, "C(Of Bar)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialGenericClassCommitOnParen() As Task
            Dim text = <text>Class Bar
End Class
                           
Partial Class C(Of Bar)
End Class

Partial Class $$</text>

            Dim expected = <text>Class Bar
End Class
                           
Partial Class C(Of Bar)
End Class

Partial Class C(</text>

            Await VerifyProviderCommitAsync(text.Value, "C(Of Bar)", expected.Value, "("c, "", SourceCodeKind.Regular)
        End Function

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/11569"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialClassWithSameMemberName() As Task
            Dim text = <text>Partial Class C(Of T)
    Sub C()
    End Sub
End Class

Partial Class $$C(Of T)
End Class</text>

            Await VerifyItemExistsAsync(text.Value, "C(Of T)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialGenericClassCommitOnTab() As Task
            Dim text = <text>Class Bar
End Class
                           
Partial Class C(Of Bar)
End Class

Partial Class $$</text>

            Dim expected = <text>Class Bar
End Class
                           
Partial Class C(Of Bar)
End Class

Partial Class C(Of Bar)</text>

            Await VerifyProviderCommitAsync(text.Value, "C(Of Bar)", expected.Value, Nothing, "", SourceCodeKind.Regular)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialGenericClassCommitOnSpace() As Task
            Dim text = <text>Partial Class C(Of T)
End Class

Partial Class $$</text>

            Dim expected = <text>Partial Class C(Of T)
End Class

Partial Class C(Of T) </text>

            Await VerifyProviderCommitAsync(text.Value, "C(Of T)", expected.Value, " "c, "", SourceCodeKind.Regular)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialClassWithModifiers() As Task
            Dim text = <text>Partial Class C
End Class

Partial Friend Class $$</text>

            Await VerifyItemExistsAsync(text.Value, "C")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialStruct() As Task
            Dim text = <text>Partial Structure S
End Structure

Partial Structure $$</text>

            Await VerifyItemExistsAsync(text.Value, "S")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialInterface() As Task
            Dim text = <text>Partial Interface I
End Interface

Partial Interface $$</text>

            Await VerifyItemExistsAsync(text.Value, "I")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialModule() As Task
            Dim text = <text>Partial Module M
End Module

Partial Module $$</text>

            Await VerifyItemExistsAsync(text.Value, "M")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeKindMatches1() As Task
            Dim text = <text>Partial Structure S
End Structure

Partial Class $$</text>

            Await VerifyNoItemsExistAsync(text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypeKindMatches2() As Task
            Dim text = <text>Partial Class C
End Class

Partial Structure $$</text>

            Await VerifyNoItemsExistAsync(text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialClassesInSameNamespace() As Task
            Dim text = <text>Namespace N
    Partial Class Goo

    End Class
End Namespace

Namespace N
    Partial Class $$

End Namespace</text>

            Await VerifyItemExistsAsync(text.Value, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotPartialClassesAcrossDifferentNamespaces() As Task
            Dim text = <text>Namespace N
    Partial Class Goo

    End Class
End Namespace

Partial Class $$</text>

            Await VerifyNoItemsExistAsync(text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotPartialClassesInOuterNamespaces() As Task
            Dim text = <text>Partial Class C

End Class

Namespace N
    Partial Class $$
End Namespace
</text>

            Await VerifyNoItemsExistAsync(text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotPartialClassesInOuterClass() As Task
            Dim text = <text>Partial Class C
    Partial Class $$
End Class
</text>

            Await VerifyNoItemsExistAsync(text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIncludeConstraints() As Task
            Dim text = <text>
Partial Class C1(Of T As Exception)
 
End Class

Partial Class $$</text>

            Await VerifyItemExistsAsync(text.Value, "C1(Of T As Exception)")
        End Function

        <WorkItem(578122, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578122")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotSuggestCurrentMember() As Task
            Dim text = <text>
Partial Class F$$
                       </text>

            Await VerifyNoItemsExistAsync(text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInTrivia() As Task
            Dim text = <text>
Partial Class C1
 
End Class

Partial Class '$$</text>

            Await VerifyNoItemsExistAsync(text.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialClassWithReservedName() As Task
            Dim text = <text>Partial Class [Class]
End Class

Partial Class $$</text>

            Dim expected = <text>Partial Class [Class]
End Class

Partial Class [Class]</text>

            Await VerifyProviderCommitAsync(text.Value, "Class", expected.Value, Nothing, "", SourceCodeKind.Regular)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialGenericClassWithReservedName() As Task
            Dim text = <text>Partial Class [Class](Of T)
End Class

Partial Class $$</text>

            Dim expected = <text>Partial Class [Class](Of T)
End Class

Partial Class [Class](Of T)</text>

            Await VerifyProviderCommitAsync(text.Value, "Class(Of T)", expected.Value, Nothing, "", SourceCodeKind.Regular)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialGenericClassWithReservedNameCommittedWithParen() As Task
            Dim text = <text>Partial Class [Class](Of T)
End Class

Partial Class $$</text>

            Dim expected = <text>Partial Class [Class](Of T)
End Class

Partial Class [Class](</text>

            Await VerifyProviderCommitAsync(text.Value, "Class(Of T)", expected.Value, "("c, "", SourceCodeKind.Regular)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPartialGenericInterfaceWithVariance() As Task
            Dim text = <text>Partial Interface G(Of Out T)
End Interface

Partial Interface $$</text>

            Dim expected = <text>Partial Interface G(Of Out T)
End Interface

Partial Interface G(Of Out T)</text>

            Await VerifyProviderCommitAsync(text.Value, "G(Of Out T)", expected.Value, Nothing, "", SourceCodeKind.Regular)
        End Function

    End Class
End Namespace

