' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Basic.Reference.Assemblies

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SymbolId

    Partial Public Class SymbolIdTest
        Inherits SymbolKeyTestBase

#Region "Metadata vs. Source"

        <Fact>
        Public Sub M2SNamedTypeSymbols01()

            Dim src1 = <compilation name="M2SNamedTypeSymbols01">
                           <file name="a.vb">
Imports System

Public Delegate Sub D(p1 As Integer, p2 As String)

Namespace N1.N2
    Public Interface I
    End Interface

    Namespace N3
        Public Class C
            Public Structure S

                Public Enum E
                    Zero
                    One
                    Two
                End Enum

                Public Sub M(n As Integer)
                    Console.WriteLine(n)
                End Sub
            End Structure
        End Class

    End Namespace
End Namespace
                   </file>
                       </compilation>

            Dim src2 = <compilation name="M2SNamedTypeSymbols11">
                           <file name="b.vb">
Imports System
Imports N1.N2.N3

Public Class App
    Inherits C

    Private Event myEvent As D

    Friend Property PRop As N1.N2.I

    Default Protected Property Item(x As Integer) As C.S.e
        Set(value As C.S.E)
        End Set
    End Property

    Public Sub M(s As C.S)
        s.M(123)
    End Sub
End Class
                        </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1)
            ' Compilation to Compilation
            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(src2, {comp1.ToMetadataReference()})

            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.DeclaredType).OrderBy(Function(s) s.Name).ToList()
            Assert.Equal(5, originalSymbols.Count)
            ' ---------------------------
            ' Metadata symbols
            Dim typesym = TryCast(comp2.SourceModule.GlobalNamespace.GetTypeMembers("App").FirstOrDefault(), INamedTypeSymbol)
            ' NYI 'D'
            Dim mtSym01 = (TryCast(typesym.GetMembers("myEvent").[Single](), IEventSymbol)).Type
            ' 'I'
            Dim mtSym02 = (TryCast(typesym.GetMembers("Prop").[Single](), IPropertySymbol)).Type
            ' 'C'
            Dim mtSym03 = typesym.BaseType
            ' 'S'
            Dim mtSym04 = (TryCast(typesym.GetMembers("M").[Single](), IMethodSymbol)).Parameters(0).Type
            ' 'E'
            Dim mtSym05 = (TryCast(typesym.GetMembers("Item").[Single](), IPropertySymbol)).Type

            ResolveAndVerifySymbol(mtSym03, originalSymbols(0), comp1, SymbolIdComparison.None)
            ResolveAndVerifySymbol(mtSym01, originalSymbols(1), comp1, SymbolIdComparison.None)
            ResolveAndVerifySymbol(mtSym05, originalSymbols(2), comp1, SymbolIdComparison.None)
            ResolveAndVerifySymbol(mtSym02, originalSymbols(3), comp1, SymbolIdComparison.IgnoreCase)
            ResolveAndVerifySymbol(mtSym04, originalSymbols(4), comp1, SymbolIdComparison.IgnoreCase)
        End Sub

        <Fact>
        Public Sub M2SNonTypeMemberSymbols01()

            Dim src1 = <compilation name="M2SNonTypeMemberSymbols01">
                           <file name="a.vb">
Imports System
Imports System.Collections.Generic

Namespace N1

    Public Interface IGoo
        Sub M(p1 As Integer, p2 As Integer)
        Sub M(ParamArray ary As Short())
        Sub M(p1 As String)
        Sub M(ByRef p1 As String)
    End Interface

    Public Structure S

        Public Event PublicEvent As Action(Of S)
        Public PublicField As IGoo
        Public Property PublicProp As String
        Default Public Property Item(p As SByte) As Short
            Get
                Return p
            End Get
        End Property
    End Structure
End Namespace
                   </file>
                       </compilation>

            Dim src2 = <compilation name="M2SNonTypeMemberSymbols11">
                           <file name="b.vb">
Imports System
Imports AN = N1

Public Class App

    Shared Sub Main()
        Dim obj = New AN.S()
        obj.Publicevent += EH           'BIND1:"obj.Publicevent"
        Dim igoo = obj.Publicfield      'BIND2:"obj.Publicfield"
        igoo.M(obj.PublicProp)          'BIND3:"obj.PublicProp"
        igoo.M(obj(12), obj(123))       'BIND4:"obj(123)"
        Dim x As Short = -1
        igoo.m(x, x)                    'BIND5:"igoo.m(x, x)"
    End Sub

    Shared Sub EH(s As AN.S)
    End Sub
