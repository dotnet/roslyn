' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class CrefCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function CreateCompletionProvider() As ICompletionProvider
            Return New CrefCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotOutsideCref()
            Dim text = <File>
Class C
    ''' $$
    Sub Foo()
    End Sub
End Class
</File>.Value

            VerifyNoItemsExist(text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotOutsideCref2()
            Dim text = <File>
Class C
    $$
    Sub Foo()
    End Sub
End Class
</File>.Value

            VerifyNoItemsExist(text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotOutsideCref3()
            Dim text = <File>
Class C
    Sub Foo()
        Me.$$
    End Sub
End Class
</File>.Value

            VerifyNoItemsExist(text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterCrefOpenQuote()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="$$
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            VerifyAnyItemExists(text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RightSideOfQualifiedName()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program.$$"
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            VerifyItemExists(text, "Foo()")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInTypeParameterContext()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of $$
''' </summary>
Class Program(Of T)
    Sub Foo()
    End Sub
End Class]]></File>.Value

            VerifyItemIsAbsent(text, "Integer")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InSignature_FirstParameter()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo($$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer)
    End Sub
End Class]]></File>.Value

            VerifyItemExists(text, "Integer")
            VerifyItemIsAbsent(text, "Foo(Integer")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InSignature_SecondParameter()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo(Integer, $$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            VerifyItemExists(text, "Integer")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotAfterSignature()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo(Integer, Integer)$$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            VerifyNoItemsExist(text)
        End Sub
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotAfterDotAfterSignature()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo(Integer, Integer).$$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            VerifyNoItemsExist(text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MethodParametersIncluded()
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="Program(Of T).$$"
''' </summary>
Class Program(Of T)
    Sub Foo(ByRef z As Integer, ByVal x As Integer, ParamArray xx As Integer())
    End Sub
End Class]]></File>.Value

            VerifyItemExists(text, "Foo(ByRef Integer, Integer, Integer())")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TypesSuggestedWithTypeParameters()
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="$$"
''' </summary>
Class Program(Of TTypeParameter)
End Class

Class Program
End Class]]></File>.Value

            VerifyItemExists(text, "Program")
            VerifyItemExists(text, "Program(Of TTypeParameter)")
        End Sub
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub Operators()
            Dim text = <File><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Class C
    ''' <summary>
    '''  <see cref="<see cref="C.$$"
    ''' </summary>
    ''' <param name="c"></param>
    ''' <returns></returns>
    Public Shared Operator +(c As C)

    End Operator
End Class]]></File>.Value

            VerifyItemExists(text, "Operator +(C)")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ModOperator()
            Dim text = <File><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Class C
    ''' <summary>
    '''  <see cref="<see cref="C.$$"
    ''' </summary>
    ''' <param name="c"></param>
    ''' <returns></returns>
    Public Shared Operator Mod (c As C, a as Integer)

    End Operator
End Class]]></File>.Value

            VerifyItemExists(text, "Operator Mod(C, Integer)")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConstructorsShown()
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
    Sub New(x as Integer)
    End Sub
End Class
]]></File>.Value

            VerifyItemExists(text, "New(Integer)")
        End Sub
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterNamespace()
            Dim text = <File><![CDATA[
Imports System
''' <summary>
''' <see cref="System.$$"/>
''' </summary>
Class C
    Sub New(x as Integer)
    End Sub
End Class
]]></File>.Value

            VerifyItemExists(text, "String")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ParameterizedProperties()
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
    Public Property Item(x As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Property Item(x As Integer, y As String) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
 

]]></File>.Value

            VerifyItemExists(text, "Item(Integer)")
            VerifyItemExists(text, "Item(Integer, String)")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoIdentifierEscaping()
            Dim text = <File><![CDATA[
''' <see cref="A.$$"/>
''' </summary>
Class A
End Class

]]></File>.Value

            VerifyItemExists(text, "GetType()")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCommitOnParen()
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Dim expected = <File><![CDATA[
''' <summary>
''' <see cref="C."/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            VerifyProviderCommit(text, "bar(Integer, Integer)", expected, "("c, "bar")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AllowTypingTypeParameters()
            Dim text = <File><![CDATA[
Imports System.Collections.Generic
''' <summary>
''' <see cref="$$"/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Dim expected = <File><![CDATA[
Imports System.Collections.Generic
''' <summary>
''' <see cref=""/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            VerifyProviderCommit(text, "List(Of T)", expected, " "c, "List(Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OfAfterParen()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Foo($$
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            VerifyItemExists(text, "Of")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OfNotAfterComma()
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Foo(a, $$
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            VerifyItemIsAbsent(text, "Of")
        End Sub
    End Class
End Namespace
