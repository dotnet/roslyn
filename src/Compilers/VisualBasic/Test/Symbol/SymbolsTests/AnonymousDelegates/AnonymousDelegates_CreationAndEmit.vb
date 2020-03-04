' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.AnonymousDelegates

    Public Class CreationAndEmit : Inherits BasicTestBase

        <Fact>
        <WorkItem(1024401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024401")>
        Public Sub DebuggerDisplayAttributeWithNoTypeMember()
            Dim src = "
Module Test
    Sub Main()
        Dim x = Function(y) y + 1
    End Sub
End Module"
            Dim comp = CreateVisualBasicCompilation(src)
            ' Expect both attributes with a normal corlib
            Dim validator As Action(Of ModuleSymbol) =
                Sub(m As ModuleSymbol)
                    Dim anonDelegate = (From sym In m.GlobalNamespace.GetMembers()
                                        Where sym.Name.Contains("AnonymousDelegate")).Single()

                    Dim expected =
                    {comp.GetWellKnownType(WellKnownType.System_Diagnostics_DebuggerDisplayAttribute),
                     comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CompilerGeneratedAttribute)}

                    Dim actual = From attribute In anonDelegate.GetAttributes()
                                 Select attribute.AttributeClass
                    AssertEx.SetEqual(actual, expected)
                End Sub

            CompileAndVerify(comp, symbolValidator:=validator)

            ' Expect no DebuggerDisplay with the type missing
            comp.MakeMemberMissing(WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__Type)

            validator =
                Sub(m As ModuleSymbol)
                    Dim anonDelegate = (From sym In m.GlobalNamespace.GetMembers()
                                        Where sym.Name.Contains("AnonymousDelegate")).Single()

                    Assert.True(anonDelegate.GetAttributes().Single().IsTargetAttribute(
                                "System.Runtime.CompilerServices",
                                "CompilerGeneratedAttribute"))
                End Sub

            CompileAndVerify(comp, symbolValidator:=validator)
        End Sub

        <Fact>
        Public Sub InferenceMergeEmit1()
            Dim compilationDef =
<compilation name="InferenceMergeEmit1">
    <file name="a.vb">
Option Strict Off

Imports System.Linq.Enumerable

