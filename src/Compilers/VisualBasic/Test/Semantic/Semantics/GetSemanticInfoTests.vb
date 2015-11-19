' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class SemanticModelTests
        <Fact>
        Public Sub BindingMaxIntPlusOneHexLiteralConst()
            Dim source =
<compilation name="Bug4400">
    <file name="a.vb">
Option Strict On

Module Module1
    Sub Main()
        Test(2147483647)
        Test(-2147483647)
        Test(-2147483648)
        Test(-2147483649)
        Test(&amp;H7FFFFFFF)
        Test(&amp;H80000000)
        Test(&amp;HFFFFFFFF)
        Test(&amp;H100000000)
    End Sub

    Sub Test(x As Integer)
        System.Console.WriteLine("Integer")
    End Sub

    Sub Test(x As Long)
        System.Console.WriteLine("Long")
    End Sub

End Module
    </file>
</compilation>

            CompileAndVerify(source, <![CDATA[
Integer
Integer
Long
Long
Integer
Integer
Integer
Long
]]>)

        End Sub

        <Fact>
        Public Sub BindingInEnum3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindingEnumMembers">
        <file name="a.vb">
Enum filePermissions
    create = 1
    read = create       
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


            Dim tree = compilation.SyntaxTrees(0)
            Dim node = FindNodeFromText(tree, "read = create")

            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Dim a = DirectCast(symbol, SourceEnumConstantSymbol)
            Assert.Equal("read", a.Name)

        End Sub

        <Fact>
        Public Sub ForEachControlVariableExpression()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each C1.field(0) In C1.field'BIND:"C1.field(0)"
        Next C1.field(0)
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

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

        <Fact>
        Public Sub ForEachNextVariableExpression()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each C1.field(0) In C1.field
        Next C1.field(0)'BIND:"C1.field(0)"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

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

        <Fact>
        Public Sub ForEachCollectionExpression()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42
        field2 = field

        For Each x as Integer In C1.field'BIND:"C1.field"
        Next x
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Collections.IEnumerable", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("C1.field As System.Int32()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ForEachInvalidControlVariable()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer
    Public Shared Property prop As Integer

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each C1.prop In C1.field 'BIND:"C1.prop"
        Next C1.prop
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Null(semanticInfo.Symbol)
            Assert.Equal(CandidateReason.NotAVariable, semanticInfo.CandidateReason)
            Assert.Equal(1, semanticInfo.CandidateSymbols.Length)
            Dim sortedCandidates = semanticInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Property C1.prop As System.Int32", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ForEachNextVariableSameAsInvalidControlVariable()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer
    Public Shared Property prop As Integer

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each C1.prop In C1.field
        Next C1.prop'BIND:"C1.prop"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Property C1.prop As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ForEachNonCollectionExpression()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer
    Public Shared Property prop As Integer

    Public Shared Function Foo() As Integer
        Return 23
    End Function

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each x As Integer In C1.Foo()'BIND:"C1.Foo()"
        Next
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function C1.Foo() As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub ForEachErrorCollectionExpression()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer
    Public Shared Property prop As Integer

    Public Shared Function Foo() As Integer
        Return 23
    End Function

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each x As Integer In C1.NotDefined()'BIND:"C1.NotDefined()"
        Next
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

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

        <Fact>
        Public Sub ForEachErrorNextVariable()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer
    Public Shared Property prop As Integer

    Public Shared Function Foo() As Integer
        Return 23
    End Function

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each x As Integer In C1.field
        Next C1.NotDefined()'BIND:"C1.NotDefined()"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

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

        <Fact>
        Public Sub ForEachErrorControlVariable()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer
    Public Shared Property prop As Integer

    Public Shared Function Foo() As Integer
        Return 23
    End Function

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each C1.NotDefined() In C1.field'BIND:"C1.NotDefined()"
        Next
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

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

        <Fact>
        Public Sub ForEachValidNextVariableOfBrokenForEach()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

    End Sub

    Public Shared field() As Integer
    Public Shared Property prop As Integer

    Public Shared Function Foo() As Integer
        Return 23
    End Function

    Public Sub DoStuff(d As Object)
        field = New Integer(1) {}
        field(0) = 23
        field(1) = 42

        For Each
        Next field(0)'BIND:"field(0)"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

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

        <Fact>
        Public Sub ForAndForEachGetDeclaredSymbolWithDeclaredVariable()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()

        for each controlVar as integer in {1,2,3} 
        next controlVar

        for controlVar2 as integer = 0 to 10
        next controlVar2
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)

            ' for each loop
            Dim node = FindNodeFromText(tree, "controlVar")

            ' on modified identifier
            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Equal("controlVar", symbol.Name)

            node = FindNodeFromText(tree, "for each controlVar as integer in {1,2,3}")
            symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Null(symbol)

            ' For loop
            node = FindNodeFromText(tree, "controlVar2")

            ' on modified identifier
            symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Equal("controlVar2", symbol.Name)

            node = FindNodeFromText(tree, "for controlVar2 as integer = 0 to 10")
            symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Null(symbol)
        End Sub

        <Fact>
        Public Sub ForAndForEachGetDeclaredSymbolWithLocallyDeclaredVariable()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System

Class C1
    Public Shared Sub Main()
        for each foo1 in {1,2,3} 
        next 

        dim foo2 as integer
        for foo2 = 0 to 10
        next 
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)

            ' for each loop
            Dim node = FindNodeFromText(tree, "for each foo1 in {1,2,3}")
            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Null(symbol)

            ' For loop
            node = FindNodeFromText(tree, "for foo2 = 0 to 10")
            symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Null(symbol)
        End Sub

        <Fact>
        Public Sub ForAndForEachGetDeclaredSymbolWithImplicitlyDeclaredVariable()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Option Infer Off
Option Explicit Off

Imports System

Class C1
    Public Shared Sub Main()

        for each foo in {1,2,3} 
        next 

        for foo2 = 0 to 10
        next 
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)

            ' for each loop
            Dim node = FindNodeFromText(tree, "for each foo in {1,2,3}")
            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Null(symbol)

            ' For loop
            node = FindNodeFromText(tree, "for foo2 = 0 to 10")
            symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Null(symbol)
        End Sub

        <WorkItem(541850, "DevDiv")>
        <Fact>
        Public Sub Bug8757_AttributeWithParamArray()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

<A>'BIND:"A"
Class A
    Inherits Attribute

    Sub New(ParamArray x As Object())
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of AttributeSyntax)(compilation, "a.vb")

            Assert.Equal("A", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal("A", semanticSummary.ConvertedType.ToTestDisplayString())

            Assert.Equal("Sub A..ctor(ParamArray x As System.Object())", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub A..ctor(ParamArray x As System.Object())", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(8641, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub Bug8641_PropertyInAttribute()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

<A(TypeOf (Foo) Is Nullable)>'BIND:"Foo"
Module Program
    Property Foo as Object
        Get
            return nothing
        End Get
        Set (v as Object)
        End Set
    End Property

    Sub Main(args As String())

    End Sub
End Module

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Object", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Property Program.Foo As System.Object", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(542186, "DevDiv")>
        <Fact>
        Public Sub Bug9321_IndexerParameter()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C

    Default Property Item(ByVal x as Integer) as Integer
        Get
            Return x 'BIND:"x"
        End Get
        
        Set (ByVal x as Integer)
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 0)

            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim symbol = semanticInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Parameter, symbol.Kind)
            Assert.Equal("x", symbol.Name)
            Assert.Equal(SymbolKind.Method, symbol.ContainingSymbol.Kind)

            Dim lookupSymbols = model.LookupSymbols(expressionSyntax.SpanStart, name:="x")
            Assert.Equal(symbol, lookupSymbols.Single())
        End Sub

        <WorkItem(542186, "DevDiv")>
        <Fact>
        Public Sub Bug9321_IndexerValueParameterWithoutParameterDeclaration()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C

    Default Property Item(ByVal x as Integer) as Integer
        Get
            Return x 
        End Get
        
        Set
           dim x = Value 'BIND:"Value"
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 0)

            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim symbol = semanticInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Parameter, symbol.Kind)
            Assert.Equal("Value", symbol.Name)
            Assert.Equal(SymbolKind.Method, symbol.ContainingSymbol.Kind)

            Dim lookupSymbols = model.LookupSymbols(expressionSyntax.SpanStart, name:="Value")
            Assert.Equal(symbol, lookupSymbols.Single())
        End Sub

        <WorkItem(542186, "DevDiv")>
        <Fact>
        Public Sub Bug9321_IndexerValueParameterWithParameterDeclaration()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C

    Default Property Item(ByVal x as Integer) as Integer
        Get
            Return x 
        End Get
        
        Set (ByVal value2 as Integer) 'BIND:"ByVal value2 as Integer"
           x = Value2 
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of ParameterSyntax)(compilation, "a.vb", 0)

            Dim parameter = model.GetDeclaredSymbol(expressionSyntax)

            Assert.NotNull(parameter)
            Assert.Equal(SymbolKind.Parameter, parameter.Kind)
            Assert.Equal("value2", parameter.Name)
            Assert.Equal(SymbolKind.Method, parameter.ContainingSymbol.Kind)
        End Sub

        <WorkItem(542186, "DevDiv")>
        <Fact>
        Public Sub Bug9321_IndexerValueParameterWithParameterDeclaration2()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C

    Default Property Item(ByVal x as Integer) as Integer
        Get
            Return x 
        End Get
        
        Set (ByVal value2 as Integer) 
           x = Value2 'BIND:"x"
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 0)

            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
            Dim symbol = semanticInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Parameter, symbol.Kind)
            Assert.Equal("x", symbol.Name)
            Assert.Equal(SymbolKind.Method, symbol.ContainingSymbol.Kind)

            Dim lookupSymbols = model.LookupSymbols(expressionSyntax.SpanStart, name:="x")
            Assert.Equal(symbol, lookupSymbols.Single())
        End Sub

        <WorkItem(542777, "DevDiv")>
        <Fact>
        Public Sub Bug10154_IndexerThisParameter()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C

    Default Property Item(ByVal x as Integer) as Integer
        Get
            Return x 
        End Get
        
        Set (ByVal value2 as Integer) 
           Console.Write(Me) 'BIND:"Me"
        End Set
    End Property
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of MeExpressionSyntax)(compilation, "a.vb", 0)

            Assert.Equal(SyntaxKind.MeExpression, expressionSyntax.Kind)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MeExpressionSyntax)(compilation, "a.vb")
            Dim symbol = semanticInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Parameter, symbol.Kind)
            Assert.True(DirectCast(symbol, ParameterSymbol).IsMe)
            ' TODO: Assert.Equal(SymbolKind.Method, symbol.ContainingSymbol.Kind)
        End Sub

        <WorkItem(542335, "DevDiv")>
        <Fact>
        Public Sub Bug9530_LabelsSymbolInfo()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Shared Sub Main()
        Goto mylabel 'BIND:"mylabel"
mylabel:
        Console.WriteLine("Hello Goto")
        
        Goto &HA 'BIND1:"&HA"
10:     'BIND2:"10:"
        Console.WriteLine("Goodbye")
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of LabelSyntax)(compilation, "a.vb", 0)
            Assert.Equal(SyntaxKind.IdentifierLabel, expressionSyntax.Kind)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LabelSyntax)(compilation, "a.vb")
            Dim symbol = semanticInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Label, symbol.Kind)
            Assert.Equal("mylabel", symbol.Name)

            expressionSyntax = CompilationUtils.FindBindingText(Of LabelSyntax)(compilation, "a.vb", 1)
            Assert.Equal(SyntaxKind.NumericLabel, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of LabelSyntax)(compilation, "a.vb", 1)
            symbol = semanticInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.Label, symbol.Kind)
            Assert.Equal("10", symbol.Name)

            Dim expressionSyntax2 = CompilationUtils.FindBindingText(Of LabelStatementSyntax)(compilation, "a.vb", 2)
            Assert.Equal(SyntaxKind.LabelStatement, expressionSyntax2.Kind)
            Dim labelSymbol = model.GetDeclaredSymbol(expressionSyntax2)
            Assert.NotNull(labelSymbol)
            Assert.Equal(SymbolKind.Label, labelSymbol.Kind)
            Assert.Equal("10", labelSymbol.Name)
        End Sub

        <Fact()>
        Public Sub LabelsSymbolInfoInErrorCase()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Shared Sub Main()
        Goto mylabel 

mylabel:    'BIND:"mylabel:"
        Console.WriteLine("Hello Goto")

MYLABEL:        
        Console.WriteLine("Goodbye")
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of LabelStatementSyntax)(compilation, "a.vb", 0)

            Assert.Equal(SyntaxKind.LabelStatement, expressionSyntax.Kind)

            Dim declaredSymbol = model.GetDeclaredSymbol(expressionSyntax)
            Assert.NotNull(declaredSymbol)
            Assert.Equal(SymbolKind.Label, declaredSymbol.Kind)
            Assert.Equal("mylabel", declaredSymbol.Name)
        End Sub

        <WorkItem(542335, "DevDiv")>
        <Fact>
        Public Sub Bug9530_LabelsDeclaredSymbol()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Public Shared Sub Main()
        Goto mylabel 
mylabel: 'BIND:"mylabel:"
        Console.WriteLine("Hello Goto")
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of LabelStatementSyntax)(compilation, "a.vb", 0)

            Assert.Equal(SyntaxKind.LabelStatement, expressionSyntax.Kind)

            Dim declaredSymbol = model.GetDeclaredSymbol(expressionSyntax)
            Assert.NotNull(declaredSymbol)
            Assert.Equal(SymbolKind.Label, declaredSymbol.Kind)
            Assert.Equal("mylabel", declaredSymbol.Name)
        End Sub

        <WorkItem(545562, "DevDiv")>
        <Fact()>
        Public Sub SymbolInfo_HexadecimalLabel()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
&HFFFFFFFF:
        GoTo 4294967295'BIND:"4294967295"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LabelSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("4294967295", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Label, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub SymbolInfo_TypeInfo_GetType()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports MyAlias1=System.Int32
Imports MyAlias2=Generic(Of AClass)

Class Generic(Of T)
End Class

Class AClass
End Class

Structure AStructure
End Structure

