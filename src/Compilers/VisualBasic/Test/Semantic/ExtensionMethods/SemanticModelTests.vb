' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class ExtendedSemanticInfoTests : Inherits SemanticModelTestBase

        <Fact>
        Public Sub Test_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
        Dim x As New C1()
        x.F1()'BIND:"F1"
    End Sub

    <Extension()>
    Function F1(ByRef this As C1) As Integer
        Return 0
    End Function
End Module

Class C1
End Class

Namespace System.Runtime.CompilerServices

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)>
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Dim method = DirectCast(semanticInfo.Symbol, MethodSymbol)
            Assert.Equal("Function C1.F1() As System.Int32", method.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, method.Kind)
            Assert.Equal(MethodKind.ReducedExtension, method.MethodKind)
            Assert.Equal("C1", method.ReceiverType.ToTestDisplayString())
            Assert.Equal("Function Module1.F1(ByRef this As C1) As System.Int32", method.ReducedFrom.ToTestDisplayString())

            Assert.Equal(MethodKind.Ordinary, method.CallsiteReducedFromMethod.MethodKind)
            Assert.Equal("Function Module1.F1(ByRef this As C1) As System.Int32", method.CallsiteReducedFromMethod.ToTestDisplayString())

            Dim reducedMethod As MethodSymbol = method.ReducedFrom.ReduceExtensionMethod(method.ReceiverType)
            Assert.Equal("Function C1.F1() As System.Int32", reducedMethod.ToTestDisplayString())
            Assert.Equal(MethodKind.ReducedExtension, reducedMethod.MethodKind)

            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C1.F1() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub Test_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
        Dim x As New C1()
        x.F1()'BIND:"F1"
    End Sub

    <Extension()>
    Function F1(this As C1) As Integer
        Return 0
    End Function
End Module

Class C1
    Function F1(x As Integer) As Integer
        Return 0
    End Function
End Class

Namespace System.Runtime.CompilerServices

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)>
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function C1.F1() As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(2, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C1.F1() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Function C1.F1(x As System.Int32) As System.Int32", sortedMethodGroup(1).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub Test_3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
        Dim x As New C1()
        x.F1()'BIND:"x.F1()"
    End Sub

    <Extension()>
    Function F1(this As C1) As Integer
        Return 0
    End Function
End Module

Class C1
    Function F1(x As Integer) As Integer
        Return 0
    End Function
End Class

Namespace System.Runtime.CompilerServices

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)>
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    ]]></file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of InvocationExpressionSyntax)(compilation, "a.vb")

            Assert.Equal("System.Int32", semanticInfo.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.Type.TypeKind)
            Assert.Equal("System.Int32", semanticInfo.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, semanticInfo.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Function C1.F1() As System.Int32", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(0, semanticInfo.MemberGroup.Length)

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub


    End Class

End Namespace


Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class SemanticModelTests

        <Fact>
        Public Sub ExtensionMethodsLookupSymbols1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
        Dim x As New C1()
        x.F1()'BIND:"x"
    End Sub

    <Extension()>
    Function F1(this As C1) As Integer
        Return 0
    End Function
End Module

Class C1
    Function F1(x As Integer) As Integer
        Return 0
    End Function
End Class

Namespace System.Runtime.CompilerServices

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)>
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    ]]></file>
</compilation>)

            Dim c1 = compilation.GetTypeByMetadataName("C1")
            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="F1", container:=c1, includeReducedExtensionMethods:=True)

            Assert.Equal(2, actual_lookupSymbols.Count)
            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C1.F1() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Function C1.F1(x As System.Int32) As System.Int32", sortedMethodGroup(1).ToTestDisplayString())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="F1", container:=c1, includeReducedExtensionMethods:=False)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Function C1.F1(x As System.Int32) As System.Int32", actual_lookupSymbols(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub ExtensionMethodsLookupSymbols2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Imports System.Runtime.CompilerServices


Module Module1
    Sub Main()
    End Sub

    <Extension()>
    Function F1(this As C1) As Integer
        Return 0
    End Function
End Module

Class C1
    Function F1(x As Integer) As Integer
        Return 0
    End Function

    Shared Sub Main()
        F1()'BIND:"F1"
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    <AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)>
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

    ]]></file>