Module Module1
    Sub Main()
        Dim x1 = Sub() System.Console.WriteLine("1")
        x1()
        Dim x1Type = x1.GetType()
        System.Console.WriteLine(x1Type)

        Dim debuggerDisplay As System.Diagnostics.DebuggerDisplayAttribute = x1Type.GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False).Single()
        System.Console.WriteLine("{0}, {1}", debuggerDisplay.Value, debuggerDisplay.Type)
        System.Console.WriteLine(x1Type.GetCustomAttributes(GetType(System.Runtime.CompilerServices.CompilerGeneratedAttribute), False).Single())

        Dim x2 = Sub() System.Console.WriteLine("2")
        x2()
        System.Console.WriteLine(x2.GetType())

        Dim x3 = Sub(p1 As Integer) System.Console.WriteLine(p1)
        x3(3)
        System.Console.WriteLine(x3.GetType())
        System.Console.WriteLine(x3.GetType().GetMethod("Invoke").GetParameters()(0).Name)
        System.Console.WriteLine(x3.GetType().GetGenericTypeDefinition())

        Dim x4 = Sub(P1 As Integer) System.Console.WriteLine(P1)
        x4(4)
        System.Console.WriteLine(x4.GetType())

        Dim x5 = Sub(P2 As Integer) System.Console.WriteLine(P2)
        x5(5)
        System.Console.WriteLine(x5.GetType())
        System.Console.WriteLine(x5.GetType().GetMethod("Invoke").GetParameters()(0).Name)

        Dim x6 = Sub(P1 As Long) System.Console.WriteLine(P1)
        x6(6)
        System.Console.WriteLine(x6.GetType())

        Dim x7 = Sub(p1 As Integer, p2 As Integer) System.Console.WriteLine(p1 + p2)
        x7(3, 4)
        System.Console.WriteLine(x7.GetType())
        System.Console.WriteLine(x7.GetType().GetGenericTypeDefinition())

        Dim x8 = Function() 8
        System.Console.WriteLine(x8())
        System.Console.WriteLine(x8.GetType())
        System.Console.WriteLine(x8.GetType().GetGenericTypeDefinition())

        Dim x9 = Function() As Long
                     Return 9
                 End Function

        System.Console.WriteLine(x9())
        System.Console.WriteLine(x9.GetType())

        Dim x10 = Function(pp1) pp1
        System.Console.WriteLine(x10(10))
        System.Console.WriteLine(x10.GetType())
        System.Console.WriteLine(x10.GetType().GetGenericTypeDefinition())
        System.Console.WriteLine(x10.GetType().GetMethod("Invoke").GetParameters()(0).Name)

        Dim x11 = Function(pP1 As Integer) pP1
        System.Console.WriteLine(x11(11))
        System.Console.WriteLine(x11.GetType())

        Dim x12 = Function(Pp1 As Long) Pp1
        System.Console.WriteLine(x12(12))
        System.Console.WriteLine(x12.GetType())

        Dim x13 = Function(ByRef PP1) PP1
        System.Console.WriteLine(x13(13))
        System.Console.WriteLine(x13.GetType())
        System.Console.WriteLine(x13.GetType().GetGenericTypeDefinition())
        System.Console.WriteLine(x13.GetType().GetMethod("Invoke").GetParameters()(0).Name)

        Dim x14 = Function(ByRef pP1 As Integer) pP1
        System.Console.WriteLine(x14(14))
        System.Console.WriteLine(x14.GetType())

        Dim x15 = Function(ByRef Pp1 As Long) Pp1
        System.Console.WriteLine(x15(15))
        System.Console.WriteLine(x15.GetType())
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, options:=TestOptions.ReleaseExe, references:={SystemCoreRef},
                             expectedOutput:=
            <![CDATA[
1
VB$AnonymousDelegate_0
<generated method>, <generated method>
System.Runtime.CompilerServices.CompilerGeneratedAttribute
2
VB$AnonymousDelegate_0
3
VB$AnonymousDelegate_1`1[System.Int32]
p1
VB$AnonymousDelegate_1`1[TArg0]
4
VB$AnonymousDelegate_1`1[System.Int32]
5
VB$AnonymousDelegate_2`1[System.Int32]
P2
6
VB$AnonymousDelegate_1`1[System.Int64]
7
VB$AnonymousDelegate_3`2[System.Int32,System.Int32]
VB$AnonymousDelegate_3`2[TArg0,TArg1]
8
VB$AnonymousDelegate_4`1[System.Int32]
VB$AnonymousDelegate_4`1[TResult]
9
VB$AnonymousDelegate_4`1[System.Int64]
10
VB$AnonymousDelegate_5`2[System.Object,System.Object]
VB$AnonymousDelegate_5`2[TArg0,TResult]
pp1
11
VB$AnonymousDelegate_5`2[System.Int32,System.Int32]
12
VB$AnonymousDelegate_5`2[System.Int64,System.Int64]
13
VB$AnonymousDelegate_6`2[System.Object,System.Object]
VB$AnonymousDelegate_6`2[TArg0,TResult]
PP1
14
VB$AnonymousDelegate_6`2[System.Int32,System.Int32]
15
VB$AnonymousDelegate_6`2[System.Int64,System.Int64]
]]>)
        End Sub

        <Fact>
        Public Sub InferenceMergeEmit2()
            Dim compilationDef =
<compilation name="InferenceMergeEmit2">
    <file name="a.vb">
Option Strict Off

Imports System.Linq.Enumerable