Class C
    Public Shared Sub Main()
        Dim a = GetType(Integer)                     'BIND:"Integer"
        Dim b = GetType(Integer())                   'BIND1:"Integer()"
        Dim c = GetType(AClass)                      'BIND2:"AClass"
        Dim d = GetType(AClass())                    'BIND3:"AClass()"
        Dim e = GetType(AStructure(,))               'BIND4:"AStructure(,)"

        Dim f = GetType(System.RuntimeTypeHandle)    'BIND5:"System.RuntimeTypeHandle"        
        Dim g = GetType(System.RuntimeTypeHandle())  'BIND6:"System.RuntimeTypeHandle()"

        Dim h = GetType(Generic(Of Integer))         'BIND7:"Generic(Of Integer)"
        Dim i = GetType(Generic(Of ))                'BIND8:"Generic(Of )"

        Dim j = GetType(System.Void)                 'BIND9:"System.Void"
        Dim k = GetType(System.Void())               'BIND10:"System.Void()"  ' is not legal to use in VB, but C# Semantic Model returns the same result here.

        Dim AClass as Integer = 42
        Dim l = GetType(AClass)                      'BIND11:"AClass"

        Dim m = GetType(MyAlias1)                    'BIND12:"MyAlias1"
        Dim n = GetType(MyAlias2)                    'BIND13:"MyAlias2"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 0)
            Assert.Equal(SyntaxKind.PredefinedType, expressionSyntax.Kind)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 0)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            Dim symbol = semanticInfo.Symbol
            Assert.Equal("System.Int32", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 1)
            Assert.Equal(SyntaxKind.ArrayType, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 1)
            Assert.Equal("System.Int32()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("System.Int32()", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 2)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 2)
            Assert.Equal("AClass", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("AClass", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 3)
            Assert.Equal(SyntaxKind.ArrayType, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 3)
            Assert.Equal("AClass()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("AClass()", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 4)
            Assert.Equal(SyntaxKind.ArrayType, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 4)
            Assert.Equal("AStructure(,)", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("AStructure(,)", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 5)
            Assert.Equal(SyntaxKind.QualifiedName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 5)
            Assert.Equal("System.RuntimeTypeHandle", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("System.RuntimeTypeHandle", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 6)
            Assert.Equal(SyntaxKind.ArrayType, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 6)
            Assert.Equal("System.RuntimeTypeHandle()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("System.RuntimeTypeHandle()", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 7)
            Assert.Equal(SyntaxKind.GenericName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 7)
            Assert.Equal("Generic(Of System.Int32)", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("Generic(Of System.Int32)", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 8)
            Assert.Equal(SyntaxKind.GenericName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 8)
            Assert.Equal("Generic(Of )", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("Generic(Of )", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 9)
            Assert.Equal(SyntaxKind.QualifiedName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 9)
            Assert.Equal("System.Void", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("System.Void", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 10)
            Assert.Equal(SyntaxKind.ArrayType, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 10)
            Assert.Equal("System.Void()", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal("System.Void()", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 11)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 11)
            Assert.Equal("AClass", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)
            Assert.Equal("AClass", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 12)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 12)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)
            Assert.Equal("System.Int32", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.NotNull(semanticInfo.Alias)
            Assert.Equal("MyAlias1=System.Int32", semanticInfo.Alias.ToDisplayString(SymbolDisplayFormat.TestFormat))

            expressionSyntax = CompilationUtils.FindBindingText(Of TypeSyntax)(compilation, "a.vb", 13)
            Assert.Equal(SyntaxKind.IdentifierName, expressionSyntax.Kind)
            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of TypeSyntax)(compilation, "a.vb", 13)
            Assert.Equal("Generic(Of AClass)", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(1, semanticInfo.AllSymbols.Length)
            symbol = semanticInfo.Symbol
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)
            Assert.Equal("Generic(Of AClass)", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))
            Assert.NotNull(semanticInfo.Alias)
            Assert.Equal("MyAlias2=Generic(Of AClass)", semanticInfo.Alias.ToDisplayString(SymbolDisplayFormat.TestFormat))
        End Sub

        <Fact()>
        Public Sub BindingModule()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindingEnumMembers">
        <file name="a.vb">
Module M1
    Public Sub Main(args As String())
    End Sub
End Module

    </file>
    </compilation>)


            Dim tree = compilation.SyntaxTrees(0)
            Dim node = FindNodeFromText(tree, "Module M1")

            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Dim a = DirectCast(symbol, SourceNamedTypeSymbol)
            Assert.Equal("M1", a.Name)
            Assert.True(a.IsModuleType)
        End Sub

        <Fact()>
        Public Sub BindingModuleMemberInQualifiedExpressionWithGlobal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindingEnumMembers">
        <file name="a.vb">
Module M1
    Public Sub Main(args As String())
        Console.WriteLine(Global.Foo)     'BIND:"Global.Foo"
        Console.WriteLine(Global.M2.Foo)  'BIND1:"Global.M2.Foo"
    End Sub
End Module

Module M2
    Public Foo as Integer = 23            

    Public Sub DoStuff()
        Console.WriteLine(Foo)            'BIND2:"Foo"
    End Sub
End Module
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 0)
            Dim sym = DirectCast(semanticInfo.Symbol, SourceFieldSymbol)
            Assert.Equal("M2.Foo As System.Int32", sym.ToTestDisplayString())
            Assert.Equal("M2", sym.ContainingType.ToTestDisplayString())
            Assert.Equal("M2", sym.ContainingSymbol.ToTestDisplayString())

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 1)
            Dim sym2 = DirectCast(semanticInfo.Symbol, SourceFieldSymbol)
            Assert.Equal("M2.Foo As System.Int32", sym.ToTestDisplayString())
            Assert.Equal("M2", sym.ContainingType.ToTestDisplayString())
            Assert.Equal("M2", sym.ContainingSymbol.ToTestDisplayString())

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Dim sym3 = DirectCast(semanticInfo.Symbol, SourceFieldSymbol)
            Assert.Equal("M2.Foo As System.Int32", sym.ToTestDisplayString())
            Assert.Equal("M2", sym.ContainingType.ToTestDisplayString())
            Assert.Equal("M2", sym.ContainingSymbol.ToTestDisplayString())

            Assert.Same(sym, sym2)
            Assert.Same(sym2, sym3)
        End Sub

        <WorkItem(543192, "DevDiv")>
        <Fact()>
        Public Sub BindingParameterDefaultValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindingParameterDefaultValue">
        <file name="a.vb">
Imports System            
Module M1
    Sub s1(optional i as integer = 100) 'BIND:"100"
    End Sub

    Sub s2(optional s as string = "Hi There") 'BIND1:""Hi There""
    End Sub

    Sub S3(optional e as AttributeTargets = AttributeTargets.Class) 'BIND2:"AttributeTargets.Class"
    End Sub

    Property p1(Optional i As Integer = 100) As Integer 'BIND3:"100"
        Get
            Return Nothing
        End Get
        Set(value As Integer)
        End Set
    End Property

    Property p2(Optional s As String = "s") As String 'BIND4:""s""
        Get
            Return Nothing
        End Get
        Set(value As String)
        End Set
    End Property

    Property p3(Optional e As AttributeTargets = AttributeTargets.Property) As AttributeTargets 'BIND5:"AttributeTargets.Property"
        Get
            Return Nothing
        End Get
        Set(value As AttributeTargets)
        End Set
    End Property

    Property p4(Optional d As DateTime = #2/24/2012#) As DateTime 'BIND6:"#2/24/2012#"
        Get
            Return Nothing
        End Get
        Set(value As DateTime)
        End Set
    End Property

    Property p5(Optional d As Decimal = 99.99D) As Decimal 'BIND7:"99.99D"
        Get                   'BIND8:"Get"  
            Return Nothing
        End Get
        Set(value As Decimal) 'BIND9:"Set(value As Decimal)"
        End Set
    End Property

    Event E(optional i as integer = 100) 'BIND10:"100" Optional is not allowed but semantic model should be able to bind

    Delegate Sub D(optional s as string = "Roslyn") 'BIND11:""Roslyn"" Optional is not allowed but semantic model should be able to bind

End Module
    </file>
    </compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 0)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(100, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 1)
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal("Hi There", semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 2)
            Assert.Equal("System.AttributeTargets", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(AttributeTargets.Class, CType(semanticInfo.ConstantValue.Value, AttributeTargets))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 3)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(100, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 4)
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal("s", semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 5)
            Assert.Equal("System.AttributeTargets", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(AttributeTargets.Property, CType(semanticInfo.ConstantValue.Value, AttributeTargets))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 6)
            Assert.Equal("System.DateTime", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(#2/24/2012#, CType(semanticInfo.ConstantValue.Value, DateTime))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 7)
            Assert.Equal("System.Decimal", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(99.99D, CType(semanticInfo.ConstantValue.Value, Decimal))

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim accessorSyntax = CompilationUtils.FindBindingText(Of AccessorStatementSyntax)(compilation, "a.vb", 8)
            Dim accessor = model.GetDeclaredSymbol(accessorSyntax)
            Dim parameter = DirectCast(accessor.Parameters.Where(Function(p) p.Name = "d").FirstOrDefault, ParameterSymbol)
            Assert.Equal("System.Decimal", parameter.Type.ToTestDisplayString())
            Assert.Equal(True, parameter.HasExplicitDefaultValue)
            Assert.Equal(99.99D, CType(parameter.ExplicitDefaultConstantValue.Value, Decimal))

            accessorSyntax = CompilationUtils.FindBindingText(Of AccessorStatementSyntax)(compilation, "a.vb", 9)
            accessor = model.GetDeclaredSymbol(accessorSyntax)
            parameter = DirectCast(accessor.Parameters.Where(Function(p) p.Name = "d").FirstOrDefault, ParameterSymbol)
            Assert.Equal("System.Decimal", parameter.Type.ToTestDisplayString())
            Assert.Equal(True, parameter.HasExplicitDefaultValue)
            Assert.Equal(99.99D, CType(parameter.ExplicitDefaultConstantValue.Value, Decimal))

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 10)
            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal(100, semanticInfo.ConstantValue.Value)

            semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of ExpressionSyntax)(compilation, "a.vb", 11)
            Assert.Equal("System.String", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(True, semanticInfo.ConstantValue.HasValue)
            Assert.Equal("Roslyn", semanticInfo.ConstantValue.Value)
        End Sub

        <WorkItem(545207, "DevDiv")>
        <Fact()>
        Public Sub BindingAttributeWithNamedArgument1()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Security.Permissions

Module Program
    <PermissionSet(SecurityAction.LinkDemand, FileAttr:="Foo")>'BIND:"PermissionSet"
    Sub Main(args As String())
        
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Security.Permissions.PermissionSetAttribute", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Security.Permissions.PermissionSetAttribute", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub System.Security.Permissions.PermissionSetAttribute..ctor(action As System.Security.Permissions.SecurityAction)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.Security.Permissions.PermissionSetAttribute..ctor(action As System.Security.Permissions.SecurityAction)", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(545558, "DevDiv")>
        <Fact()>
        Public Sub BindingAttributeWithUndefinedEnumArgument()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.ComponentModel

Module Program
    <EditorBrowsable(EditorBrowsableState.n)>'BIND:"EditorBrowsableState.n"
    Sub Main(args As String())
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("?", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticSummary.Type.TypeKind)
            Assert.Equal("?", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(529096, "DevDiv")>
        <Fact()>
        Public Sub MemberAccessExpressionResults()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindingEnumMembers">
        <file name="a.vb">
Imports system
Imports system.collections.generic

Public Class C1
    Public Shared A as Integer

    Public Shared Function B as Byte
        return 23
    End Function

    Public Shared Property C as Byte
End Class

Module Program
    Public Sub Main(args As String())
        C1.A     'BIND:"C1.A"
        C1.B     'BIND1:"C1.B"
        C1.C     'BIND2:"C1.C"
        Dim x as Integer = C1.C     'BIND3:"C1.C"
        Dim y as integer = C1.B()   'BIND4:"C1.B"       
        Dim z as integer = C1.B()   'BIND5:"C1.B()"       
       
        dim d as Func(of byte) = addressof C1.B 'BIND6:"C1.B"

        Dim o As Object
        Dim c = o.ToString(0) 'BIND7:"o.ToString"

        Dim i = F(1) 'BIND8:"F"

        Dim v = P1(0)  'BIND9:"P1"
        Dim v2 = P2(0) 'BIND10:"P2"
        Dim v3 = P3(0) 'BIND11:"P3"
 
        Dim v4 = F1(0) 'BIND12:"F1"
        Dim v5 = F2(0) 'BIND13:"F2"

        x = C1.C()     'BIND14:"C1.C"

        d = CType(addressof C1.B, Func(of byte)) 'BIND15:"C1.B"
        d = DirectCast(addressof C1.B, Func(of byte)) 'BIND16:"C1.B"
        d = TryCast(addressof C1.B, Func(of byte)) 'BIND17:"C1.B"

    End Sub

    Function F() As Func(Of Integer, Integer)
        Return Nothing
    End Function

    Property P1 As String()
    Property P2 As List(Of String)
    Property P3 As Func(Of Integer, Integer)
 
    Function F1() As String()
        Return Nothing
    End Function
 
    Function F2() As List(Of String)
        Return Nothing
    End Function
End Module
    </file>
    </compilation>)

            compilation.AssertTheseDiagnostics(<expected>
BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute..ctor' is not defined.
Module Program
       ~~~~~~~
BC30454: Expression is not a method.
        C1.A     'BIND:"C1.A"
        ~~~~
BC30545: Property access must assign to the property or use its value.
        C1.C     'BIND2:"C1.C"
        ~~~~
BC42104: Variable 'o' is used before it has been assigned a value. A null reference exception could result at runtime.
        Dim c = o.ToString(0) 'BIND7:"o.ToString"
                ~
                                               </expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 0)
            Dim symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("A", symbolInfo.Symbol.Name)
            Dim typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 1)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("B", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 2)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("C", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 3)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("C", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString)
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 4)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("B", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)

            Dim expressionSyntax2 = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 5)
            symbolInfo = model.GetSymbolInfo(expressionSyntax2)
            Assert.Equal("B", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax2)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Byte", typeInfo.Type.ToTestDisplayString)
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 6)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("B", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 7)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("ToString", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString)

            Dim expressionSyntax3 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 8)
            symbolInfo = model.GetSymbolInfo(expressionSyntax3)
            Assert.Equal("F", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax3)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString)

            expressionSyntax3 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 9)
            symbolInfo = model.GetSymbolInfo(expressionSyntax3)
            Assert.Equal("P1", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax3)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.String()", typeInfo.Type.ToTestDisplayString)

            expressionSyntax3 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 10)
            symbolInfo = model.GetSymbolInfo(expressionSyntax3)
            Assert.Equal("P2", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax3)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Collections.Generic.List(Of System.String)", typeInfo.Type.ToTestDisplayString)

            expressionSyntax3 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 11)
            symbolInfo = model.GetSymbolInfo(expressionSyntax3)
            Assert.Equal("P3", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax3)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.Type.ToTestDisplayString)

            expressionSyntax3 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 12)
            symbolInfo = model.GetSymbolInfo(expressionSyntax3)
            Assert.Equal("F1", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax3)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.String()", typeInfo.Type.ToTestDisplayString)

            expressionSyntax3 = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 13)
            symbolInfo = model.GetSymbolInfo(expressionSyntax3)
            Assert.Equal("F2", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax3)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Collections.Generic.List(Of System.String)", typeInfo.Type.ToTestDisplayString)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 14)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("C", symbolInfo.Symbol.Name)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 15)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Function C1.B() As System.Byte", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 16)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Function C1.B() As System.Byte", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)

            expressionSyntax = CompilationUtils.FindBindingText(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 17)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Function C1.B() As System.Byte", symbolInfo.Symbol.ToTestDisplayString())
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
            typeInfo = model.GetTypeInfo(expressionSyntax)
            Assert.Null(typeInfo.Type)
        End Sub

        <WorkItem(543572, "DevDiv")>
        <Fact()>
        Public Sub DefaultValueWithConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindingEnumMembers">
        <file name="a.vb">
Module Module1
    Structure S : Public i As Integer : End Structure
    Class C : Public i As Integer : End Class
    Sub dump(ByVal x As Object)
        Console.WriteLine("{0}:{1}", If(x Is Nothing, "Nothing", x.ToString), If(x Is Nothing, "Nothing", x.GetType.ToString))
    End Sub
    Sub f0(Of T)(Optional ByVal x As T = Nothing)
        dump(x)
    End Sub
 
    Sub Func(i As String)
    End Sub

    Sub Func(Optional i As Integer = 0)
    End Sub
 
    Sub Main()
        Func() 'BIND1:"Func"

        f0(Of Object)() 'BIND2:"f0(Of Object)"
        f0(Of Integer)() 'BIND3:"f0(Of Integer)"
        f0(Of String)() 'BIND4:"f0(Of String)"
        f0(Of S)() 'BIND5:"f0(Of S)"
        f0(Of C)() 'BIND6:"f0(Of C)"
    End Sub
