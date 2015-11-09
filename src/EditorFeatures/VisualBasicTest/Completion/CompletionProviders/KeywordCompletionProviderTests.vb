' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class KeywordCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New KeywordCompletionProvider()
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsCommitCharacterTest()
            VerifyCommonCommitCharacters("$$", textTypedSoFar:="")
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub IsTextualTriggerCharacterTest()
            TestCommonIsTextualTriggerCharacter()

            VerifyTextualTriggerCharacter("foo$$(", shouldTriggerWithTriggerOnLettersEnabled:=True, shouldTriggerWithTriggerOnLettersDisabled:=True)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub SendEnterThroughToEditorTest()
            VerifySendEnterThroughToEditor("$$", "Class", expected:=True)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub InEmptyFile()

            Dim markup = "$$"
            VerifyAnyItemExists(markup)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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

        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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


        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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

        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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
        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
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

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInInteger()
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = 2$$00
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInDecimal()
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = 2$$.00D
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInFloat()
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = 2$$.00
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub

        <WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub NotInDate()
            Dim code = <Text>
Class C
    Sub Main(args As String())
        dim c = #4/2$$/2015
    End Sub
End Class
</Text>.Value

            VerifyNoItemsExist(code)
        End Sub

        <WorkItem(4167, "https://github.com/dotnet/roslyn/issues/4167")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.KeywordRecommending)>
        Public Sub ImplementsAfterSub()
            Dim code = "
Interface I
End Interface

Class C
    Implements I

    Sub M() $$
    End Sub
End Class
"

            VerifyItemExists(code, "Implements")
        End Sub
    End Class
End Namespace
