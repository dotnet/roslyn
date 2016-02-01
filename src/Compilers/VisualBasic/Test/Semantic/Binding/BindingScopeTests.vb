' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    ' Test that binding APIs that take character positions determine the edges of language
    ' constructs as we expect. We just use LookupNames as the test API, since all these APIs use the
    ' same underlying helpers to find the Binder object used to answer the questions.
    Public Class BindingScopeTests
        Private Sub CheckScopeOfSymbol(comp As VisualBasicCompilation,
                                       treeName As String,
                                       symbolName As String,
                                       expectedScope As XElement)
            Dim tree As SyntaxTree = CompilationUtils.GetTree(comp, treeName)
            Dim treeText As String = tree.GetText().ToString()
            Dim expectedScopeText As String = expectedScope.Value.Replace(vbLf, vbCrLf)
            Dim expectedStart As Integer = treeText.IndexOf(expectedScopeText, StringComparison.Ordinal)
            Assert.True(expectedStart >= 0, "did not found expectedScope")
            Dim expectedEnd As Integer = expectedStart + expectedScopeText.Length

            Dim semanticModel = comp.GetSemanticModel(tree)

            For position As Integer = 0 To treeText.Length - 1
                Dim names = semanticModel.LookupNames(position)
                Dim found = names.Contains(symbolName)
                Dim expectedToFind = (position >= expectedStart AndAlso position < expectedEnd)
                If found <> expectedToFind Then
                    Dim locationText = If(treeText.Length > position + 50, treeText.Substring(position, 50), treeText.Substring(position))
                    If expectedToFind Then
                        Assert.True(found, String.Format("Should have been in scope at position {0} text '{1}...'", position, locationText))
                    Else
                        Assert.False(found, String.Format("Should have been out of scope at position {0} text '{1}...'", position, locationText))
                    End If
                End If
            Next
        End Sub

        <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
        Public Sub TestScopes1()
            Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Compilation">
        <file name="a.vb">
            Option Strict On

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
        </file>
    </compilation>)

            CheckScopeOfSymbol(comp, "a.vb", "alpha",
<expected>Public Class Apple(Of TBravo)
                    Public alpha As Integer 'alpha
                End Class 'hello</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "TBravo",
<expected>Public Class Apple(Of TBravo)
                    Public alpha As Integer 'alpha
                End Class 'hello</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "Apple",
<expected>
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
            End Namespace</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "Bar",
<expected>
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
            End Namespace</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "Delta",
<expected>
                    'Delta
                    Delegate Sub Delta(Of TCharlie _
                              )(x As Integer)
                    ' after delta
                End Namespace</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "TCharlie",
<expected>Delegate Sub Delta(Of TCharlie _
                              )(x As Integer)</expected>)
        End Sub

        <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
        Public Sub TestScopes2()
            Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Compilation">
        <file name="a.vb">
            Option Strict On

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
        </file>
    </compilation>)

            CheckScopeOfSymbol(comp, "a.vb", "TRind",
<expected>Public Sub Mango(Of TRind)(yellow as Integer) 'zzz
                        Dim red As String  'yyy
                        'before end sub
                    End Sub  ' xxx</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "yellow",
<expected>
                        Dim red As String  'yyy
                        'before end sub
                    End Sub  ' xxx</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "red",
<expected>
                        Dim red As String  'yyy
                        'before end sub
                    End Sub  ' xxx</expected>)

        End Sub

        <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
        Public Sub TestScopes3()
            Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Compilation">
        <file name="a.vb">
Public Class Apple(Of TBravo)
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
End Class 'hello
        </file>
    </compilation>)

            CheckScopeOfSymbol(comp, "a.vb", "cheetos",
<expected>
             ' inside for 1
             Dim cheetos as String 'xxx
             ' inside for 2
        Next color 'zzz</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "freetos",
<expected>Then Dim freetos As Integer : Dim chips As String </expected>)

            CheckScopeOfSymbol(comp, "a.vb", "chips",
<expected>Then Dim freetos As Integer : Dim chips As String </expected>)

            CheckScopeOfSymbol(comp, "a.vb", "water",
<expected>Else Dim water As Integer : Dim fire as String 'if statement</expected>)

            CheckScopeOfSymbol(comp, "a.vb", "fire",
<expected>Else Dim water As Integer : Dim fire as String 'if statement</expected>)

        End Sub

        <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
        Public Sub TestScopes4()
            Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Compilation">
        <file name="a.vb">
Public Class Apple(Of TBravo)
    Public Sub Mango(Of TRind)(yellow as Integer) 
        Dim red As String  
        While True 'yyy
            Dim cheetos as String 'xxx

        'hello there
    End Sub  
End Class 'hello
        </file>
    </compilation>)

            CheckScopeOfSymbol(comp, "a.vb", "cheetos",
<expected>
            Dim cheetos as String 'xxx

        'hello there
    </expected>)

        End Sub

        <Fact(), WorkItem(546396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546396")>
        Public Sub TestPropertyScopes()
            Dim comp As VisualBasicCompilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
    <compilation name="Compilation">
        <file name="c.vb">
Class C
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
End Class
        </file>
    </compilation>)

            ' Set value argument should be in scope for the body of the accessor.
            CheckScopeOfSymbol(comp, "c.vb", "val",
        <expected>
            _p = val
        End Set</expected>)

            ' Property parameters should be in scope for individual accessors.
            ' Note: The parameter is represented as separate symbols in the property and
            ' the accessors so that the ContainingSymbol property on the parameter symbol
            ' always refers to the immediately enclosing container (property or accessor).
            CheckScopeOfSymbol(comp, "c.vb", "i",
        <expected>
            Return _q
        End Get</expected>)
            CheckScopeOfSymbol(comp, "c.vb", "y",
        <expected>
            _r = Value
        End Set</expected>)

        End Sub

    End Class
End Namespace