End Module
    </file>
    </compilation>)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim expressionSyntax As ExpressionSyntax
            Dim symbolInfo As SymbolInfo

            expressionSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Sub Module1.Func([i As System.Int32 = 0])", symbolInfo.Symbol.ToTestDisplayString())

            expressionSyntax = CompilationUtils.FindBindingText(Of GenericNameSyntax)(compilation, "a.vb", 2)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Sub Module1.f0(Of System.Object)([x As System.Object = Nothing])", symbolInfo.Symbol.ToTestDisplayString())

            expressionSyntax = CompilationUtils.FindBindingText(Of GenericNameSyntax)(compilation, "a.vb", 3)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Sub Module1.f0(Of System.Int32)([x As System.Int32 = Nothing])", symbolInfo.Symbol.ToTestDisplayString())

            expressionSyntax = CompilationUtils.FindBindingText(Of GenericNameSyntax)(compilation, "a.vb", 4)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Sub Module1.f0(Of System.String)([x As System.String = Nothing])", symbolInfo.Symbol.ToTestDisplayString())

            expressionSyntax = CompilationUtils.FindBindingText(Of GenericNameSyntax)(compilation, "a.vb", 5)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Sub Module1.f0(Of Module1.S)([x As Module1.S = Nothing])", symbolInfo.Symbol.ToTestDisplayString())

            expressionSyntax = CompilationUtils.FindBindingText(Of GenericNameSyntax)(compilation, "a.vb", 6)
            symbolInfo = model.GetSymbolInfo(expressionSyntax)
            Assert.Equal("Sub Module1.f0(Of Module1.C)([x As Module1.C = Nothing])", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub NamedArgsInRaiseEvent()
            Dim compilation = CreateCompilationWithMscorlib(
           <compilation>
               <file name="a.vb"><![CDATA[
Class derive
    Shared Event myevent(ByRef aaaa As Integer)
    Sub main()
        myeventEvent(aaaa:=123)
        myeventEvent.Invoke(aaaa:=123)
        RaiseEvent myevent(aaaa:=123)'BIND:"aaaa"
    End Sub
End Class

    ]]></file>
           </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("ByRef aaaa As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamedArgsInRaiseEvent1()
            Dim compilation = CreateCompilationWithMscorlib(
           <compilation>
               <file name="a.vb"><![CDATA[
Class derive
    Shared Event myevent(ByRef aaaa As Integer)
    Sub main()
        myeventEvent(aaaa:=123) 'BIND:"aaaa"
        myeventEvent.Invoke(aaaa:=123)
        RaiseEvent myevent(aaaa:=123)
    End Sub
End Class

    ]]></file>
           </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("ByRef aaaa As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamedArgsInRaiseEvent2()
            Dim compilation = CreateCompilationWithMscorlib(
           <compilation>
               <file name="a.vb"><![CDATA[
Class derive
    Shared Event myevent(ByRef aaaa As Integer)
    Sub main()
        myeventEvent(aaaa:=123)
        myeventEvent.Invoke(aaaa:=123) 'BIND:"aaaa"
        RaiseEvent myevent(aaaa:=123)
    End Sub
End Class

    ]]></file>
           </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("ByRef aaaa As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamedArgsInRaiseEventImplemented()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Interface I1
    Event E(qwer As Integer)  
End Interface

Class cls1 : Implements I1
    Event E3(bar As Integer) Implements I1.E   '  bar means nothing here, only type matters.

    Sub moo()
        ' binds to parameter on I1.EEventhandler.invoke(foo)
        RaiseEvent E3(qwer:=123)  'BIND:"qwer"
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("qwer As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub NamedArgsInRaiseEventCustom()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class cls1

    Custom Event E1 As Action(Of Integer)
        AddHandler(value As Action(Of Integer))

        End AddHandler

        RemoveHandler(value As Action(Of Integer))

        End RemoveHandler

        RaiseEvent(objArg As Integer)

        End RaiseEvent
    End Event

    Sub moo()
        RaiseEvent E1(objArg:=123)  ' foo binds to parameter on I1.EEventhandler.invoke(foo)'BIND:"objArg"
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("objArg As System.Int32", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

#Region "ObjectInitializer"

        <Fact()>
        Public Sub FieldNameOfNamedFieldInitializer()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class C1
    Sub New()
    End Sub

    Sub New(p as integer)
    End Sub

    Public FieldInt as Long
    Public FieldStr as String

    Public Property PropInt as Integer
End Class

Public Class C2
    Public Shared Sub Main
        Dim x as C1 = new C1() With {.FieldInt = 23%} 'BIND:"FieldInt"
        x = new C1() With {.FieldInt = 23} 'BIND1:"23"
        x = new C1() With {.FieldStr = .FieldInt.ToString()} 'BIND2:".FieldInt"
        x = new C1(23) With {.FieldInt = 23} 'BIND3:"new C1(23) With {.FieldInt = 23}"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 0)
            Assert.Equal("System.Int64", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int64", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("C1.FieldInt As System.Int64", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)

            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb", 1)
            Assert.Equal("System.Int32", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int64", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticSummary.ImplicitConversion.Kind)
            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.True(semanticSummary.ConstantValue.HasValue)
            Assert.Equal(23, semanticSummary.ConstantValue.Value)

            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb", 2)
            Assert.Equal("System.Int64", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int64", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("C1.FieldInt As System.Int64", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Field, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)

            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 3)
            Assert.Equal("C1", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("C1", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("Sub C1..ctor(p As System.Int32)", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub C1..ctor()", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub C1..ctor(p As System.Int32)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)

            Dim tree = compilation.SyntaxTrees(0)
            Dim node = FindNodeFromText(tree, "x")
            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbolFromSyntaxNode(node)
            Assert.Equal("x As C1", symbol.ToTestDisplayString)
        End Sub

        <Fact()>
        Public Sub ObjectInitializersCompleteObjectCreation()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Class C2
    Public Foo as Integer
    Public Bar as Integer
End Class

Public Class C3
    Inherits C2
End Class


Class C1
    Public Shared Sub Main()
        Dim foo As Byte = 23
        Dim abcdef As New C2() With {.Foo = foo, .Bar = 23} 'BIND:"New C2() With {.Foo = foo, .Bar = 23}"
        abcdef = New C3() With {.Foo = foo, .Bar = 23} 'BIND1:"New C3() With {.Foo = foo, .Bar = 23}"
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 0)
            Assert.Equal("C2", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("C2", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("Sub C2..ctor()", semanticSummary.Symbol.ToTestDisplayString)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Assert.Equal("Sub C2..ctor()", semanticSummary.MemberGroup(0).ToTestDisplayString)
            Assert.False(semanticSummary.ConstantValue.HasValue)
            Assert.Null(semanticSummary.ConstantValue.Value)

            Dim tree = compilation.SyntaxTrees(0)
            Dim node = FindNodeFromText(tree, "abcdef")
            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbolFromSyntaxNode(node)
            Assert.Equal("abcdef As C2", symbol.ToTestDisplayString)

            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 1)
            Assert.Equal("C3", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("C2", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningReference, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("Sub C3..ctor()", semanticSummary.Symbol.ToTestDisplayString)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Assert.Equal("Sub C3..ctor()", semanticSummary.MemberGroup(0).ToTestDisplayString)
            Assert.False(semanticSummary.ConstantValue.HasValue)
            Assert.Null(semanticSummary.ConstantValue.Value)
        End Sub

#End Region

#Region "CollectionInitializer"

        <Fact()>
        Public Sub CollectionInitializersAreExpressionSyntaxNodesButNoVBExpressions()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

Public Class C2
    Public Shared Sub Main
        Dim x as new List(Of Integer)() From {23%, 42} 'BIND:"{23%, 42}"
        Dim y as new Dictionary(Of Byte, Integer)() From {{1, 42}, {2, 23}} 'BIND1:"{1, 42}"
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb", 0)
            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)
            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)

            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb", 1)
            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)
            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <WorkItem(11989, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub CollectionInitializersAreExpressionSyntaxNodes()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic

Public Module Module1
    Public Sub Main()
        Dim y As New Dictionary(Of Byte, Integer())() From {{1, {23, 42}}, {2, {42, 23}}} 'BIND:"{23, 42}"'BIND:"{23, 42}"
    End Sub
End Module

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of CollectionInitializerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Equal("System.Int32()", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Array, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Widening, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub CollectionInitializersConvertedType()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Class C2
    Implements IEnumerable

    Dim mylist As List(Of String)

    Public Sub Add(p As Long)
    End Sub

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return mylist.GetEnumerator
    End Function
End Class

Class C1
    Public Shared Sub Main()
        Dim foo As Byte = 23
        Dim a As New C2() From {foo} 'BIND:"foo"    

        const bar As Byte = 23
        Dim a As New C2() From {bar} 'BIND1:"bar"    
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 0)
            Assert.Equal("System.Byte", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int64", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("foo As System.Byte", semanticSummary.Symbol.ToTestDisplayString)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
            Assert.Null(semanticSummary.ConstantValue.Value)

            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Assert.Equal("System.Byte", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int64", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("bar As System.Byte", semanticSummary.Symbol.ToTestDisplayString)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.True(semanticSummary.ConstantValue.HasValue)
            Assert.Equal(CByte(23), semanticSummary.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub CollectionInitializersConvertedTypeTypeParameters()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Interface IAdd(Of T)
    Sub Add(p As T)
End Interface

Class C1
    Public Shared Sub DoStuff(Of T As {IAdd(Of Long), ICollection, New})()
        Dim foo As Byte = 23
        Dim a As New T() From {foo} 'BIND:"foo"    

        const bar As Byte = 23
        Dim a As New T() From {bar} 'BIND1:"bar" 
    End Sub

    Public Shared Sub Main()
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 0)
            Assert.Equal("System.Byte", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int64", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("foo As System.Byte", semanticSummary.Symbol.ToTestDisplayString)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
            Assert.Null(semanticSummary.ConstantValue.Value)

            semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Assert.Equal("System.Byte", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int64", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.WideningNumeric, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("bar As System.Byte", semanticSummary.Symbol.ToTestDisplayString)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.True(semanticSummary.ConstantValue.HasValue)
            Assert.Equal(CByte(23), semanticSummary.ConstantValue.Value)
        End Sub

        <Fact()>
        Public Sub CollectionInitializersCompleteObjectCreation()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System
Imports System.Collections
Imports System.Collections.Generic

Public Class C2
    Implements IEnumerable

    Dim mylist As List(Of String)

    Public Sub Add(p As Long)
    End Sub

    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return mylist.GetEnumerator
    End Function
End Class

Class C1
    Public Shared Sub Main()
        Dim foo As Byte = 23
        Dim abcdef As New C2() From {foo} 'BIND:"New C2() From {foo}"    
    End Sub
End Class

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", 0)
            Assert.Equal("C2", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("C2", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)
            Assert.Equal("Sub C2..ctor()", semanticSummary.Symbol.ToTestDisplayString)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(1, semanticSummary.MemberGroup.Length)
            Assert.Equal("Sub C2..ctor()", semanticSummary.MemberGroup(0).ToTestDisplayString)
            Assert.False(semanticSummary.ConstantValue.HasValue)
            Assert.Null(semanticSummary.ConstantValue.Value)

            Dim tree = compilation.SyntaxTrees(0)
            Dim node = FindNodeFromText(tree, "abcdef")
            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbolFromSyntaxNode(node)
            Assert.Equal("abcdef As C2", symbol.ToTestDisplayString)
        End Sub

#End Region

        <Fact(), WorkItem(544083, "DevDiv")>
        Public Sub PropertySpeculativeBinding()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
            <compilation>
                <file name="a.vb">
Module Module1
    Property Property1 As String
    Sub Main()
        'BINDHERE 
    End Sub    
End Module
    </file>
            </compilation>)
            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim position = compilation.SyntaxTrees.Single().ToString().IndexOf("'BINDHERE", StringComparison.Ordinal)

            Dim expr = SyntaxFactory.ParseExpression("Property1")
            Dim speculativeTypeInfo = semanticModel.GetSpeculativeTypeInfo(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal("String", speculativeTypeInfo.ConvertedType.ToDisplayString())

            Dim conv = semanticModel.GetSpeculativeConversion(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal(ConversionKind.Identity, conv.Kind)
            Assert.Equal("String", speculativeTypeInfo.Type.ToDisplayString())

            Dim speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal("Public Property Property1 As String", speculativeSymbolInfo.Symbol.ToDisplayString())
            Assert.Equal(SymbolKind.Property, speculativeSymbolInfo.Symbol.Kind)
        End Sub

        <Fact(), WorkItem(544083, "DevDiv")>
        Public Sub WriteOnlyPropertySpeculativeBinding()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
            <compilation>
                <file name="a.vb">
Module Module1
    WriteOnly Property Property1 As String
        Set
        End Set
    End Property

    Sub Main()
        'BINDHERE 
    End Sub    
End Module
    </file>
            </compilation>)
            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim position = compilation.SyntaxTrees.Single().ToString().IndexOf("'BINDHERE", StringComparison.Ordinal)

            Dim expr = SyntaxFactory.ParseExpression("Property1")
            Dim speculativeTypeInfo = semanticModel.GetSpeculativeTypeInfo(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal("String", speculativeTypeInfo.ConvertedType.ToDisplayString())

            Dim speculativeConv = semanticModel.GetSpeculativeConversion(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal(ConversionKind.Identity, speculativeConv.Kind)
            Assert.Equal("String", speculativeTypeInfo.Type.ToDisplayString())

            Dim speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal("Public WriteOnly Property Property1 As String", speculativeSymbolInfo.Symbol.ToDisplayString())
            Assert.Equal(SymbolKind.Property, speculativeSymbolInfo.Symbol.Kind)
        End Sub

        <Fact()>
        Public Sub HandlesEvent_WithEvents()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class cls1
    Public Event e1()
End Class

Module Program
    Public WithEvents ww As cls1

    Sub Main() Handles ww.e1'BIND:"e1"

    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event cls1.e1()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub HandlesContainer_WithEvents()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class cls1
    Public Event e1()
End Class

Module Program
    Public WithEvents ww As cls1

    Sub Main() Handles ww.e1'BIND:"ww"

    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of WithEventsEventContainerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("WithEvents Program.ww As cls1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub HandlesProperty_WithEvents()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.ComponentModel

Namespace Project1
    Module m1
        Public Sub main()
            Dim c = New Sink
            Dim s = New OuterClass
            c.x = s
            s.Test()
        End Sub
    End Module

    Class EventSource
        Public Event MyEvent()
        Sub test()
            RaiseEvent MyEvent()
        End Sub
    End Class


    Class OuterClass

        Private Shared SubObject As New EventSource

        <DesignOnly(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Content)>
        Public Property SomeProperty() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property


        Sub Test()
            SubObject.test()
        End Sub
    End Class

    Class Sink

        Public WithEvents x As OuterClass
        Sub foo() Handles x.SomeProperty.MyEvent   'BIND:"SomeProperty"

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub test()
            x.Test()
        End Sub
        Sub New()
            x = New OuterClass
        End Sub
    End Class
    '.....
End Namespace
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Property Project1.OuterClass.SomeProperty As Project1.EventSource", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Count)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Count)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub HandlesProperty_WithEvents001()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.ComponentModel

Namespace Project1
    Module m1
        Public Sub main()
            Dim c = New Sink
            Dim s = New OuterClass
            c.x = s
            s.Test()
        End Sub
    End Module

    Class EventSource
        Public Event MyEvent()
        Sub test()
            RaiseEvent MyEvent()
        End Sub
    End Class


    Class OuterClass

        Private Shared SubObject As New EventSource

        <DesignOnly(True)>
        <DesignerSerializationVisibility(DesignerSerializationVisibility.Content)>
        Public Property SomeProperty() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property


        Sub Test()
            SubObject.test()
        End Sub
    End Class

    Class Sink

        Public WithEvents x As OuterClass
        Sub foo() Handles x.SomeProperty.MyEvent'BIND:"x"

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub test()
            x.Test()
        End Sub
        Sub New()
            x = New OuterClass
        End Sub
    End Class
    '.....
End Namespace

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of WithEventsEventContainerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("WithEvents Project1.Sink.x As Project1.OuterClass", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Count)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Count)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub HandlesEvent_Me()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class cls1
    Shared Public Event e1()

    Sub foo() Handles Me.e1'BIND:"e1"
    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event cls1.e1()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub HandlesEvent_MybaseInBase()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base

    Shared Sub foo() Handles MyBase.e1'BIND:"e1"
    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event base.e1()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub HandlesEvent_WithEventsInclass()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base
End Class

Class cls3
    Public WithEvents we As cls1

    Public Sub foo() Handles we.e1'BIND:"e1"

    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event base.e1()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub HandlesEvent_WithEventsHandlesInDerived()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base
End Class

Class C0
    Public WithEvents we As cls1
End Class

Class C1
    Inherits C0
End Class

Class cls3
    Inherits C1

    Public Sub foo() Handles we.e1'BIND:"e1"

    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Event base.e1()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Event, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub


        <Fact()>
        Public Sub HandlesContainer_WithEventsHandlesInDerived()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base
End Class

Class C0
    Public WithEvents we As cls1
End Class

Class C1
    Inherits C0

    Public Sub moo() Handles we.e1

    End Sub
End Class

Class cls3
    Inherits C1

    Public Sub foo() Handles we.e1'BIND:"we"

    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module


    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of WithEventsEventContainerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("WithEvents C0.we As cls1", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, semanticSummary.Symbol.Kind)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub


        <Fact>
        Public Sub HandledEvent001()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="Compilation">
        <file name="q.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base
End Class

Class C0
    Inherits cls1
    Public WithEvents we As cls1
End Class

Class C1
    Inherits C0

    Public Sub moo() Handles we.e1

    End Sub
End Class

Class cls3
    Inherits C1
    Public Shadows Event e1()

    Public Sub foo() Handles we.e1, Me.e1, MyBase.e1, MyClass.e1

    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module


    </file>
    </compilation>, options:=TestOptions.ReleaseExe)

            Dim globalNS = compilation.GlobalNamespace
            Dim class_cls2 = DirectCast(globalNS.GetMembers("cls3").Single(), NamedTypeSymbol)
            Assert.Null(class_cls2.AssociatedSymbol)
            Dim meth_foo = DirectCast(class_cls2.GetMembers("foo").Single(), SourceMethodSymbol)

            Dim handledEvents = meth_foo.HandledEvents

            Assert.Equal(4, handledEvents.Length)

            Dim handledEvent0 = handledEvents(0)
            Assert.Equal(HandledEventKind.WithEvents, handledEvent0.HandlesKind)
            Assert.Equal("e1", handledEvent0.EventSymbol.Name)

            Dim we = handledEvent0.EventContainer
            Assert.Equal("C0", we.ContainingType.Name)
            Assert.Equal(False, we.IsImplicitlyDeclared)
            Assert.Equal(False, we.IsOverrides)

            Dim handledEvent1 = handledEvents(1)
            Assert.Equal(HandledEventKind.Me, handledEvent1.HandlesKind)
            Assert.NotEqual(handledEvent0.EventSymbol, handledEvent1.EventSymbol)

            Dim handledEvent2 = handledEvents(2)
            Assert.Equal(HandledEventKind.MyBase, handledEvent2.HandlesKind)
            Assert.Equal("e1", handledEvent2.EventSymbol.Name)
            Assert.Equal(handledEvent0.EventSymbol, handledEvent2.EventSymbol)
            Dim handledEvent3 = handledEvents(3)
            Assert.Equal(HandledEventKind.MyClass, handledEvent3.HandlesKind)
            Assert.NotEqual(handledEvent0.EventSymbol, handledEvent3.EventSymbol)
            Assert.Equal("Private e1Event As cls3.e1EventHandler", handledEvent3.EventSymbol.AssociatedField.ToString)
            Assert.True(handledEvent3.EventSymbol.HasAssociatedField)
            Assert.Equal(ImmutableArray(Of VisualBasicAttributeData).Empty, handledEvent3.EventSymbol.GetFieldAttributes)
            Assert.Null(handledEvent3.EventSymbol.OverriddenEvent)
            Assert.Null(handledEvent0.EventSymbol.RaiseMethod)
            Dim commonEventSymbol As IEventSymbol = handledEvent0.EventSymbol
            Assert.Equal(handledEvent0.EventSymbol.AddMethod, commonEventSymbol.AddMethod)
            Assert.Equal(handledEvent0.EventSymbol.RemoveMethod, commonEventSymbol.RemoveMethod)
            Assert.Equal(handledEvent0.EventSymbol.RaiseMethod, commonEventSymbol.RaiseMethod)
            Assert.Equal(handledEvent0.EventSymbol.Type, commonEventSymbol.Type)
            Assert.Equal(handledEvent0.EventSymbol.OverriddenEvent, commonEventSymbol.OverriddenEvent)
            Assert.Equal(handledEvent0.EventSymbol.ExplicitInterfaceImplementations.Length, commonEventSymbol.ExplicitInterfaceImplementations.Length)
            Assert.Equal(handledEvent0.EventSymbol.GetHashCode, handledEvent0.EventSymbol.GetHashCode)
        End Sub

        <Fact()>
        Public Sub Handles_WithEventsAmbiguous()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base
End Class

Class C0
    Public Property we As cls1

    Public Property we(x As Integer) As cls1
        Get
            Return Nothing
        End Get
        Set(ByVal value As cls1)

        End Set
    End Property
End Class

Class cls3
    Inherits C0

    Public Sub foo() Handles we.e1'BIND:"we"

    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of WithEventsEventContainerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.Ambiguous, semanticSummary.CandidateReason)
            Assert.Equal(2, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Property C0.we As cls1", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)
            Assert.Equal("Property C0.we(x As System.Int32) As cls1", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(1).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub Handles_NotAWithEvents()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base
End Class

Class C0
    Public Property we As cls1
End Class

Class cls3
    Inherits C0

    Public Sub foo() Handles we.e1'BIND:"we"

    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of WithEventsEventContainerSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotAWithEventsMember, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Property C0.we As cls1", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub Handles_NotAnEvent()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class base
    Public Event e1()

End Class

Class cls1
    Inherits base

    Property P1 As Integer
End Class

Class C0
    Public WithEvents we As cls1
End Class

Class cls3
    Inherits C0

    Public Sub foo() Handles we.P1'BIND:"P1"

    End Sub
End Class

Module Program
    Sub Main()

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.NotAnEvent, semanticSummary.CandidateReason)
            Assert.Equal(1, semanticSummary.CandidateSymbols.Length)
            Dim sortedCandidates = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Property cls1.P1 As System.Int32", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub DllImportSemanticModel()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.InteropServices
Module Module1
    <DllImport("user32.dll", CharSet:=CharSet.Unicode, ExactSpelling:=False, EntryPoint:="MessageBox")>
    Public Function MessageBox(hwnd As IntPtr, t As String, t1 As String, t2 As UInt32) As Integer
    End Function

    Sub Main()
        MessageBox(IntPtr.Zero, "", "", 1) 'BIND:"MessageBox"
    End Sub
End Module
]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Dim sym = DirectCast(semanticSummary.Symbol, SourceMethodSymbol)
            Assert.Equal("Function Module1.MessageBox(hwnd As System.IntPtr, t As System.String, t1 As System.String, t2 As System.UInt32) As System.Int32", sym.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sym.Kind)
            Assert.Equal("Module1", sym.ContainingSymbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub DeclareStatementSemanticModel()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Module Module1
    Public Declare Function MessageBox Lib "user32.dll" Alias "MessageBoxA" (
        hwnd As IntPtr, t As String, t2 As String, t2 As UInt32) As Integer
    Sub Main()
        MessageBox(IntPtr.Zero, "", "", 1) 'BIND:"MessageBox"
    End Sub
End Module
]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Dim sym = DirectCast(semanticSummary.Symbol, SourceMethodSymbol)
            Assert.Equal("Declare Ansi Function Module1.MessageBox Lib ""user32.dll"" Alias ""MessageBoxA"" (hwnd As System.IntPtr, t As System.String, t2 As System.String, t2 As System.UInt32) As System.Int32", sym.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, sym.Kind)
            Assert.Equal("Module1", sym.ContainingSymbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub LateBoundCall001()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim o As Object = "qqq"
        Dim i As Integer = o.Length'BIND:"Length"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Int32", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.NarrowingValue, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LateBoundCallOverloaded()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim o As Object = 1
        foo(o) 'BIND:"foo"
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(x As Integer)

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)
            Assert.Equal(2, semanticSummary.CandidateSymbols.Length)
            Dim sortedSymbols = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Program.foo(x As System.Int32)", sortedSymbols(0).ToTestDisplayString())
            Assert.Equal("Sub Program.foo(x As System.String)", sortedSymbols(1).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Program.foo(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Program.foo(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LateBoundCallOverloaded001()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim o As Object = 1
        foo(o)'BIND:"foo(o)"
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(x As Integer)

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)
            Assert.Equal(2, semanticSummary.CandidateSymbols.Length)
            Dim sortedSymbols = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Program.foo(x As System.Int32)", sortedSymbols(0).ToTestDisplayString())
            Assert.Equal("Sub Program.foo(x As System.String)", sortedSymbols(1).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Program.foo(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Program.foo(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LateBoundCallOverloaded002()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Shared Sub Main(args As String())
        Dim o As Object = 1
        Dim c As New Program
        c.foo(o)'BIND:"c.foo"
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(x As Integer)

    End Sub
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)
            Assert.Equal(2, semanticSummary.CandidateSymbols.Length)
            Dim sortedSymbols = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Program.foo(x As System.Int32)", sortedSymbols(0).ToTestDisplayString())
            Assert.Equal("Sub Program.foo(x As System.String)", sortedSymbols(1).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub Program.foo(x As System.Int32)", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Sub Program.foo(x As System.String)", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LateBoundIndex()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim o As Object = 1
        Dim x = o(o)'BIND:"o(o)"
    End Sub

    Sub foo(x As String)

    End Sub

    Sub foo(x As Integer)

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LateBoundIndexOverloaded()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Class Program
    Sub Main(args As String())
        Dim o As Object = 1
        Dim c As New Program
        Dim x = c(o)'BIND:"c(o)"
    End Sub

    Default ReadOnly Property P1(x As Integer) As Integer
        Get
            Return 1
        End Get
    End Property

    Default ReadOnly Property P1(x As String) As Integer
        Get
            Return 1
        End Get
    End Property
End Class
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)
            Assert.Equal(2, semanticSummary.CandidateSymbols.Length)
            Dim sortedSymbols = semanticSummary.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("ReadOnly Property Program.P1(x As System.Int32) As System.Int32", sortedSymbols(0).ToTestDisplayString())
            Assert.Equal("ReadOnly Property Program.P1(x As System.String) As System.Int32", sortedSymbols(1).ToTestDisplayString())

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(2, semanticSummary.MemberGroup.Length)
            Dim sortedMethodGroup = semanticSummary.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("ReadOnly Property Program.P1(x As System.Int32) As System.Int32", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("ReadOnly Property Program.P1(x As System.String) As System.Int32", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub LateBoundAddressOf()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim o As Object = "qq"
        Dim a As Action = AddressOf o.Length'BIND:"o.Length"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of MemberAccessExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Object", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact(), WorkItem(545976, "DevDiv")>
        Public Sub ArrayLiteralSpeculativeBinding()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
            <compilation>
                <file name="a.vb">
Module Module1
    Sub Main()
        'BINDHERE 
    End Sub    
End Module
    </file>
            </compilation>)
            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim position = compilation.SyntaxTrees.Single().ToString().IndexOf("'BINDHERE", StringComparison.Ordinal)

            Dim expr = SyntaxFactory.ParseExpression("{1, 2, 3}")
            Dim speculativeTypeInfo = semanticModel.GetSpeculativeTypeInfo(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Null(speculativeTypeInfo.Type)
            Assert.Equal("Integer()", speculativeTypeInfo.ConvertedType.ToDisplayString())

            Dim speculativeConversion = semanticModel.GetSpeculativeConversion(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal(ConversionKind.Widening, speculativeConversion.Kind)
        End Sub

        <WorkItem(545346, "DevDiv")>
        <Fact()>
        Public Sub Bug13693()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
            <compilation>
                <file name="a.vb">
Class C
    Sub Foo()
        Dim x As New |D(5) 'BIND:"Dim x As New |D(5)"
    End Sub
End Class
    </file>
            </compilation>)

            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim node = compilation.SyntaxTrees(0).FindNodeOrTokenByKind(SyntaxKind.NewKeyword)
            Dim info = semanticModel.GetSymbolInfo(DirectCast(node.Parent, NewExpressionSyntax).Type)
            Assert.Equal(SymbolInfo.None, info)
        End Sub

        ''' <summary>
        ''' Bind reference to property with no accessors.
        ''' </summary>
        ''' <remarks></remarks>
        <WorkItem(546182, "DevDiv")>
        <Fact>
        Public Sub ReferenceToPropertyWithNoAccessors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Class C
    Property P As Object
    End Property
End Class
Module M
    Sub M(o As C)
        Dim v As Object
        v = o.P
        o.P = v
    End Sub
End Module
]]></file>
</compilation>, {SystemCoreRef})
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30124: Property without a 'ReadOnly' or 'WriteOnly' specifier must provide both a 'Get' and a 'Set'.
    Property P As Object
             ~
BC30524: Property 'P' is 'WriteOnly'.
        v = o.P
            ~~~
BC30526: Property 'P' is 'ReadOnly'.
        o.P = v
        ~~~~~~~
]]></errors>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            ' v = o.P
            Dim syntax = FindNodeOfTypeFromText(Of AssignmentStatementSyntax)(tree, "v = o.P")
            Dim info = GetSemanticInfoSummary(model, syntax.Right)
            Assert.NotNull(info.Type)
            Assert.Equal(info.Type.SpecialType, SpecialType.System_Object)
            Assert.Null(info.Symbol)
            CheckSymbols(info.CandidateSymbols, "Property C.P As Object")
            Dim [property] = DirectCast(info.CandidateSymbols(0), PropertySymbol)
            Assert.Null([property].GetMethod)
            Assert.Null([property].SetMethod)

            ' o.P = v
            syntax = FindNodeOfTypeFromText(Of AssignmentStatementSyntax)(tree, "o.P = v")
            info = GetSemanticInfoSummary(model, syntax.Left)
            Assert.NotNull(info.Type)
            Assert.Equal(info.Type.SpecialType, SpecialType.System_Object)
            CheckSymbol(info.Symbol, "Property C.P As Object")
            [property] = DirectCast(info.Symbol, PropertySymbol)
            Assert.Null([property].GetMethod)
            Assert.Null([property].SetMethod)
        End Sub

        <Fact()>
        Public Sub SpeculativeConstantValueForGroupAggregationSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
            <compilation>
                <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim qr_Result = From i In "hello" Select a = i, b = "test" Group By a Into fielda = Group
    End Sub
End Module
    </file>
            </compilation>)
            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim source = compilation.SyntaxTrees.Single().GetCompilationUnitRoot().ToFullString()
            Dim position = source.IndexOf("fielda = Group", StringComparison.Ordinal)
            Dim syntaxNode = compilation.SyntaxTrees().Single().GetCompilationUnitRoot().FindToken(position).Parent.Parent.Parent.DescendantNodesAndSelf.OfType(Of GroupAggregationSyntax).Single()

            Dim speculativeConstantValue = semanticModel.GetSpeculativeConstantValue(syntaxNode.SpanStart, syntaxNode)
            Assert.False(speculativeConstantValue.HasValue)
        End Sub

        <WorkItem(546270, "DevDiv")>
        <Fact()>
        Public Sub SpeculativeConstantValueForLabelSyntax()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
            <compilation>
                <file name="a.vb">
Module Program
    Sub Main(args As String())
        If True Then GoTo Label1
Label1:
    End Sub
End Module
    </file>
            </compilation>)
            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim source = compilation.SyntaxTrees.Single().GetCompilationUnitRoot().ToFullString()
            Dim position = source.IndexOf("GoTo Label1", StringComparison.Ordinal)
            Dim syntaxNode = compilation.SyntaxTrees().Single().GetCompilationUnitRoot().FindToken(position).Parent.DescendantNodesAndSelf.OfType(Of LabelSyntax).Single()

            Dim speculativeTypeInfo = semanticModel.GetSpeculativeConstantValue(syntaxNode.SpanStart, syntaxNode)
            Assert.False(speculativeTypeInfo.HasValue)
        End Sub

        <Fact()>
        Public Sub Regress15532()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

#Const foo = Nothing

Module Program
    Sub Main(args As String())
#If foo = 3 + 20 Then 'BIND:"20"
        Console.WriteLine()
#Else
        console.writeline()
#End If

    End Sub
End Module

    ]]></file>
</compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of LiteralExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticSummary.Type)
            Assert.Null(semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub


#Region "Diagnostics"

        <WorkItem(541269, "DevDiv")>
        <Fact()>
        Public Sub GetDiagnosticsAddressOfOperatorWithoutMscorlibRef()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Foo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb">
Namespace Server
    Public Class Scen8
        Delegate Function DelFunction() As Integer
        Delegate Sub DelSub(x as Integer)
        Public DelField1 As DelFunction = AddressOf TestFunction
        Public DelField2 As DelSub = AddressOf TestSub

        Friend Function TestFunction() As Integer
            Return 42
        End Function

        Friend Sub TestSub(x as Integer)
        End Sub

        public sub MySub()
            Dim delLocal1 As DelFunction = AddressOf TestFunction
            Dim delLocal2 As DelSub = AddressOf TestSub
        End Sub
    End Class
End Namespace 
    </file>
</compilation>, {})

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)
            Dim diagnostics = bindingsA.GetDiagnostics()

            AssertTheseDiagnostics(compilation,
<errors>
BC30002: Type 'System.Void' is not defined.
    Public Class Scen8
    ~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'Compilation.dll' failed.
    Public Class Scen8
                 ~~~~~
BC30002: Type 'System.AsyncCallback' is not defined.
        Delegate Function DelFunction() As Integer
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.IAsyncResult' is not defined.
        Delegate Function DelFunction() As Integer
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.IntPtr' is not defined.
        Delegate Function DelFunction() As Integer
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Object' is not defined.
        Delegate Function DelFunction() As Integer
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Void' is not defined.
        Delegate Function DelFunction() As Integer
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'MulticastDelegate' from assembly or module 'Compilation.dll' failed.
        Delegate Function DelFunction() As Integer
                          ~~~~~~~~~~~
BC30002: Type 'System.Int32' is not defined.
        Delegate Function DelFunction() As Integer
                                           ~~~~~~~
BC30002: Type 'System.AsyncCallback' is not defined.
        Delegate Sub DelSub(x as Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.IAsyncResult' is not defined.
        Delegate Sub DelSub(x as Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.IntPtr' is not defined.
        Delegate Sub DelSub(x as Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Object' is not defined.
        Delegate Sub DelSub(x as Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Void' is not defined.
        Delegate Sub DelSub(x as Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Void' is not defined.
        Delegate Sub DelSub(x as Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'MulticastDelegate' from assembly or module 'Compilation.dll' failed.
        Delegate Sub DelSub(x as Integer)
                     ~~~~~~
BC30002: Type 'System.Int32' is not defined.
        Delegate Sub DelSub(x as Integer)
                                 ~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'Compilation.dll' failed.
        Public DelField1 As DelFunction = AddressOf TestFunction
                                                    ~~~~~~~~~~~~
BC31143: Method 'Friend Function TestFunction() As Integer' does not have a signature compatible with delegate 'Delegate Function Scen8.DelFunction() As Integer'.
        Public DelField1 As DelFunction = AddressOf TestFunction
                                                    ~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'Compilation.dll' failed.
        Public DelField2 As DelSub = AddressOf TestSub
                                               ~~~~~~~
BC31143: Method 'Friend Sub TestSub(x As Integer)' does not have a signature compatible with delegate 'Delegate Sub Scen8.DelSub(x As Integer)'.
        Public DelField2 As DelSub = AddressOf TestSub
                                               ~~~~~~~
BC30002: Type 'System.Int32' is not defined.
        Friend Function TestFunction() As Integer
                                          ~~~~~~~
BC30002: Type 'System.Int32' is not defined.
            Return 42
                   ~~
BC30002: Type 'System.Void' is not defined.
        Friend Sub TestSub(x as Integer)
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30002: Type 'System.Int32' is not defined.
        Friend Sub TestSub(x as Integer)
                                ~~~~~~~
BC30002: Type 'System.Void' is not defined.
        public sub MySub()
        ~~~~~~~~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'Compilation.dll' failed.
            Dim delLocal1 As DelFunction = AddressOf TestFunction
                                                     ~~~~~~~~~~~~
BC31143: Method 'Friend Function TestFunction() As Integer' does not have a signature compatible with delegate 'Delegate Function Scen8.DelFunction() As Integer'.
            Dim delLocal1 As DelFunction = AddressOf TestFunction
                                                     ~~~~~~~~~~~~
BC31091: Import of type 'Object' from assembly or module 'Compilation.dll' failed.
            Dim delLocal2 As DelSub = AddressOf TestSub
                                                ~~~~~~~
BC31143: Method 'Friend Sub TestSub(x As Integer)' does not have a signature compatible with delegate 'Delegate Sub Scen8.DelSub(x As Integer)'.
            Dim delLocal2 As DelSub = AddressOf TestSub
                                                ~~~~~~~
</errors>)
        End Sub

        <WorkItem(541271, "DevDiv")>
        <Fact()>
        Public Sub GetDiagnosticsSubInsideAnInterfaceWithoutMscorlibRef()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Foo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithReferences(
<compilation name="Compilation">
    <file name="a.vb">
Friend Interface I10
    Sub foo()
End Interface
    </file>
</compilation>, {})

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)
            Dim diagnostics = bindingsA.GetDiagnostics()

            Assert.Equal(1, diagnostics.Length())
            AssertTheseDiagnostics(diagnostics,
<expected>
BC30002: Type 'System.Void' is not defined.
    Sub foo()
    ~~~~~~~~~                                               
</expected>)
        End Sub

        <WorkItem(541304, "DevDiv")>
        <Fact()>
        Public Sub GetDiagnosticsDoLoopWithConditionAtBottomAndTopPart()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Foo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())
        Do Until
        Loop Until
    End Sub
End Module
    </file>
</compilation>, options)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim diagnostics = compilation.GetDiagnostics()

            Assert.InRange(diagnostics.Length(), 1, Integer.MaxValue)
            AssertTheseDiagnostics(diagnostics,
<expected>
BC30201: Expression expected.
        Do Until
                ~
BC30238: 'Loop' cannot have a condition if matching 'Do' has one.
        Loop Until
             ~~~~~
BC30201: Expression expected.
        Loop Until
                  ~
</expected>)
        End Sub

        <Fact()>
        Public Sub GetDiagnosticsDoLoopWithConditionAtBottomAndTopPart2()
            Dim options = TestOptions.ReleaseDll.WithRootNamespace("Foo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Module Program
    Sub Main(ByVal args As String())
        dim a as Integer = 23
        ' test that this only shows one diagnostic for using both conditions
        Do Until a &gt; 42
        Loop Until a = 23

        ' test that diagnostics get reported from both conditions
        Do Until (foo() + 23)
        Loop Until a = (foo() + 23)
    End Sub

    Sub foo()
    end sub
End Module
    </file>
</compilation>, options)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")

            AssertTheseDiagnostics(compilation.GetDiagnostics(),
<expected>
BC30238: 'Loop' cannot have a condition if matching 'Do' has one.
        Loop Until a = 23
             ~~~~~
BC30491: Expression does not produce a value.
        Do Until (foo() + 23)
                  ~~~~~
BC30238: 'Loop' cannot have a condition if matching 'Do' has one.
        Loop Until a = (foo() + 23)
             ~~~~~
BC30491: Expression does not produce a value.
        Loop Until a = (foo() + 23)
                        ~~~~~
</expected>)
        End Sub

        <WorkItem(541407, "DevDiv")>
        <Fact>
        Public Sub GetDiagnosticsWithRootNamespace()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Option Strict Off
Imports System

Class C
    Sub M()
        Dim a As String
        a = New Guid()
    End Sub
End Class
    </file>
    <file name="b.vb">
Namespace N1
    Public Class Class2
        Sub b()
            Return 43
        End Sub
    End Class
End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseDll.WithRootNamespace("Foo.Bar"))

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim semanticModelA = compilation.GetSemanticModel(treeA)
            CompilationUtils.AssertTheseDiagnostics(semanticModelA.GetDiagnostics(),
<expected>
BC30311: Value of type 'Guid' cannot be converted to 'String'.
        a = New Guid()
            ~~~~~~~~~~
</expected>)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim semanticModelB = compilation.GetSemanticModel(treeB)
            CompilationUtils.AssertTheseDiagnostics(semanticModelB.GetDiagnostics(),
<expected>
BC30647: 'Return' statement in a Sub or a Set cannot return a value.
            Return 43
            ~~~~~~~~~
</expected>)

        End Sub

        <Fact, WorkItem(541479, "DevDiv")>
        Public Sub GetDiagnosticsPropNameAsForLoopVariable()
            CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Friend Module CtFor001_01mod

    Sub main()
        'COMPILEERROR: BC30039, "prop1"
        For prop1 = 1 To 10
            Dim vvv As Short
            vvv = prop1
        Next

    End Sub

    Public Property prop1() As Short
        Set(ByVal Value As Short)
        End Set
        Get
            Return 1
        End Get
    End Property
End Module
    </file>
</compilation>).VerifyDiagnostics(Diagnostic(ERRID.ERR_LoopControlMustNotBeProperty, "prop1"))

        End Sub

        <WorkItem(541480, "DevDiv")>
        <Fact()>
        Public Sub GetDiagnosticsWithEventsInStruct()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Imports System

Friend Module StructFunc040mod
    Class c1
    End Class

    Structure struct1
        'COMPILEERROR: BC30435,"withevents"
        Dim WithEvents c As c1

    End Structure

End Module
    </file>
</compilation>)

            Dim errs = compilation.GetDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(compilation,
<errors>
BC30435: Members in a Structure cannot be declared 'WithEvents'.
        Dim WithEvents c As c1
            ~~~~~~~~~~
</errors>)

        End Sub

        <WorkItem(541559, "DevDiv")>
        <Fact()>
        Public Sub BindIncompleteFieldDeclAsArray()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Public Module publicHMod
    Public DecimalVarArr(
    </file>
</compilation>)

            Dim errs = compilation.GetDiagnostics()
            Assert.NotEqual(0, errs.Length())
        End Sub

        <WorkItem(541578, "DevDiv")>
        <Fact()>
        Public Sub PassByRefArgumentWithAlias()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Imports System
Imports ProjRoot = Qlid036

Namespace Qlid036
    Friend Module QLID036BAS
        Public VariableA As Integer

        Sub proc307(ByRef tempvar As Object)
        End Sub

        Sub S()
            proc307(ProjRoot.[VariableA])
        End Sub
    End Module
End Namespace
    </file>
</compilation>)

            compilation.AssertNoDiagnostics()
        End Sub

        <WorkItem(541579, "DevDiv")>
        <Fact()>
        Public Sub InvalidLabelsWithNumericSuffix()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
  Friend Module GotoRegress003mod
    Sub Main()

' COMPILEERROR: BC30035, "19I"
19I:
' COMPILEERROR: BC30035, "23L"
23L:
' Was COMPILEERROR: BC30801, "24", BC30451, "M" in Dev10
' Roslyn: BC30801: Labels that are numbers must be followed by colons.
24M:
' again a BC30801: Labels that are numbers must be followed by colons.
' but because the M and Main from these labels are now skipped tokens of the colon
' we'll get a duplicate label as well (same behavior as Dev10)
24Main:

' COMPILEERROR: BC30035, "31S"
31S:

' BC31395: Type characters are not allowed in label identifiers.
C$:

' BC36637: The '?' character cannot be used here.
C?:

' BC30035: Syntax error.
12%:
    End Sub
  End Module
    </file>
</compilation>)

            Dim errs = compilation.GetDiagnostics()
            Assert.NotEqual(0, errs.Length())
            AssertTheseDiagnostics(compilation,
<Expected>
BC30035: Syntax error.
19I:
~~~
BC30035: Syntax error.
23L:
~~~
BC30801: Labels that are numbers must be followed by colons.
24M:
~~
BC30094: Label '24' is already defined in the current method.
24Main:
~~
BC30801: Labels that are numbers must be followed by colons.
24Main:
~~
BC30035: Syntax error.
31S:
~~~
BC31395: Type characters are not allowed in label identifiers.
C$:
~~
BC30451: 'C' is not declared. It may be inaccessible due to its protection level.
C?:
~
BC36637: The '?' character cannot be used here.
C?:
 ~
BC30035: Syntax error.
12%:
~~~
</Expected>)
        End Sub

        <WorkItem(541619, "DevDiv")>
        <Fact()>
        Public Sub ExceptionVariableUsedInLambda()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation name="Compilation">
    <file name="a.vb">
Imports System

Module Program
    Sub Main(args As String())
        Try
        Catch ex As Exception
            Dim x1 As Func(Of Integer, String) = Function(x) ex.Message
        End Try
    End Sub
End Module
    </file>
</compilation>)

            Dim errs = compilation.GetDiagnostics()
            Assert.Equal(0, errs.Length())
        End Sub

        <WorkItem(543393, "DevDiv")>
        <Fact()>
        Public Sub MemberOfIncompleteClassDecl()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
Module TestModule
    Sub Main()
        Dim val = TestMethod.NUMBER
    End Sub
End Module

Class TestMethod
    Public Const NUMBER 
    </file>
</compilation>)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim rootA = treeA.GetCompilationUnitRoot()
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Assert.NotEmpty(bindingsA.GetDiagnostics())
        End Sub

        <WorkItem(529095, "DevDiv")>
        <Fact>
        Public Sub CannotConvertConstExprToType()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim b As ULong = Integer.MaxValue + 1
    End Sub
End Module
    ]]></file>
</compilation>)

            ' (3,26): error BC30439: Constant expression not representable in type 'Integer'.
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_ExpressionOverflow1, "Integer.MaxValue + 1").WithArguments("Integer"))
        End Sub

        Private Function GetTypeParameterSymbol(compilation As VisualBasicCompilation,
                                             semanticModel As SemanticModel,
                                             treeName As String,
                                             stringInDecl As String,
                                             ByRef syntax As TypeParameterSyntax) As ITypeParameterSymbol
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim node = CompilationUtils.FindTokenFromText(tree, stringInDecl).Parent
            While Not (TypeOf node Is TypeParameterSyntax)
                node = node.Parent
                Assert.NotNull(node)
            End While

            syntax = DirectCast(node, TypeParameterSyntax)
            Return semanticModel.GetDeclaredSymbol(syntax)
        End Function

        <WorkItem(543603, "DevDiv")>
        <Fact()>
        Public Sub BC30282ERR_InvalidConstructorCall_AddressOfConstructor()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Friend Module RDNegConstructormod
    Class C1
        Public Sub New(ByVal t As Byte)
        End Sub
    End Class

    Public Delegate Sub delInteger(ByVal a As Integer)

    Sub Main()
        Dim class1 As C1 = New C1(1)
        Dim dInteger As delInteger = AddressOf class1.New
    End Sub
End Module
    ]]></file>
</compilation>)

            ' (11): error BC30282: Constructor call is valid only as the first statement in an instance constructor.
            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InvalidConstructorCall, "class1.New"))
        End Sub

        <WorkItem(544648, "DevDiv")>
        <Fact>
        Public Sub SpeculativelyBindExtensionMethod()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
    <compilation name="DetectingExtensionAttributeOnImport">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Reflection

