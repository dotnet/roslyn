' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class CompletionListTagCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_EnumTypeDotMemberAlways()
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As Color = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
''' <completionlist cref="Color"/>
Public Class Color
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Always)>
    Public Shared X as Integer = 3
    Public Shared Y as Integer = 4
End Class

]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Color.X",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_EnumTypeDotMemberNever()
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As Color = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
 ''' <completionlist cref="Color"/>
Public Class Color
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Shared X as Integer = 3
    Public Shared Y as Integer = 4
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Color.X",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EditorBrowsable_EnumTypeDotMemberAdvanced()
            Dim markup = <Text><![CDATA[
Class P
    Sub S()
        Dim d As Color = $$
    End Sub
End Class</a>
]]></Text>.Value
            Dim referencedCode = <Text><![CDATA[
''' <completionlist cref="Color"/>
Public Class Color
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)>
    Public Shared X as Integer = 3
    Public Shared Y as Integer = 4
End Class
]]></Text>.Value
            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Color.X",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=0,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=True)

            VerifyItemInEditorBrowsableContexts(
                markup:=markup,
                referencedCode:=referencedCode,
                item:="Color.X",
                expectedSymbolsSameSolution:=1,
                expectedSymbolsMetadataReference:=1,
                sourceLanguage:=LanguageNames.VisualBasic,
                referencedLanguage:=LanguageNames.VisualBasic,
                hideAdvancedMembers:=False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TriggeredOnOpenParen()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        ' type after this line
        Bar($$
    End Sub
 
    Sub Bar(f As Color)
    End Sub
End Module
 
''' <completionlist cref="Color"/>
Public Class Color
    Public Shared X as Integer = 3
    Public Shared Property Y as Integer = 4
End Class

]]></Text>.Value

            VerifyItemExists(markup, "Color.X", usePreviousCharAsTrigger:=True)
            VerifyItemExists(markup, "Color.Y", usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RightSideOfAssignment()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x as Color
        x = $$
    End Sub
End Module
 
''' <completionlist cref="Color"/>
Public Class Color
    Public Shared X as Integer = 3
    Public Shared Property Y as Integer = 4
End Class
]]></Text>.Value

            VerifyItemExists(markup, "Color.X", usePreviousCharAsTrigger:=True)
            VerifyItemExists(markup, "Color.Y", usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotCrashInObjectInitializer()
            Dim markup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim z = New Foo() With {.z$$ }
    End Sub

    Class Foo
        Property A As Integer
            Get

            End Get
            Set(value As Integer)

            End Set
        End Property
    End Class
End Module
]]></Text>.Value

            VerifyNoItemsExist(markup)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InYieldReturn()
            Dim markup = <Text><![CDATA[
Imports System
Imports System.Collections.Generic

''' <completionlist cref="Color"/>
Public Class Color
    Public Shared X as Integer = 3
    Public Shared Property Y as Integer = 4
End Class


Class C
    Iterator Function M() As IEnumerable(Of Color)
        Yield $$
    End Function
End Class
]]></Text>.Value

            VerifyItemExists(markup, "Color.X")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InAsyncMethodReturnStatement()
            Dim markup = <Text><![CDATA[
Imports System.Threading.Tasks

''' <completionlist cref="Color"/>
Public Class Color
    Public Shared X as Integer = 3
    Public Shared Property Y as Integer = 4
End Class
Class C
    Async Function M() As Task(Of Color)
        Await Task.Delay(1)
        Return $$
    End Function
End Class
]]></Text>.Value

            VerifyItemExists(markup, "Color.X")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InIndexedProperty()
            Dim markup = <Text><![CDATA[
Module Module1

''' <completionlist cref="Color"/>
Public Class Color
    Public Shared X as Integer = 3
    Public Shared Property Y as Integer = 4
End Class

    Public Class MyClass1
        Public WriteOnly Property MyProperty(ByVal val1 As Color) As Boolean
            Set(ByVal value As Boolean)

            End Set
        End Property

        Public Sub MyMethod(ByVal val1 As Color)

        End Sub
    End Class

    Sub Main()
        Dim var As MyClass1 = New MyClass1
        ' MARKER
        var.MyMethod(Color.X)
        var.MyProperty($$Color.Y) = True
    End Sub

End Module
]]></Text>.Value

            VerifyItemExists(markup, "Color.Y")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FullyQualified()
            Dim markup = <Text><![CDATA[
Namespace ColorNamespace
    ''' <completionlist cref="Color"/>
    Public Class Color
        Public Shared X as Integer = 3
        Public Shared Property Y as Integer = 4
    End Class
End Namespace

Class C
    Public Sub M(day As ColorNamespace.Color)
        M($$)
    End Sub

End Class
]]></Text>.Value
            VerifyItemExists(markup, "ColorNamespace.Color.X", glyph:=CType(Glyph.EnumMember, Integer))
            VerifyItemExists(markup, "ColorNamespace.Color.Y", glyph:=CType(Glyph.EnumMember, Integer))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TriggeredForNamedArgument()
            Dim markup = <Text><![CDATA[
Class C
    Public Sub M(day As Color)
        M(day:=$$)
    End Sub
''' <completionlist cref="Color"/>
Public Class Color
    Public Shared X as Integer = 3
    Public Shared Property Y as Integer = 4
End Class

End Class
]]></Text>.Value
            VerifyItemExists(markup, "Color.X", usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInObjectCreation()
            Dim markup = <Text><![CDATA[
''' <completionlist cref="Program"/>
Class Program
    Public Shared Foo As Integer

    Sub Main(args As String())
        Dim p As Program = New $$
    End Sub
End Class
]]></Text>.Value
            VerifyItemIsAbsent(markup, "Program.Foo")
        End Sub

        <WorkItem(954694)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AnyAccessibleMember()
            Dim markup = <Text><![CDATA[
Public Class Program
     Private Shared field1 As Integer
 
    ''' <summary>
    ''' </summary>
    ''' <completionList cref="Program"></completionList>
    Public Class Program2
        Public Sub M()
            Dim obj As Program2 =$$
        End Sub
    End Class
End Class
]]></Text>.Value
            VerifyItemExists(markup, "Program.field1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(815963)>
        Public Sub LocalNoAs()
            Dim markup = <Text><![CDATA[
Enum E
    A
End Enum
 
Class C
    Sub M()
        Const e As E = e$$
    End Sub
End Class
]]></Text>.Value
            VerifyItemIsAbsent(markup, "e As E")
        End Sub

        <WorkItem(3518, "https://github.com/dotnet/roslyn/issues/3518")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInTrivia()
            Dim markup = <Text><![CDATA[
Class C
    Sub Test()
        M(Type2.A)
        ' $$
    End Sub

    Private Sub M(a As Type1)
        Throw New NotImplementedException()
    End Sub
End Class
''' <completionlist cref="Type2"/>
Public Class Type1
End Class

Public Class Type2
    Public Shared A As Type1
    Public Shared B As Type1
End Class
]]></Text>.Value
            VerifyNoItemsExist(markup)
        End Sub

        <WorkItem(3518, "https://github.com/dotnet/roslyn/issues/3518")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotAfterInvocationWithCompletionListTagTypeAsFirstParameter()
            Dim markup = <Text><![CDATA[
Class C
    Sub Test()
        M(Type2.A)
        $$
    End Sub

    Private Sub M(a As Type1)
        Throw New NotImplementedException()
    End Sub
End Class
''' <completionlist cref="Type2"/>
Public Class Type1
End Class

Public Class Type2
    Public Shared A As Type1
    Public Shared B As Type1
End Class
]]></Text>.Value
            VerifyNoItemsExist(markup)
        End Sub


        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New CompletionListTagCompletionProvider()
        End Function
    End Class
End Namespace