End Class
                        </file>
                       </compilation>

            Dim comp1 = CreateCompilationWithMscorlib40(src1)
            ' Compilation to Compilation
            Dim comp2 = CreateCompilationWithMscorlib40AndReferences(src2, {comp1.ToMetadataReference()})

            ''  ---------------------------
            ''  Source symbols
            Dim originalSymbols = GetSourceSymbols(comp1, SymbolCategory.NonTypeMember)
            originalSymbols = originalSymbols.Where(Function(s) Not s.IsAccessor() AndAlso s.Kind <> SymbolKind.Parameter).OrderBy(Function(s) s.Name).ToList()
            ' 
            Assert.Equal(8, originalSymbols.Count)

            ' ---------------------------
            ' Metadata symbols
            Dim model = comp2.GetSemanticModel(comp2.SyntaxTrees(0))
            Dim list = GetBindNodes(Of ExpressionSyntax)(comp2, "b.vb", 5)
            Assert.Equal(5, list.Count)

            ResolveAndVerifySymbol(list(0), originalSymbols(5), model, comp1, SymbolIdComparison.IgnoreCase)
            ''  field
            ResolveAndVerifySymbol(list(1), originalSymbols(6), model, comp1, SymbolIdComparison.IgnoreCase)
            ''  prop
            ResolveAndVerifySymbol(list(2), originalSymbols(7), model, comp1, SymbolIdComparison.None)
            ''  default prop
            ResolveAndVerifySymbol(list(3), originalSymbols(0), model, comp1, SymbolIdComparison.None)
            ''  M(params short[] ary)
            ResolveAndVerifySymbol(list(4), originalSymbols(2), model, comp1, SymbolIdComparison.IgnoreCase)
        End Sub

#End Region

#Region "Metadata vs. Metadata"

        <Fact>
        Public Sub M2MMultiTargetingMsCorLib01()

            Dim src1 = <compilation name="M2MMultiTargetingMsCorLib01">
                           <file name="a.vb">
Imports System
Imports System.IO

Public Class A

    Public Function GetFileInfo(path As String) As FileInfo
        If File.Exists(path) Then
            Return New FileInfo(path)
        End If

        Return Nothing
    End Function

    Public Sub PrintInfo(ary As Array, ByRef time As DateTime)
        If ary IsNot Nothing Then
            Console.WriteLine(ary)
        Else
            Console.WriteLine("Nothing")
        End If

        time = DateTime.Now
    End Sub
End Class
                           </file>
                       </compilation>

            Dim src2 = <compilation name="M2MMultiTargetingMsCorLib11">
                           <file name="b.vb">
Imports System

Class Test
    Shared Sub Main()
        Dim a = New A()
        Dim fi = a.GetFileInfo(Nothing)
        Console.WriteLine(fi)
        Dim dt = DateTime.Now
        Dim ary = Array.CreateInstance(GetType(String), 2)
        a.PrintInfo(ary, dt)
    End Sub
End Class
                           </file>
                       </compilation>

            Dim comp20 = CreateEmptyCompilationWithReferences(src1, {Net40.References.mscorlib}, TestOptions.ReleaseDll)
            ' "Compilation 2 Assembly"
            Dim comp40 = CreateCompilationWithMscorlib40AndReferences(src2, {comp20.ToMetadataReference()})

            Dim ver20Symbols = GetSourceSymbols(comp20, SymbolCategory.NonTypeMember Or SymbolCategory.Parameter).OrderBy(Function(s) s.Name).ToList()
            Assert.Equal(5, ver20Symbols.Count)

            Dim typeA = comp20.SourceModule.GlobalNamespace.GetTypeMembers("A").[Single]()

            ' ====================
            Dim ver40Symbols = GetSourceSymbols(comp40, SymbolCategory.Local)
            Assert.Equal(4, ver40Symbols.Count)
            Dim localSymbols = ver40Symbols.OrderBy(Function(s) s.Name).[Select](Function(s) DirectCast(s, ILocalSymbol)).ToList()

            ' a
            ResolveAndVerifySymbol(localSymbols(0).Type, typeA, comp20, SymbolIdComparison.IgnoreCase)
            ' ary
            ResolveAndVerifySymbol(localSymbols(1).Type, DirectCast(ver20Symbols(0), IParameterSymbol).Type, comp20, SymbolIdComparison.IgnoreCase)
            ' dt
            ResolveAndVerifySymbol(localSymbols(2).Type, DirectCast(ver20Symbols(4), IParameterSymbol).Type, comp20, SymbolIdComparison.IgnoreCase)
            ' fi
            ResolveAndVerifySymbol(localSymbols(3).Type, DirectCast(ver20Symbols(1), IMethodSymbol).ReturnType, comp20, SymbolIdComparison.IgnoreCase)

        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542725")>
        Public Sub M2MMultiTargetingMsCorLib02()

            Dim src1 = <compilation name="M2MMultiTargetingMsCorLib02">
                           <file name="a.vb">
Imports System
Namespace Mscorlib20

    Public Interface IGoo
        ' interface
        ReadOnly Property Prop As IDisposable
    End Interface

    Public Class CGoo
        Implements IGoo
        ' enum
        Public PublicField As DayOfWeek
        Public ReadOnly Property Prop As IDisposable Implements IGoo.Prop
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace
                           </file>
                       </compilation>

            Dim src2 = <compilation name="M2MMultiTargetingMsCorLib12">
                           <file name="b.vb">
Imports System
Imports N20 = Mscorlib20