Module Program
    Sub Main()
        Dim fields As FieldInfo() = GetType(Exception).GetFields()
        Console.WriteLine(fields.Any(TryCast(Function(field) field.IsStatic, Func(Of FieldInfo, Boolean))))
    End Sub

    &lt;System.Runtime.CompilerServices.Extension&gt;
    Function Any(Of T)(s As IEnumerable(Of T), predicate As Func(Of T, Boolean)) As Boolean
        Return False
    End Function

    &lt;System.Runtime.CompilerServices.Extension&gt;
    Function Any(Of T)(s As ICollection(Of T), predicate As Func(Of T, Boolean)) As Boolean
        Return True
    End Function
End Module
        </file>
    </compilation>, {SystemCoreRef})
            comp.VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim originalSyntax = tree.GetCompilationUnitRoot().DescendantNodes.OfType(Of InvocationExpressionSyntax).Last()
            Assert.True(originalSyntax.ToString().StartsWith("fields", StringComparison.Ordinal))

            Dim info1 = model.GetSymbolInfo(originalSyntax)
            Dim method1 = TryCast(info1.Symbol, MethodSymbol)
            Assert.NotNull(method1)

            Assert.Equal("Function System.Collections.Generic.ICollection(Of System.Reflection.FieldInfo).Any(predicate As System.Func(Of System.Reflection.FieldInfo, System.Boolean)) As System.Boolean", method1.ToTestDisplayString())
            Assert.Equal("Function Program.Any(Of T)(s As System.Collections.Generic.ICollection(Of T), predicate As System.Func(Of T, System.Boolean)) As System.Boolean", method1.ReducedFrom.ToTestDisplayString())
            Assert.Equal("System.Collections.Generic.ICollection(Of System.Reflection.FieldInfo)", method1.ReceiverType.ToTestDisplayString())
            Assert.Equal("System.Reflection.FieldInfo", method1.GetTypeInferredDuringReduction(method1.ReducedFrom.TypeParameters(0)).ToTestDisplayString())

            Assert.Throws(Of InvalidOperationException)(Sub() method1.ReducedFrom.GetTypeInferredDuringReduction(Nothing))
            Assert.Throws(Of ArgumentNullException)(Sub() method1.GetTypeInferredDuringReduction(Nothing))
            Assert.Throws(Of ArgumentException)(Sub() method1.GetTypeInferredDuringReduction(
                                                    comp.SourceModule.GlobalNamespace.GetTypeMember("Program").GetMembers("Any").
                                                        Where(Function(m) m IsNot method1.ReducedFrom).Cast(Of MethodSymbol)().Single().TypeParameters(0)))

            Assert.Equal("Any", method1.Name)
            Dim reducedFrom1 = method1.CallsiteReducedFromMethod
            Assert.NotNull(reducedFrom1)
            Assert.Equal("Function Program.Any(Of System.Reflection.FieldInfo)(s As System.Collections.Generic.ICollection(Of System.Reflection.FieldInfo), predicate As System.Func(Of System.Reflection.FieldInfo, System.Boolean)) As System.Boolean", reducedFrom1.ToTestDisplayString())
            Assert.Equal("Program", reducedFrom1.ReceiverType.ToTestDisplayString())
            Assert.Equal(SpecialType.System_Collections_Generic_ICollection_T, CType(reducedFrom1.Parameters(0).Type.OriginalDefinition, TypeSymbol).SpecialType)

            Dim speculativeSyntax = SyntaxFactory.ParseExpression("fields.Any(Function(field) field.IsStatic)") ' cast removed
            Assert.Equal(SyntaxKind.InvocationExpression, speculativeSyntax.Kind)

            Dim info2 = model.GetSpeculativeSymbolInfo(originalSyntax.SpanStart, speculativeSyntax, SpeculativeBindingOption.BindAsExpression)
            Dim method2 = TryCast(info2.Symbol, MethodSymbol)
            Assert.NotNull(method2)
            Assert.Equal("Any", method2.Name)
            Dim reducedFrom2 = method2.CallsiteReducedFromMethod
            Assert.NotNull(reducedFrom2)
            Assert.Equal(SpecialType.System_Collections_Generic_ICollection_T, CType(reducedFrom2.Parameters(0).Type.OriginalDefinition, TypeSymbol).SpecialType)

            Assert.Equal(reducedFrom1, reducedFrom2)
            Assert.Equal(method1, method2)
        End Sub

        <WorkItem(546126, "DevDiv")>
        <Fact>
        Public Sub SpeculativelyBindExtensionMethod2()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
    <compilation name="DetectingExtensionAttributeOnImport">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim l = CType({1, 2, 3}, IEnumerable(Of Integer)).ToList
    End Sub
