' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

            Assert.Throws(Of ArgumentException)(Sub()
                                                    Dim compilation = GetTestCompilation()
                                                    compilation.ContainsSymbolsWithName("", SymbolFilter.None)
                                                End Sub)

            Assert.Throws(Of ArgumentException)(Sub()
                                                    Dim compilation = GetTestCompilation()
                                                    compilation.GetSymbolsWithName("", SymbolFilter.None)
                                                End Sub)
        End Sub

        <Fact>
        Public Sub TestPredicateNull()
            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        Dim compilation = GetTestCompilation()
                                                        compilation.ContainsSymbolsWithName(predicate:=Nothing)
                                                    End Sub)

            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        Dim compilation = GetTestCompilation()
                                                        compilation.GetSymbolsWithName(predicate:=Nothing)
                                                    End Sub)
        End Sub

        <Fact>
        Public Sub TestStringNull()
            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        Dim compilation = GetTestCompilation()
                                                        compilation.ContainsSymbolsWithName(name:=Nothing)
                                                    End Sub)

            Assert.Throws(Of ArgumentNullException)(Sub()
                                                        Dim compilation = GetTestCompilation()
                                                        compilation.GetSymbolsWithName(name:=Nothing)
                                                    End Sub)
        End Sub

        <Fact>
        Public Sub TestMergedNamespace()
            Dim compilation = GetTestCompilation()
            TestNameAndPredicate(compilation, "System", includeNamespace:=True, includeType:=False, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "System", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "System", includeNamespace:=True, includeType:=False, includeMember:=True, count:=1)
            TestNameAndPredicate(compilation, "System", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            TestNameAndPredicate(compilation, "System", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            TestNameAndPredicate(compilation, "System", includeNamespace:=False, includeType:=True, includeMember:=False, count:=0)
            TestNameAndPredicate(compilation, "System", includeNamespace:=False, includeType:=True, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestSourceNamespace()
            Dim compilation = GetTestCompilation()
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace:=True, includeType:=False, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace:=True, includeType:=False, includeMember:=True, count:=1)
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace:=False, includeType:=True, includeMember:=False, count:=0)
            TestNameAndPredicate(compilation, "MyNamespace", includeNamespace:=False, includeType:=True, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestClassInMergedNamespace()
            Dim compilation = GetTestCompilation()
            TestNameAndPredicate(compilation, "Test", includeNamespace:=False, includeType:=True, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "Test", includeNamespace:=False, includeType:=True, includeMember:=True, count:=1)
            TestNameAndPredicate(compilation, "Test", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "Test", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            TestNameAndPredicate(compilation, "Test", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            TestNameAndPredicate(compilation, "Test", includeNamespace:=True, includeType:=False, includeMember:=False, count:=0)
            TestNameAndPredicate(compilation, "Test", includeNamespace:=True, includeType:=False, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestClassInSourceNamespace()
            Dim compilation = GetTestCompilation()
            TestNameAndPredicate(compilation, "Test1", includeNamespace:=False, includeType:=True, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "Test1", includeNamespace:=False, includeType:=True, includeMember:=True, count:=1)
            TestNameAndPredicate(compilation, "Test1", includeNamespace:=True, includeType:=True, includeMember:=False, count:=1)
            TestNameAndPredicate(compilation, "Test1", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            TestNameAndPredicate(compilation, "Test1", includeNamespace:=False, includeType:=False, includeMember:=True, count:=0)
            TestNameAndPredicate(compilation, "Test1", includeNamespace:=True, includeType:=False, includeMember:=False, count:=0)
            TestNameAndPredicate(compilation, "Test1", includeNamespace:=True, includeType:=False, includeMember:=True, count:=0)
        End Sub

        <Fact>
        Public Sub TestMembers()
            Dim compilation = GetTestCompilation()
            TestNameAndPredicate(compilation, "myField", includeNamespace:=False, includeType:=False, includeMember:=True, count:=1)
            TestNameAndPredicate(compilation, "myField", includeNamespace:=False, includeType:=True, includeMember:=True, count:=1)
            TestNameAndPredicate(compilation, "myField", includeNamespace:=True, includeType:=False, includeMember:=True, count:=1)
            TestNameAndPredicate(compilation, "myField", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)

            TestNameAndPredicate(compilation, "myField", includeNamespace:=False, includeType:=True, includeMember:=False, count:=0)
            TestNameAndPredicate(compilation, "myField", includeNamespace:=True, includeType:=False, includeMember:=False, count:=0)
            TestNameAndPredicate(compilation, "myField", includeNamespace:=True, includeType:=True, includeMember:=False, count:=0)
        End Sub

        <Fact>
        Public Sub TestCaseInsensitivity()
            Dim compilation = GetTestCompilation()
            TestName(compilation, "system", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)
            TestName(Compilation, "mynamespace", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)
            TestName(Compilation, "test", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)
            TestName(Compilation, "test1", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)
            TestName(Compilation, "myfield", includeNamespace:=True, includeType:=True, includeMember:=True, count:=1)
        End Sub

        <Fact>
        Public Sub TestPartialSearch()
            Dim compilation = GetTestCompilation()

            TestPredicate(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=False, includeType:=False, includeMember:=True, count:=4)
            TestPredicate(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=False, includeType:=True, includeMember:=False, count:=4)
            TestPredicate(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=False, includeType:=True, includeMember:=True, count:=8)
            TestPredicate(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=False, includeMember:=False, count:=1)
            TestPredicate(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=False, includeMember:=True, count:=5)
            TestPredicate(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=True, includeMember:=False, count:=5)
            TestPredicate(compilation, Function(n) n.IndexOf("my", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=True, includeMember:=True, count:=9)
            TestPredicate(compilation, Function(n) n.IndexOf("enum", StringComparison.OrdinalIgnoreCase) >= 0, includeNamespace:=True, includeType:=True, includeMember:=True, count:=2)
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

        Private Shared Sub TestNameAndPredicate(compilation As VisualBasicCompilation, name As String, includeNamespace As Boolean, includeType As Boolean, includeMember As Boolean, count As Integer)
            TestName(compilation, name, includeNamespace, includeType, includeMember, count)
            TestPredicate(compilation, Function(n) n = name, includeNamespace, includeType, includeMember, count)
        End Sub

        Private Shared Sub TestName(compilation As VisualBasicCompilation, name As String, includeNamespace As Boolean, includeType As Boolean, includeMember As Boolean, count As Integer)
            Dim filter = ComputeFilter(includeNamespace, includeType, includeMember)

            Assert.Equal(count > 0, compilation.ContainsSymbolsWithName(name, filter))
            Assert.Equal(count, compilation.GetSymbolsWithName(name, filter).Count())
        End Sub

        Private Shared Sub TestPredicate(compilation As VisualBasicCompilation, predicate As Func(Of String, Boolean), includeNamespace As Boolean, includeType As Boolean, includeMember As Boolean, count As Integer)
            Dim filter = ComputeFilter(includeNamespace, includeType, includeMember)

            Assert.Equal(count > 0, compilation.ContainsSymbolsWithName(predicate, filter))
            Assert.Equal(count, compilation.GetSymbolsWithName(predicate, filter).Count())
        End Sub

        Private Shared Function ComputeFilter(includeNamespace As Boolean, includeType As Boolean, includeMember As Boolean) As SymbolFilter
            Dim filter = SymbolFilter.None
            filter = If(includeNamespace, filter Or SymbolFilter.Namespace, filter)
            filter = If(includeType, filter Or SymbolFilter.Type, filter)
            filter = If(includeMember, filter Or SymbolFilter.Member, filter)
            Return filter
        End Function
    End Class
End Namespace
