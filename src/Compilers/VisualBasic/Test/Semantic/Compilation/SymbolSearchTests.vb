' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SymbolSearchTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestSymbolFilterNone()
            Assert.Throws(Of ArgumentException)(Sub()
                                                    Dim compilation = GetTestCompilation()
                                                    compilation.ContainsSymbolsWithName(Function(n) True, SymbolFilter.None)
                                                End Sub)

            Assert.Throws(Of ArgumentException)(Sub()
                                                    Dim compilation = GetTestCompilation()
                                                    compilation.GetSymbolsWithName(Function(n) True, SymbolFilter.None)
                                                End Sub)
        End Sub

        <Fact>
        Public Sub TestPredicateNull()
            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        Dim compilation = GetTestCompilation()
                                                        compilation.ContainsSymbolsWithName(Nothing)
                                                    End Sub)

            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        Dim compilation = GetTestCompilation()
                                                        compilation.GetSymbolsWithName(Nothing)
                                                    End Sub)
        End Sub

        <Fact>
        Public Sub TestMergedNamespace()
            Dim compilation = GetTestCompilation()
            Test(compilation, Function(n) n = "System", includeNamespace:=True, includeType:=False, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "System", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "System", includeNamespace:=True, includeType:=False, includeMember:=True, count:=1)
            Test(compilation, Function(n) n = "System", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            Test(compilation, Function(n) n = "System", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            Test(compilation, Function(n) n = "System", includeNamespace:=False, includeType:=True, includeMember:=False, count:=0)
            Test(compilation, Function(n) n = "System", includeNamespace:=False, includeType:=True, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestSourceNamespace()
            Dim compilation = GetTestCompilation()
            Test(compilation, Function(n) n = "MyNamespace", includeNamespace:=True, includeType:=False, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "MyNamespace", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "MyNamespace", includeNamespace:=True, includeType:=False, includeMember:=True, count:=1)
            Test(compilation, Function(n) n = "MyNamespace", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            Test(compilation, Function(n) n = "MyNamespace", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            Test(compilation, Function(n) n = "MyNamespace", includeNamespace:=False, includeType:=True, includeMember:=False, count:=0)
            Test(compilation, Function(n) n = "MyNamespace", includeNamespace:=False, includeType:=True, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestClassInMergedNamespace()
            Dim compilation = GetTestCompilation()
            Test(compilation, Function(n) n = "Test", includeNamespace:=False, includeType:=True, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "Test", includeNamespace:=False, includeType:=True, includeMember:=True, count:=1)
            Test(compilation, Function(n) n = "Test", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "Test", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            Test(compilation, Function(n) n = "Test", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            Test(compilation, Function(n) n = "Test", includeNamespace:=True, includeType:=False, includeMember:=False, count:=0)
            Test(compilation, Function(n) n = "Test", includeNamespace:=True, includeType:=False, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestClassInSourceNamespace()
            Dim compilation = GetTestCompilation()
            Test(compilation, Function(n) n = "Test1", includeNamespace:=False, includeType:=True, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "Test1", includeNamespace:=False, includeType:=True, includeMember:=True, count:=1)
            Test(compilation, Function(n) n = "Test1", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            Test(compilation, Function(n) n = "Test1", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            Test(compilation, Function(n) n = "Test1", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            Test(compilation, Function(n) n = "Test1", includeNamespace:=True, includeType:=False, includeMember:=False, count:=0)
            Test(compilation, Function(n) n = "Test1", includeNamespace:=True, includeType:=False, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestMembers()
            Dim compilation = GetTestCompilation()
            Test(compilation, Function(n) n = "myField", includeNamespace:=False, includeType:=False, includeMember:=True, count:=1)
            Test(compilation, Function(n) n = "myField", includeNamespace:=False, includeType:=True, includeMember:=True, count:=1)
            Test(compilation, Function(n) n = "myField", includeNamespace:=True, includeType:=False, includeMember:=True, count:=1)
            Test(compilation, Function(n) n = "myField", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            Test(compilation, Function(n) n = "myField", includeNamespace:=False, includeType:=True, includeMember:=False, count:=0)
            Test(compilation, Function(n) n = "myField", includeNamespace:=True, includeType:=False, includeMember:=False, count:=0)
            Test(compilation, Function(n) n = "myField", includeNamespace:=True, includeType:=True, includeMember:=False, count:=0)
        End Sub

        <Fact>
        Public Sub TestPartialSearch()
            Dim compilation = GetTestCompilation()

            Test(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=False, includeType:=False, includeMember:=True, count:=4)
            Test(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=False, includeType:=True, includeMember:=False, count:=4)
            Test(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=False, includeType:=True, includeMember:=True, count:=8)
            Test(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=False, includeMember:=False, count:=1)
            Test(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=False, includeMember:=True, count:=5)
            Test(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=True, includeMember:=False, count:=5)
            Test(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=True, includeMember:=True, count:=9)
            Test(compilation, Function(n) n.IndexOf("enum", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=True, includeMember:=True, count:=2)
        End Sub

        Private Shared Function GetTestCompilation() As VisualBasicCompilation
            Dim source As String = <text>
Namespace System

    Public Class Test
    End Class
End Namespace

Namespace MyNamespace

    Public Class Test1
    End Class
End Namespace

Public Class [MyClass]

    Private myField As Integer

    Friend Property MyProperty As Integer

    Sub MyMethod()
    End Sub

    Public Event MyEvent As EventHandler

    Delegate Function MyDelegate(i As Integer) As String
End Class

Structure MyStruct
End Structure

Interface MyInterface
End Interface

Enum [Enum]
    EnumValue
End Enum
</text>.Value
            Return CreateCompilationWithMscorlib40({source})
        End Function

        Private Shared Sub Test(compilation As VisualBasicCompilation, predicate As Func(Of String, Boolean), includeNamespace As Boolean, includeType As Boolean, includeMember As Boolean, count As Integer)
            Dim filter = SymbolFilter.None
            filter = If(includeNamespace, filter Or SymbolFilter.Namespace, filter)
            filter = If(includeType, filter Or SymbolFilter.Type, filter)
            filter = If(includeMember, filter Or SymbolFilter.Member, filter)

            Assert.Equal(count > 0, compilation.ContainsSymbolsWithName(predicate, filter))
            Assert.Equal(count, compilation.GetSymbolsWithName(predicate, filter).Count())
        End Sub
    End Class
End Namespace