</compilation>)

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="F1", includeReducedExtensionMethods:=True)

            Assert.Equal(2, actual_lookupSymbols.Count)
            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Function C1.F1() As System.Int32", sortedMethodGroup(0).ToTestDisplayString())
            Assert.Equal("Function C1.F1(x As System.Int32) As System.Int32", sortedMethodGroup(1).ToTestDisplayString())
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub ExtensionMethodsLookupSymbols3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As New C1()
                x.Test1() 'BIND:"Test1"
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T1)(this As NS1.NS2.Module1.C1)
            End Sub

            Class C1
            End Class
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test1(Of T1, T2)(this As NS1.NS2.Module1.C1)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(Of T1, T2, T3, T4)(this As NS1.NS2.Module1.C1)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7, T8)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", includeReducedExtensionMethods:=True)

            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.Test1(Of T1)(this As NS1.NS2.Module1.C1)", actual_lookupSymbols(0).ToTestDisplayString())

            Dim c1 = compilation.GetTypeByMetadataName("NS1.NS2.Module1+C1")
            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=c1, includeReducedExtensionMethods:=True)

            Assert.Equal(8, actual_lookupSymbols.Count)
            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Dim expected() As String = {"Sub NS1.NS2.Module1.C1.Test1(Of T1)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            For i As Integer = 0 To Math.Max(sortedMethodGroup.Length, expected.Length) - 1
                Assert.Equal(expected(i), sortedMethodGroup(i).ToTestDisplayString())
            Next

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=c1)
            Assert.Equal(0, actual_lookupSymbols.Count)

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=c1, arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub ExtensionMethodsLookupSymbols4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T1)(this As NS1.NS2.Module1.C1)
            End Sub

            Class C1
                Sub Main()
                    Test1() 'BIND:"Test1"
                End Sub
            End Class
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test1(Of T1, T2)(this As NS1.NS2.Module1.C1)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(Of T1, T2, T3, T4)(this As NS1.NS2.Module1.C1)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7, T8)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", includeReducedExtensionMethods:=True)

            Assert.Equal(8, actual_lookupSymbols.Count)
            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Dim expected() As String = {"Sub NS1.NS2.Module1.C1.Test1(Of T1)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            For i As Integer = 0 To Math.Max(sortedMethodGroup.Length, expected.Length) - 1
                Assert.Equal(expected(i), sortedMethodGroup(i).ToTestDisplayString())
            Next

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1")
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.Test1(Of T1)(this As NS1.NS2.Module1.C1)", actual_lookupSymbols(0).ToTestDisplayString())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub ExtensionMethodsLookupSymbols5()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As C1 = Nothing
                x.Test1() 'BIND:"Test1"
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T1)(this As NS1.NS2.Module1.C1)
            End Sub

            Interface C1
            End Interface
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test1(Of T1, T2)(this As NS1.NS2.Module1.C1)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(Of T1, T2, T3, T4)(this As NS1.NS2.Module1.C1)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7, T8)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", includeReducedExtensionMethods:=True)

            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.Test1(Of T1)(this As NS1.NS2.Module1.C1)", actual_lookupSymbols(0).ToTestDisplayString())

            Dim c1 = compilation.GetTypeByMetadataName("NS1.NS2.Module1+C1")
            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=c1, includeReducedExtensionMethods:=True)

            Assert.Equal(8, actual_lookupSymbols.Count)
            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Dim expected() As String = {"Sub NS1.NS2.Module1.C1.Test1(Of T1)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            For i As Integer = 0 To Math.Max(sortedMethodGroup.Length, expected.Length) - 1
                Assert.Equal(expected(i), sortedMethodGroup(i).ToTestDisplayString())
            Next

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=c1)
            Assert.Equal(0, actual_lookupSymbols.Count)

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=c1, arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.C1.Test1(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())

        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:="https://github.com/dotnet/roslyn/issues/29531")>
        Public Sub ExtensionMethodsLookupSymbols6()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main(Of T)(x as T)
                x.Test1() 'BIND:"Test1"
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T, T1)(this As T)
            End Sub
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test1(Of T, T1, T2)(this As T)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test1(Of T, T1, T2, T3)(this As T)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test1(Of T, T1, T2, T3, T4)(this As T)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test1(Of T, T1, T2, T3, T4, T5)(this As T)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test1(Of T, T1, T2, T3, T4, T5, T6)(this As T)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test1(Of T, T1, T2, T3, T4, T5, T6, T7)(this As T)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test1(Of T, T1, T2, T3, T4, T5, T6, T7, T8)(this As T)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test1(Of T, T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As T)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", includeReducedExtensionMethods:=True)

            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.Test1(Of T, T1)(this As T)", actual_lookupSymbols(0).ToTestDisplayString())

            Dim module1 = compilation.GetTypeByMetadataName("NS1.NS2.Module1")
            Dim main = DirectCast(module1.GetMember("Main"), MethodSymbol)
            Dim t = main.TypeParameters(0)

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=t, includeReducedExtensionMethods:=True)

            Assert.Equal(8, actual_lookupSymbols.Count)
            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Dim expected() As String = {"Sub T.Test1(Of T1)()",
                                        "Sub T.Test1(Of T1, T2)()",
                                        "Sub T.Test1(Of T1, T2, T3)()",
                                        "Sub T.Test1(Of T1, T2, T3, T4)()",
                                        "Sub T.Test1(Of T1, T2, T3, T4, T5)()",
                                        "Sub T.Test1(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub T.Test1(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub T.Test1(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            For i As Integer = 0 To Math.Max(sortedMethodGroup.Length, expected.Length) - 1
                Assert.Equal(expected(i), sortedMethodGroup(i).ToTestDisplayString())
            Next

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=t)
            Assert.Equal(0, actual_lookupSymbols.Count)

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test1", container:=t, arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub T.Test1(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub ExtensionMethodsLookupSymbols7()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As New C1()
                x.Test1() 'BIND:"Test1"
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T1)(this As NS1.NS2.Module1.C1)
            End Sub

            Class C1
            End Class
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test2(Of T1, T2)(this As NS1.NS2.Module1.C1)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test3(Of T1, T2, T3)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test4(Of T1, T2, T3, T4)(this As NS1.NS2.Module1.C1)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test5(Of T1, T2, T3, T4, T5)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test6(Of T1, T2, T3, T4, T5, T6)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test7(Of T1, T2, T3, T4, T5, T6, T7)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test8(Of T1, T2, T3, T4, T5, T6, T7, T8)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test9(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim c1 = compilation.GetTypeByMetadataName("NS1.NS2.Module1+C1")
            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=c1, includeReducedExtensionMethods:=True)

            Assert.Equal(14, actual_lookupSymbols.Count)
            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Dim expected() As String = {"Function System.Object.Equals(obj As System.Object) As System.Boolean",
                                        "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
                                        "Function System.Object.GetHashCode() As System.Int32",
                                        "Function System.Object.GetType() As System.Type",
                                        "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
                                        "Function System.Object.ToString() As System.String",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1)()",
                                        "Sub NS1.NS2.Module1.C1.Test2(Of T1, T2)()",
                                        "Sub NS1.NS2.Module1.C1.Test3(Of T1, T2, T3)()",
                                        "Sub NS1.NS2.Module1.C1.Test4(Of T1, T2, T3, T4)()",
                                        "Sub NS1.NS2.Module1.C1.Test5(Of T1, T2, T3, T4, T5)()",
                                        "Sub NS1.NS2.Module1.C1.Test6(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub NS1.NS2.Module1.C1.Test7(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub NS1.NS2.Module1.C1.Test8(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            For i As Integer = 0 To Math.Max(sortedMethodGroup.Length, expected.Length) - 1
                Assert.Equal(expected(i), sortedMethodGroup(i).ToTestDisplayString())
            Next

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=c1)
            Assert.Equal(6, actual_lookupSymbols.Count)
            sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()

            For i As Integer = 0 To sortedMethodGroup.Length - 1
                Assert.Equal(expected(i), sortedMethodGroup(i).ToTestDisplayString())
            Next

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=c1, name:="Test4", arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.C1.Test4(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub ExtensionMethodsLookupSymbols8()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T1)(this As NS1.NS2.Module1.C1)
            End Sub

            Class C1
                Sub Main()
                    Test1() 'BIND:"Test1"
                End Sub
            End Class
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test2(Of T1, T2)(this As NS1.NS2.Module1.C1)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test3(Of T1, T2, T3)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test4(Of T1, T2, T3, T4)(this As NS1.NS2.Module1.C1)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test5(Of T1, T2, T3, T4, T5)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test6(Of T1, T2, T3, T4, T5, T6)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test7(Of T1, T2, T3, T4, T5, T6, T7)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test8(Of T1, T2, T3, T4, T5, T6, T7, T8)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test9(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", includeReducedExtensionMethods:=True)

            Dim sortedMethodGroup = actual_lookupSymbols.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Dim expected() As String = {"Function System.Object.Equals(obj As System.Object) As System.Boolean",
                                        "Function System.Object.Equals(objA As System.Object, objB As System.Object) As System.Boolean",
                                        "Function System.Object.GetHashCode() As System.Int32",
                                        "Function System.Object.GetType() As System.Type",
                                        "Function System.Object.ReferenceEquals(objA As System.Object, objB As System.Object) As System.Boolean",
                                        "Function System.Object.ToString() As System.String",
                                        "Sub NS1.NS2.Module1.C1.Test1(Of T1)()",
                                        "Sub NS1.NS2.Module1.C1.Test2(Of T1, T2)()",
                                        "Sub NS1.NS2.Module1.C1.Test3(Of T1, T2, T3)()",
                                        "Sub NS1.NS2.Module1.C1.Test4(Of T1, T2, T3, T4)()",
                                        "Sub NS1.NS2.Module1.C1.Test5(Of T1, T2, T3, T4, T5)()",
                                        "Sub NS1.NS2.Module1.C1.Test6(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub NS1.NS2.Module1.C1.Test7(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub NS1.NS2.Module1.C1.Test8(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            Assert.Equal(expected.Length, Aggregate name In expected Join symbol In actual_lookupSymbols
                                          On symbol.ToTestDisplayString() Equals name Select name Distinct Into Count())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb")

            Assert.Equal(0, Aggregate symbol In actual_lookupSymbols Join name In expected.Skip(6)
                                          On symbol.ToTestDisplayString() Equals name Into Count())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", name:="Test4", arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.C1.Test4(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub ExtensionMethodsLookupSymbols9()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main()
                Dim x As C1 = Nothing
                x.Test1() 'BIND:"Test1"
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T1)(this As NS1.NS2.Module1.C1)
            End Sub

            Interface C1
            End Interface
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test2(Of T1, T2)(this As NS1.NS2.Module1.C1)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test3(Of T1, T2, T3)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test4(Of T1, T2, T3, T4)(this As NS1.NS2.Module1.C1)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test5(Of T1, T2, T3, T4, T5)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test6(Of T1, T2, T3, T4, T5, T6)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test7(Of T1, T2, T3, T4, T5, T6, T7)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test8(Of T1, T2, T3, T4, T5, T6, T7, T8)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test9(Of T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As NS1.NS2.Module1.C1)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim c1 = compilation.GetTypeByMetadataName("NS1.NS2.Module1+C1")
            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=c1, includeReducedExtensionMethods:=True)

            Dim expected() As String = {"Sub NS1.NS2.Module1.C1.Test1(Of T1)()",
                                        "Sub NS1.NS2.Module1.C1.Test2(Of T1, T2)()",
                                        "Sub NS1.NS2.Module1.C1.Test3(Of T1, T2, T3)()",
                                        "Sub NS1.NS2.Module1.C1.Test4(Of T1, T2, T3, T4)()",
                                        "Sub NS1.NS2.Module1.C1.Test5(Of T1, T2, T3, T4, T5)()",
                                        "Sub NS1.NS2.Module1.C1.Test6(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub NS1.NS2.Module1.C1.Test7(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub NS1.NS2.Module1.C1.Test8(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            Assert.Equal(expected.Length, Aggregate name In expected Join symbol In actual_lookupSymbols
                                          On symbol.ToTestDisplayString() Equals name Select name Distinct Into Count())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=c1)
            Assert.Equal(0, Aggregate name In expected Join symbol In actual_lookupSymbols
                                          On symbol.ToTestDisplayString() Equals name Select name Distinct Into Count())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=c1, name:="Test4", arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub NS1.NS2.Module1.C1.Test4(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())

        End Sub

        <Fact>
        Public Sub ExtensionMethodsLookupSymbols10()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System.Console
