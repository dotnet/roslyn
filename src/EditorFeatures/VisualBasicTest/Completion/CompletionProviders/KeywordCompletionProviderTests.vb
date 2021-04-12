﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class KeywordCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(KeywordCompletionProvider)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function IsCommitCharacterTest() As Task
            Await VerifyCommonCommitCharactersAsync("$$", textTypedSoFar:="C")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsTextualTriggerCharacterTest()
            TestCommonIsTextualTriggerCharacter()

            VerifyTextualTriggerCharacter("goo$$(", shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=True)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function SendEnterThroughToEditorTest() As Task
            Await VerifySendEnterThroughToEditorAsync("$$", "Class", expected:=True)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function InEmptyFile() As Task

            Dim markup = "$$"
            Await VerifyAnyItemExistsAsync(markup)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInInactiveCode() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
#If False Then
        $$
#End If
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInString() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = "$$"
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInUnterminatedString() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = "$$
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInSingleLineComment() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = '$$
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <WorkItem(968256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")>
        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestUnionOfKeywordsFromBothFiles() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="GOO=true">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
            Dim x As Integer
#if GOO then
    sub goo()
#End If
        $$
#If GOO Then
    end sub

#End If
End Class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences=" true" AssemblyName="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            Await VerifyItemInLinkedFilesAsync(markup, "Public", Nothing)
            Await VerifyItemInLinkedFilesAsync(markup, "For", Nothing)
        End Function

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInInteger() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = 2$$00
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInDecimal() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = 2$$.00D
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInFloat() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = 2$$.00
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function TestNotInDate() As Task
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = #4/2$$/2015
    End Sub
End Class
</Text>.Value

            Await VerifyNoItemsExistAsync(code)
        End Function

        <WorkItem(4167, "https://github.com/dotnet/roslyn/issues/4167")>
        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Async Function ImplementsAfterSub() As Task
            Dim code = "
Interface I
End Interface

Class C
    Implements I

    Sub M() $$
    End Sub
End Class
"

            Await VerifyItemExistsAsync(code, "Implements")
        End Function
    End Class
End Namespace
