' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EncapsulateField
    Public Class EncapsulateFieldCommandHandlerTests
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Sub PrivateField()
            Dim text = <File>
Class C
    Private foo$$ As Integer

    Sub bar()
        foo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private foo As Integer

    Public Property Foo1 As Integer
        Get
            Return foo
        End Get
        Set(value As Integer)
            foo = value
        End Set
    End Property

    Sub bar()
        Foo1 = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Using state = New EncapsulateFieldTestState(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Sub NonPrivateField()
            Dim text = <File>
Class C
    Protected foo$$ As Integer

    Sub bar()
        foo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private _foo As Integer

    Protected Property Foo As Integer
        Get
            Return _foo
        End Get
        Set(value As Integer)
            _foo = value
        End Set
    End Property

    Sub bar()
        Foo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Using state = New EncapsulateFieldTestState(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Sub

        <WorkItem(1086632)>
        <Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Sub EncapsulateTwoFields()
            Dim text = "
Class Program
    [|Shared A As Integer = 1
    Shared B As Integer = A|]

    Sub Main(args As String())
        System.Console.WriteLine(A)
        System.Console.WriteLine(B)
    End Sub
End Class
"
            Dim expected = "
Class Program
    Shared A As Integer = 1
    Shared B As Integer = A1

    Public Shared Property A1 As Integer
        Get
            Return A
        End Get
        Set(value As Integer)
            A = value
        End Set
    End Property

    Public Shared Property B1 As Integer
        Get
            Return B
        End Get
        Set(value As Integer)
            B = value
        End Set
    End Property

    Sub Main(args As String())
        System.Console.WriteLine(A1)
        System.Console.WriteLine(B1)
    End Sub
End Class
"

            Using state = New EncapsulateFieldTestState(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Sub
    End Class
End Namespace