End Module
        </file>
    </compilation>, {SystemCoreRef})
            comp.AssertNoDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim originalSyntax = tree.GetCompilationUnitRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax).Single()
            Assert.True(originalSyntax.ToString().EndsWith(".ToList", StringComparison.Ordinal))

            Dim info1 = model.GetSymbolInfo(originalSyntax)
            Dim method1 = TryCast(info1.Symbol, MethodSymbol)
            Assert.NotNull(method1)
            Assert.Equal("ToList", method1.Name)
            Dim reducedFrom1 = method1.CallsiteReducedFromMethod
            Assert.NotNull(reducedFrom1)

            Dim speculativeSyntax = SyntaxFactory.ParseExpression("{1, 2, 3}.ToList") ' cast removed
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, speculativeSyntax.Kind)

            Dim info2 = model.GetSpeculativeSymbolInfo(originalSyntax.SpanStart, speculativeSyntax, SpeculativeBindingOption.BindAsExpression)
            Dim method2 = TryCast(info2.Symbol, MethodSymbol)
            Assert.NotNull(method2)
            Assert.Equal("ToList", method2.Name)
            Dim reducedFrom2 = method2.CallsiteReducedFromMethod
            Assert.NotNull(reducedFrom2)

            Assert.Equal(reducedFrom1, reducedFrom2)
            Assert.Equal(method1, method2)
        End Sub

