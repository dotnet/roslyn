' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Namespace Semantics.Binding
        ' Test that binding APIs that take character positions determine the edges of language
        ' constructs as we expect. We just use LookupNames as the test API, since all these APIs use the
        ' same underlying helpers to find the Binder object used to answer the questions.
        Public Class BindingScopeTests
            Private Sub CheckScopeOfSymbol(
                                        comp As VisualBasicCompilation,
                                        treeName As String,
                                        symbolName As String,
                                        expectedScope As String
                                      )
                Dim tree As SyntaxTree = CompilationUtils.GetTree(comp, treeName)
                Dim treeText As String = tree.GetText().ToString()
                Dim expectedScopeText As String = expectedScope
                Dim expectedStart As Integer = treeText.IndexOf(expectedScopeText, StringComparison.Ordinal)
                Assert.True(expectedStart >= 0, "did not found " & expectedScope)
                Dim expectedEnd As Integer = expectedStart + expectedScopeText.Length

                Dim semanticModel = comp.GetSemanticModel(tree)

                For position As Integer = 0 To treeText.Length - 1
                    Dim names = semanticModel.LookupNames(position)
                    Dim found = names.Contains(symbolName)
                    Dim expectedToFind = (position >= expectedStart AndAlso position < expectedEnd)
                    If found <> expectedToFind Then
                        Dim locationText = If(treeText.Length > position + 50, treeText.Substring(position, 50), treeText.Substring(position))
                        If expectedToFind Then
                            Assert.True(found, $"Should have been in scope at position {position} text '{locationText}...'")
                        Else
                            Dim Delta0 = expectedStart - position
                            Dim Delta1 = expectedEnd - position
                            Assert.False(found, $"({Delta0},{Delta1}) Should have been out of scope at position {position} text '{locationText}...'")
                        End If
                    End If
                Next
            End Sub

            <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
            Public Sub TestScopes1()
                Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    Unit.Make("Compilation").
        With_a_vb(
"Option Strict On

Imports System
Imports System.Collections

Namespace Foo.Bar
    ' ClassA
    Public Class Apple(Of TBravo)
        Public alpha As Integer 'alpha
    End Class 'hello

    ' in between

    Namespace Baz
        'Delta
        Delegate Sub Delta(Of TCharlie _
                  )(x As Integer)
        ' after delta
    End Namespace

    'about to end Bar
End Namespace

' outside namespaces
"))

                CheckScopeOfSymbol(comp, "a.vb", "alpha",
"Public Class Apple(Of TBravo)
        Public alpha As Integer 'alpha
    End Class 'hello")

                CheckScopeOfSymbol(comp, "a.vb", "TBravo",
"Public Class Apple(Of TBravo)
        Public alpha As Integer 'alpha
    End Class 'hello")

                CheckScopeOfSymbol(comp, "a.vb", "Apple",
"
    ' ClassA
    Public Class Apple(Of TBravo)
        Public alpha As Integer 'alpha
    End Class 'hello

    ' in between

    Namespace Baz
        'Delta
        Delegate Sub Delta(Of TCharlie _
                  )(x As Integer)
        ' after delta
    End Namespace

    'about to end Bar
End Namespace")

                CheckScopeOfSymbol(comp, "a.vb", "Bar",
"
    ' ClassA
    Public Class Apple(Of TBravo)
        Public alpha As Integer 'alpha
    End Class 'hello

    ' in between

    Namespace Baz
        'Delta
        Delegate Sub Delta(Of TCharlie _
                  )(x As Integer)
        ' after delta
    End Namespace

    'about to end Bar
End Namespace")

                CheckScopeOfSymbol(comp, "a.vb", "Delta",
"
        'Delta
        Delegate Sub Delta(Of TCharlie _
                  )(x As Integer)
        ' after delta
    End Namespace")

                CheckScopeOfSymbol(comp, "a.vb", "TCharlie",
"Delegate Sub Delta(Of TCharlie _
                  )(x As Integer)")
            End Sub

            <Fact(), WorkItem(546396, "http:     //vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
            Public Sub TestScopes2()
                Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
Unit.Make("Compilation").With_a_vb(
"Option Strict On

Imports System
Imports System.Collections

Namespace Foo.Bar
    ' ClassA
    Public Class Apple(Of TBravo)
        ' this is before mango
        Public Sub Mango(Of TRind)(yellow as Integer) 'zzz
            Dim red As String  'yyy
            'before end sub
        End Sub  ' xxx
        ' this is after mango
    End Class 'hello
    'about to end Bar
End Namespace

' outside namespaces
"))

                CheckScopeOfSymbol(comp, "a.vb", "TRind",
"Public Sub Mango(Of TRind)(yellow as Integer) 'zzz
            Dim red As String  'yyy
            'before end sub
        End Sub  ' xxx")

                CheckScopeOfSymbol(comp, "a.vb", "yellow",
"
            Dim red As String  'yyy
            'before end sub
        End Sub  ' xxx")

                CheckScopeOfSymbol(comp, "a.vb", "red",
"
            Dim red As String  'yyy
            'before end sub
        End Sub  ' xxx")

            End Sub

            <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
            Public Sub TestScopes3()
                Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
Unit.Make("Compilation").With_a_vb(
"Public Class Apple(Of TBravo)
    Public Sub Mango(Of TRind)(yellow as Integer) 
        Dim red As String  
        ' before For
        For color = 1 to 10 'yyy
             ' inside for 1
             Dim cheetos as String 'xxx
             ' inside for 2
        Next color 'zzz
        'after for
        If True = False Then Dim freetos As Integer : Dim chips As String Else Dim water As Integer : Dim fire as String 'if statement
    End Sub  
End Class 'hello"))

                CheckScopeOfSymbol(comp, "a.vb", "cheetos",
"
             ' inside for 1
             Dim cheetos as String 'xxx
             ' inside for 2
        Next color 'zzz")

                CheckScopeOfSymbol(comp, "a.vb", "freetos",
"Then Dim freetos As Integer : Dim chips As String ")

                CheckScopeOfSymbol(comp, "a.vb", "chips",
"Then Dim freetos As Integer : Dim chips As String ")

                CheckScopeOfSymbol(comp, "a.vb", "water",
"Else Dim water As Integer : Dim fire as String 'if statement")

                CheckScopeOfSymbol(comp, "a.vb", "fire",
"Else Dim water As Integer : Dim fire as String 'if statement")

            End Sub

            <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
            Public Sub TestScopes4()
                Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
Unit.Make("Compilation").With_a_vb(
"Public Class Apple(Of TBravo)
    Public Sub Mango(Of TRind)(yellow as Integer) 
        Dim red As String  
        While True 'yyy
            Dim cheetos as String 'xxx

        'hello there
    End Sub  
End Class 'hello"))

                CheckScopeOfSymbol(comp, "a.vb", "cheetos",
"
            Dim cheetos as String 'xxx

        'hello there
    ")

            End Sub

            <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
            Public Sub TestPropertyScopes()
                Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
Unit.Make("Compilation").WithFile("c.vb",
"Class C
    Private _p
    Private _q
    Private _r
    Property P
        Get
            Return _p
        End Get
        Set(val)
            _p = val
        End Set
    End Property
    ReadOnly Property Q(i As Integer)
        Get
            Return _q
        End Get
    End Property
    WriteOnly Property R(x, y)
        Set
            _r = Value
        End Set
    End Property
End Class"))

                ' Set value argument should be in scope for the body of the accessor.
                CheckScopeOfSymbol(comp, "c.vb", "val",
        "
            _p = val
        End Set")

                ' Property parameters should be in scope for individual accessors.
                ' Note: The parameter is represented as separate symbols in the property and
                ' the accessors so that the ContainingSymbol property on the parameter symbol
                ' always refers to the immediately enclosing container (property or accessor).
                CheckScopeOfSymbol(comp, "c.vb", "i",
        "
            Return _q
        End Get")
                CheckScopeOfSymbol(comp, "c.vb", "y",
        "
            _r = Value
        End Set")

            End Sub

        End Class
    End Namespace
End Namespace