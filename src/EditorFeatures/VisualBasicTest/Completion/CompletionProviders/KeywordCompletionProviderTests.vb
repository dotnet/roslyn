' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class KeywordCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function CreateCompletionProvider() As ICompletionProvider
            Return New KeywordCompletionProvider()
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsCommitCharacterTest()
            TestCommonIsCommitCharacter()
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsTextualTriggerCharacterTest()
            TestCommonIsTextualTriggerCharacter()

            VerifyTextualTriggerCharacter("foo$$(", shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=True)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SendEnterThroughToEditorTest()
            TestCommonSendEnterThroughToEditor()
        End Sub


        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InEmptyFile()

            Dim markup = "$$"
            VerifyAnyItemExists(markup)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInInactiveCode()
            Dim code = <Text>
Class C
    Sub Main(args As String())
#If False Then
        $$
#End If
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInString()
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = "$$"
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub


        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInUnterminatedString()
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = "$$
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInSingleLineComment()
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = '$$
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub

        <WorkItem(968256)>
        <Fact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub UnionOfKeywordsFromBothFiles()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferences="true" AssemblyName="Proj1" PreprocessorSymbols="FOO=true">
                                 <Document FilePath="CurrentDocument.vb"><![CDATA[
Class C
            Dim x As Integer
#if FOO then
    sub foo()
#End If
        $$
#If FOO Then
    end sub

#End If
End Class]]>
                                 </Document>
                             </Project>
                             <Project Language="Visual Basic" CommonReferences=" true" AssemblyName="Proj2">
                                 <Document IsLinkFile="true" LinkAssemblyName="Proj1" LinkFilePath="CurrentDocument.vb"/>
                             </Project>
                         </Workspace>.ToString().NormalizeLineEndings()

            VerifyItemInLinkedFiles(markup, "Public", Nothing)
            VerifyItemInLinkedFiles(markup, "For", Nothing)
        End Sub
    End Class
End Namespace
