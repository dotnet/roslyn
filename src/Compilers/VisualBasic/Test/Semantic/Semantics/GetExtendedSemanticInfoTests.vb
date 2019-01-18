' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Partial Public Class GetExtendedSemanticInfoTests : Inherits SemanticModelTestBase

        <Fact>
        Public Sub LambdaInInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
            Class C
            Private Shared AmbiguousInNSError As Func(Of C, D) =
                Function(syms As C) As D
                    If C IsNot Nothing
                        Return New D()'BIND:"D"
                    Else
                        Return New D()
                    End If
                End Function
            End Class
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            'simply not crashing is the goal for now.
        End Sub

        <Fact>
        Public Sub BindLambdasInArgsOfBadParent()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
'note that T is not defined which causes the object creation expression
'to be bad. This test ensures that the arguments are still bound and analyzable.
        Module M
        Private Shared Function Meth() As T
            Return New T(Function() String.Empty)'BIND:"String"
        End Function
        End Module
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of PredefinedTypeSyntax)(compilation, "a.vb")
            'not crashing 
        End Sub

        <Fact>
        Public Sub RunningAfoulOfExtensions()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
        Imports System.Runtime.CompilerServices 
        Imports System.Linq.Expressions
        Module M
        Private Shared Function GetExpressionType(x As Symbol) As TypeSymbol
            Select Case x.Kind
                Case Else
                    Dim type = TryCast(x, TypeSymbol)'BIND:"x"
                    If type IsNot Nothing Then
                        Return type
                    End If
            End Select

            Return Nothing
        End Function
            &lt;Extension()&gt;
            Sub Type(ByVal x As Integer)
            End Sub
            &lt;Extension()&gt;
            Sub Type(x As String)
            End Sub
        End Module
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            'not crashing 
        End Sub

        <Fact>
        Public Sub BindPredefinedTypeOutsideMethodBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())'BIND:"String"

    End Sub
End Module
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of PredefinedTypeSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub BindPredefinedTypeInsideMethodBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x = String.Empty 'BIND:"String"

    End Sub