#End Region

        <Fact>
        Public Sub NamedTypeSymbol_CodeCoverage()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Class base
    <obsolete> Public Event e1()
End Class

Class cls1
    Inherits base
End Class

Class C0
    Inherits cls1
    <obsolete> Public WithEvents we As cls1
End Class

Class C1
    Inherits C0
    Public Sub moo() Handles we.e1
    End Sub
End Class

Class cls3
    Inherits C1
    Public Shadows Event e1()
    Public Sub foo() Handles we.e1, Me.e1, MyBase.e1, MyClass.e1
    End Sub
End Class

Class SharedCons
    Shared Sub New()
    End Sub
End Class

Module Program
    Sub Main()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim globalNS As NamespaceSymbol = compilation.GlobalNamespace
            Dim class_cls2 As NamedTypeSymbol = DirectCast(globalNS.GetMembers("cls3").Single, NamedTypeSymbol)
            Dim sharedSymb As NamedTypeSymbol = DirectCast(globalNS.GetMembers("SharedCons").Single(), NamedTypeSymbol)

            Assert.NotEqual(0, sharedSymb.SharedConstructors.Length)
            sharedSymb = DirectCast(globalNS.GetMembers("Program").Single, NamedTypeSymbol)
            Assert.Equal(0, sharedSymb.SharedConstructors.Length)
            Assert.Equal(0, class_cls2.SharedConstructors.Length)
            Assert.False(sharedSymb.IsScriptClass)
            Assert.False(sharedSymb.IsSubmissionClass)
            Assert.NotNull(RuntimeHelpers.GetObjectValue(sharedSymb.ToString))
            Assert.Equal(sharedSymb.Language, "Visual Basic")
            Assert.Equal(globalNS.Language, "Visual Basic")
        End Sub

        <Fact>
        Public Sub TypeParameterSymbolMethod_IsReferenceOrValueType()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As New C1(Of String, Integer)
        x.K(Of Single)(1.1)
        Dim x3 as New Derived
        x3.MySub(Of s1)(New s1)
    End Sub
End Module

Class C1(Of TTT, UUU)
    Public Sub K(Of VVV)(a As VVV)
    End Sub
End Class

Class C1(Of TTT)
End Class

Class C2R(of AAA as Class)
End Class

Class C2S(of BBB as Structure)
End Class

Structure S1
    Dim x as integer
End Structure

Class C3R(of CCC as RefTypConstraint)
End Class

Class RefTypConstraint
End Class

Class BaseGeneric(Of {EEE, S1})
        Public Overridable Sub MySub(Of T As EEE)(param As T)
        End Sub
End Class

Class Derived
        Inherits BaseGeneric(Of S1)
        Public Overrides Sub MySub(Of FFF As S1)(param As FFF)
        End Sub
End Class
    ]]></file>
</compilation>)

            Dim treeA As SyntaxTree = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)
            Dim syntax As TypeParameterSyntax = Nothing

            Dim tpSymbol1 = GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "TTT", syntax)
            Assert.NotEqual(tpSymbol1.TypeParameterKind, TypeParameterKind.Method)
            Assert.Null(tpSymbol1.DeclaringMethod)
            Assert.False(tpSymbol1.IsReferenceType)
            Assert.False(tpSymbol1.IsValueType)
            Assert.Equal("C1(Of TTT, UUU)", VisualBasic.SymbolDisplay.ToDisplayString(tpSymbol1.DeclaringType, Nothing))
            tpSymbol1 = Me.GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "VVV", syntax)
            Assert.Null(tpSymbol1.DeclaringType)
            Assert.Equal(tpSymbol1.TypeParameterKind, TypeParameterKind.Method)
            Assert.Equal("Public Sub K(Of VVV)(a As VVV)", VisualBasic.SymbolDisplay.ToDisplayString(tpSymbol1.DeclaringMethod, Nothing))
            Assert.False(tpSymbol1.IsReferenceType)
            Assert.False(tpSymbol1.IsValueType)
            tpSymbol1 = Me.GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "AAA", syntax)
            Assert.True(tpSymbol1.IsReferenceType)
            Assert.False(tpSymbol1.IsValueType)
            tpSymbol1 = Me.GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "BBB", syntax)
            Assert.False(tpSymbol1.IsReferenceType)
            Assert.True(tpSymbol1.IsValueType)
            tpSymbol1 = Me.GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "CCC", syntax)
            Assert.True(tpSymbol1.IsReferenceType)
            Assert.False(tpSymbol1.IsValueType)
            tpSymbol1 = Me.GetTypeParameterSymbol(compilation, bindingsA, "a.vb", "FFF", syntax)
            Assert.False(tpSymbol1.IsReferenceType)
            Assert.True(tpSymbol1.IsValueType)
        End Sub

        <WorkItem(546520, "DevDiv")>
        <Fact()>
        Public Sub ContainingSymbolOfBinderMayNotFindMembers()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
Module DelegateModule

    Namespace My

        Public Class A

            Public writeonly Property FFooX() As object
                Set(ByVal value As Type)
                End Set
            End Property

            Public Custom Event Click As Object
                AddHandler(ByVal value As EventHandler)
                    EventHandlerList.Add(value)
                End AddHandler 
            End Event 

        end class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim node = DirectCast(FindNodeFromText(tree, "Type"), IdentifierNameSyntax)
            Dim typeInfo = compilation.GetSemanticModel(tree).GetTypeInfo(node)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("Type", typeInfo.Type.Name)

            node = DirectCast(FindNodeFromText(tree, "EventHandler"), IdentifierNameSyntax)
            typeInfo = compilation.GetSemanticModel(tree).GetTypeInfo(node)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("EventHandler", typeInfo.Type.Name)
        End Sub

        <Fact>
        Public Sub GetDeclaredSymbolForInvalidAccessorsDoesNotThrown()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation>
        <file name="a.vb">
Namespace N
    ' invalid code.
    AddHandler()
End Namespace
    </file>
    </compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim node = FindNodeFromText(tree, "AddHandler")

            Dim symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(node)
            Assert.Null(symbol)
        End Sub

        <Fact, WorkItem(531304, "DevDiv")>
        Public Sub GetPreprocessingSymbolInfoForIdentifierInIfDirective()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = 0
#If CCCONST = 0 Then'BIND:"CCCONST"
#End If
    ]]></file>
</compilation>)

            Dim symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("ccConst", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.True(symbolInfo.IsDefined, "must have a constant value")
            Assert.Equal(0, symbolInfo.ConstantValue)

            Assert.True(symbolInfo.Symbol.Equals(symbolInfo.Symbol))
            Assert.False(symbolInfo.Symbol.Equals(Nothing))
            Dim symbolInfo2 = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.NotSame(symbolInfo.Symbol, symbolInfo2.Symbol)
            Assert.Equal(symbolInfo.Symbol, symbolInfo2.Symbol)
            Assert.Equal(symbolInfo.Symbol.GetHashCode(), symbolInfo2.Symbol.GetHashCode())
        End Sub

        <Fact, WorkItem(531304, "DevDiv")>
        Public Sub GetPreprocessingSymbolInfoForIdentifierInElseIfDirective()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = 0
#If CCCONST = 0 Then
#ElseIf CCCONST = 1 Then'BIND:"CCCONST"
#End If
    ]]></file>
</compilation>)

            Dim symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("ccConst", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.True(symbolInfo.IsDefined, "must have a constant value")
            Assert.Equal(0, symbolInfo.ConstantValue)
        End Sub

        <Fact, WorkItem(531304, "DevDiv")>
        Public Sub GetPreprocessingSymbolInfoForIdentifierInConstDirective()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = "SomeValue"
#Const ccConst2 = CCCONST + "Suffix"'BIND:"CCCONST"
    ]]></file>
</compilation>)

            Dim symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("ccConst", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.True(symbolInfo.IsDefined, "must have a constant value")
            Assert.Equal("SomeValue", symbolInfo.ConstantValue)
        End Sub

        <Fact, WorkItem(531304, "DevDiv")>
        Public Sub GetPreprocessingSymbolInfoForIdentifierInBinaryExpression()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = "SomeValue"
#Const ccConst2 = CCCONST + "Suffix"

' Binary expression
#If CCconST = CCCONST2 Then'BIND:"CCCONST2"
#End If
    ]]></file>
</compilation>)

            Dim symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("ccConst2", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.True(symbolInfo.IsDefined, "must have a constant value")
            Assert.Equal("SomeValueSuffix", symbolInfo.ConstantValue)
        End Sub

        <Fact, WorkItem(531304, "DevDiv")>
        Public Sub GetPreprocessingSymbolInfoForIdentifierUsedBeforeDefinition()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = "SomeValue"

' Undefined constant ccConst2 (it's defined later).
#if CCCONST2 = ccCONSt + "Suffix" then'BIND:"CCCONST2"
#end if

#Const ccConst2 = CCCONST + "Suffix"
    ]]></file>
</compilation>)

            Dim symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("CCCONST2", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.False(symbolInfo.IsDefined, "must not have a constant value before definition")
        End Sub

        <Fact, WorkItem(531304, "DevDiv")>
        Public Sub GetPreprocessingSymbolInfoForIdentifierWithMultipleDefinitions()
            ' Multiple definitions for ccConst, last definition wins.
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = "OldValue"
#Const ccConst = "NewValue"

#If CCCONST = "NewValue" Then'BIND:"CCCONST"
#End If
    ]]></file>
</compilation>)

            Dim symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("ccConst", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.True(symbolInfo.IsDefined, "must have a constant value")
            Assert.Equal("NewValue", symbolInfo.ConstantValue)

            ' New definition later in the source file, old definition wins.
            compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = "OldValue"

#If CCCONST = "NewValue" Then'BIND:"CCCONST"
#End If

#Const ccConst = "NewValue"
    ]]></file>
</compilation>)

            symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("ccConst", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.True(symbolInfo.IsDefined, "must have a constant value")
            Assert.Equal("OldValue", symbolInfo.ConstantValue)

            ' New definition with different value type.
            compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const ccConst = "OldValue"
#Const ccConst = 1

#If CCCONST = "NewValue" Then'BIND:"CCCONST"
#End If

    ]]></file>
