' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
    Public Class KeywordCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(KeywordCompletionProvider)
        End Function

        <Fact>
        Public Async Function IsCommitCharacterTest() As Task
            Await VerifyCommonCommitCharactersAsync("$$", textTypedSoFar:="C")
        End Function

        <Fact>
        Public Sub IsTextualTriggerCharacterTest()
            TestCommonIsTextualTriggerCharacter()

            VerifyTextualTriggerCharacter("goo$$(", shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=True)
        End Sub

        <Fact>
        Public Async Function SendEnterThroughToEditorTest() As Task
            Await VerifySendEnterThroughToEditorAsync("$$", "Class", expected:=True)
        End Function

        <Fact>
        Public Async Function InEmptyFile() As Task

            Dim markup = "$$"
            Await VerifyAnyItemExistsAsync(markup)
        End Function

        <Fact>
        Public Async Function TestNoTypeKeywordsInAsyncMemberDeclaration() As Task
            Dim code = <Text>
Class C
    Public Async Function Test() As $$
        
    End Function
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(code, "Boolean")
            Await VerifyItemIsAbsentAsync(code, "Byte")
            Await VerifyItemIsAbsentAsync(code, "Char")
            Await VerifyItemIsAbsentAsync(code, "Date")
            Await VerifyItemIsAbsentAsync(code, "Decimal")
            Await VerifyItemIsAbsentAsync(code, "Double")
            Await VerifyItemIsAbsentAsync(code, "Integer")
            Await VerifyItemIsAbsentAsync(code, "Long")
            Await VerifyItemIsAbsentAsync(code, "Object")
            Await VerifyItemIsAbsentAsync(code, "SByte")
            Await VerifyItemIsAbsentAsync(code, "Short")
            Await VerifyItemIsAbsentAsync(code, "Single")
            Await VerifyItemIsAbsentAsync(code, "String")
            Await VerifyItemIsAbsentAsync(code, "UInteger")
            Await VerifyItemIsAbsentAsync(code, "ULong")
            Await VerifyItemIsAbsentAsync(code, "UShort")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/968256")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1736")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1736")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1736")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1736")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/4167")>
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
