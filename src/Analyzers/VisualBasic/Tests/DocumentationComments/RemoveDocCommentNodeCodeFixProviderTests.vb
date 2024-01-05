' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.DocumentationComments

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.DocumentationComments
    Public Class RemoveDocCommentNodeCodeFixProviderTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicRemoveDocCommentNodeCodeFixProvider())
        End Function

        Private Overloads Async Function TestAsync(ByVal initial As String, ByVal expected As String) As Task
            Dim parseOptions = TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose)
            Await TestAsync(initial, expected, parseOptions:=parseOptions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateParamTag() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' [|<param name=""value""></param>|]
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"
            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateParamTag_OnlyParamTags() As Task
            Dim initial =
"Class Program
    ''' <param name=""value""></param>
    ''' [|<param name=""value""></param>|]
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateParamTag_TagBelowOffendingParamTag() As Task
            Dim initial =
"Class Program
    ''' <param name=""value""></param>
    ''' [|<param name=""value""></param>|]
    ''' <returns></returns>
    Public Function Fizz(ByVal value As Integer) As Integer
        Return 0
    End Function
End Class"

            Dim expected =
"Class Program
    ''' <param name=""value""></param>
    ''' <returns></returns>
    Public Function Fizz(ByVal value As Integer) As Integer
        Return 0
    End Function
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateParamTag_BothParamTagsOnSameLine_DocCommentTagBetweenThem() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>    ''' [|<param name=""value""></param>|]
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateParamTag_BothParamTagsOnSameLine_WhitespaceBetweenThem() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>    [|<param name=""value""></param>|]
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/13436")>
        Public Async Function RemovesParamTag_BothParamTagsOnSameLine() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' [|<param name=""a""></param>|]<param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/13436")>
        Public Async Function RemovesParamTag_TrailingText1() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' [|<param name=""a""></param>|] a
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    '''  a
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateParamTag_BothParamTagsOnSameLine_NothingBetweenThem() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>[|<param name=""value""></param>|]
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesParamTagWithNoMatchingParam() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' [|<param name=""buzz""></param>|]
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateParamTag_RawTextBeforeAndAfterNode() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' some comment[|<param name=""value""></param>|]out of the XML nodes
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"
            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' some commentout of the XML nodes
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateTypeparamTag() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' [|<typeparam name=""T""></typeparam>|]
    ''' <typeparam name=""U""></typeparam>
    ''' <param name=""value""></param>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <param name=""value""></param>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesTypeparamTagWithNoMatchingType() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' [|<typeparam name=""A""></typeparam>|]
    ''' <param name=""value""></param>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <param name=""value""></param>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesReturnsTagOnSub() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' [|<returns></returns>|]
    Public Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Public Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesDuplicateReturnsTag() As Task
            Dim initial =
"Class Program
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    ''' <returns></returns>
    ''' [|<returns></returns>|]
    Public Function Fizz(ByVal value As Integer) As Integer
        Return 0
    End Function
End Class"

            Dim expected =
"Class Program
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    ''' <returns></returns>
    Public Function Fizz(ByVal value As Integer) As Integer
        Return 0
    End Function
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesIllegalReturnsTagOnWriteOnlyProperty() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' [|<returns></returns>|]
    WriteOnly Property P As Integer
        Set(value As Integer)
        End Set
    End Property
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    WriteOnly Property P As Integer
        Set(value As Integer)
        End Set
    End Property
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesIllegalReturnsTagOnDeclareSub() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' [|<returns></returns>|]
    Declare Sub Goo Lib ""User"" ()
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    Declare Sub Goo Lib ""User"" ()
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesParamTag_NestedInSummaryTag() As Task
            Dim initial =
"Class Program
    ''' <summary>
    ''' <param name=""value""></param>
    ''' [|<param name=""value""></param>|]
    ''' </summary>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    ''' <param name=""value""></param>
    ''' </summary>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        Public Async Function RemovesParamTag_NestedInSummaryTag_WithChildren() As Task
            Dim initial =
"Class Program
    ''' <summary>
    '''   <param name=""value""></param>
    '''   [|<param name=""value"">
    '''     <xmlnode></xmlnode>
    '''   </param>|]
    ''' </summary>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Dim expected =
"Class Program
    ''' <summary>
    '''   <param name=""value""></param>
    ''' </summary>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class"

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllTypeparamInDocument_FixesDuplicateParamTags() As Task
            ' This fixes both because VB.NET has one diagnostic for all doc comment nodes with the same attributes

            Dim initial =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' {|FixAllInDocument:<typeparam name=""T""></typeparam>|}
    ''' <typeparam name=""U""></typeparam>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <returns></returns>
    Function Buzz(Of T, U)(value As Integer) As Integer
        Return 0
    End Function
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Dim expected =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <returns></returns>
    Function Buzz(Of T, U)(value As Integer) As Integer
        Return 0
    End Function
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllTypeparamInDocument_DoesNotFixIllegalReturnsOnSub() As Task
            ' This fixes both because VB.NET has one diagnostic for all doc comment nodes with the same attributes

            Dim initial =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' {|FixAllInDocument:<typeparam name=""T""></typeparam>|}
    ''' <typeparam name=""U""></typeparam>
    ''' <returns></returns>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <returns></returns>
    Function Buzz(Of T, U)(value As Integer) As Integer
        Return 0
    End Function
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Dim expected =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <returns></returns>
    Sub Fizz(Of T, U)(ByVal value As Integer)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <typeparam name=""T""></typeparam>
    ''' <typeparam name=""U""></typeparam>
    ''' <returns></returns>
    Function Buzz(Of T, U)(value As Integer) As Integer
        Return 0
    End Function
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument() As Task
            Dim initial =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' {|FixAllInDocument:<param name=""value""></param>|}
    Sub Fizz(ByVal value As Integer)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    ''' <returns></returns>
    Function Buzz(value As Integer) As Integer
        Return 0
    End Function
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Dim expected =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <returns></returns>
    Function Buzz(value As Integer) As Integer
        Return 0
    End Function
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInProject() As Task
            Dim initial =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' {|FixAllInProject:<param name=""value""></param>|}
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Dim expected =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document><![CDATA[
Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveDocCommentNode)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution() As Task
            Dim initial =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
            <![CDATA[Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' {|FixAllInSolution:<param name=""value""></param>|}
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
        <Document>
            <![CDATA[Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
            <![CDATA[Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Dim expected =
"<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <Document>
            <![CDATA[Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
        <Document>
            <![CDATA[Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly2"" CommonReferences=""true"" DocumentationMode=""Diagnose"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
            <![CDATA[Class Program
    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name=""value""></param>
    Sub Fizz(ByVal value As Integer)
    End Sub
End Class]]>
        </Document>
    </Project>
</Workspace>"

            Await TestAsync(initial, expected)
        End Function
    End Class
End Namespace