</compilation>)

            symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Equal("ccConst", symbolInfo.Symbol.Name, StringComparer.Ordinal)
            Assert.True(symbolInfo.IsDefined, "must have a constant value")
            Assert.Equal(1, symbolInfo.ConstantValue)
        End Sub

        <Fact, WorkItem(531304, "DevDiv")>
        Public Sub GetPreprocessingSymbolInfoForIdentifierNotInPreprocessingDirective()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
#Const A = "SomeValue"

Class C
    Function Foo(A as Integer) As Integer
        Return A 'BIND:"A"
    End Function
End Class
    ]]></file>
</compilation>)

            Dim symbolInfo = CompilationUtils.GetPreprocessingSymbolInfo(compilation, "a.vb")
            Assert.Null(symbolInfo.Symbol)
        End Sub

        <WorkItem(531536, "DevDiv")>
        <Fact()>
        Public Sub Bug18263()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
ReadOnly Property SharedFolderBrowseButton() As Button
        ReadOnly Property NE2000ComboBox() As ComboBox
#Region "Enums for RadioButton groups"
        '''-----------------------------------------------------------------------------
            orientation270 = 3
            Public Const Pixels As String = ";pixels;Win32DialogItemString;DeviceEmulatorUI.dll;1112;11215"  'pixels
            Public Const EnableBattery As String = ";&Battery:;Win32DialogItemString;DeviceEmulatorUI.dll;1114;1039"
            Public Const FlashMemoryFileTextBox As Integer = &H2B61
            Public Const SkinFileTextBox As Integer = &H2BC3

        Protected m_cachedFunckeyTextBox As TextBox

        ''' <history>
            searchP.ClassName = WindowClassNames.Dialog
            Catch ex As Exceptions.WindowNotFoundException
        ''' <value>An interface that groups all of the dialog's control properties together</value>
                Return Controls.SpecifyROMImageAddressCheckBox.Checked
            End Set
        ''' <summary>
        '''	[cbrochu] 5/11/2004 Created
                Return Controls.NE2000ComboBox.Text
            End Set
        ''' Routine to set/get the text in control FlashMemoryFile
        ''' <history>
        Public Overridable Property SelectedSkinOrVideoRadio() As SkinVideoSelection
        ''' <summary>
        '''	[cbrochu] 5/11/2004 Created
            Get
                Controls.EnableTooltipsCheckBox.Checked = Value 'BIND:"Controls"

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")
        End Sub

        <WorkItem(531549, "DevDiv")>
        <Fact()>
        Public Sub Bug531549()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Module Program
    Sub Main(args As String())
        Dim y As Long? = x 'BIND1:"x"

        Dim x As Integer = 2

        Dim z As Long? = x 'BIND2:"x"
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node2 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 2)
            Dim info2 = semanticModel.GetTypeInfo(node2)

            Assert.NotNull(info2)
            Assert.Equal("System.Int32", info2.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Int64)", info2.ConvertedType.ToTestDisplayString())

            Dim conv2 = semanticModel.GetConversion(node2)
            Assert.Equal(ConversionKind.WideningNullable, conv2.Kind)

            Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)
            Dim info1 = semanticModel.GetTypeInfo(node1)

            Assert.NotNull(info1)
            Assert.Equal("System.Int32", info1.Type.ToTestDisplayString())
            Assert.Equal("System.Nullable(Of System.Int64)", info1.ConvertedType.ToTestDisplayString())

            Dim conv1 = semanticModel.GetConversion(node1)
            Assert.Equal(ConversionKind.WideningNullable, conv1.Kind)
        End Sub

        <WorkItem(633340, "DevDiv")>
        <Fact>
        Public Sub MemberOfInaccessibleType()
            Dim text =
                <compilation>
                    <file name="a.vb">
Public Class A
    Private Class Nested
        Public Class Another
        End Class
    End Class
End Class

Public Class B
    Inherits A

    Public a as Nested.Another
End Class
                    </file>
                </compilation>

            Dim compilation = CreateCompilationWithMscorlib(text)

            Dim [global] = compilation.GlobalNamespace
            Dim classA = [global].GetMember(Of NamedTypeSymbol)("A")
            Dim classNested = classA.GetMember(Of NamedTypeSymbol)("Nested")
            Dim classAnother = classNested.GetMember(Of NamedTypeSymbol)("Another")

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim asClauseSyntax = tree.GetRoot().DescendantNodes().OfType(Of SimpleAsClauseSyntax)().Single()

            Dim qualifiedSyntax = DirectCast(asClauseSyntax.Type, QualifiedNameSyntax)
            Dim leftSyntax = qualifiedSyntax.Left
            Dim rightSyntax = qualifiedSyntax.Right

            Dim leftInfo = model.GetSymbolInfo(leftSyntax)
            Assert.Equal(CandidateReason.Inaccessible, leftInfo.CandidateReason)
            Assert.Equal(classNested, leftInfo.CandidateSymbols.Single())

            Dim rightInfo = model.GetSymbolInfo(rightSyntax)
            Assert.Equal(CandidateReason.Inaccessible, rightInfo.CandidateReason)
            Assert.Equal(classAnother, rightInfo.CandidateSymbols.Single())

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InaccessibleSymbol2, "Nested").WithArguments("A.Nested", "Private"))
        End Sub

        <WorkItem(633340, "DevDiv")>
        <Fact>
        Public Sub NotReferencableMemberOfInaccessibleType()
            Dim text =
                <compilation>
                    <file name="a.vb">
Public Class A
    Private Class Nested
        Public Property P As Integer
    End Class
End Class

Public Class B
    Inherits A

    Function Test(nested as Nested) as Integer
        Return nested.get_P()
    End Function
End Class
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlib(text)

            Dim [global] = compilation.GlobalNamespace
            Dim classA = [global].GetMember(Of NamedTypeSymbol)("A")
            Dim classNested = classA.GetMember(Of NamedTypeSymbol)("Nested")
            Dim propertyP = classNested.GetMember(Of PropertySymbol)("P")

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim memberAccessSyntax = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()

            ' No different from a NotReferencable member of an accessible type.
            Dim info = model.GetSymbolInfo(memberAccessSyntax)
            Assert.Null(info.Symbol)
            Assert.Equal(CandidateReason.None, info.CandidateReason)
            Assert.Equal(0, info.CandidateSymbols.Length)

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InaccessibleSymbol2, "Nested").WithArguments("A.Nested", "Private"))
        End Sub

        <WorkItem(633340, "DevDiv")>
        <Fact>
        Public Sub AccessibleMethodOfInaccessibleType()
            Dim text =
                <compilation>
                    <file name="a.vb">
Public Class A
    Private Class Nested
    End Class
End Class

Public Class B
    Inherits A

    Sub Test()
        Nested.ReferenceEquals(Nothing, Nothing) ' Actually object.ReferenceEquals.
    End Sub
End Class
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlib(text)

            Dim [global] = compilation.GlobalNamespace
            Dim classA = [global].GetMember(Of NamedTypeSymbol)("A")
            Dim classNested = classA.GetMember(Of NamedTypeSymbol)("Nested")

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim callSyntax = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Dim methodAccessSyntax = DirectCast(callSyntax.Expression, MemberAccessExpressionSyntax)
            Dim nestedTypeAccessSyntax = methodAccessSyntax.Expression

            Dim typeInfo = model.GetSymbolInfo(nestedTypeAccessSyntax)
            Assert.Equal(CandidateReason.Inaccessible, typeInfo.CandidateReason)
            Assert.Equal(classNested, typeInfo.CandidateSymbols.Single())

            Dim methodInfo = model.GetSymbolInfo(callSyntax)
            Assert.Equal(compilation.GetSpecialTypeMember(SpecialMember.System_Object__ReferenceEquals), methodInfo.Symbol)

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InaccessibleSymbol2, "Nested").WithArguments("A.Nested", "Private"))
        End Sub

        <WorkItem(761212, "DevDiv")>
        <Fact>
        Public Sub AccessiblePropertyOfInaccessibleType()
            Dim text =
                <compilation>
                    <file name="a.vb">
Class A
    Friend Shared ReadOnly Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Private Class B
        Inherits A
    End Class
End Class
Class C
    Private F As Object = A.B.P(Nothing)
End Class
                    </file>
                </compilation>
            Dim compilation = CreateCompilationWithMscorlib(text)

            Dim [global] = compilation.GlobalNamespace
            Dim classA = [global].GetMember(Of NamedTypeSymbol)("A")
            Dim classB = classA.GetMember(Of NamedTypeSymbol)("B")

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim callSyntax = tree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Dim methodAccessSyntax = DirectCast(callSyntax.Expression, MemberAccessExpressionSyntax)
            Dim nestedTypeAccessSyntax = methodAccessSyntax.Expression

            Dim typeInfo = model.GetSymbolInfo(nestedTypeAccessSyntax)
            Assert.Equal(CandidateReason.Inaccessible, typeInfo.CandidateReason)
            Assert.Equal(classB, typeInfo.CandidateSymbols.Single())

            Dim propertyInfo = model.GetSymbolInfo(callSyntax)
            Assert.Equal(classA.GetMember(Of PropertySymbol)("P"), propertyInfo.Symbol)

            compilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_InaccessibleSymbol2, "A.B").WithArguments("A.B", "Private"))
        End Sub

        <Fact, WorkItem(652039, "DevDiv")>
        Public Sub Bug652039()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
    <compilation name="BindingEnumMembers">
        <file name="a.vb"><![CDATA[
{Interface
End Namespace End Class

    Interface IA

    End  IA

    End Class

    Class B

   d Class

    Class A
        Implementsessage &= " -4- "
        End Sub
    En        Sub Method4(Of T)()
            M    Return "xxx"
        End Function


            Message &= " -3- "
        ub

        Function Method3() As String         Message &= " -2- "
        End S As TMethod)

        Sub Method2()
   egate Sub ArgsTMethod(Of TMethod)(ByVal a1rgsTClass(ByVal a1 As TClass)
        Dels MyObj(Of TClass)
        Delegate Sub AArgsInt(ByVal a1 As Integer)


    Clas ArgsIA(ByVal a1 As IA)
    Delegate Sub  a1 As A, ByVal a2 As B)
    Delegate Subal a1 As A)
    Delegate Sub ArgsAB(ByValate Sub Args()
    Delegate Sub ArgsA(ByV    End Sub

    End Module

    Delegd1()
            Message &= " -1- "
     Try
        End Sub

        Sub Metho              apEndTest()
            Endy
                ' Exit test routine
           apErrorTrap()
            Finall       ' Handle unexpected errors
       3-  -4- ")

            Catch
                   apCompare(Message, " -1-  -2-  -ng)
                d7_4(New B())
      ethod(Of B) = AddressOf mo.Method4(Of Stri            Dim d7_4 As MyObj(Of A).ArgsTMthod3
                d7_3(New B())
    (Of A).ArgsTMethod(Of B) = AddressOf mo.Meew B())
                Dim d7_3 As MyObjdressOf mo.Method2
                d7_2(Nd7_2 As MyObj(Of A).ArgsTMethod(Of B) = Ad       d7_1(New B())
                Dim ethod(Of B) = AddressOf Method1
                     Dim d7_1 As MyObj(Of A).ArgsTM> ()")
                Message = ""
       apInitScenario("7. generic T_Method B ----------------------------

}]]></file>
    </compilation>, {SystemCoreRef, SystemRef, SystemDataRef}, TestOptions.ReleaseDll.WithOptionExplicit(False).WithOptionInfer(True))

            compilation.GetDiagnostics()

            Dim tree = compilation.SyntaxTrees(0)
            Dim node = FindNodeFromText(tree, "Nd7_2")

            Dim symbol = compilation.GetSemanticModel(tree).GetTypeInfo(DirectCast(node, ExpressionSyntax))
        End Sub

        <Fact, WorkItem(665920, "DevDiv")>
        Public Sub ObjectCreation1()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections

Namespace Test
    Class C
        Implements IEnumerable

        Property P1 As Integer

        Sub Add(x As Integer)
        End Sub

        Public Shared Sub Main()
            Dim x1 = New C() 'BIND1:"New C()"
            Dim x2 = New C() With {.P1 = 1} 'BIND2:"New C() With {.P1 = 1}"
            Dim x3 = New C() From {1, 2} 'BIND3:"New C() From {1, 2}"
        End Sub

        Public Shared Sub Main2()
            Dim x1 = New Test.C() 'BIND4:"New Test.C()"
            Dim x2 = New Test.C() With {.P1 = 1} 'BIND5:"New Test.C() With {.P1 = 1}"
            Dim x3 = New Test.C() From {1, 2} 'BIND6:"New Test.C() From {1, 2}"
        End Sub

        Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Return Nothing
        End Function
    End Class
End Namespace
    ]]></file>
</compilation>)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            For i As Integer = 1 To 6
                Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", i)

                Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
                Assert.Equal("Test.C", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Dim memberGroup = model.GetMemberGroup(creation.Type)
                Assert.Equal(0, memberGroup.Length)

                Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
                Assert.Null(typeInfo.Type)
                Assert.Null(typeInfo.ConvertedType)

                Dim conv = model.GetConversion(creation.Type)
                Assert.True(conv.IsIdentity)

                symbolInfo = model.GetSymbolInfo(creation)
                Assert.Equal("Sub Test.C..ctor()", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                memberGroup = model.GetMemberGroup(creation)
                Assert.Equal(1, memberGroup.Length)
                Assert.Equal("Sub Test.C..ctor()", memberGroup(0).ToTestDisplayString())

                typeInfo = model.GetTypeInfo(creation)
                Assert.Equal("Test.C", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("Test.C", typeInfo.ConvertedType.ToTestDisplayString())

                conv = model.GetConversion(creation)
                Assert.True(conv.IsIdentity)
            Next
        End Sub

        <Fact, WorkItem(665920, "DevDiv")>
        Public Sub ObjectCreation2()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections

Namespace Test

    Public Class CoClassI
        Implements IEnumerable

        Property P1 As Integer

        Sub Add(x As Integer)
        End Sub

        Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Return Nothing
        End Function
    End Class

    <System.Runtime.InteropServices.CoClass(GetType(CoClassI))>
    Public Interface I
        Inherits IEnumerable

        Property P1 As Integer

        Sub Add(x As Integer)
    End Interface

    Class C

        Public Shared Sub Main()
            Dim x1 = New I() 'BIND1:"New I()"
            Dim x2 = New I() With {.P1 = 1} 'BIND2:"New I() With {.P1 = 1}"
            Dim x3 = New I() From {1, 2} 'BIND3:"New I() From {1, 2}"
        End Sub

        Public Shared Sub Main2()
            Dim x1 = New Test.I() 'BIND4:"New Test.I()"
            Dim x2 = New Test.I() With {.P1 = 1} 'BIND5:"New Test.I() With {.P1 = 1}"
            Dim x3 = New Test.I() From {1, 2} 'BIND6:"New Test.I() From {1, 2}"
        End Sub
    End Class
End Namespace
    ]]></file>
</compilation>)

            AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            For i As Integer = 1 To 6
                Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", i)

                Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
                Assert.Equal("Test.I", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Dim memberGroup = model.GetMemberGroup(creation.Type)
                Assert.Equal(0, memberGroup.Length)

                Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
                Assert.Null(typeInfo.Type)
                Assert.Null(typeInfo.ConvertedType)

                Dim conv = model.GetConversion(creation.Type)
                Assert.True(conv.IsIdentity)

                symbolInfo = model.GetSymbolInfo(creation)
                Assert.Equal("Sub Test.CoClassI..ctor()", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                memberGroup = model.GetMemberGroup(creation)
                Assert.Equal(1, memberGroup.Length)
                Assert.Equal("Sub Test.CoClassI..ctor()", memberGroup(0).ToTestDisplayString())

                typeInfo = model.GetTypeInfo(creation)
                Assert.Equal("Test.I", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("Test.I", typeInfo.ConvertedType.ToTestDisplayString())
                Assert.True(conv.IsIdentity)
            Next
        End Sub

        <Fact, WorkItem(665920, "DevDiv")>
        Public Sub ObjectCreation3()
            Dim pia = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
imports System
Imports System.Collections
imports System.Runtime.InteropServices
imports System.Runtime.CompilerServices

<assembly: ImportedFromTypeLib("GeneralPIA.dll")>
<assembly: Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58257")>

Namespace Test

    <Guid("f9c2d51d-4f44-45f0-9eda-c9d599b5827A")>
    Public Class CoClassI
        Implements IEnumerable

        Property P1 As Integer

        Sub Add(x As Integer)
        End Sub

        Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
            Return Nothing
        End Function
    End Class

    <ComImport()>
    <Guid("f9c2d51d-4f44-45f0-9eda-c9d599b58279")>
    <System.Runtime.InteropServices.CoClass(GetType(CoClassI))>
    Public Interface I
        Inherits IEnumerable

        Property P1 As Integer

        Sub Add(x As Integer)
    End Interface
End Namespace
    ]]></file>
</compilation>, TestOptions.ReleaseDll)

            AssertTheseDiagnostics(pia, <expected></expected>)

            Dim compilation = CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Namespace Test
    Class C

        Public Shared Sub Main()
            Dim x1 = New I() 'BIND1:"New I()"
            Dim x2 = New I() With {.P1 = 1} 'BIND2:"New I() With {.P1 = 1}"
            Dim x3 = New I() From {1, 2} 'BIND3:"New I() From {1, 2}"
        End Sub

        Public Shared Sub Main2()
            Dim x1 = New Test.I() 'BIND4:"New Test.I()"
            Dim x2 = New Test.I() With {.P1 = 1} 'BIND5:"New Test.I() With {.P1 = 1}"
            Dim x3 = New Test.I() From {1, 2} 'BIND6:"New Test.I() From {1, 2}"
        End Sub
    End Class
End Namespace
    ]]></file>
</compilation>, {New VisualBasicCompilationReference(pia, embedInteropTypes:=True)})

            AssertTheseDiagnostics(compilation, <expected></expected>)

            Dim model = GetSemanticModel(compilation, "a.vb")

            For i As Integer = 1 To 6
                Dim creation As ObjectCreationExpressionSyntax = CompilationUtils.FindBindingText(Of ObjectCreationExpressionSyntax)(compilation, "a.vb", i)

                Dim symbolInfo As SymbolInfo = model.GetSymbolInfo(creation.Type)
                Assert.Equal("Test.I", symbolInfo.Symbol.ToTestDisplayString())
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

                Dim memberGroup = model.GetMemberGroup(creation.Type)
                Assert.Equal(0, memberGroup.Length)

                Dim typeInfo As TypeInfo = model.GetTypeInfo(creation.Type)
                Assert.Null(typeInfo.Type)
                Assert.Null(typeInfo.ConvertedType)

                Dim conv = model.GetConversion(creation.Type)
                Assert.True(conv.IsIdentity)

                symbolInfo = model.GetSymbolInfo(creation)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason)

                memberGroup = model.GetMemberGroup(creation)
                Assert.Equal(0, memberGroup.Length)

                typeInfo = model.GetTypeInfo(creation)
                Assert.Equal("Test.I", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("Test.I", typeInfo.ConvertedType.ToTestDisplayString())

                conv = model.GetConversion(creation)
                Assert.True(conv.IsIdentity)
            Next
        End Sub

        <WorkItem(530931, "DevDiv")>
        <Fact()>
        Public Sub SemanticModelLateBoundInvocation()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Option Infer On

Imports System

Module M
    Sub Main()
        Try
            Dim x = 1
            Foo(CObj(x)).GetHashCode() 'BIND:"Foo(CObj(x))"
        Catch
            Console.WriteLine("Catch")
        End Try
    End Sub
    Sub Foo(Of T, S)(x As Func(Of T))
    End Sub
End Module
    ]]></file>
</compilation>, {SystemCoreRef})

            CompileAndVerify(compilation)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Object", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(CandidateReason.LateBound, semanticSummary.CandidateReason)

            Assert.NotNull(semanticSummary.Symbol)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Equal("Sub M.Foo(Of T, S)(x As System.Func(Of T))", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact, WorkItem(709331, "DevDiv")>
        Public Sub ClassifyConversionFromLambdaToExplicitDirectCastType()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call DirectCast((Sub(y) Call New X().Foo(y)), Action(Of Object))("HI")'BIND:"Sub(y) Call New X().Foo(y)"
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim lambdaExpr = CompilationUtils.FindBindingText(Of LambdaExpressionSyntax)(compilation, "a.vb")
            Dim parenthesizedExpr = DirectCast(lambdaExpr.Parent, ParenthesizedExpressionSyntax)
            Dim directCastExpr = DirectCast(parenthesizedExpr.Parent, DirectCastExpressionSyntax)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim directCastType = DirectCast(model.GetTypeInfo(directCastExpr.Type).Type, TypeSymbol)
            Assert.Equal("System.Action(Of System.Object)", directCastType.ToTestDisplayString())
            Dim lambdaExprToDirectCastType As Conversion = model.ClassifyConversion(lambdaExpr, directCastType)
            Assert.Equal(ConversionKind.Widening Or ConversionKind.Lambda, lambdaExprToDirectCastType.Kind)
        End Sub

        <Fact, WorkItem(709331, "DevDiv")>
        Public Sub ClassifyConversionFromLambdaToExplicitTryCastType()
            Dim compilation = CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Program
    Public Sub Main()
        Call TryCast((Sub(y) Call New X().Foo(y)), Action(Of Object))("HI")'BIND:"Sub(y) Call New X().Foo(y)"
    End Sub

End Module

Public Class X
    Public Sub Foo(x As Object)
        Console.WriteLine(1)
    End Sub

    Public Sub Foo(x As String)
        Console.WriteLine(2)
    End Sub
End Class
    ]]></file>
</compilation>)

            Dim lambdaExpr = CompilationUtils.FindBindingText(Of LambdaExpressionSyntax)(compilation, "a.vb")
            Dim parenthesizedExpr = DirectCast(lambdaExpr.Parent, ParenthesizedExpressionSyntax)
            Dim tryCastExpr = DirectCast(parenthesizedExpr.Parent, TryCastExpressionSyntax)

            Dim model = GetSemanticModel(compilation, "a.vb")
            Dim tryCastType = DirectCast(model.GetTypeInfo(tryCastExpr.Type).Type, TypeSymbol)
            Assert.Equal("System.Action(Of System.Object)", tryCastType.ToTestDisplayString())
            Dim lambdaExprToDirectCastType As Conversion = model.ClassifyConversion(lambdaExpr, tryCastType)
            Assert.Equal(ConversionKind.Widening Or ConversionKind.Lambda, lambdaExprToDirectCastType.Kind)
        End Sub

        <WorkItem(849371, "DevDiv")>
        <WorkItem(854543, "DevDiv")>
        <WorkItem(854548, "DevDiv")>
        <Fact> ' If this starts failing when 854543 is fixed, the new skip reason is 854548.
        Public Sub SemanticModelLambdaErrorRecovery()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System

Class Program
    Shared Sub Main()
        M(Function() 1) ' Neither overload wins.
    End Sub

    Shared Sub M(p As Func(Of String))
    End Sub

    Shared Sub M(p As Func(Of Char))
    End Sub
End Class
]]>
                             </file>
                         </compilation>

            If True Then
                Dim comp = CreateCompilationWithMscorlib(source)

                Dim tree = comp.SyntaxTrees.Single()
                Dim model = comp.GetSemanticModel(tree)

                Dim lambdaSyntax = tree.GetRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()

                Dim otherFuncType = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(comp.GetSpecialType(SpecialType.System_Int32))

                Dim typeInfo = model.GetTypeInfo(lambdaSyntax)
                Assert.Null(typeInfo.Type)
                Assert.NotEqual(otherFuncType, typeInfo.ConvertedType)
            End If

            If True Then
                Dim comp = CreateCompilationWithMscorlib(source)

                Dim tree = comp.SyntaxTrees.Single()
                Dim model = comp.GetSemanticModel(tree)

                Dim lambdaSyntax = tree.GetRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()

                Dim otherFuncType = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(comp.GetSpecialType(SpecialType.System_Int32))
                Dim conversion = model.ClassifyConversion(lambdaSyntax, otherFuncType)

                Dim typeInfo = model.GetTypeInfo(lambdaSyntax)
                Assert.Null(typeInfo.Type)
                Assert.NotEqual(otherFuncType, typeInfo.ConvertedType) ' Not affected by call to ClassifyConversion.
            End If
        End Sub

        <WorkItem(854543, "DevDiv")>
        <Fact>
        Public Sub ClassifyConversionOnNothingLiteral()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Class Program
    Shared Sub Main()
        M(Nothing) ' Ambiguous.
    End Sub

    Shared Sub M(a As A)
    End Sub

    Shared Sub M(b As B)
    End Sub
End Class

Class A
End Class

Class B
End Class

Class C
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(a As A)': Not most specific.
    'Public Shared Sub M(b As B)': Not most specific.
        M(Nothing) ' Ambiguous.
        ~
]]></errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim nullSyntax = tree.GetRoot().DescendantNodes().OfType(Of LiteralExpressionSyntax)().Single()

            Dim typeC = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")

            Dim conversion = model.ClassifyConversion(nullSyntax, typeC)
            Assert.Equal(ConversionKind.WideningNothingLiteral, conversion.Kind)
        End Sub

        <WorkItem(854543, "DevDiv")>
        <Fact>
        Public Sub ClassifyConversionOnLambda()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System

Class Program
    Shared Sub Main()
        M(Function() Nothing)
    End Sub

    Shared Sub M(f As Func(Of A))
    End Sub
End Class

Class A
End Class

Class B
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertNoDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim lambdaSyntax = tree.GetRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()

            Dim typeB = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("B")
            Dim typeFuncB = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(typeB)

            Dim conversion = model.ClassifyConversion(lambdaSyntax, typeFuncB)
            Assert.Equal(ConversionKind.Widening Or ConversionKind.Lambda, conversion.Kind)
        End Sub

        <WorkItem(854543, "DevDiv")>
        <Fact>
        Public Sub ClassifyConversionOnAmbiguousLambda()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System

Class Program
    Shared Sub Main()
        M(Function() Nothing) ' Ambiguous.
    End Sub

    Shared Sub M(a As Func(Of A))
    End Sub

    Shared Sub M(b As Func(Of B))
    End Sub
End Class

Class A
End Class

Class B
End Class

Class C
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30521: Overload resolution failed because no accessible 'M' is most specific for these arguments:
    'Public Shared Sub M(a As Func(Of A))': Not most specific.
    'Public Shared Sub M(b As Func(Of B))': Not most specific.
        M(Function() Nothing) ' Ambiguous.
        ~
]]></errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim lambdaSyntax = tree.GetRoot().DescendantNodes().OfType(Of LambdaExpressionSyntax)().Single()

            Dim typeC = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim typeFuncC = comp.GetWellKnownType(WellKnownType.System_Func_T).Construct(typeC)

            Dim conversion = model.ClassifyConversion(lambdaSyntax, typeFuncC)
            Assert.Equal(ConversionKind.Widening Or ConversionKind.Lambda, conversion.Kind)
        End Sub

        <WorkItem(854543, "DevDiv")>
        <Fact>
        Public Sub ClassifyConversionOnAmbiguousMethodGroup()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Imports System

Class Base(Of T)
    Public Function N(t As T) As A
        Return Nothing
    End Function
    Public Function N(t As Integer) As B
        Return Nothing
    End Function
End Class

Class Derived : Inherits Base(Of Integer)
    Sub Test()
        M(N) ' Ambiguous.
    End Sub

    Shared Sub M(f As Func(Of Integer, A))
    End Sub

    Shared Sub M(f As Func(Of Integer, B))
    End Sub
End Class

Class A
End Class

Class B
End Class

Class C
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlib(source)
            comp.AssertTheseDiagnostics(<errors><![CDATA[
BC30516: Overload resolution failed because no accessible 'N' accepts this number of arguments.
        M(N) ' Ambiguous.
          ~
]]></errors>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim methodGroupSyntax = tree.GetRoot().DescendantNodes().OfType(Of SimpleArgumentSyntax)().Single().Expression

            Dim [global] = comp.GlobalNamespace
            Dim typeA = [global].GetMember(Of NamedTypeSymbol)("A")
            Dim typeB = [global].GetMember(Of NamedTypeSymbol)("B")
            Dim typeC = [global].GetMember(Of NamedTypeSymbol)("C")

            Dim typeInt = comp.GetSpecialType(SpecialType.System_Int32)
            Dim typeFunc = comp.GetWellKnownType(WellKnownType.System_Func_T2)
            Dim typeFuncA = typeFunc.Construct(typeInt, typeA)
            Dim typeFuncB = typeFunc.Construct(typeInt, typeB)
            Dim typeFuncC = typeFunc.Construct(typeInt, typeC)

            Dim conversionA = model.ClassifyConversion(methodGroupSyntax, typeFuncA)
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, conversionA.Kind)

            Dim conversionB = model.ClassifyConversion(methodGroupSyntax, typeFuncB)
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, conversionB.Kind)

            Dim conversionC = model.ClassifyConversion(methodGroupSyntax, typeFuncC)
            Assert.Equal(ConversionKind.DelegateRelaxationLevelNone, conversionC.Kind)
        End Sub

        <Fact, WorkItem(1068547, "DevDiv")>
        Public Sub Bug1068547_01()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
Module Program
    <System.Diagnostics.DebuggerDisplay(Me)>
    Sub Main(args As String())

    End Sub
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)

            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.MeExpression).Cast(Of MeExpressionSyntax)().Single()

            Dim symbolInfo = model.GetSymbolInfo(node)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.NotReferencable, symbolInfo.CandidateReason)
        End Sub

        <Fact, WorkItem(1068547, "DevDiv")>
        Public Sub Bug1068547_02()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
    <System.Diagnostics.DebuggerDisplay(Me)>
]]>
                             </file>
                         </compilation>

            Dim comp = CreateCompilationWithMscorlibAndVBRuntime(source)

            Dim tree = comp.SyntaxTrees(0)
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().Where(Function(n) n.Kind() = SyntaxKind.MeExpression).Cast(Of MeExpressionSyntax)().Single()

            Dim symbolInfo = model.GetSymbolInfo(node)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.NotReferencable, symbolInfo.CandidateReason)
        End Sub

        <WorkItem(1108036, "DevDiv")>
        <Fact()>
        Public Sub Bug1108036()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb">
Class Color
    Public Shared Sub Cat()
    End Sub
End Class

Class Program
    Shared Sub Main()
        Color.Cat()
    End Sub
 
    ReadOnly Property Color(Optional x As Integer = 0) As Integer
        Get
            Return 0
        End Get
    End Property
 
    ReadOnly Property Color(Optional x As String = "") As Color
        Get
            Return Nothing
        End Get
    End Property
End Class
    </file>
</compilation>)

            AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Color' is most specific for these arguments:
    'Public ReadOnly Property Color([x As Integer = 0]) As Integer': Not most specific.
    'Public ReadOnly Property Color([x As String = ""]) As Color': Not most specific.
        Color.Cat()
        ~~~~~
</expected>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes.OfType(Of MemberAccessExpressionSyntax)().Single().Expression
            Assert.Equal(node.ToString(), "Color")

            Dim symbolInfo = model.GetSymbolInfo(node)

            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason)

            Dim sortedCandidates = symbolInfo.CandidateSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("ReadOnly Property Program.Color([x As System.Int32 = 0]) As System.Int32", sortedCandidates(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(0).Kind)
            Assert.Equal("ReadOnly Property Program.Color([x As System.String = """"]) As Color", sortedCandidates(1).ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, sortedCandidates(1).Kind)
        End Sub
    End Class
End Namespace