Class Test
    Public Function M() As IDisposable
        Dim obj = New N20.CGoo()
        Dim igoo As N20.IGoo = obj
        If obj.Publicfield = DayOfWeek.Friday Then  'BIND1:"obj.Publicfield"
            Return igoo.Prop                        'BIND2:"igoo.Prop"
        End If

        Return Nothing
    End Function

    Public Sub MyEveHandler(o As Object)
    End Sub
End Class
                           </file>
                       </compilation>

            Dim comp20 = CreateEmptyCompilationWithReferences(src1, {Net40.References.mscorlib}, TestOptions.ReleaseDll)
            '
            Dim comp40 = CreateCompilationWithMscorlib40AndReferences(src2, {comp20.ToMetadataReference()})

            Dim ver20Symbols = GetSourceSymbols(comp20, SymbolCategory.NonTypeMember).Where(Function(s) Not s.IsAccessor() And s.Kind <> SymbolKind.Parameter).OrderBy(Function(s) s.Name)
            ' ver20Symbols = ver20Symbols.Where(Function(s) Not IsAccessor(s) And s.Kind <> SymbolKind.Parameter).OrderBy(Function(s) s.Name).[Select](Function(s) s).ToList()
            ''  IGoo.Prop, CGoo.Prop, Field
            Assert.Equal(3, ver20Symbols.Count)

            ' ====================
            Dim model = comp40.GetSemanticModel(comp40.SyntaxTrees(0))
            Dim list = GetBindNodes(Of ExpressionSyntax)(comp40, "b.vb", 2)
            Assert.Equal(2, list.Count)

            '  PublicField
            ResolveAndVerifySymbol(list(0), ver20Symbols(2), model, comp20)
            '  DayOfWeek
            ResolveAndVerifyTypeSymbol(list(0), DirectCast(ver20Symbols(2), IFieldSymbol).Type, model, comp20)
            '  Prop
            ResolveAndVerifySymbol(list(1), ver20Symbols(0), model, comp20)
            '  IDisposable
            ResolveAndVerifyTypeSymbol(list(1), DirectCast(ver20Symbols(0), IPropertySymbol).Type, model, comp20)

        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542992")>
        Public Sub M2MMultiTargetingMsCorLib03()

            Dim src1 = <compilation name="M2MMultiTargetingMsCorLib03">
                           <file name="a.vb">
Imports System

Namespace Mscorlib20

    Public Interface IGoo
        ' class
        Default Property Item(t As ArgumentException) As Exception
    End Interface

    Public Class CGoo
        Implements IGoo
        ' delegate
        Public Event PublicEventField As System.Threading.ParameterizedThreadStart

        Default Public Property Item(t As ArgumentException) As Exception Implements IGoo.Item
            Get
                Return t
            End Get
            Set(value As Exception)

            End Set
        End Property
    End Class
End Namespace
                           </file>
                       </compilation>

            Dim src2 = <compilation name="M2MMultiTargetingMsCorLib03">
                           <file name="b.vb">
Imports System
Imports N20 = Mscorlib20

Class Test
    Public Sub M()
        Dim obj = New N20.CGoo()
        AddHandler obj.Publiceventfield, AddressOf MyEveHandler 'BIND1:"obj.Publiceventfield"
        Dim igoo As N20.IGoo = obj
        Dim local = igoo(Nothing)                               'BIND2:"igoo(Nothing)"
    End Sub

    Public Sub MyEveHandler(o As Object)
    End Sub
End Class
                           </file>
                       </compilation>

            Dim comp20 = CreateEmptyCompilationWithReferences(src1, {Net40.References.mscorlib}, TestOptions.ReleaseDll)
            Dim comp40 = CreateCompilationWithMscorlib40AndReferences(src2, {comp20.ToMetadataReference()})

            Dim ver20Symbols = GetSourceSymbols(comp20, SymbolCategory.NonTypeMember).Where(Function(s) Not s.IsAccessor() And s.Kind <> SymbolKind.Parameter).OrderBy(Function(s) s.Name).ToList()
            ' default property IGoo.Item, CGoo.Item, Event 
            Assert.Equal(3, ver20Symbols.Count)

            ' ====================
            Dim model = comp40.GetSemanticModel(comp40.SyntaxTrees(0))
            Dim list = GetBindNodes(Of ExpressionSyntax)(comp40, "b.vb", 2)
            Assert.Equal(2, list.Count)

            ' obj.Publiceventfield
            ResolveAndVerifySymbol(list(0), ver20Symbols(2), model, comp20)
            ResolveAndVerifyTypeSymbol(list(0), DirectCast(ver20Symbols(2), IEventSymbol).Type, model, comp20)

            ' igoo(Nothing)
            ResolveAndVerifySymbol(list(1), ver20Symbols(0), model, comp20)
            ResolveAndVerifyTypeSymbol(list(1), DirectCast(ver20Symbols(0), IPropertySymbol).Type, model, comp20)
        End Sub

#End Region

    End Class
End Namespace
