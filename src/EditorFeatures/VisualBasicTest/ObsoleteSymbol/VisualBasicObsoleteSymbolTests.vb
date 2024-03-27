' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.ObsoleteSymbol

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ObsoleteSymbol
    Public Class VisualBasicObsoleteSymbolTests
        Inherits AbstractObsoleteSymbolTests

        Protected Overrides Function CreateWorkspace(markup As String) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(markup)
        End Function

        <Theory>
        <InlineData("Class")>
        <InlineData("Structure")>
        <InlineData("Interface")>
        <InlineData("Module")>
        <InlineData("Enum")>
        Public Async Function TestObsoleteTypeDefinition(keyword As String) As Task
            Await TestAsync(
                $"
                <System.Obsolete>
                {keyword} [|ObsoleteType|]
                End {keyword}

                {keyword} NonObsoleteType
                End {keyword}
                ")
        End Function

        <Fact>
        Public Async Function TestObsoleteDelegateTypeDefinition() As Task
            Await TestAsync(
                "
                <System.Obsolete>
                Delegate Sub [|ObsoleteType|]()

                Delegate Sub NonObsoleteType()
                ")
        End Function

        <Fact>
        Public Async Function TestDeclarationAndUseOfObsoleteAlias() As Task
            Await TestAsync(
                "
                Imports [|ObsoleteAlias|] = [|ObsoleteType|]

                <System.Obsolete>
                Class [|ObsoleteType|]
                End Class

                ''' <seealso cref=""[|ObsoleteType|]""/>
                ''' <seealso cref=""[|ObsoleteAlias|]""/>
                Class NonObsoleteType
                    Dim field As [|ObsoleteAlias|] = New [|ObsoleteType|]()
                End Class
                ")
        End Function

        <Fact>
        Public Async Function TestParametersAndReturnTypes() As Task
            Await TestAsync(
                "
                <System.Obsolete>
                Class [|ObsoleteType|]
                End Class

                Class NonObsoleteType
                    Function Method(arg As [|ObsoleteType|]) As [|ObsoleteType|]
                        Return New [|ObsoleteType|]()
                    End Function

                    Dim field As System.Func(Of [|ObsoleteType|], [|ObsoleteType|]) = Function(arg As [|ObsoleteType|]) New [|ObsoleteType|]()
                End Class
                ")
        End Function

        <Fact>
        Public Async Function TestImplicitType() As Task
            Await TestAsync(
                "
                <System.Obsolete>
                Class [|ObsoleteType|]
                    Public Sub New()
                    End Sub

                    <System.Obsolete>
                    Public Sub New(x As Integer)
                    End Sub
                End Class

                Class ObsoleteCtor
                    Public Sub New()
                    End Sub

                    <System.Obsolete>
                    Public Sub New(x As Integer)
                    End Sub
                End Class

                Class NonObsoleteType
                    Sub Method()
                        Dim t1 As New [|ObsoleteType|]()
                        Dim t2 As [|New|] [|ObsoleteType|](3)
                        [|Dim|] t3 = New [|ObsoleteType|]()
                        [|Dim|] t4 = [|New|] [|ObsoleteType|](3)
                        Dim t5 As [|ObsoleteType|] = New [|ObsoleteType|]()
                        Dim t6 As [|ObsoleteType|] = [|New|] [|ObsoleteType|](3)
                        [|Dim|] t7 = CreateObsoleteType()
                        Dim t8 = NameOf([|ObsoleteType|])

                        Dim u1 As New ObsoleteCtor()
                        Dim u2 As [|New|] ObsoleteCtor(3)
                        Dim u3 = New ObsoleteCtor()
                        Dim u4 = [|New|] ObsoleteCtor(3)
                        Dim u5 As ObsoleteCtor = New ObsoleteCtor()
                        Dim u6 As ObsoleteCtor = [|New|] ObsoleteCtor(3)
                        Dim u8 = NameOf(ObsoleteCtor)
                    End Sub

                    Function CreateObsoleteType() As [|ObsoleteType|]
                        Return New [|ObsoleteType|]()
                    End Function
                End Class
                ")
        End Function

        <Fact>
        Public Async Function TestDeclarators() As Task
            Await TestAsync(
                "
                <System.Obsolete>
                Class [|ObsoleteType|]
                End Class

                Class NonObsoleteType
                    Sub Method()
                        ' In this method, only t5 has an implicit type, but the Dim keyword applies to all declared
                        ' variables. Currently this feature does not analyze a Dim keyword when more than one variable
                        ' is declared.
                        Dim t1, t2 As New [|ObsoleteType|](), t3, t4 As [|ObsoleteType|], t5 = New [|ObsoleteType|]()
                    End Sub
                End Class
                ")
        End Function

        <Fact>
        Public Async Function TestExtensionMethods() As Task
            Await TestAsync(
                "
                <System.Obsolete>
                Module [|ObsoleteType|]
                    <System.Runtime.CompilerServices.Extension>
                    Public Shared Sub ObsoleteMember1(ignored As C)
                    End Sub

                    <System.Obsolete>
                    <System.Runtime.CompilerServices.Extension>
                    Public Shared Sub [|ObsoleteMember2|](ignored As C)
                    End Sub
                End Module

                Class C
                    Sub Method()
                        Me.ObsoleteMember1()
                        Me.[|ObsoleteMember2|]()
                        [|ObsoleteType|].ObsoleteMember1(Me)
                        [|ObsoleteType|].[|ObsoleteMember2|](Me)
                    End Sub
                End Class
                ")
        End Function

        <Fact>
        Public Async Function TestGenerics() As Task
            Await TestAsync(
                "
                <System.Obsolete>
                Class [|ObsoleteType|]
                End Class

                <System.Obsolete>
                Structure [|ObsoleteValueType|]
                End Structure

                Class G(Of T)
                End Class

                Class C
                    Sub M(Of T)()
                    End Sub

                    ''' <summary>
                    ''' Visual Basic, unlike C#, resolves concrete type names in generic argument positions in doc
                    ''' comment references.
                    ''' </summary>
                    ''' <seealso cref=""G(Of [|ObsoleteType|])""/>
                    Sub Method()
                        Dim x1 = New G(Of [|ObsoleteType|])()
                        Dim x2 = New G(Of G(Of [|ObsoleteType|]))()
                        M(Of [|ObsoleteType|])()
                        M(Of G(Of [|ObsoleteType|]))()
                        M(Of G(Of G(Of [|ObsoleteType|])))()

                        ' Mark 'Dim' as obsolete even when it points to Nullable(Of T) where T is obsolete
                        [|Dim|] nullableValue = CreateNullableValueType()
                    End Sub

                    Function CreateNullableValueType() As [|ObsoleteValueType|]?
                        Return New [|ObsoleteValueType|]()
                    End Function
                End Class
                ")
        End Function
    End Class
End Namespace