Module Module1
    Sub Main()
        Dim x1 = Sub() System.Console.WriteLine("1") 'BIND1:"x1"
        x1()
        Dim x1Type = x1.GetType()
        System.Console.WriteLine(x1Type)

        Dim debuggerDisplay As System.Diagnostics.DebuggerDisplayAttribute = x1Type.GetCustomAttributes(GetType(System.Diagnostics.DebuggerDisplayAttribute), False).Single()
        System.Console.WriteLine("{0}, {1}", debuggerDisplay.Value, debuggerDisplay.Type)
        System.Console.WriteLine(x1Type.GetCustomAttributes(GetType(System.Runtime.CompilerServices.CompilerGeneratedAttribute), False).Single())

        Dim x2 = Sub() System.Console.WriteLine("2") 'BIND2:"x2"
        x2()
        System.Console.WriteLine(x2.GetType())

        Dim x3 = Sub(p1 As Integer) System.Console.WriteLine(p1) 'BIND3:"x3"
        x3(3)
        System.Console.WriteLine(x3.GetType())
        System.Console.WriteLine(x3.GetType().GetMethod("Invoke").GetParameters()(0).Name)
        System.Console.WriteLine(x3.GetType().GetGenericTypeDefinition())

        Dim x4 = Sub(P1 As Integer) System.Console.WriteLine(P1) 'BIND4:"x4"
        x4(4)
        System.Console.WriteLine(x4.GetType())

        Dim x5 = Sub(P2 As Integer) System.Console.WriteLine(P2) 'BIND5:"x5"
        x5(5)
        System.Console.WriteLine(x5.GetType())
        System.Console.WriteLine(x5.GetType().GetMethod("Invoke").GetParameters()(0).Name)

        Dim x6 = Sub(P1 As Long) System.Console.WriteLine(P1) 'BIND6:"x6"
        x6(6)
        System.Console.WriteLine(x6.GetType())

        Dim x7 = Sub(p1 As Integer, p2 As Integer) System.Console.WriteLine(p1 + p2)  'BIND7:"x7"
        x7(3, 4)
        System.Console.WriteLine(x7.GetType())
        System.Console.WriteLine(x7.GetType().GetGenericTypeDefinition())

        Dim x8 = Function() 8 'BIND8:"x8"
        System.Console.WriteLine(x8())
        System.Console.WriteLine(x8.GetType())
        System.Console.WriteLine(x8.GetType().GetGenericTypeDefinition())

        Dim x9 = Function() As Long 'BIND9:"x9"
                     Return 9
                 End Function

        System.Console.WriteLine(x9())
        System.Console.WriteLine(x9.GetType())

        Dim x10 = Function(pp1) pp1 'BIND10:"x10"
        System.Console.WriteLine(x10(10))
        System.Console.WriteLine(x10.GetType())
        System.Console.WriteLine(x10.GetType().GetGenericTypeDefinition())
        System.Console.WriteLine(x10.GetType().GetMethod("Invoke").GetParameters()(0).Name)

        Dim x11 = Function(pP1 As Integer) pP1 'BIND11:"x11"
        System.Console.WriteLine(x11(11))
        System.Console.WriteLine(x11.GetType())

        Dim x12 = Function(Pp1 As Long) Pp1 'BIND12:"x12"
        System.Console.WriteLine(x12(12))
        System.Console.WriteLine(x12.GetType())

        Dim x13 = Function(ByRef PP1) PP1 'BIND13:"x13"
        System.Console.WriteLine(x13(13))
        System.Console.WriteLine(x13.GetType())
        System.Console.WriteLine(x13.GetType().GetGenericTypeDefinition())
        System.Console.WriteLine(x13.GetType().GetMethod("Invoke").GetParameters()(0).Name)

        Dim x14 = Function(ByRef pP1 As Integer) pP1 'BIND14:"x14" 
        System.Console.WriteLine(x14(14))
        System.Console.WriteLine(x14.GetType())

        Dim x15 = Function(ByRef Pp1 As Long) Pp1 'BIND15:"x15" 
        System.Console.WriteLine(x15(15))
        System.Console.WriteLine(x15.GetType())

        Dim x16 = Function(ByRef Pp1 As Long) Pp1 'BIND16:"x16" 
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node16 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 16)
            Dim x16 = DirectCast(semanticModel.GetDeclaredSymbol(node16), LocalSymbol).Type

            Assert.True(x16.IsAnonymousType)
            Assert.Equal("Function <generated method>(ByRef Pp1 As System.Int64) As System.Int64", x16.ToTestDisplayString())
            Assert.Equal(MethodKind.Constructor, x16.GetMethod(".ctor").MethodKind)
            Assert.Equal("Sub <generated method>..ctor(TargetObject As System.Object, TargetMethod As System.IntPtr)", x16.GetMethod(".ctor").ToTestDisplayString())
            Assert.Equal(MethodKind.DelegateInvoke, x16.GetMember(Of MethodSymbol)("Invoke").MethodKind)
            Assert.Equal("Function <generated method>.Invoke(ByRef Pp1 As System.Int64) As System.Int64", x16.GetMember("Invoke").ToTestDisplayString())
            Assert.Equal(MethodKind.Ordinary, x16.GetMember(Of MethodSymbol)("BeginInvoke").MethodKind)
            Assert.Equal("Function <generated method>.BeginInvoke(ByRef Pp1 As System.Int64, DelegateCallback As System.AsyncCallback, DelegateAsyncState As System.Object) As System.IAsyncResult", x16.GetMember("BeginInvoke").ToTestDisplayString())
            Assert.Equal(MethodKind.Ordinary, x16.GetMember(Of MethodSymbol)("EndInvoke").MethodKind)
            Assert.Equal("Function <generated method>.EndInvoke(ByRef Pp1 As System.Int64, DelegateAsyncResult As System.IAsyncResult) As System.Int64", x16.GetMember("EndInvoke").ToTestDisplayString())

            Assert.IsType(GetType(AnonymousTypeManager.AnonymousDelegatePublicSymbol), x16)
            Assert.False(DirectCast(x16, INamedTypeSymbol).IsSerializable)

            Dim node15 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 15)
            Dim x15 = DirectCast(semanticModel.GetDeclaredSymbol(node15), LocalSymbol).Type

            Assert.True(x15.IsAnonymousType)
            Assert.Equal("Function <generated method>(ByRef Pp1 As System.Int64) As System.Int64", x15.ToTestDisplayString())
            Assert.Equal("Sub <generated method>..ctor(TargetObject As System.Object, TargetMethod As System.IntPtr)", x15.GetMethod(".ctor").ToTestDisplayString())
            Assert.Equal("Function <generated method>.Invoke(ByRef Pp1 As System.Int64) As System.Int64", x15.GetMember("Invoke").ToTestDisplayString())
            Assert.Equal("Function <generated method>.BeginInvoke(ByRef Pp1 As System.Int64, DelegateCallback As System.AsyncCallback, DelegateAsyncState As System.Object) As System.IAsyncResult", x15.GetMember("BeginInvoke").ToTestDisplayString())
            Assert.Equal("Function <generated method>.EndInvoke(ByRef Pp1 As System.Int64, DelegateAsyncResult As System.IAsyncResult) As System.Int64", x15.GetMember("EndInvoke").ToTestDisplayString())
            Assert.NotSame(x16, x15)
            Assert.Equal(x16, x15)

            Dim node14 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 14)
            Dim x14 = DirectCast(semanticModel.GetDeclaredSymbol(node14), LocalSymbol).Type

            Assert.True(x14.IsAnonymousType)
            Assert.Equal("Function <generated method>(ByRef pP1 As System.Int32) As System.Int32", x14.ToTestDisplayString())
            Assert.Equal("Function <generated method>.Invoke(ByRef pP1 As System.Int32) As System.Int32", x14.GetMember("Invoke").ToTestDisplayString())
            Assert.Equal("Function <generated method>.BeginInvoke(ByRef pP1 As System.Int32, DelegateCallback As System.AsyncCallback, DelegateAsyncState As System.Object) As System.IAsyncResult", x14.GetMember("BeginInvoke").ToTestDisplayString())
            Assert.Equal("Function <generated method>.EndInvoke(ByRef pP1 As System.Int32, DelegateAsyncResult As System.IAsyncResult) As System.Int32", x14.GetMember("EndInvoke").ToTestDisplayString())
            Assert.NotSame(x16, x14)
            Assert.NotSame(x15, x14)

            Dim node13 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 13)
            Dim x13 = DirectCast(semanticModel.GetDeclaredSymbol(node13), LocalSymbol).Type

            Assert.True(x13.IsAnonymousType)
            Assert.Equal("Function <generated method>(ByRef PP1 As System.Object) As System.Object", x13.ToTestDisplayString())
            Assert.Equal("Function <generated method>.Invoke(ByRef PP1 As System.Object) As System.Object", x13.GetMember("Invoke").ToTestDisplayString())
            Assert.Equal("Function <generated method>.BeginInvoke(ByRef PP1 As System.Object, DelegateCallback As System.AsyncCallback, DelegateAsyncState As System.Object) As System.IAsyncResult", x13.GetMember("BeginInvoke").ToTestDisplayString())
            Assert.Equal("Function <generated method>.EndInvoke(ByRef PP1 As System.Object, DelegateAsyncResult As System.IAsyncResult) As System.Object", x13.GetMember("EndInvoke").ToTestDisplayString())
            Assert.NotSame(x16, x13)
            Assert.NotSame(x15, x13)
            Assert.NotSame(x14, x13)

            Dim node12 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 12)
            Dim x12 = DirectCast(semanticModel.GetDeclaredSymbol(node12), LocalSymbol).Type

            Assert.True(x12.IsAnonymousType)
            Assert.Equal("Function <generated method>(Pp1 As System.Int64) As System.Int64", x12.ToTestDisplayString())

            Dim node11 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 11)
            Dim x11 = DirectCast(semanticModel.GetDeclaredSymbol(node11), LocalSymbol).Type

            Assert.True(x11.IsAnonymousType)
            Assert.Equal("Function <generated method>(pP1 As System.Int32) As System.Int32", x11.ToTestDisplayString())

            Dim node10 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 10)
            Dim x10 = DirectCast(semanticModel.GetDeclaredSymbol(node10), LocalSymbol).Type

            Assert.True(x10.IsAnonymousType)
            Assert.Equal("Function <generated method>(pp1 As System.Object) As System.Object", x10.ToTestDisplayString())

            Dim node7 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 7)
            Dim x7 = DirectCast(semanticModel.GetDeclaredSymbol(node7), LocalSymbol).Type

            Assert.True(x7.IsAnonymousType)
            Assert.Equal("Sub <generated method>(p1 As System.Int32, p2 As System.Int32)", x7.ToTestDisplayString())
            Assert.Equal("Sub <generated method>.Invoke(p1 As System.Int32, p2 As System.Int32)", x7.GetMember("Invoke").ToTestDisplayString())
            Assert.Equal("Function <generated method>.BeginInvoke(p1 As System.Int32, p2 As System.Int32, DelegateCallback As System.AsyncCallback, DelegateAsyncState As System.Object) As System.IAsyncResult", x7.GetMember("BeginInvoke").ToTestDisplayString())
            Assert.Equal("Sub <generated method>.EndInvoke(DelegateAsyncResult As System.IAsyncResult)", x7.GetMember("EndInvoke").ToTestDisplayString())

            Dim node5 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 5)
            Dim x5 = DirectCast(semanticModel.GetDeclaredSymbol(node5), LocalSymbol).Type

            Assert.True(x5.IsAnonymousType)
            Assert.Equal("Sub <generated method>(P2 As System.Int32)", x5.ToTestDisplayString())

            Dim node4 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 4)
            Dim x4 = DirectCast(DirectCast(semanticModel.GetDeclaredSymbol(node4), LocalSymbol).Type, NamedTypeSymbol)

            Assert.True(x4.IsAnonymousType)
            Assert.Equal("Sub <generated method>(P1 As System.Int32)", x4.ToTestDisplayString())
            Assert.False(x4.IsGenericType)
            Assert.False(x4.MangleName)

            Dim node2 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 2)
            Dim x2 = DirectCast(DirectCast(semanticModel.GetDeclaredSymbol(node2), LocalSymbol).Type, NamedTypeSymbol)

            Assert.True(x2.IsAnonymousType)
            Assert.Equal("Sub <generated method>()", x2.ToTestDisplayString())
            Assert.Equal("Sub <generated method>..ctor(TargetObject As System.Object, TargetMethod As System.IntPtr)", x2.GetMethod(".ctor").ToTestDisplayString())
            Assert.Equal("Sub <generated method>.Invoke()", x2.GetMember("Invoke").ToTestDisplayString())
            Assert.True(x2.IsDefinition)
            Assert.False(x2.IsGenericType)
            Assert.False(x2.MangleName)

            Dim node1 As ModifiedIdentifierSyntax = CompilationUtils.FindBindingText(Of ModifiedIdentifierSyntax)(compilation, "a.vb", 1)
            Dim x1 = DirectCast(DirectCast(semanticModel.GetDeclaredSymbol(node1), LocalSymbol).Type, NamedTypeSymbol)

            Assert.True(x1.IsAnonymousType)
            Assert.Equal("Sub <generated method>()", x1.ToTestDisplayString())
            Assert.True(x1.IsDefinition)
            Assert.NotSame(x2, x1)
            Assert.Equal(x2, x1)
            Assert.False(x1.IsGenericType)
            Assert.False(x1.MangleName)

            Dim semanticModel2 = compilation.GetSemanticModel(tree)
            Assert.NotSame(semanticModel, semanticModel2)

            Dim x16_2 = DirectCast(semanticModel2.GetDeclaredSymbol(node16), LocalSymbol).Type
            Assert.NotSame(x16, x16_2)
            Assert.Equal(x16, x16_2)

            Dim x1_2 = DirectCast(semanticModel2.GetDeclaredSymbol(node1), LocalSymbol).Type
            Assert.True(x1_2.IsDefinition)
            Assert.NotSame(x1, x1_2)
            Assert.Equal(x1, x1_2)

            CompileAndVerify(compilation, <![CDATA[
1
VB$AnonymousDelegate_0
<generated method>, <generated method>
System.Runtime.CompilerServices.CompilerGeneratedAttribute
2
VB$AnonymousDelegate_0
3
VB$AnonymousDelegate_1`1[System.Int32]
p1
VB$AnonymousDelegate_1`1[TArg0]
4
VB$AnonymousDelegate_1`1[System.Int32]
5
VB$AnonymousDelegate_2`1[System.Int32]
P2
6
VB$AnonymousDelegate_1`1[System.Int64]
7
VB$AnonymousDelegate_3`2[System.Int32,System.Int32]
VB$AnonymousDelegate_3`2[TArg0,TArg1]
8
VB$AnonymousDelegate_4`1[System.Int32]
VB$AnonymousDelegate_4`1[TResult]
9
VB$AnonymousDelegate_4`1[System.Int64]
10
VB$AnonymousDelegate_5`2[System.Object,System.Object]
VB$AnonymousDelegate_5`2[TArg0,TResult]
pp1
11
VB$AnonymousDelegate_5`2[System.Int32,System.Int32]
12
VB$AnonymousDelegate_5`2[System.Int64,System.Int64]
13
VB$AnonymousDelegate_6`2[System.Object,System.Object]
VB$AnonymousDelegate_6`2[TArg0,TResult]
PP1
14
VB$AnonymousDelegate_6`2[System.Int32,System.Int32]
15
VB$AnonymousDelegate_6`2[System.Int64,System.Int64]
]]>)
        End Sub

        <Fact>
        <WorkItem(2928, "https://github.com/dotnet/roslyn/issues/2928")>
        Public Sub ContainingSymbol()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Module Test
    Sub Main()
        Dim x = Function(y) y + 1
        System.Console.WriteLine(x)
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.DebugExe.WithRootNamespace("Ns1.Ns2"))

            Dim tree As SyntaxTree = comp.SyntaxTrees.Single()
            Dim semanticModel = comp.GetSemanticModel(tree)
            Dim x = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "x").Single()

            Dim type = semanticModel.GetTypeInfo(x).Type
            Assert.Equal("Function <generated method>(y As System.Object) As System.Object", type.ToTestDisplayString())
            Assert.True(type.ContainingNamespace.IsGlobalNamespace)

            Dim validator As Action(Of ModuleSymbol) =
                Sub(m As ModuleSymbol)
                    Dim anonDelegate = (From sym In m.GlobalNamespace.GetMembers()
                                        Where sym.Name.Contains("AnonymousDelegate")).Single()
                End Sub

            CompileAndVerify(comp, symbolValidator:=validator, expectedOutput:="VB$AnonymousDelegate_0`2[System.Object,System.Object]")
        End Sub

    End Class
End Namespace