End Module
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of PredefinedTypeSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ConvertedLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As Integer
        Dim y As Long
        y = x'BIND:"x"

    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("x As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Dim a As A.B'BIND:"B"
    Sub Main(args As String())
    End Sub
End Module

Class A
    Private Class B
        Public Sub f()

        End Sub
    End Class
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("A.B", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("A.B", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("A.B", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleType2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        A.B.f()'BIND:"B"
    End Sub
End Module

Class A
    Private Class B
        Public Sub f()

        End Sub
    End Class
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("A.B", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("A.B", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("A.B", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleType3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Dim z As A.B'BIND:"B"
    End Sub
End Module

Class A
    Private Class B
        Public Sub f()

        End Sub
    End Class
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("A.B", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("A.B", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("A.B", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub AmbiguousType1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports N1, N2

Module Program
    Dim x As A'BIND:"A"
    Sub Main(args As String())

    End Sub
End Module

Namespace N1
    Class A
    End Class
End Namespace

Namespace N2
    Class A

    End Class
End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("A", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("A", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("N1.A", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)
            Assert.Equal("N2.A", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub AmbiguousType2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports N1, N2

Module Program
    Sub Main(args As String())
        Dim x As A'BIND:"A"
    End Sub
End Module

Namespace N1
    Class A
    End Class
End Namespace

Namespace N2
    Class A

    End Class
End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("A", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("A", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("N1.A", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)
            Assert.Equal("N2.A", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub AmbiguousType3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports N1, N2

Module Program
    Sub Main(args As String())
        A.goo()'BIND:"A"
    End Sub
End Module

Namespace N1
    Class A
    End Class
End Namespace

Namespace N2
    Class A

    End Class
End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("A", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("A", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("N1.A", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)
            Assert.Equal("N2.A", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleSharedField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System


Module Program
    Sub Main(args As String())
        Dim z As Integer = A.fi'BIND:"fi"
    End Sub
End Module

Class A
    Private Shared fi As Integer
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("A.fi As System.Int32", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub MethodGroup1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        f(3)'BIND:"f"
    End Sub

    Sub f()
    End Sub

    Sub f(x As Integer)
    End Sub

    Function f(ByVal a As Integer, b As Long) As String
        Return "hi"
    End Function
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub Program.f(x As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(3, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function Program.f(a As System.Int32, b As System.Int64) As System.String", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Program.f()", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub Program.f(x As System.Int32)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub MethodGroup2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1()
        x.f(3, 7)'BIND:"x.f"
    End Sub

End Module

Class Class1
    Public Sub f()
    End Sub

    Public Sub f(x As Integer)
    End Sub

    Public Function f(ByVal a As Integer, b As Long) As String
        Return "hi"
    End Function
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function Class1.f(a As System.Int32, b As System.Int64) As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(3, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function Class1.f(a As System.Int32, b As System.Int64) As System.String", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1.f()", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub Class1.f(x As System.Int32)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleMethodGroup()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1()
        x.f(3, 7)'BIND:"x.f"
    End Sub

End Module

Class Class1
    Protected Sub f()
    End Sub

    Protected Sub f(x As Integer)
    End Sub

    Private Function f(ByVal a As Integer, b As Long) As String
        Return "hi"
    End Function
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            ' This test should return all three inaccessible methods. I am
            ' leaving it in so it doesn't regress further, but it should be
            ' better.

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function Class1.f(a As System.Int32, b As System.Int64) As System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(3, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function Class1.f(a As System.Int32, b As System.Int64) As System.String", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1.f()", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub Class1.f(x As System.Int32)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleMethodGroup_Constructors_ObjectCreationExpressionSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1(3, 7)'BIND:"New Class1(3, 7)"
    End Sub

End Module

Class Class1
    Protected Sub New()
    End Sub

    Protected Sub New(x As Integer)
    End Sub

    Private Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Class1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Class1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", semanticSummary.CandidateSymbols(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(3, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)

        End Sub

        <Fact>
        Public Sub InaccessibleMethodGroup_Constructors_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1(3, 7)'BIND:"Class1"
    End Sub

End Module

Class Class1
    Protected Sub New()
    End Sub

    Protected Sub New(x As Integer)
    End Sub

    Private Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Class1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleMethodGroup_AttributeSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program

    <Class1(3, 7)>'BIND:"Class1(3, 7)"
    Sub Main(args As String())
    End Sub

End Module

Class Class1
    Inherits Attribute
    Protected Sub New()
    End Sub

    Protected Sub New(x As Integer)
    End Sub

    Private Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("Class1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticSummary.Type.TypeKind)
            Assert.Equal("Class1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(3, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedCandidates(2).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(2).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(3, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleMethodGroup_Attribute_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program

    <Class1(3, 7)>'BIND:"Class1"
    Sub Main(args As String())
    End Sub

End Module

Class Class1
    Inherits Attribute
    Protected Sub New()
    End Sub

    Protected Sub New(x As Integer)
    End Sub

    Private Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Class1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Class1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(3, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedCandidates(2).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(2).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(3, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleConstructorsFiltered_ObjectCreationExpressionSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1(3, 7)'BIND:"New Class1(3, 7)"
    End Sub

End Module

Class Class1
    Protected Sub New()
    End Sub

    Public Sub New(x As Integer)
    End Sub

    Public Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Class1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Class1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)


        End Sub

        <Fact>
        Public Sub InaccessibleConstructorsFiltered_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1(3, 7)'BIND:"Class1"
    End Sub

End Module

Class Class1
    Protected Sub New()
    End Sub

    Public Sub New(x As Integer)
    End Sub

    Public Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Class1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleConstructorsFiltered_IdentifierNameSyntax2()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1(3, 7)'BIND:"New Class1(3, 7)"
    End Sub

End Module

Class Class1
    Protected Sub New()
    End Sub

    Public Sub New(x As Integer)
    End Sub

    Public Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Class1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Class1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleConstructorsFiltered_AttributeSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program

    <Class1(3, 7)>'BIND:"Class1(3, 7)"
    Sub Main(args As String())
    End Sub

End Module

Class Class1
    Inherits Attribute
    Protected Sub New()
    End Sub

    Public Sub New(x As Integer)
    End Sub

    Public Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("Class1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Class1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleConstructorsFiltered_Attribute_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Imports System

Module Program

    <Class1(3, 7)>'BIND:"Class1"
    Sub Main(args As String())
    End Sub

End Module

Class Class1
    Inherits Attribute
    Protected Sub New()
    End Sub

    Public Sub New(x As Integer)
    End Sub

    Public Sub New(ByVal a As Integer, b As Long)
    End Sub
End Class
    ]]></file>
    </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Class1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Class1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1..ctor(a As System.Int32, b As System.Int64)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Class1..ctor(x As System.Int32)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub Invocation1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        f(3, 7)'BIND:"f(3, 7)"
    End Sub

    Sub f()
    End Sub

    Sub f(x As Integer)
    End Sub

    Function f(ByVal a As Integer, b As Long) As String
        Return "hi"
    End Function
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function Program.f(a As System.Int32, b As System.Int64) As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub Invocation2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1()
        x.f(3, 7)'BIND:"x.f(3, 7)"
    End Sub

End Module

Class Class1
    Public Sub f()
    End Sub

    Public Sub f(x As Integer)
    End Sub

    Public Function f(ByVal a As Integer, b As Long) As String
        Return "hi"
    End Function
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function Class1.f(a As System.Int32, b As System.Int64) As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub InaccessibleInvocation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Dim x As New Class1()
        x.f(3, 7)'BIND:"x.f(3, 7)"
    End Sub

End Module

Class Class1
    Protected Sub f()
    End Sub

    Protected Sub f(x As Integer)
    End Sub

    Private Function f(ByVal a As Integer, b As Long) As String
        Return "hi"
    End Function
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function Class1.f(a As System.Int32, b As System.Int64) As System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub Property1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim a As New X
        a.Prop = 4'BIND:"Prop"
    End Sub
End Module

Class X
    Public Property Prop As String
        Get
            Return ""
        End Get
        Set
        End Set
    End Property
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Property X.Prop As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub Property2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim a As New X
        Dim u As String = a.Prop'BIND:"a.Prop"
    End Sub
End Module

Class X
    Public Property Prop As String
        Get
            Return ""
        End Get
        Set
        End Set
    End Property
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Property X.Prop As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub SimpleConstantExpression()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i As Integer = 10 * 10'BIND:"10 * 10"
    End Sub
End Module

Class X
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of BinaryExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function System.Int32.op_Multiply(left As System.Int32, right As System.Int32) As System.Int32",
                         semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(100, semanticInfo.ConstantValue.Value)
        End Sub

        <Fact>
        Public Sub InaccessibleParameter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub goo(a As Outer.Inner)
        Dim q As Integer
        q = a.x'BIND:"a"
    End Sub
End Module

Class Outer
    Private Class Inner
        Public x As Integer
    End Class
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Outer.Inner", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("Outer.Inner", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("a As Outer.Inner", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim paramSym As ParameterSymbol = DirectCast(semanticInfo.Symbol, ParameterSymbol)
            Assert.Equal(TypeKind.Error, paramSym.Type.TypeKind)
        End Sub

        <WorkItem(538447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538447")>
        <Fact>
        Public Sub CastInterfaceToArray()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim i As ICloneable = New Integer() {}
        Dim arr() As Integer
        arr = CType(i, Integer())'BIND:"CType(i, Integer())"
        arr = DirectCast(i, Integer())
        arr = TryCast(i, Integer())
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of CastExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32()", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ImplementsClause1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface IA(Of T)
    Overloads Sub Goo(ByVal x As T)
    Overloads Sub Goo(ByVal x As String)
End Interface

Interface IC(Of T)
    Inherits IA(Of T)
End Interface

Interface IB
    Overloads Sub Bar(x As String)
    ReadOnly Property P As Long
    Property R(x As String) As Long
End Interface

Class K
    Implements IC(Of Integer)
    Implements IB

    Public Overloads Sub F(x As String) Implements IB.Bar, IC(Of Integer).Goo 'BIND1:"IB.Bar" 'BIND2:"Goo"
    End Sub

    Public Overloads Sub F(x As Integer) Implements IA(Of Integer).Goo  'BIND3:"IA(Of Integer).Goo" 
    End Sub

    Public Property Q(x As String) As Long Implements IB.R 'BIND4:"IB.R"
        Get
            Return 0
        End Get
        Set
        End Set
    End Property

    Public Property Q As Long Implements IB.P 'BIND5:"IB"
        Get
            Return 0
        End Get
        Set
        End Set
    End Property
End Class

    </file>
</compilation>)
            'IB.Bar
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb", 1)

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub IB.Bar(x As System.String)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            'Goo
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub IA(Of System.Int32).Goo(x As System.String)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            ' IA(Of Integer).Goo
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb", 3)

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub IA(Of System.Int32).Goo(x As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            ' IB.R
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb", 4)

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Property IB.R(x As System.String) As System.Int64", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            ' IB
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 5)

            Assert.Equal("IB", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticInfo.Type.TypeKind)
            Assert.Equal("IB", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("IB", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ImplementsClause2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Interface I1
    Sub goo(x As Integer)
End Interface

Class C1
    Implements I1

    Public Function goo(x As Integer) As String Implements I1.goo'BIND:"I1.goo"
        Throw New NotImplementedException()
    End Function
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub I1.goo(x As System.Int32)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub ImplementsClause3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Interface I1
    Sub goo(x As Integer)
    Sub goo(x As Integer, y As Integer)
End Interface

Class C1
    Implements I1

    Public Sub goo(x As Long) Implements I1.goo'BIND:"I1.goo"
        Throw New NotImplementedException()
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(2, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub I1.goo(x As System.Int32)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Sub I1.goo(x As System.Int32, y As System.Int32)", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ImplementsClause4()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Interface I1
    Sub goo(x As Integer)
    Sub goo(x As Integer, y As Integer)
End Interface

Class C1
    Public Sub goo(x As Integer) Implements I1.goo 'BIND:"I1.goo"'BIND:"I1.goo"
        Throw New NotImplementedException()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotReferencable, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub I1.goo(x As System.Int32)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub ImplementsClause5()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Interface I1
    Private Sub goo(x As Integer)
End Interface

Class C1
    Implements I1
    Public Sub goo(x As Integer) Implements I1.goo 'BIND:"I1.goo"'BIND:"I1.goo"
        Throw New NotImplementedException()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub I1.goo(x As System.Int32)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub ImplementsClause6()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Interface I1
    Inherits I2, I3
End Interface

Interface I2
    Sub goo(x As Integer, z As String)
End Interface

Interface I3
    Sub goo(x As Integer, y As Integer)
End Interface

Class C1
    Public Sub goo(x As Integer) Implements I1.goo 'BIND:"I1.goo"'BIND:"I1.goo"
        Throw New NotImplementedException()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(2, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub I2.goo(x As System.Int32, z As System.String)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal("Sub I3.goo(x As System.Int32, y As System.Int32)", sortedCandidates(1).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ImplementsClause7()
            Dim compilation = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Module m1
    Public Sub main()

    End Sub
End Module

Interface I1
    Inherits I2, I3
End Interface

Interface I2
    Event E1()
End Interface

Interface I3
    Event E2 As I1.E1EventHandler
End Interface

Class C1
    Implements I1

    Public Event E3() Implements I2.E1, I3.E2'BIND:"I3.E2"
End Class
    ]]></file>
            </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event I3.E2 As I2.E1EventHandler", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub InterfaceImplementationCantFindMatching()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Interface IA
    Overloads Sub Goo(ByVal x As Long)
    Overloads Sub Goo(ByVal x As String)
End Interface

Class K
    Implements IA

    Public Overloads Sub F(x As Integer) Implements IA.Goo'BIND:"IA.Goo"
    End Sub
End Class

    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.True(semanticInfo.CandidateSymbols.Length > 0, "Should have candidate symbols")
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.False(semanticInfo.MemberGroup.Length > 0, "Shouldn't have member group")

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(539111, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539111")>
        <Fact>
        Public Sub MethodReferenceWithImplicitTypeArguments()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Goo(Of Integer)'BIND:"Goo(Of Integer)"
    End Sub

    Sub Goo(a As String)
    End Sub

    Sub Goo(ByRef a As String, b As String)
    End Sub

    Sub Goo(Of T)(a As String, b As String)
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of GenericNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Module1.Goo(Of System.Int32)(a As System.String, b As System.String)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Module1.Goo(Of System.Int32)(a As System.String, b As System.String)", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(538452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538452")>
        <Fact>
        Public Sub InvalidMethodInvocationExpr()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Sub Main()
    T()'BIND:"T()"
End Sub

Sub T()
End Sub
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal(SpecialType.System_Void, semanticInfo.Type.SpecialType)
            Assert.Equal(SpecialType.System_Void, semanticInfo.ConvertedType.SpecialType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("T", semanticInfo.Symbol.Name)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub UnaryPlusExprWithoutMsCorlibRef()
            Dim compilation = CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim count As Integer
        count = +10 'BIND:"+10"'BIND:"+10"
    End Sub
End Module
    </file>
</compilation>, {})

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of UnaryExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32[missing]", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32[missing]", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function System.Int32[missing].op_UnaryPlus(value As System.Int32[missing]) As System.Int32[missing]",
                         semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(4280, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub BindingIsNothingFunc()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports Microsoft.VisualBasic
Module Module1
    Sub Main()
        Dim var1 As Object
        If IsNothing(var1) Then'BIND:"IsNothing(var1)"
        End If
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Boolean", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
        End Sub

        <Fact>
        Public Sub MaxIntPlusOneHexLiteralConst()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim lngPass As Integer
        lngPass = &amp;H80000000'BIND:"&amp;H80000000"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(CInt(-2147483648), semanticInfo.ConstantValue.Value)
        End Sub

        <WorkItem(539017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539017")>
        <Fact>
        Public Sub ParenExprInMultiDimArrayDeclWithError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Mod1
    Sub Main()
        Dim scen3(, 5,6,) As Integer
        Dim x((,)) As Integer 'BIND:"(,)"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TupleExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("(?, ?)", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Struct, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(Nothing, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub InvocExprWithImplicitlyTypedArgument()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module Module1
    Sub VerifyByteArray(ByRef arry() As Byte, ByRef lbnd As Integer)
    End Sub

    Sub Main()
        Dim vsarry() As Byte
        Dim Max = 140000
        VerifyByteArray(vsarry, Max)'BIND:"VerifyByteArray(vsarry, Max)"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub Module1.VerifyByteArray(ByRef arry As System.Byte(), ByRef lbnd As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(4512, "DevDiv_Projects/Roslyn")>
        <Fact>
        Public Sub MultiDimArrayCreationExpr()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim y = New Object() {1, 2}
        Dim x As Object = New Object()() {y, y}'BIND:"New Object()() {y, y}"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ArrayCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object()()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(527716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527716")>
        <Fact>
        Public Sub EmptyParenExprInArrayDeclWithError()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module Mod1
    Sub Main()
        Dim x1 = New Single(0, (* ) - 1) {{2}, {4}}'BIND:"(* )"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ParenthesizedExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("?", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(538918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538918")>
        <Fact>
        Public Sub MeSymbol()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Sub Test()
        Me.Test()'BIND:"Me"
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MeExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Me As C", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.True(DirectCast(semanticInfo.Symbol, ParameterSymbol).IsMe, "should be Me symbol")

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(527818, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527818")>
        <Fact>
        Public Sub BindingFuncNoBracket()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Namespace VBNS
    Class Test

        Function MyFunc() As Byte
            Return Nothing
        End Function

        Sub MySub()
            Dim ret As Byte = MyFunc'BIND:"MyFunc"
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Byte", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Byte", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function VBNS.Test.MyFunc() As System.Byte", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub NamespaceAlias()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports S = System

Namespace NS
    Class Test
        Dim x As S.Exception'BIND:"S"
    End Class
End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.NotNull(semanticInfo.Symbol)
            Assert.NotNull(semanticInfo.Alias)
            Assert.Equal("S=System", semanticInfo.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticInfo.Alias.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Dim ns = DirectCast(semanticInfo.Alias.Target, NamespaceSymbol)
            Assert.Equal(ns.Name, "System")

            Assert.Equal(compilation.SourceModule, semanticInfo.Alias.ContainingModule)
            Assert.Equal(compilation.Assembly, semanticInfo.Alias.ContainingAssembly)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub NamespaceAlias2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports N = NS1.NS2

Namespace NS1.NS2
    Public Class A
        Public Shared Sub M
            Dim o As N.A'BIND:"N"

        End Sub
    End Class
End Namespace

    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.NotNull(semanticInfo.Symbol)
            Assert.NotNull(semanticInfo.Alias)
            Assert.Equal("N=NS1.NS2", semanticInfo.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticInfo.Alias.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Dim ns = DirectCast(semanticInfo.Alias.Target, NamespaceSymbol)
            Assert.Equal("NS1.NS2", ns.ToTestDisplayString())

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub TypeAlias()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports S = System.String

Namespace NS
    Class Test
        Sub Goo
            Dim x As String
            x = S.Empty'BIND:"S"
        End Sub
    End Class
End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.NotNull(semanticInfo.Symbol)
            Assert.NotNull(semanticInfo.Alias)
            Assert.Equal("S=System.String", semanticInfo.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticInfo.Alias.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal("String", semanticInfo.Alias.Target.Name)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub TypeAlias2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports T = System.Guid

Module Program
    Sub Main(args As String())
        Dim a As Type
        a = GetType(T)'BIND:"T"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Guid", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Guid", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.NotNull(semanticInfo.Symbol)
            Assert.NotNull(semanticInfo.Alias)
            Assert.Equal("T=System.Guid", semanticInfo.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticInfo.Alias.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub TypeAlias3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports T = System.Guid

Module Program
    Dim a As T'BIND:"T"
    Sub Main(args As String())

    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Guid", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Guid", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.NotNull(semanticInfo.Symbol)
            Assert.NotNull(semanticInfo.Alias)
            Assert.Equal("T=System.Guid", semanticInfo.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticInfo.Alias.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(540279, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540279")>
        <Fact>
        Public Sub NoMembersForVoidReturnType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Sub Test()
        System.Console.WriteLine()'BIND:"System.Console.WriteLine()"
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub System.Console.WriteLine()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim semanticModel = compilation.GetSemanticModel(CompilationUtils.GetTree(compilation, "a.vb"))
            Dim methodSymbol As MethodSymbol = DirectCast(semanticInfo.Symbol, MethodSymbol)
            Dim returnType = methodSymbol.ReturnType
            Dim symbols = semanticModel.LookupSymbols(0, returnType)
            Assert.Equal(0, symbols.Length)
        End Sub

        <Fact>
        Public Sub EnumMember1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Enum filePermissions
    create = 1
    read = create'BIND:"create"
    write = 4
    delete = 8
End Enum
Class c1
    Public Shared Sub Main(args As String())
        Dim file1Perm As filePermissions
        file1Perm = filePermissions.create Or filePermissions.read
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("filePermissions", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Enum, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric Or ConversionKind.InvolvesEnumTypeConversions, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("filePermissions.create", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind)
            Assert.True(TypeOf semanticInfo.Symbol Is SourceEnumConstantSymbol, "Should have bound to synthesized enum constant")
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(1, semanticInfo.ConstantValue.Value)
        End Sub

        <Fact>
        Public Sub CatchVariable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class c1
    Public Shared Sub Main()
        Try 
        Catch ex as Exception  'BIND:"ex"
        End Try
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Exception", semanticInfo.Type.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub CatchVariable1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class c1
    Public Shared Sub Main()
        dim a as Action = Sub()
                            Try 
                            Catch ex as Exception  'BIND:"ex"
                            End Try
                          End Sub
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Exception", semanticInfo.Type.ToTestDisplayString())
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <WorkItem(540050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540050")>
        <Fact>
        Public Sub StaticLocalSymbol()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Function goo() As Integer
        Static i As Integer = 23
        i = i + 1'BIND:"i"
        Return i
    End Function

    Public Shared Sub Main()
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("i As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim iSymbol = DirectCast(semanticInfo.Symbol, LocalSymbol)
            Assert.True(iSymbol.IsStatic)
            Assert.False(iSymbol.IsShared)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub IncompleteWriteLine()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine( 'BIND:"WriteLine"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(12, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString(), StringComparer.InvariantCulture).ToArray()
            Assert.Equal("Sub System.Console.WriteLine(buffer As System.Char())", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Boolean)", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Char)", sortedCandidates(2).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(2).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Decimal)", sortedCandidates(3).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(3).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Double)", sortedCandidates(4).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(4).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", sortedCandidates(5).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(5).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int64)", sortedCandidates(6).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(6).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Object)", sortedCandidates(7).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(7).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.Single)", sortedCandidates(8).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(8).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.String)", sortedCandidates(9).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(9).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.UInt32)", sortedCandidates(10).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(10).Kind)
            Assert.Equal("Sub System.Console.WriteLine(value As System.UInt64)", sortedCandidates(11).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(11).Kind)

            Assert.Equal(19, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString(), StringComparer.InvariantCulture).ToArray()
            Assert.Equal("Sub System.Console.WriteLine()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(buffer As System.Char())", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(buffer As System.Char(), index As System.Int32, count As System.Int32)", sortedMethodGroup(2).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(format As System.String, arg0 As System.Object)", sortedMethodGroup(3).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(format As System.String, arg0 As System.Object, arg1 As System.Object)", sortedMethodGroup(4).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(format As System.String, arg0 As System.Object, arg1 As System.Object, arg2 As System.Object)", sortedMethodGroup(5).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(format As System.String, arg0 As System.Object, arg1 As System.Object, arg2 As System.Object, arg3 As System.Object)", sortedMethodGroup(6).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(format As System.String, ParamArray arg As System.Object())", sortedMethodGroup(7).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Boolean)", sortedMethodGroup(8).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Char)", sortedMethodGroup(9).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Decimal)", sortedMethodGroup(10).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Double)", sortedMethodGroup(11).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int32)", sortedMethodGroup(12).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Int64)", sortedMethodGroup(13).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Object)", sortedMethodGroup(14).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.Single)", sortedMethodGroup(15).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.String)", sortedMethodGroup(16).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.UInt32)", sortedMethodGroup(17).ToTestDisplayString())
            Assert.Equal("Sub System.Console.WriteLine(value As System.UInt64)", sortedMethodGroup(18).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ReturnedNothingLiteral()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Module Program
    Function Main(args As String()) As String
        Return Nothing'BIND:"Nothing"
    End Function
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNothingLiteral, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Null(semanticInfo.ConstantValue.Value)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub FailedConstructorCall()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Class C
End Class

Class V
    Sub goo
        Dim c As C
        c = New C(13)'BIND:"C"
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("C", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub FailedConstructorCall2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System

Class C
End Class

Class V
    Sub goo
        Dim c As C
        c = New C(13)'BIND:"New C(13)"
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub C..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub C..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ExplicitCallToDefaultProperty1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class X
    Public ReadOnly Property Goo As Y
        Get
            Return Nothing
        End Get
    End Property
End Class

Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class

Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.Goo.Item(4)'BIND:"Item"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("ReadOnly Property Y.Item(a As System.Int32) As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("ReadOnly Property Y.Item(a As System.Int32) As System.String", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ExplicitCallToDefaultProperty2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class X
    Public ReadOnly Property Goo As Y
        Get
            Return Nothing
        End Get
    End Property
End Class

Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class

Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.Goo.Item(4)'BIND:"b.Goo.Item(4)"
    End Sub
End Module
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("ReadOnly Property Y.Item(a As System.Int32) As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <WorkItem(541240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541240")>
        <Fact()>
        Public Sub ConstFieldInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Const ClassId As String = Nothing 'BIND:"Nothing"
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Null(semanticInfo.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub ConstFieldInitializer2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Const ClassId As Integer = 23 'BIND:"23"
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
            Assert.Equal(semanticInfo.ConstantValue.Value, 23)
            Assert.True(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ConstFieldInitializer3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Const ClassDate As DateTime = #11/04/2008# 'BIND:"#11/04/2008#"
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_DateTime, semanticInfo.Type.SpecialType)
            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(#11/4/2008#, semanticInfo.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub ConstFieldInitializer4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Const ClassId As Integer = 2 + 2 'BIND:"2 + 2"
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
            Assert.Equal(semanticInfo.ConstantValue.Value, 4)
            Assert.True(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ConstFieldInitializer5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public Const A As Integer = 4
    Public Const B As Integer = 7 + 2 * A 'BIND:"2 * A"
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
            Assert.Equal(semanticInfo.ConstantValue.Value, 8)
            Assert.True(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ConstFieldInitializersMultipleSymbols()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Const X, Y As Integer = 6
    Const Z As Integer = Y
End Class
    </file>
</compilation>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            Dim node = FindNodeFromText(tree, "X")
            Dim symbol = DirectCast(model.GetDeclaredSymbol(node), FieldSymbol)
            Assert.Equal(symbol.Name, "X")
            Assert.Equal(System_Int32, symbol.Type.SpecialType)
            Assert.Equal(symbol.ConstantValue, 6)

        End Sub

        <Fact()>
        Public Sub FieldInitializer1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public ClassId As Integer = 23 'BIND:"23"
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
            Assert.Equal(semanticInfo.ConstantValue.Value, 23)
            Assert.True(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub FieldInitializer2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C
    Public ClassId As Integer = C.goo() 'BIND:"goo"

    shared Function goo() as Integer
        return 23    
    End Function
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Assert.Equal("Function C.goo() As System.Int32", semanticInfo.MemberGroup(0).ToDisplayString(SymbolDisplayFormat.TestFormat))
        End Sub

        <WorkItem(541243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541243")>
        <Fact()>
        Public Sub CollectionInitializerNoMscorlibRef()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim numbers() As Integer = New Integer() {0, 1, 2, 3, 4} 'BIND:"{0, 1, 2, 3, 4}"
    End Sub
End Module        
    </file>
</compilation>, {})

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32[missing]()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32[missing]()", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind)
        End Sub

        <Fact()>
        Public Sub CollectionInitializerWithoutConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict On
Class C
    Shared Sub M(s as string)
        Dim s = New String() {s} 'BIND:"{s}"
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")
            Assert.Equal("System.String()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String()", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
        End Sub

        <WorkItem(541422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541422")>
        <Fact()>
        Public Sub CollectionInitializerWithConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Option Strict On
Class C
    Shared Sub M(o As Object)
        Dim s = New String() {o} 'BIND:"{o}"
    End Sub
End Class
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")
            Assert.Equal("System.String()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String()", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
        End Sub

        <Fact()>
        Public Sub ArrayInitializerMemberWithConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim s As String
        Dim x As Object() = New Object() {s}'BIND:"s"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("s As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TwoDArrayInitializerMember()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main()
        Dim s1 As String = "hi"
        Dim s2 As String = "hello"
        Dim o1 As Object = Nothing
        Dim o2 As Object = Nothing
        Dim arr As Object(,) = New Object(,) {{o1, o2}, {s1, s2}}'BIND:"s2"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("s2 As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub PartialArrayInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main()
        Dim s1 As String = "hi"
        Dim s2 As String = "hello"
        Dim o1 As Object = Nothing
        Dim o2 As Object = Nothing
        Dim arr As Object(,) = New Object(,) {{o1, o2}, {s1, s2}}'BIND:"{s1, s2}"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub



        <WorkItem(541270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541270")>
        <Fact()>
        Public Sub GetSemanticInfoOfNothing()
            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
    End Sub
End Module        
    </file>
</compilation>, {})

            Dim semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees(0))

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetAliasInfo(Nothing)
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetSymbolInfo(DirectCast(Nothing, AttributeSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetSymbolInfo(DirectCast(Nothing, ExpressionRangeVariableSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetSymbolInfo(DirectCast(Nothing, ExpressionSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetSymbolInfo(DirectCast(Nothing, FunctionAggregationSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetSymbolInfo(DirectCast(Nothing, OrderingSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetSymbolInfo(DirectCast(Nothing, QueryClauseSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetTypeInfo(DirectCast(Nothing, AttributeSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetTypeInfo(DirectCast(Nothing, ExpressionSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetConstantValue(DirectCast(Nothing, ExpressionSyntax))
                                                    End Function
            )

            Assert.Throws(Of ArgumentNullException)(Function()
                                                        Return semanticModel.GetMemberGroup(DirectCast(Nothing, ExpressionSyntax))
                                                    End Function
            )

        End Sub

        <WorkItem(541390, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541390")>
        <Fact()>
        Public Sub ErrorLambdaParamInsideFieldInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Module1
    Dim f1 As Func(Of Integer, Integer) = Function(lambdaParam As Integer)
                                              Return lambdaParam + 1 'BIND:"lambdaParam"
                                          End Function
End Module      
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
        End Sub

        <Fact()>
        Public Sub ErrorLambdaParamInsideLocalInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim f1 As Func(Of Integer, Integer) = Function(lambdaParam As Integer)
                                                  Return lambdaParam + 1 'BIND:"lambdaParam"
                                              End Function
    End Sub
End Module      
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
        End Sub

        <WorkItem(541390, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541390")>
        <Fact()>
        Public Sub LambdaParamInsideFieldInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System        
Module Module1
    Dim f1 As Func(Of Integer, Integer) = Function(lambdaParam)
                                              Return lambdaParam + 1 'BIND:"lambdaParam"
                                          End Function
End Module      
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
        End Sub

        <Fact()>
        Public Sub LambdaParamInsideLocalInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Module Module1
    Sub Main()
        Dim f1 As Func(Of Integer, Integer) = Function(lambdaParam)
                                                  Return lambdaParam + 1 'BIND:"lambdaParam"
                                              End Function
    End Sub
End Module      
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
        End Sub

        <WorkItem(541418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541418")>
        <Fact()>
        Public Sub BindAttributeInstanceWithoutAttributeSuffix()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
<Assembly: My> 'BIND:"My"
Class MyAttribute : Inherits System.Attribute
End Class]]>
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Symbol)
            Assert.Equal("Sub MyAttribute..ctor()", semanticInfo.Symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.Equal("MyAttribute", semanticInfo.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))
        End Sub

        <WorkItem(541401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541401")>
        <Fact()>
        Public Sub BindingAttributeParameter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class MineAttribute

    Inherits Attribute
    Public Sub New(p As Short)
    End Sub

End Class

<Mine(123)> 'BIND:"123"
Class C
End Class
]]>
    </file>
</compilation>)

            ' NOTE: VB doesn't allow same line comments after attribute
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
            Assert.Equal(System_Int16, semanticInfo.ConvertedType.SpecialType)
            Assert.Equal(semanticInfo.ConstantValue.Value, 123)
            Assert.True(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub BindAttributeNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
        <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace n1

    Module Program

        <n1.Program.Test1(1)>'BIND:"n1"
        Class A
        End Class

        <AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
        Class Test1
            Inherits Attribute

              Public Sub New(i As Integer)
            End Sub

        End Class

    End Module

End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("n1", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(541418, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541418")>
        <Fact()>
        Public Sub BindingAttributeClassName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
<System.AttributeUsage(AttributeTargets.All, AllowMultiple:=true)> _ 'BIND:"AttributeUsage"
Class ZAttribute
    Inherits Attribute

End Class
<ZAttribute()>
Class scen1
    Shared Sub Main()
        Dim x = 1
        Console.WriteLine(x)
    End Sub
End Class
]]>
    </file>
</compilation>)

            ' NOTE: VB doesn't allow same line comments after attribute
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal("System.AttributeUsageAttribute", semanticInfo.Type.ToString())
            Assert.False(DirectCast(semanticInfo.Type, TypeSymbol).IsErrorType)
        End Sub

        <Fact()>
        Public Sub TestAttributeFieldName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace n1

    Module Program

        <Test1(fi:=10)>'BIND:"fi"
        Class A
        End Class

    <AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
    Class Test1
        Inherits Attribute

        Public Sub New(i As Integer)
        End Sub

        Public fi As Integer

    End Class

    End Module

End Namespace]]>
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("n1.Program.Test1.fi As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TestAttributePropertyName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace n1

    Module Program

        Class A
            <Test1(1, Pi:=2)>'BIND:"Pi"
            Sub s
            End Sub

        End Class

    <AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
    Class Test1
        Inherits Attribute

        Public Sub New()
        End Sub

        Public Sub New(i As Integer)
            End Sub

            Public fi As Integer

            Public Property Pi As Integer

        End Class

    End Module

End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Property n1.Program.Test1.Pi As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TestAttributePositionalArgOnParameter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace n1

    Module Program

        Class A
            Function f( <Test1("parameter")> x As Integer) As Integer'BIND:""parameter""
                Return 0
            End Function
        End Class

    <AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
    Class Test1
        Inherits Attribute

            Public Sub New(i As String)
            End Sub

        End Class

    End Module

End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal("parameter", semanticInfo.ConstantValue.Value)
        End Sub


        <Fact()>
        Public Sub TestAttributeClassNameOnReturnValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace n1

    Module Program

        Class A

            Function f(x As Integer) As <Test1(4)> Integer'BIND:"Test1"
                Return 0
            End Function
        End Class

    <AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
    Class Test1
        Inherits Attribute

        Public Sub New(i As Integer)
        End Sub

        End Class

    End Module

End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("n1.Program.Test1", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("n1.Program.Test1", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub n1.Program.Test1..ctor(i As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TestAttributeCannotBindToUnqualifiedClassMember()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace n1

    Module Program

        <Test1(C1)>'BIND:"C1"
        Class A
            Public Const C1 As Integer = 99
        End Class

    <AttributeUsageAttribute(AttributeTargets.All, AllowMultiple:=True)>
    Class Test1
        Inherits Attribute

            Public Sub New(i As Integer)
            End Sub

        End Class

    End Module

End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("?", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("?", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub AttributeSemanticInfo_OverloadResolutionFailure_01()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
<Module: ObsoleteAttribute(GetType())>'BIND:"ObsoleteAttribute"
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.ObsoleteAttribute", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.ObsoleteAttribute", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(3, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedCandidates(2).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(2).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(3, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub AttributeSemanticInfo_OverloadResolutionFailure_02()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
<Module: Obsolete(GetType())>'BIND:"Module: Obsolete(GetType())"
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("System.ObsoleteAttribute", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal("System.ObsoleteAttribute", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(3, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedCandidates(2).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(2).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(3, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedMethodGroup(2).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(541481, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541481")>
        <Fact()>
        Public Sub BindingPredefinedCastExpression()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports Microsoft.VisualBasic

Module Test

    Sub Main()
        Dim exp As Integer = 123
        Dim act As String = CStr(exp) 'BIND:"CStr(exp)"
    End Sub

End Module
]]>
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of PredefinedCastExpressionSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal("String", semanticInfo.Type.ToString())
        End Sub

        <WorkItem(541498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541498")>
        <Fact()>
        Public Sub DictionaryAccessExpressionErrorType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Sub Main()
        Dim c As Collection
        Dim b = c!A 'BIND:"c!A"
    End Sub
End Module
]]>
    </file>
</compilation>)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(semanticInfo.Type.TypeKind, TypeKind.Error)
        End Sub

        <Fact()>
        Public Sub DictionaryAccessExpressionErrorExpr()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Private Shared F As System.Collections.Generic.Dictionary(Of String, Integer)
End Class
Class B
    Shared Sub M()
        Dim o = A.F!x 'BIND:"A.F!x"
    End Sub
End Class
]]>
    </file>
</compilation>)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            CompilationUtils.CheckSymbol(semanticInfo.Symbol, "Property Dictionary(Of String, Integer).Item(key As String) As Integer")
            Assert.Equal(semanticInfo.Type.SpecialType, System_Int32)
        End Sub

        <Fact()>
        Public Sub DictionaryAccessExpressionNoType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim o = (AddressOf M)!x 'BIND:"(AddressOf M)!x"
    End Sub
End Class
]]>
    </file>
</compilation>)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(semanticInfo.Type.TypeKind, TypeKind.Error)
        End Sub

        <Fact()>
        Public Sub DictionaryAccessExpression()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Friend Shared F As System.Collections.Generic.Dictionary(Of String, Integer)
End Class
Class B
    Shared Sub M()
        Dim o = A.F!x 'BIND:"A.F!x"
    End Sub
End Class
]]>
    </file>
</compilation>)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            CompilationUtils.CheckSymbol(semanticInfo.Symbol, "Property Dictionary(Of String, Integer).Item(key As String) As Integer")
            Assert.Equal(semanticInfo.Type.SpecialType, System_Int32)
        End Sub

        <WorkItem(541384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541384")>
        <Fact()>
        Public Sub DictionaryAccessKey()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Shared Function M(d As System.Collections.Generic.Dictionary(Of String, Integer)) As Integer
        Return d!key 'BIND:"key"
    End Function
End Class
]]>
    </file>
</compilation>)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.NotNull(semanticInfo)
            Assert.NotNull(semanticInfo.Type)
            Assert.Equal(semanticInfo.Type.SpecialType, System_String)
            Assert.Equal(semanticInfo.ConstantValue.Value, "key")
            Assert.Null(semanticInfo.Symbol)
        End Sub

        <WorkItem(541518, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541518")>
        <Fact()>
        Public Sub AssignAddressOfPropertyToDelegate()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Delegate Function del2() As Integer
    Property p() As Integer
        Get
            Return 10
        End Get
        Set(ByVal Value As Integer)

        End Set
    End Property

    Sub Main()
        Dim var2 = New del2(AddressOf p)'BIND:"p"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Property Module1.p As System.Int32", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Property Module1.p As System.Int32", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub FieldAccess()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class B
    Public f1 As Integer
End Class

Class M
    Public Sub Main()
        Dim bInstance As B
        bInstance = New B()
        Console.WriteLine(bInstance.f1)'BIND:"f1"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("B.f1 As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LocalVariable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class B
    Public f1 As Integer
End Class

Class M
    Public Sub Main()
        Dim bInstance As B
        bInstance = New B()
        Console.WriteLine(bInstance.f1)'BIND:"bInstance"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("B", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("B", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("bInstance As B", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NotFunctionReturnLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class M
    Public Function Goo() As Integer
        Goo()'BIND:"Goo"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function M.Goo() As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function M.Goo() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub FunctionReturnLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class M
    Public Function Goo() As Integer
        Goo = 4'BIND:"Goo"
    End Function
End Class

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Goo As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub MeSemanticInfo()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class M
    Public x As Integer
    Public Function Goo() As Integer
        Me.x = 5'BIND:"Me"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MeExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("M", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("M", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Me As M", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.True(DirectCast(semanticInfo.Symbol, ParameterSymbol).IsMe)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LocalVariableInConstructor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class B
    Public f1 As Integer
End Class

Class M
    Public Sub New()
        Dim bInstance As B
        bInstance = New B()
        Console.WriteLine(bInstance.f1)'BIND:"bInstance"
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("B", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("B", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("bInstance As B", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LocalVariableInSharedConstructor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class B
    Public f1 As Integer
End Class

Class M
    Shared Sub New()
        Dim bInstance As B
        bInstance = New B()
        Console.WriteLine(bInstance.f1)'BIND:"bInstance"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("B", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("B", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("bInstance As B", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LocalVariableInModuleConstructor()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class B
    Public f1 As Integer
End Class

Module M
    Sub New()
        Dim bInstance As B
        bInstance = New B()
        Console.WriteLine(bInstance.f1)'BIND:"bInstance"
    End Sub
End Module

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("B", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("B", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("bInstance As B", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ConstructorConstructorCall_Structure_Me_New_WithParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Structure Program
    Public Sub New(i As Integer)
    End Sub

    Public Sub New(s As String)
        Me.New(1)'BIND:"Me.New(1)"
    End Sub
End Structure
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub Program..ctor(i As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim method = DirectCast(semanticInfo.Symbol, MethodSymbol)
            Assert.Equal(MethodKind.Constructor, method.MethodKind)
            Assert.False(method.IsShared)
            Assert.False(method.IsImplicitlyDeclared)
        End Sub

        <Fact()>
        Public Sub ConstructorConstructorCall_Class_Me_New_WithParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class Program
    Public Sub New(i As Integer)
    End Sub

    Public Sub New(s As String)
        Me.New(1)'BIND:"Me.New(1)"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub Program..ctor(i As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim method = DirectCast(semanticInfo.Symbol, MethodSymbol)
            Assert.Equal(MethodKind.Constructor, method.MethodKind)
            Assert.False(method.IsShared)
            Assert.False(method.IsImplicitlyDeclared)
        End Sub

        <Fact()>
        Public Sub ConstructorConstructorCall_Structure_Me_New_NoParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Structure Program
    Public Sub New(s As String)
        Me.New()'BIND:"Me.New()"
    End Sub
End Structure
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub Program..ctor()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim method = DirectCast(semanticInfo.Symbol, MethodSymbol)
            Assert.Equal(MethodKind.Constructor, method.MethodKind)
            Assert.False(method.IsShared)
            Assert.True(method.IsImplicitlyDeclared)
        End Sub

        <Fact()>
        Public Sub ConstructorConstructorCall_Class_Me_New_NoParameters()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Class Program
    Public Sub New()
    End Sub

    Public Sub New(s As String)
        Me.New()'BIND:"Me.New()"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Void", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub Program..ctor()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)

            Dim method = DirectCast(semanticInfo.Symbol, MethodSymbol)
            Assert.Equal(MethodKind.Constructor, method.MethodKind)
            Assert.False(method.IsShared)
            Assert.False(method.IsImplicitlyDeclared)
        End Sub

        <Fact()>
        Public Sub Invocation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As String = F()'BIND:"F()"
    End Sub

    Private Function F() As String
        Return "Hello"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function C.F() As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub MethodGroup()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As String = F()'BIND:"F"
    End Sub

    Private Function F() As String
        Return "Hello"
    End Function

    Private Function F(arg As String) As String
        Return "Goodbye"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function C.F() As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(2, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C.F() As System.String", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Function C.F(arg As System.String) As System.String", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub InvocationNoMatchingOverloads()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As String = F()'BIND:"F()"
    End Sub

    Private Function F(arg As Integer) As String
        Return "Hello"
    End Function

    Private Function F(arg As String) As String
        Return "Goodbye"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C.F(arg As System.Int32) As System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Function C.F(arg As System.String) As System.String", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(540580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540580")>
        <WorkItem(541567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541567")>
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub NoMatchingOverloads2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As String = F(6, 3)'BIND:"F(6, 3)"
    End Sub

    Private Function F() As String
        Return "Hello"
    End Function
End Class

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Char", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("ReadOnly Property System.String.Chars(index As System.Int32) As System.Char", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NoMatchingOverloads3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As String = F.Chars(6, 3)'BIND:"F.Chars(6, 3)"
    End Sub

    Private Function F() As String
        Return "Hello"
    End Function
End Class

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Char", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("ReadOnly Property System.String.Chars(index As System.Int32) As System.Char", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(541567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541567")>
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub NoMatchingOverloads4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim a As New C1()
        Dim b As Integer

        b = a(5, 6, 7)'BIND:"a(5, 6, 7)"
        b = a.P1(5, 6, 7)
    End Sub
End Module

Class C1
    Public Default Property P1(x As Integer) As String
        Get
            Return "hello"
        End Get
        Set(ByVal value As String)

        End Set
    End Property

    Public Default Property P1(x As Integer, y As Integer) As String
        Get
            Return "hi"
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.NarrowingString, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Property C1.P1(x As System.Int32) As System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)
            Assert.Equal("Property C1.P1(x As System.Int32, y As System.Int32) As System.String", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <WorkItem(540580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540580")>
        <Fact()>
        Public Sub PropertyPassedByRef()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C
    Private Sub M()
        S(P)'BIND:"P"
    End Sub

    Public Property P As String

    Private Sub S(ByRef a As String)
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Property C.P As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub InvocationWithNoMatchingOverloadsAndNonMatchingReturnTypes()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As String = F()'BIND:"F()"
    End Sub

    Private Function F(arg As Integer) As String
        Return "Hello"
    End Function

    Private Function F(arg As String) As Integer
        Return "Goodbye"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("?", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C.F(arg As System.Int32) As System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Function C.F(arg As System.String) As System.Int32", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub IncompleteInvocation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As String = F(:'BIND:"F("
    End Sub

    Private Function F(arg As Integer) As String
        Return "Hello"
    End Function

    Private Function F(arg As String) As String
        Return "Goodbye"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C.F(arg As System.Int32) As System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal("Function C.F(arg As System.String) As System.String", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TypeNameInsideMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Sub Main()
        Dim cInstance As C'BIND:"C"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("C", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TypeNameInsideMethodWithConflictingLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Sub Main()
        Dim cInstance As C'BIND:"C"
        Dim C As String
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("C", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TypeNameOutsideMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Sub Main(ByVal arg As D)'BIND:"D"
    End Sub
End Class

Class D
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("D", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("D", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("D", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamespaceName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As System.String = F()'BIND:"System"
    End Sub

    Private Function F() As String
        Return "Hello"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("System", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamespaceNameInDeclaration()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M(arg As System.String)'BIND:"System"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("System", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamespaceNameWithConflictingField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As System.String = F()'BIND:"System"
    End Sub

    Private Function F() As String
        Return "Hello"
    End Function

    Public System As Integer
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("System", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub MissingNamespaceName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M()
        Dim x As Baz.Goo.String'BIND:"Goo"
    End Sub

    Private Function F() As String
        Return "Hello"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Baz.Goo", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("Baz.Goo", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub RightSideOfQualifiedName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As N.D = Nothing'BIND:"D"
    End Sub
End Class

Public Class D
End Class

Namespace N
    Public Class D
    End Class
End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("N.D", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("N.D", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("N.D", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub RHSInDeclaration()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Sub M(arg As System.String)'BIND:"String"
    End Sub
End Class
Class [String]
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ImportsRHS()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports NS1.NS2'BIND:"NS2"
    ]]></file>
    <file name="b.vb"><![CDATA[
Namespace NS1
    Namespace NS2
        Class X
        End Class
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("NS1.NS2", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ImportsLHS()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports NS1.NS2'BIND:"NS1"
    ]]></file>
    <file name="b.vb"><![CDATA[
Namespace NS1
    Namespace NS2
        Class X
        End Class
    End Namespace
End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("NS1", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AmbiguousType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As AAA'BIND:"AAA"
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Module Q
Class AAA
End Class
End Module

Module R
    Class AAA
    End Class
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("AAA", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("AAA", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Q.AAA", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)
            Assert.Equal("R.AAA", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AmbiguousField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As String
        x = elvis'BIND:"elvis"
    End Sub
End Class
    ]]></file>
    <file name="b.vb"><![CDATA[
Module Q
Public elvis As Integer
End Module

Module R
    Public elvis As String
End Module

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("?", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(Nothing, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Ambiguous, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Q.elvis As System.Int32", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, sortedCandidates(0).Kind)
            Assert.Equal("R.elvis As System.String", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, sortedCandidates(1).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub InNamespaceDeclaration()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace AAA.BBB'BIND:"AAA"
    Class AAA
    End Class
End Namespace
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("AAA", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Namespace, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LocalInitializerWithConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As Long = 5, y As Double = 17'BIND:"5"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(5, semanticInfo.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub LocalInitializerWithConversion2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As Long = 5, y As Double = 17'BIND:"17"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Double", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(17, semanticInfo.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub ArgumentWithParentConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As Integer = 5
        Dim y As Long = func(x)'BIND:"x"
    End Sub

    Function func(x As Long) As Long
        Return x
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("x As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LocalWithConversionInParent()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As Integer = 5
        Dim y As Long = x'BIND:"x"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("x As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(539179, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539179")>
        <Fact()>
        Public Sub LocalWithConversionInParent2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class C
    Sub M()
        Dim x As UShort = 99 + 1
        Dim y As ULong
        y = x'BIND:"x"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.UInt16", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.UInt64", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("x As System.UInt16", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub FieldInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private F = 1 + G()'BIND:"1 + G()"
    Shared Function G() As Integer
        Return 1
    End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of BinaryExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function System.Int32.op_Addition(left As System.Int32, right As System.Int32) As System.Int32",
                         semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AutoPropertyInitializers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Private Const F As Integer = 1
    Property P = 1 + F'BIND:"1 + F"
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of BinaryExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningValue, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function System.Int32.op_Addition(left As System.Int32, right As System.Int32) As System.Int32",
                         semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(2, semanticInfo.ConstantValue.Value)
        End Sub

        <WorkItem(541562, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541562")>
        <Fact()>
        Public Sub ObjectCreationInAsNew()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C1
    Dim Scen2 As New C2() 'BIND:"New C2()"

    Class C2
        Class C3
        End Class
    End Class
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal("C1.C2", semanticInfo.Type.ToString())
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.False(semanticInfo.ConstantValue.HasValue)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(541563, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541563")>
        <Fact()>
        Public Sub NewDelegateCreation()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Test
    Delegate Sub DSub()
    Sub DMethod()
    End Sub

    Sub Main()
        Dim dd As DSub = New DSub(AddressOf DMethod) 'BIND:"New DSub(AddressOf DMethod)"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo.Type)
            Assert.Equal("Test.DSub", semanticInfo.Type.ToString())
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.False(semanticInfo.ConstantValue.HasValue)
            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(541581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541581")>
        <Fact()>
        Public Sub ImplicitConversionTestFieldInit()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1
    Dim x1 As Long = 44 'BIND:"44"

    Sub Main(ByVal args As String())
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal(System_Int32, semanticInfo.Type.SpecialType)
            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(44, semanticInfo.ConstantValue.Value)
            Assert.True(semanticInfo.ImplicitConversion.IsWidening)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal(System_Int64, semanticInfo.ConvertedType.SpecialType)
        End Sub


        <Fact()>
        Public Sub IdentityCIntConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim x As Integer = 7
        Dim y As Integer
        y = CInt(x)'BIND:"CInt(x)"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of PredefinedCastExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <WorkItem(528541, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528541")>
        <Fact()>
        Public Sub ImplicitConversionTestLongNumericToInteger()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x1 As Integer = 45L 'BIND:"45L"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal(System_Int64, semanticInfo.Type.SpecialType)
            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(45L, semanticInfo.ConstantValue.Value)
            Assert.Equal(System_Int32, semanticInfo.ConvertedType.SpecialType)

            ' Perhaps surprisingly, this is a widening conversion.
            ' Section 8.8: Widening conversions: 
            '     From a constant expression of type ULong, Long, UInteger, Integer, UShort, Short, Byte, or SByte 
            '     to a narrower type, provided the value of the constant expression is within the range of the destination type.

            Assert.True(semanticInfo.ImplicitConversion.IsNumeric)
            Assert.False(semanticInfo.ImplicitConversion.IsNarrowing)
            Assert.True(semanticInfo.ImplicitConversion.IsWidening)
            Assert.True((semanticInfo.ImplicitConversion.Kind And ConversionKind.InvolvesNarrowingFromNumericConstant) <> 0, "includes bit InvolvesNarrowingFromNumericConstant")
        End Sub

        <WorkItem(541596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541596")>
        <Fact()>
        Public Sub ImplicitConversionExprReturnedByLambda()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Dim x1 As Func(Of Long) = Function() 45 'BIND:"45"
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int64", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticInfo.ImplicitConversion.Kind)

            Assert.False(semanticInfo.ImplicitConversion.IsIdentity)
            Assert.False(semanticInfo.ImplicitConversion.IsNarrowing)
            Assert.True(semanticInfo.ImplicitConversion.IsNumeric)
            Assert.True(semanticInfo.ImplicitConversion.IsWidening)

            Assert.True(semanticInfo.ConstantValue.HasValue)
            Assert.Equal(45, semanticInfo.ConstantValue.Value)
        End Sub

        <WorkItem(541608, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541608")>
        <Fact()>
        Public Sub IncompleteAttributeOnMethod()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class B
    <A(
    Sub Main() 'BIND:"Main"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.NotNull(semanticInfo)
            Assert.Equal(SymbolKind.ErrorType, semanticInfo.Type.Kind)
        End Sub

        <WorkItem(541625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541625")>
        <Fact()>
        Public Sub ImplicitConvExtensionMethodReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Module StringExtensions
    <Extension()>
    Public Sub Print(ByVal aString As Object)
    End Sub
End Module

Module Program
    Sub Main(args As String())
        Dim example As String = "Hello"
        example.Print()'BIND:"example"
    End Sub
End Module
    ]]></file>
</compilation>, {ExtensionAssemblyRef})

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)
            Assert.True(semanticInfo.ImplicitConversion.IsReference)
            Assert.True(semanticInfo.ImplicitConversion.IsWidening)

            Assert.Equal("example As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
        End Sub

        <Fact()>
        Public Sub NamedArgument1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Class1
    Public Sub f(a As Integer)
    End Sub

    Public Sub f(b As Integer, a As String)
    End Sub

End Class

Public Module M1
    Sub goo()
        Dim x As New Class1

        x.f(4, a:="hello")'BIND:"a"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("a As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamedArgument2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class Class1
    Public Sub f(a As Integer)
    End Sub

    Public Sub f(b As Integer, a As String)
    End Sub

    Public Sub f(q As Integer, b As String, c As Integer)
    End Sub

    Public Sub f(q As Integer, b As String, c As Guid)
    End Sub

End Class

Public Module M1
    Sub goo()
        Dim x As New Class1

        x.f(4, "hithere", b:="hello")'BIND:"b"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(2, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString() & s.ContainingSymbol.ToTestDisplayString()).ToArray()
            Assert.Equal("b As System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, sortedCandidates(0).Kind)
            Assert.Equal("Sub Class1.f(q As System.Int32, b As System.String, c As System.Guid)", sortedCandidates(0).ContainingSymbol.ToTestDisplayString())

            Assert.Equal("b As System.String", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, sortedCandidates(1).Kind)
            Assert.Equal("Sub Class1.f(q As System.Int32, b As System.String, c As System.Int32)", sortedCandidates(1).ContainingSymbol.ToTestDisplayString())

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamedArgument3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class Class1
    Public Sub f(a As Integer)
    End Sub

    Public Sub f(b As Integer, a As String)
    End Sub

    Public Sub f(q As Integer, b As String, c As Integer)
    End Sub

    Public Sub f(q As Integer, b As String, c As Guid)
    End Sub

End Class

Public Module M1
    Sub goo()
        Dim x As New Class1

        x.f(4, "hithere", zappa:="hello")'BIND:"zappa"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamedArgumentInOnProperty()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class Class1
    Public ReadOnly Property f(a As Integer) As Integer
        Get
            Return 1
        End Get
    End Property

    Public ReadOnly Property f(b As Integer, a As String)
        Get
            Return 1
        End Get
    End Property

    Public ReadOnly Property f(q As Integer, b As String, c As Integer)
        Get
            Return 1
        End Get
    End Property

    Public ReadOnly Property f(q As Integer, b As String, c As Guid)
        Get
            Return 1
        End Get
    End Property

End Class

Public Module M1
    Sub goo()
        Dim x As New Class1

        Dim y As Integer = x.f(4, c:=12, b:="hi") 'BIND:"c"'BIND:"c"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("c As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.Equal("ReadOnly Property Class1.f(q As System.Int32, b As System.String, c As System.Int32) As System.Object", semanticInfo.Symbol.ContainingSymbol.ToTestDisplayString())
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


        <WorkItem(541412, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541412")>
        <Fact()>
        Public Sub TestGetSemanticInfoFromAttributeSyntax_Error_MissingSystemImport()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class AAttribute
    Inherits Attribute

    Public Sub New()
    End Sub
End Class

Class B
    <A()> 'BIND:"A()"
    Sub S()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("AAttribute", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal("AAttribute", semanticSummary.ConvertedType.ToTestDisplayString())

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticSummary.CandidateReason)

            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString() & s.ContainingSymbol.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute..ctor()", sortedCandidates(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TestGetSemanticInfoFromAttributeSyntax_Error_MustInheritAttributeClass()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

MustInherit Class AAttribute
    Inherits Attribute
    Public Sub New()
    End Sub

    ' Inaccessible constructors shouldn't appear in semantic info method group.
    Private Sub New(x as Integer)
    End Sub
End Class

Class B
    <A()>'BIND:"A()"
    Sub S()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("AAttribute", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal("AAttribute", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TestGetSemanticInfoFromIdentifierSyntax_Error_MustInheritAttributeClass()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

MustInherit Class AAttribute
    Inherits Attribute
    Public Sub New()
    End Sub
End Class

Class B
    <A()>'BIND:"A"
    Sub S()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("AAttribute", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("AAttribute", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub TestGetSemanticInfoFromAttributeSyntax_Error_GenericAttributeClass()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class AAttribute(Of T)
    Public Sub New()
    End Sub
End Class

Class B
    <AAttribute()>'BIND:"AAttribute()"
    Sub S()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("AAttribute(Of T)", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal("AAttribute(Of T)", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.WrongArity, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute(Of T)..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AAttribute(Of T)..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(539822, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539822")>
        <Fact()>
        Public Sub UseTypeAsVariable()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module Program
    Sub Main(args As String())
        [String] = "hello"'BIND:"[String]"
    End Sub
End Module

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.NotAValue, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("System.String", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ForEachWithOneDimensionalArray()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        For Each element as Integer In arr 'BIND:"For Each element as Integer In arr"
            Console.WriteLine(element)
        Next
    End Sub
End Class   
]]></file>
</compilation>)

            Dim getEnumerator = DirectCast(DirectCast(compilation.GetSpecialType(System_Array), TypeSymbol).GetMember("GetEnumerator"), MethodSymbol)
            Dim moveNext = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), MethodSymbol)
            Dim current = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), PropertySymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(current, semanticInfoEx.CurrentProperty)
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachWithMultiDimensionalArray()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        Dim arr(1,1) As Integer
        arr(0,0) = 1
        arr(0,1) = 2
        arr(1,0) = 3
        arr(1,1) = 4

        For Each element as Integer In arr 'BIND:"For Each element as Integer In arr"
            Console.WriteLine(element)
        Next
    End Sub
End Class   
]]></file>
</compilation>)

            Dim getEnumerator = DirectCast(DirectCast(compilation.GetSpecialType(System_Array), TypeSymbol).GetMember("GetEnumerator"), MethodSymbol)
            Dim moveNext = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), MethodSymbol)
            Dim current = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), PropertySymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(current, semanticInfoEx.CurrentProperty)
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachOverString()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Option Infer On

Imports System

Class C1
    Public Shared Sub Main()
        Dim coll as String = "Hello!"
        For Each element In coll 'BIND:"For Each element In coll"
            Console.WriteLine(element)
        Next
    End Sub
End Class   
]]></file>
</compilation>)

            Dim getEnumerator = DirectCast(DirectCast(compilation.GetSpecialType(System_String), TypeSymbol).GetMember("GetEnumerator"), MethodSymbol)
            Dim moveNext = DirectCast(getEnumerator.ReturnType.GetMember("MoveNext"), MethodSymbol)
            Dim current = DirectCast(getEnumerator.ReturnType.GetMember("get_Current"), MethodSymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(DirectCast(current.AssociatedSymbol, PropertySymbol), semanticInfoEx.CurrentProperty)
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachCustomCollection()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection 'BIND:"For Each element as Custom In myCustomCollection"
            Console.WriteLine("goo")
        Next
    End Sub
End Class        
]]></file>
</compilation>)

            Dim getEnumerator = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetMember("GetEnumerator"), MethodSymbol)
            Dim moveNext = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetTypeMember("CustomEnumerator").GetMember("MoveNext"), MethodSymbol)
            Dim current = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetTypeMember("CustomEnumerator").GetMember("Current"), PropertySymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If


                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(current, semanticInfoEx.CurrentProperty)
                    Assert.Null(semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachCustomCollectionWithDispose()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Implements IDisposable

        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property

        Public Sub Dispose() implements IDisposable.Dispose
        End Sub
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection 'BIND:"For Each element as Custom In myCustomCollection"
            Console.WriteLine("goo")
        Next
    End Sub
End Class        
]]></file>
</compilation>)

            Dim getEnumerator = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetMember("GetEnumerator"), MethodSymbol)
            Dim moveNext = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetTypeMember("CustomEnumerator").GetMember("MoveNext"), MethodSymbol)
            Dim current = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetTypeMember("CustomEnumerator").GetMember("Current"), PropertySymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(current, semanticInfoEx.CurrentProperty)
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachCustomCollectionWithDisposeError()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Implements IDisposable

        Public Function MoveNext() As Boolean
            Return False
        End Function

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection 'BIND:"For Each element as Custom In myCustomCollection"
            Console.WriteLine("goo")
        Next
    End Sub
End Class        
]]></file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30149: Class 'CustomEnumerator' must implement 'Sub Dispose()' for interface 'IDisposable'.
        Implements IDisposable
                   ~~~~~~~~~~~
</expected>)

            Dim getEnumerator = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetMember("GetEnumerator"), MethodSymbol)
            Dim moveNext = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetTypeMember("CustomEnumerator").GetMember("MoveNext"), MethodSymbol)
            Dim current = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetTypeMember("CustomEnumerator").GetMember("Current"), PropertySymbol)
            ' the type claimed to implement IDisposable and we believed it ...
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If


                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(current, semanticInfoEx.CurrentProperty)
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachCustomCollectionWithMissingMoveNext()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections

Class Custom
    Public Function GetEnumerator() As CustomEnumerator
        Return Nothing
    End Function

    Public Class CustomEnumerator
        Implements IDisposable

        Public ReadOnly Property Current As Custom
            Get
                Return Nothing
            End Get
        End Property

        Public Sub Dispose() implements IDisposable.Dispose
        End Sub
    End Class
End Class

Class C1
    Public Shared Sub Main()
        Dim myCustomCollection As Custom = nothing

        For Each element as Custom In myCustomCollection 'BIND:"For Each element as Custom In myCustomCollection"
            Console.WriteLine("goo")
        Next
    End Sub
End Class        
]]></file>
</compilation>)

            Dim getEnumerator = DirectCast(compilation.GlobalNamespace.GetTypeMember("Custom").GetMember("GetEnumerator"), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod) ' methods are partly present up to the point where the pattern was violated.
                    Assert.Null(semanticInfoEx.MoveNextMethod)
                    Assert.Null(semanticInfoEx.CurrentProperty)
                    Assert.Null(semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachNotMatchingDesignPatternIEnumerable()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable() 'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Implements System.Collections.IEnumerable
    ' Explicit implementation won't match pattern.
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class
]]></file>
</compilation>)

            Dim getEnumerator = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator), MethodSymbol)
            Dim moveNext = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), MethodSymbol)
            Dim current = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__Current), PropertySymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(current, semanticInfoEx.CurrentProperty)
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachNotMatchingDesignPatternGenericIEnumerable()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable(Of Integer)() 'BIND:"For Each x In New Enumerable(Of Integer)()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable(Of T)
    Implements System.Collections.Generic.IEnumerable(Of Integer)

    ' Explicit implementation won't match pattern.
    Public Function System_Collections_Generic_IEnumerable_GetEnumerator() As System.Collections.Generic.IEnumerator(Of Integer) Implements System.Collections.Generic.IEnumerable(Of Integer).GetEnumerator
        Return Nothing
    End Function

    Public Function GetEnumerator1() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class
]]></file>
</compilation>)

            ' IEnumerable is preferred to IEnumerable(Of T)
            Dim ienumerable = compilation.GetSpecialType(System_Collections_Generic_IEnumerable_T).Construct(ImmutableArray.Create(Of TypeSymbol)(compilation.GetSpecialType(System_Int32)))
            Dim ienumerator = compilation.GetSpecialType(System_Collections_Generic_IEnumerator_T).Construct(ImmutableArray.Create(Of TypeSymbol)(compilation.GetSpecialType(System_Int32)))
            Dim getEnumerator = DirectCast(ienumerable.GetMember("GetEnumerator"), MethodSymbol)
            Dim moveNext = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), MethodSymbol)
            Dim current = DirectCast(ienumerator.GetMember("Current"), PropertySymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal(getEnumerator, semanticInfoEx.GetEnumeratorMethod)
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    Assert.Equal(current, semanticInfoEx.CurrentProperty)
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachGenericIEnumerable()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Class C
    Public Shared Sub Main()
        Dim collection as IEnumerable(Of Integer)
        For Each x In collection 'BIND:"For Each x In collection"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
]]></file>
</compilation>)

            ' the first matching symbol on IEnumerable(Of T)
            Dim moveNext = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), MethodSymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal("Function System.Collections.Generic.IEnumerable(Of System.Int32).GetEnumerator() As System.Collections.Generic.IEnumerator(Of System.Int32)",
                                 semanticInfoEx.GetEnumeratorMethod.ToDisplayString(SymbolDisplayFormat.TestFormat))
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)
                    ' the first matching symbol on IEnumerable(Of T)
                    Assert.Equal("ReadOnly Property System.Collections.Generic.IEnumerator(Of System.Int32).Current As System.Int32",
                                 semanticInfoEx.CurrentProperty.ToDisplayString(SymbolDisplayFormat.TestFormat))
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachInvalidCollection()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
 Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        For Each element as Integer In 1 'BIND:"For Each element as Integer In 1"
            Console.WriteLine(element)
        Next
    End Sub
End Class   
]]></file>
</compilation>)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Null(semanticInfoEx.GetEnumeratorMethod)
                    Assert.Null(semanticInfoEx.MoveNextMethod)
                    Assert.Null(semanticInfoEx.CurrentProperty)
                    Assert.Null(semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub ForEachLateBinding()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict off
Class C
    Public Shared Sub Main()
        Dim collection as Object = {1, 2, 3}
        For Each x In collection 'BIND:"For Each x In collection"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
]]></file>
</compilation>)

            ' the first matching symbol on IEnumerable
            Dim moveNext = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerator__MoveNext), MethodSymbol)
            Dim dispose = DirectCast(compilation.GetSpecialType(System_Object).ContainingAssembly.GetSpecialTypeMember(SpecialMember.System_IDisposable__Dispose), MethodSymbol)

            For Each useInterface In {True, False}
                For Each useBlock In {True, False}

                    Dim semanticInfoEx As ForEachStatementInfo
                    If useInterface Then
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, SemanticModel)(compilation, "a.vb",
                                                                                                                              useParent:=useBlock),
                                                    ForEachStatementInfo)
                    Else
                        semanticInfoEx = DirectCast(GetBlockOrStatementInfoForTest(Of ForEachStatementSyntax, VBSemanticModel)(compilation, "a.vb",
                                                                                                                             useParent:=useBlock),
                                                    ForEachStatementInfo)
                    End If

                    Assert.Equal("Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator",
                                 semanticInfoEx.GetEnumeratorMethod.ToDisplayString(SymbolDisplayFormat.TestFormat))
                    Assert.Equal(moveNext, semanticInfoEx.MoveNextMethod)

                    Assert.Equal("ReadOnly Property System.Collections.IEnumerator.Current As System.Object",
                                 semanticInfoEx.CurrentProperty.ToDisplayString(SymbolDisplayFormat.TestFormat))
                    Assert.Equal(dispose, semanticInfoEx.DisposeMethod)
                Next
            Next
        End Sub

        <Fact()>
        Public Sub NewDelegate()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del 
        d = New Del(AddressOf goo)'BIND:"Del"
    End Sub

    Sub goo(x As Integer)
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Del", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewDelegate2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del 
        d = New Del(AddressOf goo) 'BIND:"New Del(AddressOf goo)"
    End Sub

    Sub goo(x As Integer)
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Del", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind)
            Assert.Equal("Del", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewDelegateWrongMethodSignature()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del
        d = New Del(AddressOf goo)'BIND:"Del"
    End Sub

    Sub goo(y As String, z As String)
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Del", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewDelegateWrongMethodSignature2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del
        d = New Del(AddressOf goo)'BIND:"New Del(AddressOf goo)"
    End Sub

    Sub goo(y As String, z As String)
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Del", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind)
            Assert.Equal("Del", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewDelegateOnLambda()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del
        d = New Del(Sub(x) Console.WriteLine(x)) 'BIND:"Del"'BIND:"Del"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Del", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewDelegateOnLambda2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del
        d = New Del(Sub(x) Console.WriteLine(x)) 'BIND:"New Del(Sub(x) Console.WriteLine(x))"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Del", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind)
            Assert.Equal("Del", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewDelegateOnMismatchedLambda()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del
        d = New Del(Sub(x, y) Console.WriteLine(x)) 'BIND:"Del"'BIND:"Del"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Del", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewDelegateOnMismatchedLambda2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Delegate Sub Del(x As Integer)

Module Program
    Sub Main(args As String())
        Dim d As Del
        d = New Del(Sub(x, y) Console.WriteLine(x)) 'BIND:"New Del(Sub(x, y) Console.WriteLine(x))"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Del", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.Type.TypeKind)
            Assert.Equal("Del", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfInaccessibleClass()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class X
    Private Class Y
    End Class
End Class

Module Program
    Sub Main(args As String())
        Dim o As Object
        o = New X.Y(3)'BIND:"X.Y"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Assert.Equal("X.Y", semanticInfo.CandidateSymbols(0).ToTestDisplayString())

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfInaccessibleClass2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class X
    Private Class Y
    End Class
End Class

Module Program
    Sub Main(args As String())
        Dim o As Object
        o = New X.Y(3)'BIND:"New X.Y(3)"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("X.Y", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Assert.Equal("Sub X.Y..ctor()", semanticInfo.CandidateSymbols(0).ToTestDisplayString())

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Assert.Equal("Sub X.Y..ctor()", semanticInfo.MemberGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub OverloadResolutionFailureOnNew()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class X
    Public Sub New(x As Integer)

    End Sub
End Class

Module Program
    Sub Main(args As String())
        Dim o As Object
        o = New X(3, 4)'BIND:"X"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("X", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticInfo.CandidateReason)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub OverloadResolutionFailureOnNew2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class X
    Public Sub New(x As Integer)

    End Sub
End Class

Module Program
    Sub Main(args As String())
        Dim o As Object
        o = New X(3, 4)'BIND:"New X(3, 4)"
    End Sub

    Sub goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("X", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(542695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542695")>
        <Fact()>
        Public Sub TestCandidateReasonForInaccessibleMethod()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Class1
    Private Class NestedClass
        Private Shared Sub Method1()

        End Sub
    End Class

    Sub Method1
        NestedClass.Method1()'BIND:"NestedClass.Method1()"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Void", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Class1.NestedClass.Method1()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)
        End Sub

        <WorkItem(542701, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542701")>
        <Fact()>
        Public Sub GenericTypeWithNoTypeArgsOnAttribute_AttributeSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Gen(Of T)

End Class

<Gen()>'BIND:"Gen()"
Class Test
    Sub Method1()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("Gen(Of T)", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal("Gen(Of T)", semanticSummary.ConvertedType.ToTestDisplayString())

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.WrongArity, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Gen(Of T)..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Gen(Of T)..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub GenericTypeWithNoTypeArgsOnAttribute_IdentifierSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Gen(Of T)

End Class

<Gen()>'BIND:"Gen"
Class Test
    Sub Method1()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Gen(Of T)", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Gen(Of T)", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.WrongArity, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Gen(Of T)..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Gen(Of T)..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NextVariable()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class X
    Sub Goo()
        For i As Integer = 1 To 10

        Next i'BIND:"i"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("i As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(542009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542009")>
        <Fact()>
        Public Sub Bug8966()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Option Infer On

Imports System.Runtime.CompilerServices
Imports System

Module M
    Sub Main()
        Dim s As String = ""
        Dim x As Action(Of String) = AddressOf s.ToLowerInvariant'BIND:"ToLowerInvariant"
        x(Nothing)
    End Sub

    <Extension()>
    Sub ToLowerInvariant(ByVal x As Object)
        Console.WriteLine(1)
    End Sub
End Module


    ]]></file>
</compilation>, {SystemCoreRef}, TestOptions.ReleaseExe)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            CompilationUtils.AssertNoErrors(compilation)

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function System.String.ToLowerInvariant() As System.String", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(2, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function System.String.ToLowerInvariant() As System.String", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.Object.ToLowerInvariant()", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off
Option Strict On
Option Infer On

Imports System

Module Program
    Sub Main(args As String())
        If True Then
            x$ = "hello" 'BIND2:"x$"
        End If
        y = x'BIND1:"x"
        Console.WriteLine(y)
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim node2 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            Dim semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node1)

            Assert.Equal("System.String", semanticInfo1.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo1.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo1.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo1.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo1.ImplicitConversion.Kind)

            Assert.Equal("x As System.String", semanticInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo1.Symbol.Kind)

            Dim semanticInfo2 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node2)

            Assert.Equal("System.String", semanticInfo2.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo2.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo2.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo2.ConvertedType.TypeKind)

            Assert.Equal("x As System.String", semanticInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo2.Symbol.Kind)

            Assert.Same(semanticInfo1.Symbol, semanticInfo2.Symbol)
        End Sub

        <Fact()>
        Public Sub ImplicitLocal2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off
Option Strict On
Option Infer On

Imports System

Module Program
    Sub Main(args As String())
        Dim a1 As Action = Sub()
                               x$ = "hello"'BIND2:"x$"
                           End Sub
        Dim a2 As Action = Sub()
                               y = x'BIND1:"x"
                               Console.WriteLine(y)
                           End Sub
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node1 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim node2 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            Dim semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node1)

            Assert.Equal("System.String", semanticInfo1.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo1.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo1.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo1.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo1.ImplicitConversion.Kind)

            Assert.Equal("x As System.String", semanticInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo1.Symbol.Kind)

            Dim semanticInfo2 = CompilationUtils.GetSemanticInfoSummary(semanticModel, node2)

            Assert.Equal("System.String", semanticInfo2.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo2.Type.TypeKind)
            Assert.Equal("System.String", semanticInfo2.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo2.ConvertedType.TypeKind)

            Assert.Equal("x As System.String", semanticInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo2.Symbol.Kind)

            Assert.Same(semanticInfo1.Symbol, semanticInfo2.Symbol)
        End Sub

        <Fact()>
        Public Sub ImplicitLocalInLambdaInInitializer()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off
Option Strict On
Option Infer On

Imports System

Module Module1
    Dim y As Integer = InvokeMultiple(Function()
                                          x% = x% + 1'BIND1:"x%"
                                          Return x%
                                      End Function, 2) +
                        InvokeMultiple(Function()
                                           x% = x% + 1
                                           Return x%'BIND2:"x%"
                                       End Function, 7)

    Function InvokeMultiple(f As Func(Of Integer), times As Integer) As Integer
        Dim result As Integer = 0
        For i As Integer = 1 To times
            result = f()
        Next
        Return result
    End Function

    Sub Main()
        Console.WriteLine(y)
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticInfo1 = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            Assert.Equal("System.Int32", semanticInfo1.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo1.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo1.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo1.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo1.ImplicitConversion.Kind)

            Assert.Equal("x As System.Int32", semanticInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo1.Symbol.Kind)
            Assert.Equal(0, semanticInfo1.CandidateSymbols.Length)

            Assert.Equal(SymbolKind.Method, semanticInfo1.Symbol.ContainingSymbol.Kind)
            Dim containingMethod1 = DirectCast(semanticInfo1.Symbol.ContainingSymbol, MethodSymbol)
            Assert.True(containingMethod1.IsLambdaMethod, "variable should be contained by a lambda")

            Dim semanticInfo2 = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 2)

            Assert.Equal("System.Int32", semanticInfo2.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo2.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo2.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo2.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo2.ImplicitConversion.Kind)

            Assert.Equal("x As System.Int32", semanticInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Local, semanticInfo2.Symbol.Kind)
            Assert.Equal(0, semanticInfo2.CandidateSymbols.Length)

            Assert.Equal(SymbolKind.Method, semanticInfo2.Symbol.ContainingSymbol.Kind)
            Dim containingMethod2 = DirectCast(semanticInfo2.Symbol.ContainingSymbol, MethodSymbol)
            Assert.True(containingMethod2.IsLambdaMethod, "variable should be contained by a lambda")

            ' Should be different variables in different lambdas.
            Assert.NotSame(semanticInfo1.Symbol, semanticInfo2.Symbol)
            Assert.NotEqual(semanticInfo1.Symbol, semanticInfo2.Symbol)
            Assert.NotSame(containingMethod1, containingMethod2)
            Assert.NotEqual(containingMethod1, containingMethod2)
        End Sub


        <WorkItem(542301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542301")>
        <Fact()>
        Public Sub Bug9489()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Collections
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim col = New ObjectModel.Collection(Of MetadataReference)() From {ref1, ref2, ref3}'BIND:"Collection(Of MetadataReference)"
    End Sub
End Module



    ]]></file>
</compilation>, {SystemCoreRef}, TestOptions.ReleaseExe)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of GenericNameSyntax)(compilation, "a.vb")

        End Sub

        <WorkItem(542596, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542596")>
        <Fact()>
        Public Sub BindMethodInvocationWhenUnnamedArgFollowsNamed()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Module Module1

    Sub M1(x As Integer, y As Integer)
    End Sub

    Sub Main()
        M1(x:=2, 3) 'BIND:"M1(x:=2, 3)"
    End Sub

End Module
        </file>
    </compilation>, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            compilation.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        M1(x:=2, 3) 'BIND:"M1(x:=2, 3)"
                 ~
                                               </errors>)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")
            Assert.Equal("Sub Module1.M1(x As System.Int32, y As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())

            compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
    <compilation>
        <file name="a.vb">
Module Module1

    Sub M1(x As Integer, y As Integer)
    End Sub

    Sub M1(x As Integer, y As Long)
    End Sub

    Sub Main()
        M1(x:=2, 3) 'BIND:"M1(x:=2, 3)"
    End Sub

End Module
        </file>
    </compilation>, parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15))

            compilation.AssertTheseDiagnostics(<errors>
BC30241: Named argument expected. Please use language version 15.5 or greater to use non-trailing named arguments.
        M1(x:=2, 3) 'BIND:"M1(x:=2, 3)"
                 ~
                                               </errors>)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Sub Module1.M1(x As System.Int32, y As System.Int32)", semanticInfo.Symbol.ToTestDisplayString())

        End Sub

        <WorkItem(542332, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542332")>
        <Fact()>
        Public Sub BindArrayBoundOfField()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class class1
    Public zipf As Integer = 7
    Public b, quux(zipf) As Integer'BIND:"zipf"
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("class1.zipf As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(542858, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542858")>
        <Fact()>
        Public Sub TypeNamesInsideCastExpression()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())
        Dim func1 As Func(Of Integer, Integer) = Function(x) x + 1 
        Dim type1 = CType(func1, Func(Of Integer, Integer)) 'BIND:"Func(Of Integer, Integer)"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of GenericNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Func(Of System.Int32, System.Int32)", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Func(Of System.Int32, System.Int32)", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.Func(Of System.Int32, System.Int32)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(542933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542933")>
        <Fact()>
        Public Sub CTypeOnALambdaExpr()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim f2 As Object = CType(Function(x) x + 5, Func(Of Integer, Integer))'BIND:"CType(Function(x) x + 5, Func(Of Integer, Integer))"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CTypeExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Func(Of System.Int32, System.Int32)", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference Or ConversionKind.DelegateRelaxationLevelWideningToNonLambda, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AliasInLocalDecl()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports A = System.String

Module Program
    Sub Main(args As String())
        Dim local As A = Nothing'BIND:"A"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.String", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal("A=System.String", semanticSummary.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticSummary.Alias.Kind)
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Alias.Target.Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AliasInTryCast()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports A = System.String

Module Program
    Sub Main(args As String())
        Dim local As A = Nothing
        Dim x = TryCast(local, A)'BIND:"A"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.String", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal("A=System.String", semanticSummary.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticSummary.Alias.Kind)
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Alias.Target.Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AliasInDirectCast()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports A = System.String

Module Program
    Sub Main(args As String())
        Dim local As A = Nothing
        Dim x = DirectCast(local, A)'BIND:"A"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.String", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal("A=System.String", semanticSummary.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticSummary.Alias.Kind)
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Alias.Target.Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AliasInCType()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports A = System.String

Module Program
    Sub Main(args As String())
        Dim local As A = Nothing
        Dim x = CType(local, A)'BIND:"A"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.String", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal("A=System.String", semanticSummary.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticSummary.Alias.Kind)
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Alias.Target.Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AliasInGenericTypeArgument()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports A = System.String

Module Program
    Sub Main(args As String())
        Dim local As A = Nothing
        Dim x = CType(local, IEnumerable(Of A))'BIND:"A"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.String", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.String", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.String", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal("A=System.String", semanticSummary.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticSummary.Alias.Kind)
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Alias.Target.Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(542885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542885")>
        <Fact()>
        Public Sub AliasInGenericArgOfLocal()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Threading
Imports Thr = System.Threading.Thread

Module Program
    Sub Main(args As String())
        Dim q = New List(Of Thr)'BIND:"Thr"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Threading.Thread", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Threading.Thread", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.Threading.Thread", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal("Thr=System.Threading.Thread", semanticSummary.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticSummary.Alias.Kind)
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Alias.Target.Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(542841, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542841")>
        <Fact()>
        Public Sub AliasInArrayCreation()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports IClon = System.ICloneable

Module M
    Sub Goo()
        Dim y = New IClon() {}'BIND:"IClon"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.ICloneable", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticSummary.Type.TypeKind)
            Assert.Equal("System.ICloneable", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("System.ICloneable", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal("IClon=System.ICloneable", semanticSummary.Alias.ToTestDisplayString())
            Assert.Equal(SymbolKind.Alias, semanticSummary.Alias.Kind)
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Alias.Target.Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(542808, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542808")>
        <Fact()>
        Public Sub InvocationWithMissingCloseParen()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Diagnostics
Class C
    Sub M()
        Dim watch = Stopwatch.StartNew('BIND:"Stopwatch.StartNew"
        watch.Start()
    End Sub
End Class
    ]]></file>
</compilation>, references:={SystemRef})

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function System.Diagnostics.Stopwatch.StartNew() As System.Diagnostics.Stopwatch", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function System.Diagnostics.Stopwatch.StartNew() As System.Diagnostics.Stopwatch", sortedMethodGroup(0).ToTestDisplayString())
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub FailedOverloadResolutionOnCallShouldHaveMemberGroup()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim v As A = New A()
        dim r = v.Goo("hello")'BIND:"Goo"
    End Sub


End Module

Class A
    Public Function Goo() As Integer
        Return 1
    End Function
    'Public Function Goo(x as integer, y as Integer) As Integer
    '    Return 1
    'End Function
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function A.Goo() As System.Int32", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function A.Goo() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub MyBaseNew()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Class B
    Sub New()
        Console.WriteLine("B constructor")
    End Sub
End Class

Class C
    Inherits B

    Sub New()
        'Dim q = 42

        MyBase.New()'BIND:"New"
    End Sub

End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub B..ctor()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub B..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub MyBaseNew2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Class B
    Sub New()
        Console.WriteLine("B constructor")
    End Sub
End Class

Class C
    Inherits B

    Sub New()
        Dim q = 42

        MyBase.New()'BIND:"New"
    End Sub

End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub B..ctor()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub B..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub MyBaseNew3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Class B
    Sub New()
        Console.WriteLine("B constructor")
    End Sub
End Class

Class C
    Inherits B

    Sub New()
        'Dim q = 42

        MyBase.New()'BIND:"MyBase.New()"
    End Sub

End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Void", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub B..ctor()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub MyBaseNew4()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Class B
    Sub New()
        Console.WriteLine("B constructor")
    End Sub
End Class

Class C
    Inherits B

    Sub New()
        Dim q = 42

        MyBase.New()'BIND:"MyBase.New()"
    End Sub

End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Void", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub B..ctor()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(542941, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542941")>
        <Fact()>
        Public Sub QualifiedTypeInDim()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class Program
    Shared Sub Main()
        Dim c = HttpContext.Current'BIND:"HttpContext"
    End Sub
End Class
Class HttpContext
    Public Shared Property Current As Object
End Class


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("HttpContext", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("HttpContext", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("HttpContext", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(543031, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543031")>
        <Fact()>
        Public Sub MissingIdentifierSyntaxNodeIncompleteMethodDecl()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As 

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim node As ExpressionSyntax = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName).AsNode(), ExpressionSyntax)
            Dim info = compilation.GetSemanticModel(tree).GetTypeInfo(node)

            Assert.NotNull(info)
            Assert.Equal(TypeInfo.None, info)
        End Sub

        <WorkItem(543099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543099")>
        <Fact()>
        Public Sub GetSymbolForOptionalParamMethodCall()
            Dim compilation = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
Imports System
Module Program
    Sub Goo(x As Integer, Optional y As Double = #1/1/2001#)

    End Sub
    Sub Main(args As String())
        Goo(1)'BIND:"Goo"
    End Sub
End Module

    ]]></file>
            </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub Program.Goo(x As System.Int32, [y As System.Double])", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Program.Goo(x As System.Int32, [y As System.Double])", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub
        <WorkItem(10607, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub GetSymbolForOptionalParamMethodCallWithOutParenthesis()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Goo(Optional i As Integer = 1)
    End Sub
    Sub Main(args As String())
        Goo'BIND:"Goo"
    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Void", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Void", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub Program.Goo([i As System.Int32 = 1])", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub
        <WorkItem(542217, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542217")>
        <Fact()>
        Public Sub ConflictingAliases()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports S = System
Imports S = System.IO

Module Program
    Sub Main()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim aliases = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of SimpleImportsClauseSyntax).ToArray()

            Assert.Equal(2, aliases.Length)

            Dim alias1 = model.GetDeclaredSymbol(aliases(0))
            Assert.NotNull(alias1)
            Assert.Equal("System", alias1.Target.ToTestDisplayString())

            Dim alias2 = model.GetDeclaredSymbol(aliases(1))
            Assert.NotNull(alias2)
            Assert.Equal("System.IO", alias2.Target.ToTestDisplayString())

            Assert.NotEqual(alias1.Locations.Single(), alias2.Locations.Single())

            ' This symbol we re-use.
            Dim alias1b = model.GetDeclaredSymbol(aliases(0))
            Assert.Same(alias1, alias1b)

            ' This symbol we generate on-demand.
            Dim alias2b = model.GetDeclaredSymbol(aliases(1))
            Assert.NotSame(alias2, alias2b)
            Assert.Equal(alias2, alias2b)
        End Sub



        <Fact()>
        Public Sub StaticLocals()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Function goo() As Integer
        Static i As Integer = 23
        i = i + 1
        Return i
    End Function

    Public Shared Sub Main()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim staticLocals = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax).ToArray()
            Assert.Equal(1, staticLocals.Length)

            Dim SLDeclaration As LocalDeclarationStatementSyntax = staticLocals(0)

            'Static Locals are Not Supported for this API
            Assert.Throws(Of NotSupportedException)(Sub()
                                                        Dim i = model.GetDeclaredSymbolFromSyntaxNode(SLDeclaration)
                                                    End Sub)


            Dim containingType = DirectCast(model, SemanticModel).GetEnclosingSymbol(SLDeclaration.SpanStart)
            Assert.Equal("Function C.goo() As System.Int32", DirectCast(containingType, Symbol).ToTestDisplayString())

            'GetSymbolInfo
            'GetSpeculativeSymbolInfo()
            'GetTypeInfo()
            Dim TI = DirectCast(model, SemanticModel).GetTypeInfo(SLDeclaration)
            Dim mG = DirectCast(model, SemanticModel).GetAliasInfo(SLDeclaration)

            Dim lus1 = DirectCast(model, SemanticModel).LookupSymbols(SLDeclaration.SpanStart, name:="i")


            'GetAliasImports - only applicable for Imports statements
            'ConstantValue
        End Sub

        <WorkItem(530631, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530631")>
        <Fact()>
        Public Sub Bug16603()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module Program
    Sub Main()
        Dim x As Action = Sub() x = Sub(, y = x
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim identifiers = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax).ToArray()
            Assert.Equal(4, identifiers.Length)

            Dim id As IdentifierNameSyntax = identifiers(3)

            ' No crashes
            Dim ai = DirectCast(model, SemanticModel).GetAliasInfo(id)
            Dim si = DirectCast(model, SemanticModel).GetSymbolInfo(id)
        End Sub

        <WorkItem(543278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543278")>
        <Fact()>
        Public Sub ModuleNameInObjectCreationExpr()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x1 = New Program() 'BIND:"Program"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, semanticSummary.CandidateReason)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Program", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ModuleNameInObjectCreationExpr2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x1 = New Program() 'BIND:"New Program()"
    End Sub
End Module
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor' is not defined.
Module Program
       ~~~~~~~
BC30371: Module 'Program' cannot be used as a type.
        Dim x1 = New Program() 'BIND:"New Program()"
                     ~~~~~~~
                                               </expected>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("Program", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Module, semanticSummary.Type.TypeKind)
            Assert.Equal("Program", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Module, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ClassWithInaccessibleConstructorsInObjectCreationExpr()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C1
    private sub new()
    end Sub
end class

Module Program
    Sub Main(args As String())
        Dim x1 = New C1() 'BIND:"C1"


    End Sub

public mustinherit class Goo
public sub new()
end sub
end class
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("C1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub ClassWithInaccessibleConstructorsInObjectCreationExpr2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C1
    private sub new()
    end Sub
end class

Module Program
    Sub Main(args As String())
        Dim x1 = New C1() 'BIND:"New C1()"


    End Sub

public mustinherit class Goo
public sub new()
end sub
end class
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("C1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("Sub C1..ctor()", semanticSummary.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.CandidateSymbols(0).Kind)
            Assert.False(semanticSummary.ConstantValue.HasValue)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Assert.Equal("Sub C1..ctor()", semanticSummary.MemberGroup(0).ToTestDisplayString())
        End Sub

        <WorkItem(542844, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542844")>
        <Fact()>
        Public Sub Bug10246()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C(Of T)
    Public Field As T
End Class

Class D
    Sub M()
        Call New C(Of Integer).Field.ToString() 'BIND1:"Field"
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel1 = compilation.GetSemanticModel(tree)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo = semanticModel1.GetSymbolInfo(node1)

            Assert.Equal(CandidateReason.NotATypeOrNamespace, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("C(Of System.Int32).Field As System.Int32", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Null(symbolInfo.Symbol)
        End Sub

        <WorkItem(530093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530093")>
        <Fact()>
        Public Sub Bug530093()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Shared Field As C = Me 'BIND1:"Me"
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel1 = compilation.GetSemanticModel(tree)
            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Dim symbolInfo = semanticModel1.GetSymbolInfo(node1)
            Assert.Equal(CandidateReason.StaticInstanceMismatch, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Me As C", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Null(symbolInfo.Symbol)
        End Sub

        <WorkItem(530093, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530093")>
        <Fact()>
        Public Sub Bug530093b()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Shared Field As Object = MyBase 'BIND1:"MyBase"
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel1 = compilation.GetSemanticModel(tree)
            Dim node1 As ExpressionSyntax = CompilationUtils.FindBindingText(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Dim symbolInfo = semanticModel1.GetSymbolInfo(node1)
            Assert.Equal(CandidateReason.StaticInstanceMismatch, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Me As C", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Null(symbolInfo.Symbol)
        End Sub

        <Fact()>
        Public Sub NewOfModule()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim a As Object = New X()'BIND:"X"
    End Sub
End Module

Module X
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("X", semanticSummary.CandidateSymbols(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfModule2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim a As Object = New X()'BIND:"New X()"
    End Sub
End Module

Module X
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("X", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Module, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfInterface()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim a As Object = New X()'BIND:"X"
    End Sub
End Module

Interface X
End Interface
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("X", semanticSummary.CandidateSymbols(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfInterface2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Sub Main(args As String())
        Dim a As Object = New X()'BIND:"New X()"
    End Sub
End Module

Interface X
End Interface
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("X", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfNotCreatable()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C
    Sub F()
        Dim a As Object = New X()'BIND:"X"
    End Sub
End Class

MustInherit Class X
End Class


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("X", semanticSummary.CandidateSymbols(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfNotCreatable2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C
    Sub F()
        Dim a As Object = New X()'BIND:"New X()"
    End Sub
End Class

MustInherit Class X
End Class


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("X", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Inaccessible, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("Sub X..ctor()", semanticSummary.CandidateSymbols(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub X..ctor()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact(), WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")>
        Public Sub NewOfNotCreatable3()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

MustInherit Class X

    Protected Sub New(x as integer)
    End Sub

    Protected Sub New(x as string)
    End Sub

    Sub F()
        Dim a As Object = New X()'BIND1:"New X()"
    End Sub
End Class


    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Count)
            Assert.Equal("X", symbolInfo.CandidateSymbols(0).ToTestDisplayString())

            Dim memberGroup = model.GetMemberGroup(creation.Type)
            Assert.Equal(0, memberGroup.Count)

            Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
            Assert.Null(typeInfo.Type)
            Assert.Null(typeInfo.ConvertedType)
            Dim conv = model.GetConversion(creation.Type)
            Assert.True(conv.IsIdentity)

            memberGroup = model.GetMemberGroup(creation)
            Dim sortedMethodGroup As ISymbol() = memberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal(2, sortedMethodGroup.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub X..ctor(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(creation)
            Assert.Null(symbolInfo.Symbol)
            sortedMethodGroup = symbolInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal(2, sortedMethodGroup.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub X..ctor(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)


            typeInfo = model.GetTypeInfo(creation)
            Assert.Equal("X", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString())
            conv = model.GetConversion(creation)
            Assert.Equal(ConversionKind.WideningReference, conv.Kind)
        End Sub

        <Fact(), WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")>
        Public Sub NewOfNotCreatable4()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

MustInherit Class X

    Protected Sub New(x as integer)
    End Sub

    Protected Sub New(x as string)
    End Sub

    Sub F()
        Dim a As Object = New X(1)'BIND1:"New X(1)"
    End Sub
End Class


    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Count)
            Assert.Equal("X", symbolInfo.CandidateSymbols(0).ToTestDisplayString())

            Dim memberGroup = model.GetMemberGroup(creation.Type)
            Assert.Equal(0, memberGroup.Count)

            Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
            Assert.Null(typeInfo.Type)
            Assert.Null(typeInfo.ConvertedType)
            Dim conv = model.GetConversion(creation.Type)
            Assert.True(conv.IsIdentity)

            memberGroup = model.GetMemberGroup(creation)
            Dim sortedMethodGroup = memberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal(2, sortedMethodGroup.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub X..ctor(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(creation)
            Assert.Equal("Sub X..ctor(x As System.Int32)", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)


            typeInfo = model.GetTypeInfo(creation)
            Assert.Equal("X", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString())
            conv = model.GetConversion(creation)
            Assert.Equal(ConversionKind.WideningReference, conv.Kind)
        End Sub

        <Fact(), WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")>
        Public Sub NewOfNotCreatable5()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C
    Sub F()
        Dim a As Object = New X(1)'BIND1:"New X(1)"
    End Sub
End Class

MustInherit Class X

    Protected Sub New(x as integer)
    End Sub

    Protected Sub New(x as string)
    End Sub

End Class


    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Count)
            Assert.Equal("X", symbolInfo.CandidateSymbols(0).ToTestDisplayString())

            Dim memberGroup = model.GetMemberGroup(creation.Type)
            Assert.Equal(0, memberGroup.Count)

            Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
            Assert.Null(typeInfo.Type)
            Assert.Null(typeInfo.ConvertedType)
            Dim conv = model.GetConversion(creation.Type)
            Assert.True(conv.IsIdentity)

            memberGroup = model.GetMemberGroup(creation)
            Dim sortedMethodGroup = memberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal(2, sortedMethodGroup.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub X..ctor(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(creation)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)

            typeInfo = model.GetTypeInfo(creation)
            Assert.Equal("X", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString())
            conv = model.GetConversion(creation)
            Assert.Equal(ConversionKind.WideningReference, conv.Kind)
        End Sub

        <Fact(), WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")>
        Public Sub NewOfNotCreatable6()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C
    Sub F()
        Dim a As Object = New X()'BIND1:"New X()"
    End Sub
End Class

MustInherit Class X

    Protected Sub New(x as integer)
    End Sub

    Protected Sub New(x as string)
    End Sub

End Class


    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Count)
            Assert.Equal("X", symbolInfo.CandidateSymbols(0).ToTestDisplayString())

            Dim memberGroup = model.GetMemberGroup(creation.Type)
            Assert.Equal(0, memberGroup.Count)

            Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
            Assert.Null(typeInfo.Type)
            Assert.Null(typeInfo.ConvertedType)
            Dim conv = model.GetConversion(creation.Type)
            Assert.True(conv.IsIdentity)

            memberGroup = model.GetMemberGroup(creation)
            Dim sortedMethodGroup As ISymbol() = memberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal(2, sortedMethodGroup.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub X..ctor(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(creation)
            Assert.Null(symbolInfo.Symbol)
            sortedMethodGroup = symbolInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal(2, sortedMethodGroup.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub X..ctor(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)

            typeInfo = model.GetTypeInfo(creation)
            Assert.Equal("X", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString())
            conv = model.GetConversion(creation)
            Assert.Equal(ConversionKind.WideningReference, conv.Kind)
        End Sub

        <Fact(), WorkItem(665920, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665920")>
        Public Sub NewOfNotCreatable7()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

MustInherit Class X

    Protected Sub New(x as integer)
    End Sub

    Sub F()
        Dim a As Object = New X()'BIND1:"New X()"
    End Sub
End Class


    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 1)

            Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, symbolInfo.CandidateReason)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Count)
            Assert.Equal("X", symbolInfo.CandidateSymbols(0).ToTestDisplayString())

            Dim memberGroup = model.GetMemberGroup(creation.Type)
            Assert.Equal(0, memberGroup.Count)

            Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
            Assert.Null(typeInfo.Type)
            Assert.Null(typeInfo.ConvertedType)
            Dim conv = model.GetConversion(creation.Type)
            Assert.True(conv.IsIdentity)

            memberGroup = model.GetMemberGroup(creation)
            Dim sortedMethodGroup = memberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal(1, sortedMethodGroup.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())

            symbolInfo = model.GetSymbolInfo(creation)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub X..ctor(x As System.Int32)", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)


            typeInfo = model.GetTypeInfo(creation)
            Assert.Equal("X", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString())
            conv = model.GetConversion(creation)
            Assert.Equal(ConversionKind.WideningReference, conv.Kind)
        End Sub

        <Fact()>
        Public Sub NewOfUnconstrainedTypeParameter()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C(Of T)
    Sub F()
        Dim a As Object = New T()'BIND:"T"
    End Sub
End Class




    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("T", semanticSummary.CandidateSymbols(0).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NewOfUnconstrainedTypeParameter2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class C(Of T)
    Sub F()
        Dim a As Object = New T()'BIND:"New T()"
    End Sub
End Class




    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("T", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.TypeParameter, semanticSummary.Type.TypeKind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(543534, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543534")>
        <Fact()>
        Public Sub InterfaceCreationExpression()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I
    Property X() As Integer
End Interface

Class Program
    Private Shared Sub Main()
        Dim x = New I()'BIND:"I"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotCreatable, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("I", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub InterfaceCreationExpression2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I
    Property X() As Integer
End Interface

Class Program
    Private Shared Sub Main()
        Dim x = New I()'BIND:"New I()"
    End Sub
End Class
    ]]></file>
</compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC30375: 'New' cannot be used on an interface.
        Dim x = New I()'BIND:"New I()"
                ~~~~~~~
                                               </expected>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("I", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticSummary.Type.TypeKind)
            Assert.Equal("I", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(543515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543515")>
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub AliasAttributeName()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
imports A = A1

Class A1
    Inherits System.Attribute
End Class

<A> 'BIND:"A"
Class C
End Class
    ]]></file>
</compilation>)


            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.Equal("A1", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("A1", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("Sub A1..ctor()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub A1..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.False(semanticInfo.ConstantValue.HasValue)
            Dim aliasInfo = GetAliasInfoForTest(compilation, "a.vb")
            Assert.NotNull(aliasInfo)
            Assert.Equal("A=A1", aliasInfo.ToTestDisplayString())
            Assert.Equal(SymbolKind.[Alias], aliasInfo.Kind)
        End Sub


        <WorkItem(543515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543515")>
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub AliasAttributeName_02_AttributeSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
imports GooAttribute = System.ObsoleteAttribute

<Goo> 'BIND:"Goo"
Class C
End Class
    ]]></file>
</compilation>)


            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(3, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedMethodGroup(2).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Dim aliasInfo = GetAliasInfoForTest(compilation, "a.vb")
            Assert.NotNull(aliasInfo)
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString())
            Assert.Equal(SymbolKind.[Alias], aliasInfo.Kind)
        End Sub


        <WorkItem(543515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543515")>
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub AliasAttributeName_02_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
imports GooAttribute = System.ObsoleteAttribute

<Goo> 'BIND:"Goo"
Class C
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(3, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedMethodGroup(2).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Dim aliasInfo = GetAliasInfoForTest(compilation, "a.vb")
            Assert.NotNull(aliasInfo)
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString())
            Assert.Equal(SymbolKind.[Alias], aliasInfo.Kind)
        End Sub


        <WorkItem(543515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543515")>
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub AliasAttributeName_03_AttributeSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
            <compilation>
                <file name="a.vb"><![CDATA[
imports GooAttribute = System.ObsoleteAttribute

<GooAttribute> 'BIND:"GooAttribute"
Class C
End Class
    ]]></file>
            </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(3, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedMethodGroup(2).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Dim aliasInfo = GetAliasInfoForTest(compilation, "a.vb")
            Assert.NotNull(aliasInfo)
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString())
            Assert.Equal(SymbolKind.[Alias], aliasInfo.Kind)
        End Sub

        <WorkItem(543515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543515")>
        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub AliasAttributeName_03_IdentifierNameSyntax()
            Dim compilation = CreateCompilationWithMscorlib40(
          <compilation>
              <file name="a.vb"><![CDATA[
imports GooAttribute = System.ObsoleteAttribute

<GooAttribute> 'BIND:"GooAttribute"
Class C
End Class
    ]]></file>
          </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("System.ObsoleteAttribute", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(3, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", sortedMethodGroup(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", sortedMethodGroup(2).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Dim aliasInfo = GetAliasInfoForTest(compilation, "a.vb")
            Assert.NotNull(aliasInfo)
            Assert.Equal("GooAttribute=System.ObsoleteAttribute", aliasInfo.ToTestDisplayString())
            Assert.Equal(SymbolKind.[Alias], aliasInfo.Kind)
        End Sub


        <WorkItem(543515, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543515")>
        <Fact()>
        Public Sub AliasQualifiedAttributeName_01()

            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
namespace N
    <global.AttributeClass.SomeClass()> 'BIND:"AttributeClass"
    class C 
    end class

    class AttributeClass 
        inherits System.Attribute 
    end class
end namespace

class AttributeClass 
    inherits System.Attribute

    class SomeClass
    end class

end class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.Equal("AttributeClass", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("AttributeClass", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("AttributeClass", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Assert.[False](SyntaxFacts.IsAttributeName((DirectCast(semanticInfo.Symbol, SourceNamedTypeSymbol)).SyntaxReferences.First().GetSyntax()), "IsAttributeName can be true only for alias name being qualified")
        End Sub

        <Fact()>
        Public Sub AliasQualifiedAttributeName_02()

            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
namespace N
    <global.AttributeClass.SomeClass()> 'BIND:"global.AttributeClass"
    class C 
    end class

    class AttributeClass 
        inherits System.Attribute 
    end class
end namespace

class AttributeClass 
    inherits System.Attribute

    class SomeClass
    end class

end class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")
            Assert.Equal("AttributeClass", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("AttributeClass", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("AttributeClass", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Assert.[False](SyntaxFacts.IsAttributeName((DirectCast(semanticInfo.Symbol, SourceNamedTypeSymbol)).SyntaxReferences.First().GetSyntax()), "IsAttributeName can be true only for alias name being qualified")
        End Sub


        <Fact()>
        Public Sub AliasQualifiedAttributeName_03()

            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
namespace N
    <global.AttributeClass.SomeClass()> 'BIND:"SomeClass"
    class C 
    end class

    class AttributeClass 
        inherits System.Attribute 
    end class
end namespace

class AttributeClass 
    inherits System.Attribute

    class SomeClass
    end class

end class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.Equal("AttributeClass.SomeClass", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("AttributeClass.SomeClass", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AttributeClass.SomeClass..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AttributeClass.SomeClass..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AliasQualifiedAttributeName_04()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
namespace N
    <global.AttributeClass.SomeClass()> 'BIND:"global.AttributeClass.SomeClass"
    class C 
    end class

    class AttributeClass 
        inherits System.Attribute 
    end class
end namespace

class AttributeClass 
    inherits System.Attribute

    class SomeClass
    end class

end class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of QualifiedNameSyntax)(compilation, "a.vb")
            Assert.Equal("AttributeClass.SomeClass", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("AttributeClass.SomeClass", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AttributeClass.SomeClass..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub AttributeClass.SomeClass..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub AliasAttributeName_NonAttributeAlias()

            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
imports GooAttribute = C

<GooAttribute> 'BIND:"GooAttribute"
Class C
end class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.Equal("C", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("C", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub C..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub C..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Dim aliasInfo = GetAliasInfoForTest(compilation, "a.vb")
            Assert.Null(aliasInfo)
        End Sub

        <Fact()>
        Public Sub AliasAttributeName_NonAttributeAlias_GenericType()

            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
imports GooAttribute = Gen(of Integer)

<GooAttribute> 'BIND:"GooAttribute"
Class Gen(of T)
end class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Assert.Equal("Gen(Of System.Int32)", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.Type.TypeKind)
            Assert.Equal("Gen(Of System.Int32)", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.[Class], semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.NotAnAttributeType, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Gen(Of System.Int32)..ctor()", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sortedCandidates(0).Kind)
            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Gen(Of System.Int32)..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.[False](semanticInfo.ConstantValue.HasValue)
            Dim aliasInfo = GetAliasInfoForTest(compilation, "a.vb")
            Assert.Null(aliasInfo)
        End Sub

        <Fact(), WorkItem(545085, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545085")>
        Public Sub ColorColorBug13346()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Compilation
    Public Shared Function M(a As Integer) As Boolean
        Return False
    End Function
End Class

Friend Class Program2
    Public ReadOnly Property Compilation As Compilation
        Get
            Return Nothing
        End Get
    End Property
    Public Sub Main()
        Dim x = Compilation.M(a:=123)'BIND:"Compilation"
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("Compilation", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("Compilation", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Compilation", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(529702, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529702")>
        <Fact()>
        Public Sub ColorColorBug14084()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Shared Sub Goo()
    End Sub
End Class

Class A(Of T As C)
    Class B
        Dim t As T 
        Sub Goo()
            T.Goo()'BIND:"T"
        End Sub
    End Class
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("T", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.TypeParameter, semanticSummary.Type.TypeKind)
            Assert.Equal("T", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.TypeParameter, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("A(Of T).B.t As T", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(546097, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546097")>
        <Fact()>
        Public Sub LambdaParametersAsOptional()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module Test
    Sub Main()
        Dim s1 = Function(Optional x = 3) x > 5 'BIND:"3"
    End Sub
End Module
    ]]></file>
</compilation>)
            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")
            Assert.Null(semanticSummary.Symbol)

            compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Private Function F1() As Object
        Return Nothing
    End Function
    Private F2 = Function(Optional o = F1()) Nothing 'BIND:"F1"
End Module
    ]]></file>
</compilation>)
            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of VisualBasicSyntaxNode)(compilation, "a.vb")
            CheckSymbol(semanticSummary.Symbol, "Function M.F1() As Object")
        End Sub

        <Fact()>
        Public Sub OptionalParameterOutsideType()
            ' Method.
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Sub M(Optional o As Object = 3) 'BIND:"3"
End Sub
    ]]></file>
</compilation>)
            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")
            Assert.Null(semanticSummary.Symbol)

            ' Method with type arguments.
            compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Sub M(Of T)(Optional o As Object = 3) 'BIND:"3"
End Sub
    ]]></file>
</compilation>)
            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")
            Assert.Null(semanticSummary.Symbol)

            ' Property.
            compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
ReadOnly Property P(Optional o As Object = 3) As Object 'BIND:"3"
    Get
        Return Nothing
    End Get
End Property
    ]]></file>
</compilation>)
            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")
            Assert.Null(semanticSummary.Symbol)

            ' Event.
            compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Event E(Optional o As Object = 3) 'BIND:"3"
    ]]></file>
</compilation>)
            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")
            Assert.Null(semanticSummary.Symbol)

        End Sub

        <Fact>
        Public Sub ConstantUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Const F As Object = Nothing
    Function M() As Object
        Return Me.F
    End Function
End Class
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            Dim info = semanticModel.GetSemanticInfoSummary(expr)
            CheckSymbol(info.Type, "A")
        End Sub

        <Fact>
        Public Sub CallUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Shared Function F() As Object
        Return Nothing
    End Function
    Function M() As Object
        Return Me.F()
    End Function
End Class
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            Dim info = semanticModel.GetSemanticInfoSummary(expr)
            CheckSymbol(info.Type, "A")
        End Sub

        <Fact>
        Public Sub AddressOfUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Shared Sub M()
    End Sub
    Function F() As System.Action
        Return AddressOf (Me).M
    End Function
End Class
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            Dim info = semanticModel.GetSemanticInfoSummary(expr)
            CheckSymbol(info.Type, "A")
        End Sub

        <Fact>
        Public Sub TypeExpressionUnevaluatedReceiver()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Class B
        Friend Const F As Object = Nothing
    End Class
    Function M() As Object
        Return (Me.B).F
    End Function
End Class
    ]]></file>
</compilation>, references:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")
            Dim info = semanticModel.GetSemanticInfoSummary(expr)
            CheckSymbol(info.Type, "A")
        End Sub

        ''' <summary>
        ''' SymbolInfo and TypeInfo should implement IEquatable&lt;T&gt;.
        ''' </summary>
        <WorkItem(792647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792647")>
        <Fact>
        Public Sub ImplementsIEquatable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Function F()
        Return Me
    End Function
End Class
    ]]></file>
</compilation>)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim expr = FindNodeOfTypeFromText(Of ExpressionSyntax)(tree, "Me")

            Dim symbolInfo1 = model.GetSymbolInfo(expr)
            Dim symbolInfo2 = model.GetSymbolInfo(expr)
            Dim symbolComparer = DirectCast(symbolInfo1, IEquatable(Of SymbolInfo))
            Assert.True(symbolComparer.Equals(symbolInfo2))

            Dim typeInfo1 = model.GetTypeInfo(expr)
            Dim typeInfo2 = model.GetTypeInfo(expr)
            Dim typeComparer = DirectCast(typeInfo1, IEquatable(Of TypeInfo))
            Assert.True(typeComparer.Equals(typeInfo2))
        End Sub

        <Fact, WorkItem(2805, "https://github.com/dotnet/roslyn/issues/2805")>
        Public Sub AliasWithAnError()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports ShortName = LongNamespace
Namespace NS
    Class Test
        Public Function Method1() As Object
            Return (New ShortName.Class1()).Prop
        End Function
    End Class
End Namespace
    ]]></file>
</compilation>, options:=TestOptions.DebugDll)

            compilation.AssertTheseDiagnostics(<expected>
BC40056: Namespace or type specified in the Imports 'LongNamespace' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.
Imports ShortName = LongNamespace
                    ~~~~~~~~~~~~~
BC30002: Type 'ShortName.Class1' is not defined.
            Return (New ShortName.Class1()).Prop
                        ~~~~~~~~~~~~~~~~
                                               </expected>)

            Dim tree = compilation.SyntaxTrees.Single()

            Dim node = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(id) id.Identifier.ValueText = "ShortName").Single()

            Assert.Equal("ShortName.Class1", node.Parent.ToString())

            Dim model = compilation.GetSemanticModel(tree)

            Dim [alias] = model.GetAliasInfo(node)
            Assert.Equal("ShortName=LongNamespace", [alias].ToTestDisplayString())
            Assert.Equal(SymbolKind.ErrorType, [alias].Target.Kind)
            Assert.Equal("LongNamespace", [alias].Target.ToTestDisplayString())

            Dim symbolInfo = model.GetSymbolInfo(node)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
        End Sub

    End Class
End Namespace