Imports System.Runtime.CompilerServices
Imports NS3.Module5
Imports NS3

Namespace NS1
    Namespace NS2

        Module Module1

            Sub Main(Of T)(x as T)
                x.Test1() 'BIND:"Test1"
            End Sub

            &lt;Extension()&gt;
            Sub Test1(Of T, T1)(this As T)
            End Sub
        End Module

        Module Module2
            &lt;Extension()&gt;
            Sub Test2(Of T, T1, T2)(this As T)
            End Sub
        End Module
    End Namespace

    Module Module3
        &lt;Extension()&gt;
        Sub Test3(Of T, T1, T2, T3)(this As T)
        End Sub
    End Module
End Namespace

Module Module4
    &lt;Extension()&gt;
    Sub Test4(Of T, T1, T2, T3, T4)(this As T)
    End Sub
End Module

Namespace NS3
    Module Module5
        &lt;Extension()&gt;
        Sub Test5(Of T, T1, T2, T3, T4, T5)(this As T)
        End Sub
    End Module

    Module Module6
        &lt;Extension()&gt;
        Sub Test6(Of T, T1, T2, T3, T4, T5, T6)(this As T)
        End Sub
    End Module
End Namespace

Namespace NS4
    Module Module7
        &lt;Extension()&gt;
        Sub Test7(Of T, T1, T2, T3, T4, T5, T6, T7)(this As T)
        End Sub
    End Module

    Module Module8
        &lt;Extension()&gt;
        Sub Test8(Of T, T1, T2, T3, T4, T5, T6, T7, T8)(this As T)
        End Sub
    End Module
End Namespace

Namespace NS5
    Module Module9
        &lt;Extension()&gt;
        Sub Test9(Of T, T1, T2, T3, T4, T5, T6, T7, T8, T9)(this As T)
        End Sub
    End Module
End Namespace

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"NS4.Module7", "NS4"})))

            Dim module1 = compilation.GetTypeByMetadataName("NS1.NS2.Module1")
            Dim main = DirectCast(module1.GetMember("Main"), MethodSymbol)
            Dim t = main.TypeParameters(0)

            Dim actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=t, includeReducedExtensionMethods:=True)

            Dim expected() As String = {"Sub T.Test1(Of T1)()",
                                        "Sub T.Test2(Of T1, T2)()",
                                        "Sub T.Test3(Of T1, T2, T3)()",
                                        "Sub T.Test4(Of T1, T2, T3, T4)()",
                                        "Sub T.Test5(Of T1, T2, T3, T4, T5)()",
                                        "Sub T.Test6(Of T1, T2, T3, T4, T5, T6)()",
                                        "Sub T.Test7(Of T1, T2, T3, T4, T5, T6, T7)()",
                                        "Sub T.Test8(Of T1, T2, T3, T4, T5, T6, T7, T8)()"}

            Assert.Equal(expected.Length, Aggregate name In expected Join symbol In actual_lookupSymbols
                                          On symbol.ToTestDisplayString() Equals name Select name Distinct Into Count())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=t, includeReducedExtensionMethods:=False)
            Assert.Equal(0, Aggregate name In expected Join symbol In actual_lookupSymbols
                                          On symbol.ToTestDisplayString() Equals name Select name Distinct Into Count())

            actual_lookupSymbols = GetLookupSymbols(compilation, "a.vb", container:=t, name:="Test4", arity:=4, includeReducedExtensionMethods:=True)
            Assert.Equal(1, actual_lookupSymbols.Count)
            Assert.Equal("Sub T.Test4(Of T1, T2, T3, T4)()", actual_lookupSymbols(0).ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub Bug8942_1()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Module M
    &lt;Extension()&gt;
    Sub Goo(x As Exception)
    End Sub
End Module

Class E
    Inherits Exception
    Sub Bar()
        Me.Goo() 'BIND:"Goo"
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub System.Exception.Goo()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.Exception.Goo()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <Fact>
        Public Sub Bug8942_2()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports System
Imports System.Runtime.CompilerServices

Module M
    &lt;Extension()&gt;
    Sub Goo(x As Exception)
    End Sub
End Module

Class E
    Inherits Exception
    Sub Bar()
        Goo() 'BIND:"Goo"
    End Sub
End Class

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace
    </file>
</compilation>)

            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of IdentifierNameSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Type)
            Assert.Null(semanticInfo.ConvertedType)
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)

            Assert.Equal("Sub System.Exception.Goo()", semanticInfo.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)

            Assert.Equal(1, semanticInfo.MemberGroup.Length)
            Dim sortedMethodGroup = semanticInfo.MemberGroup.AsEnumerable().OrderBy(Function(s) s.ToTestDisplayString()).ToArray()
            Assert.Equal("Sub System.Exception.Goo()", sortedMethodGroup(0).ToTestDisplayString())

            Assert.False(semanticInfo.ConstantValue.HasValue)
        End Sub

        <WorkItem(544933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544933")>
        <Fact>
        Public Sub LookupSymbolsGenericExtensionMethodWithConstraints()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Class A
End Class
Class B
End Class
Module E
    Sub M(_a As A, _b As B)
        _a.F()
        _b.F()
    End Sub
    <Extension()>
    Sub F(Of T As A)(o As T)
    End Sub
End Module
]]></file>
</compilation>, {SystemCoreRef})
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30456: 'F' is not a member of 'B'.
        _b.F()
        ~~~~
]]></errors>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim position = FindNodeFromText(tree, "_a.F()").SpanStart
            Dim method = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("E").GetMember(Of MethodSymbol)("M")

            ' No type.
            Dim symbols = model.LookupSymbols(position, container:=Nothing, name:="F", includeReducedExtensionMethods:=True)
            CheckSymbols(symbols, "Sub E.F(Of T)(o As T)")

            ' Type satisfying constraints.
            symbols = model.LookupSymbols(position, container:=method.Parameters(0).Type, name:="F", includeReducedExtensionMethods:=True)
            CheckSymbols(symbols, "Sub A.F()")

            ' Type not satisfying constraints.
            symbols = model.LookupSymbols(position, container:=method.Parameters(1).Type, name:="F", includeReducedExtensionMethods:=True)
            CheckSymbols(symbols)
        End Sub

        <Fact, WorkItem(963125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/963125")>
        Public Sub Bug963125()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports alias2 = System

Module Module1
    Sub Main()
        alias1.Console.WriteLine()
        alias2.Console.WriteLine()
    End Sub

End Module

    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"alias1 = System"})))

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim node1 = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "alias1").Single()
            Dim alias1 = model.GetAliasInfo(node1)
            Assert.Equal("alias1=System", alias1.ToTestDisplayString())
            Assert.Equal(LocationKind.None, alias1.Locations.Single().Kind)

            Dim node2 = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "alias2").Single()
            Dim alias2 = model.GetAliasInfo(node2)
            Assert.Equal("alias2=System", alias2.ToTestDisplayString())
            Assert.Equal(LocationKind.SourceFile, alias2.Locations.Single().Kind)
        End Sub

    End Class

End Namespace
