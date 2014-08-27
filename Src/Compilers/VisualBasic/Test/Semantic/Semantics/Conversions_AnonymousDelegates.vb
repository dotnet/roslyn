' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class Conversions_AnonymousDelegateInference
        Inherits BasicTestBase

        <Fact()>
        Public Sub Identity1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Sub(x As System.Collections.Generic.IEnumerable(Of Integer))
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As Action(Of System.Collections.Generic.IEnumerable(Of Integer)) = d1 'BIND1:"d1"
        x1(New Integer() {})
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Sub <generated method>(x As System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Action(Of System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal("Widening, AnonymousDelegate", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="System.Int32[]")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__1(Object, System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0020:  ldftn      "Sub VB$AnonymousDelegate_0(Of System.Collections.Generic.IEnumerable(Of Integer)).Invoke(System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0026:  newobj     "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
  IL_002b:  ldc.i4.0
  IL_002c:  newarr     "Integer"
  IL_0031:  callvirt   "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer)).Invoke(System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0036:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__1",
                <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       "Sub System.Console.WriteLine(Object)"
  IL_0006:  ret
}
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub Identity2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Sub(x As System.Collections.Generic.IEnumerable(Of Integer))
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As Action(Of System.Collections.Generic.IEnumerable(Of Integer))
        x1 = DirectCast(d1, Action(Of System.Collections.Generic.IEnumerable(Of Integer))) 'BIND1:"d1"
        x1 = TryCast(d1, Action(Of System.Collections.Generic.IEnumerable(Of Integer))) 'BIND2:"d1"
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Sub <generated method>(x As System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("Sub <generated method>(x As System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal("Identity", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30311: Value of type 'Sub <generated method>(x As System.Collections.Generic.IEnumerable(Of Integer))' cannot be converted to 'System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))'.
        x1 = DirectCast(d1, Action(Of System.Collections.Generic.IEnumerable(Of Integer))) 'BIND1:"d1"
                        ~~
BC30311: Value of type 'Sub <generated method>(x As System.Collections.Generic.IEnumerable(Of Integer))' cannot be converted to 'System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))'.
        x1 = TryCast(d1, Action(Of System.Collections.Generic.IEnumerable(Of Integer))) 'BIND2:"d1"
                     ~~
]]></expected>)

            Next
        End Sub

        <Fact()>
        Public Sub Identity3()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Sub(x As System.Collections.Generic.IEnumerable(Of Integer))
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As Action(Of System.Collections.Generic.IEnumerable(Of Integer))
        x1 = CType(d1, Action(Of System.Collections.Generic.IEnumerable(Of Integer))) 'BIND1:"d1"
        x1(New Integer() {})
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Sub <generated method>(x As System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("Sub <generated method>(x As System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal("Identity", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="System.Int32[]")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__1(Object, System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0020:  ldftn      "Sub VB$AnonymousDelegate_0(Of System.Collections.Generic.IEnumerable(Of Integer)).Invoke(System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0026:  newobj     "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
  IL_002b:  ldc.i4.0
  IL_002c:  newarr     "Integer"
  IL_0031:  callvirt   "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer)).Invoke(System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0036:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__1",
                <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       "Sub System.Console.WriteLine(Object)"
  IL_0006:  ret
}
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub ArgumentIsVbOrBoxWidening()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim d1 = Sub(x As Object)
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As System.Action(Of Integer) = d1 'BIND1:"d1"
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(x As System.Object)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Action(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Widening, DelegateRelaxationLevelWidening, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       63 (0x3f)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Object) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__2(Object, Object)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4(Integer)"
  IL_0033:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0038:  ldc.i4.2
  IL_0039:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_003e:  ret
}
]]>)

            verifier.VerifyIL("Program._Lambda$__2",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0006:  call       "Sub System.Console.WriteLine(Object)"
  IL_000b:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_0006:  ldarg.1
  IL_0007:  box        "Integer"
  IL_000c:  callvirt   "Sub VB$AnonymousDelegate_0(Of Object).Invoke(Object)"
  IL_0011:  ret
}
]]>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(x As System.Object)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Action(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Widening, DelegateRelaxationLevelWidening, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="2")
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(x As System.Object)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Action(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Widening, DelegateRelaxationLevelWidening, AnonymousDelegate, NeedAStub", conv.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="2")
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub ArgumentIsNarrowing()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim d1 = Sub(x As Integer)
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As System.Action(Of Object) = d1 'BIND1:"d1"
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(x As System.Int32)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Action(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__2(Object, Integer)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4(Object)"
  IL_0033:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_0038:  ldc.i4.2
  IL_0039:  box        "Integer"
  IL_003e:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_0043:  ret
}
]]>)

            verifier.VerifyIL("Program._Lambda$__2",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
            <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_0006:  ldarg.1
  IL_0007:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_000c:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
  IL_0011:  ret
}
]]>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(x As System.Int32)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Action(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="2")
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42016: Implicit conversion from 'Object' to 'Integer'.
        Dim x1 As System.Action(Of Object) = d1 'BIND1:"d1"
                                             ~~
BC42016: Implicit conversion from 'Sub <generated method>(x As Integer)' to 'System.Action(Of Object)'.
        Dim x1 As System.Action(Of Object) = d1 'BIND1:"d1"
                                             ~~
]]></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(x As System.Int32)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Action(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Sub <generated method>(x As Integer)' to 'System.Action(Of Object)'.
        Dim x1 As System.Action(Of Object) = d1 'BIND1:"d1"
                                             ~~
]]></expected>)
        End Sub

        <WorkItem(543114, "DevDiv")>
        <Fact()>
        Public Sub ArgumentIsNarrowing2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim d1 = Sub(x As Integer)
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As System.Action(Of Object) = CType(d1, System.Action(Of Object))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__2(Object, Integer)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4(Object)"
  IL_0033:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_0038:  ldc.i4.2
  IL_0039:  box        "Integer"
  IL_003e:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_0043:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__2",
                <![CDATA[
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0006:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
                <![CDATA[
{
    // Code size       18 (0x12)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
    IL_0006:  ldarg.1
    IL_0007:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_000c:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
    IL_0011:  ret
}
]]>)
            Next

        End Sub

        <WorkItem(543114, "DevDiv")>
        <Fact()>
        Public Sub ArgumentIsNarrowing3()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim d1 = Sub(x As Integer)
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As System.Action(Of Object) = CType(AddressOf d1.Invoke, System.Action(Of Object))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__2(Object, Integer)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4(Object)"
  IL_0033:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_0038:  ldc.i4.2
  IL_0039:  box        "Integer"
  IL_003e:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_0043:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__2",
                <![CDATA[
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0006:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
                <![CDATA[
{
    // Code size       18 (0x12)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
    IL_0006:  ldarg.1
    IL_0007:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_000c:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
    IL_0011:  ret
}
]]>)
            Next

        End Sub

        <WorkItem(543114, "DevDiv")>
        <Fact()>
        Public Sub ArgumentIsNarrowing3_2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim d1 = Sub(x As Integer)
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As System.Action(Of Object) = DirectCast(AddressOf d1.Invoke, System.Action(Of Object))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__2(Object, Integer)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4(Object)"
  IL_0033:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_0038:  ldc.i4.2
  IL_0039:  box        "Integer"
  IL_003e:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_0043:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__2",
                <![CDATA[
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0006:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
                <![CDATA[
{
    // Code size       18 (0x12)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
    IL_0006:  ldarg.1
    IL_0007:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_000c:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
    IL_0011:  ret
}
]]>)
            Next

        End Sub


        <WorkItem(543114, "DevDiv")>
        <Fact()>
        Public Sub ArgumentIsNarrowing3_3()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim d1 = Sub(x As Integer)
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As System.Action(Of Object) = TryCast(AddressOf d1.Invoke, System.Action(Of Object))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       68 (0x44)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__2(Object, Integer)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4(Object)"
  IL_0033:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_0038:  ldc.i4.2
  IL_0039:  box        "Integer"
  IL_003e:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_0043:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__2",
                <![CDATA[
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0006:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
                <![CDATA[
{
    // Code size       18 (0x12)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
    IL_0006:  ldarg.1
    IL_0007:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_000c:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
    IL_0011:  ret
}
]]>)
            Next

        End Sub

        <Fact()>
        Public Sub ArgumentIsClrWidening()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Sub(x As System.Collections.IEnumerable)
                     System.Console.WriteLine(x)
                 End Sub

        Dim x1 As Action(Of System.Collections.Generic.IEnumerable(Of Integer)) = d1 'BIND1:"d1"
        x1(New Integer() {})
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Sub <generated method>(x As System.Collections.IEnumerable)", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Action(Of System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal("Widening, DelegateRelaxationLevelWidening, AnonymousDelegate", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="System.Int32[]")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__1(Object, System.Collections.IEnumerable)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of System.Collections.IEnumerable)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0020:  ldftn      "Sub VB$AnonymousDelegate_0(Of System.Collections.IEnumerable).Invoke(System.Collections.IEnumerable)"
  IL_0026:  newobj     "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
  IL_002b:  ldc.i4.0
  IL_002c:  newarr     "Integer"
  IL_0031:  callvirt   "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer)).Invoke(System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0036:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__1",
                <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       "Sub System.Console.WriteLine(Object)"
  IL_0006:  ret
}
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub ArgumentConversionError()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Sub(x As System.Guid)
                 End Sub
        Dim x1 As Action(Of Integer) = d1 'BIND1:"d1"
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))
                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Sub <generated method>(x As System.Guid)", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Action(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal(0, conv.Kind)
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30311: Value of type 'Sub <generated method>(x As System.Guid)' cannot be converted to 'System.Action(Of Integer)'.
        Dim x1 As Action(Of Integer) = d1 'BIND1:"d1"
                                       ~~
]]></expected>)
            Next
        End Sub

        <Fact()>
        Public Sub ReturnValueIsDropped1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d2 = Function()
                     System.Console.WriteLine("x2")
                     Return 2
                 End Function
        Dim x2 As Action = d2 'BIND1:"d2"

        x2()
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Function <generated method>() As System.Int32", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Action", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal("Widening, DelegateRelaxationLevelWideningDropReturnOrArgs, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="x2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d2
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Function Program._Lambda$__2(Object) As Integer"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4()"
  IL_0033:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0038:  callvirt   "Sub System.Action.Invoke()"
  IL_003d:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__2",
                <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldstr      "x2"
  IL_0005:  call       "Sub System.Console.WriteLine(String)"
  IL_000a:  ldc.i4.2
  IL_000b:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
                <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_0006:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_000b:  pop       
  IL_000c:  ret       
}
]]>)

            Next
        End Sub

        <Fact()>
        Public Sub SubToFunction()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Sub()
                 End Sub
        Dim x1 As Func(Of Integer) = d1 'BIND1:"d1"
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))
                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Sub <generated method>()", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal(0, conv.Kind)
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30311: Value of type 'Sub <generated method>()' cannot be converted to 'System.Func(Of Integer)'.
        Dim x1 As Func(Of Integer) = d1 'BIND1:"d1"
                                     ~~
]]></expected>)
            Next
        End Sub

        <Fact()>
        Public Sub ReturnIsWidening()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Function() CObj(1)

        Dim x2 As Func(Of Integer) = d1 'BIND1:"d1"

        System.Console.WriteLine(x2())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Function <generated method>() As System.Object", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="1")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Object) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Function Program._Lambda$__2(Object) As Object"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Function Program._Closure$__1._Lambda$__4() As Integer"
  IL_0033:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0038:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_003d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0042:  ret
}
]]>)

            verifier.VerifyIL("Program._Lambda$__2",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1  
  IL_0001:  box        "Integer"
  IL_0006:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
            <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_0006:  callvirt   "Function VB$AnonymousDelegate_0(Of Object).Invoke() As Object"
  IL_000b:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_0010:  ret       
}
]]>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Function <generated method>() As System.Object", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="1")

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42016: Implicit conversion from 'Function <generated method>() As Object' to 'System.Func(Of Integer)'.
        Dim x2 As Func(Of Integer) = d1 'BIND1:"d1"
                                     ~~
BC42016: Implicit conversion from 'Object' to 'Integer'.
        Dim x2 As Func(Of Integer) = d1 'BIND1:"d1"
                                     ~~
]]></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Function <generated method>() As System.Object", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Function <generated method>() As Object' to 'System.Func(Of Integer)'.
        Dim x2 As Func(Of Integer) = d1 'BIND1:"d1"
                                     ~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ReturnIsIsVbOrBoxNarrowing1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Function() As Integer
                     Return 2
                 End Function

        Dim x1 As Func(Of Object) = d1 'BIND1:"d1"

        System.Console.WriteLine(x1())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Function <generated method>() As System.Int32", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Func(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal("Widening, DelegateRelaxationLevelWidening, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       72 (0x48)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Function Program._Lambda$__2(Object) As Integer"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Function Program._Closure$__1._Lambda$__4() As Object"
  IL_0033:  newobj     "Sub System.Func(Of Object)..ctor(Object, System.IntPtr)"
  IL_0038:  callvirt   "Function System.Func(Of Object).Invoke() As Object"
  IL_003d:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0042:  call       "Sub System.Console.WriteLine(Object)"
  IL_0047:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__2",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.2  
  IL_0001:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
                <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_0006:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_000b:  box        "Integer"
  IL_0010:  ret       
}
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub ReturnIsClrNarrowing()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Function() As Collections.Generic.IEnumerable(Of Integer)
                     Return New Integer() {}
                 End Function
        Dim x1 As Func(Of Collections.IEnumerable) = d1 'BIND1:"d1"

        System.Console.WriteLine(x1())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Function <generated method>() As System.Collections.Generic.IEnumerable(Of System.Int32)", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Func(Of System.Collections.IEnumerable)", typeInfo.ConvertedType.ToTestDisplayString())
                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal("Widening, DelegateRelaxationLevelWidening, AnonymousDelegate", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="System.Int32[]")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       54 (0x36)
  .maxstack  2
  IL_0000:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Function Program._Lambda$__1(Object) As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__2 As <generated method>"
  IL_0020:  ldftn      "Function VB$AnonymousDelegate_0(Of System.Collections.Generic.IEnumerable(Of Integer)).Invoke() As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_0026:  newobj     "Sub System.Func(Of System.Collections.IEnumerable)..ctor(Object, System.IntPtr)"
  IL_002b:  callvirt   "Function System.Func(Of System.Collections.IEnumerable).Invoke() As System.Collections.IEnumerable"
  IL_0030:  call       "Sub System.Console.WriteLine(Object)"
  IL_0035:  ret
}
]]>)

                verifier.VerifyIL("Program._Lambda$__1",
                <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0  
  IL_0001:  newarr     "Integer"
  IL_0006:  ret
}
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub ReturnNoConversion()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Function() As Integer
                     Return 1
                 End Function
        Dim x1 As Func(Of Guid) = d1 'BIND1:"d1"
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Function <generated method>() As System.Int32", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Func(Of System.Guid)", typeInfo.ConvertedType.ToTestDisplayString())
                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal(0, conv.Kind)
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30311: Value of type 'Function <generated method>() As Integer' cannot be converted to 'System.Func(Of System.Guid)'.
        Dim x1 As Func(Of Guid) = d1 'BIND1:"d1"
                                  ~~
]]></expected>)
            Next
        End Sub

        <Fact()>
        Public Sub AllArgumentsIgnored()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Function() 1

        Dim x2 As Func(Of Integer, Integer) = d1 'BIND1:"d1"

        System.Console.WriteLine(x2(13))
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Function <generated method>() As System.Int32", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelWideningDropReturnOrArgs, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="1")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       69 (0x45)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Integer) V_0) //d1
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Function Program._Lambda$__2(Object) As Integer"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Function Program._Closure$__1._Lambda$__4(Integer) As Integer"
  IL_0033:  newobj     "Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)"
  IL_0038:  ldc.i4.s   13
  IL_003a:  callvirt   "Function System.Func(Of Integer, Integer).Invoke(Integer) As Integer"
  IL_003f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0044:  ret
}
]]>)

            verifier.VerifyIL("Program._Lambda$__2",
            <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_0006:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_000b:  ret
}
]]>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Function <generated method>() As System.Int32", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelWideningDropReturnOrArgs, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="1")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Function <generated method>() As System.Int32", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelWideningDropReturnOrArgs, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Function <generated method>() As Integer' to 'System.Func(Of Integer, Integer)'.
        Dim x2 As Func(Of Integer, Integer) = d1 'BIND1:"d1"
                                              ~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub ParameterCountMismatch()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Function(y As Integer) 1

        Dim x2 As Func(Of Integer) = d1 'BIND1:"d1"
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Function <generated method>(y As System.Int32) As System.Int32", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal(0, conv.Kind)
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30311: Value of type 'Function <generated method>(y As Integer) As Integer' cannot be converted to 'System.Func(Of Integer)'.
        Dim x2 As Func(Of Integer) = d1 'BIND1:"d1"
                                     ~~
]]></expected>)
            Next
        End Sub

        <Fact()>
        Public Sub ByRefMismatch()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim d1 = Function(ByRef y As Integer) 1
        Dim x1 As Func(Of Integer, Integer) = d1 'BIND1:"d1"
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                If True Then
                    Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                    Dim semanticModel = compilation.GetSemanticModel(tree)
                    Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                    Assert.Equal("Function <generated method>(ByRef y As System.Int32) As System.Int32", typeInfo.Type.ToTestDisplayString())
                    Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1)
                    Assert.Equal(0, conv.Kind)
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30311: Value of type 'Function <generated method>(ByRef y As Integer) As Integer' cannot be converted to 'System.Func(Of Integer, Integer)'.
        Dim x1 As Func(Of Integer, Integer) = d1 'BIND1:"d1"
                                              ~~
]]></expected>)
            Next
        End Sub

        <Fact()>
        Public Sub ByRefArgumentIsWidening()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Delegate Sub d1(ByRef a As String)

Module Program
  Sub Main()
        Dim d = Sub(ByRef y As Object)
                    System.Console.WriteLine(y)
                    y = "2"
                End Sub

        Dim x1 As d1 = d 'BIND1:"d"

        Dim x2 As String = "1"
        x1(x2)
        System.Console.WriteLine(x2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(ByRef y As System.Object)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("d1", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="1" & vbCrLf & "2" & vbCrLf)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       76 (0x4c)
  .maxstack  3
  .locals init (VB$AnonymousDelegate_0(Of Object) V_0, //d
                String V_1) //x2
  IL_0000:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub Program._Lambda$__2(Object, ByRef Object)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "Program._ClosureCache$__3 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  newobj     "Sub Program._Closure$__1..ctor()"
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_002d:  ldftn      "Sub Program._Closure$__1._Lambda$__4(ByRef String)"
  IL_0033:  newobj     "Sub d1..ctor(Object, System.IntPtr)"
  IL_0038:  ldstr      "1"
  IL_003d:  stloc.1
  IL_003e:  ldloca.s   V_1
  IL_0040:  callvirt   "Sub d1.Invoke(ByRef String)"
  IL_0045:  ldloc.1
  IL_0046:  call       "Sub System.Console.WriteLine(String)"
  IL_004b:  ret
}
]]>)

            verifier.VerifyIL("Program._Lambda$__2",
            <![CDATA[
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldind.ref
  IL_0002:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0007:  call       "Sub System.Console.WriteLine(Object)"
  IL_000c:  ldarg.1
  IL_000d:  ldstr      "2"
  IL_0012:  stind.ref
  IL_0013:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__1._Lambda$__4",
            <![CDATA[
{
  // Code size       25 (0x19)
  .maxstack  2
  .locals init (Object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "Program._Closure$__1.$VB$NonLocal_ As <generated method>"
  IL_0006:  ldarg.1
  IL_0007:  ldind.ref
  IL_0008:  stloc.0
  IL_0009:  ldloca.s   V_0
  IL_000b:  callvirt   "Sub VB$AnonymousDelegate_0(Of Object).Invoke(ByRef Object)"
  IL_0010:  ldarg.1
  IL_0011:  ldloc.0
  IL_0012:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_0017:  stind.ref
  IL_0018:  ret
}
]]>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(ByRef y As System.Object)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("d1", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="1" & vbCrLf & "2" & vbCrLf)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC41999: Implicit conversion from 'Object' to 'String' in copying the value of 'ByRef' parameter 'y' back to the matching argument.
        Dim x1 As d1 = d 'BIND1:"d"
                       ~
BC42016: Implicit conversion from 'Sub <generated method>(ByRef y As Object)' to 'd1'.
        Dim x1 As d1 = d 'BIND1:"d"
                       ~
]]></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As IdentifierNameSyntax = CompilationUtils.FindBindingText(Of IdentifierNameSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(node1)

                Assert.Equal("Sub <generated method>(ByRef y As System.Object)", typeInfo.Type.ToTestDisplayString())
                Assert.Equal("d1", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1)
                Assert.Equal("Narrowing, DelegateRelaxationLevelNarrowing, AnonymousDelegate, NeedAStub", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30512: Option Strict On disallows implicit conversions from 'Sub <generated method>(ByRef y As Object)' to 'd1'.
        Dim x1 As d1 = d 'BIND1:"d"
                       ~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
    Sub Test1(x As Func(Of Func(Of Object, Integer)))
        System.Console.WriteLine(x)
    End Sub

    Sub Test1(x As Func(Of Func(Of Integer, Integer)))
        System.Console.WriteLine(x)
    End Sub

    Function Test2(a As Object) As Integer
        Return 1
    End Function

  Sub Main()
        Dim d1 = Function() Function(a As Object) 1

10:     Test1(d1)


        Dim d2 = Function() As Func(Of Object, Integer)
                      Return Function(a As Object)
                                 Return 1
                             End Function
                  End Function

50:     Test1(d2)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As System.Func(Of System.Func(Of Object, Integer)))': Not most specific.
    'Public Sub Test1(x As System.Func(Of System.Func(Of Integer, Integer)))': Not most specific.
10:     Test1(d1)
        ~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
    Sub Test1(x As Func(Of Object, Integer))
        System.Console.WriteLine(x)
    End Sub

    Sub Test1(x As Func(Of Integer, Integer))
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim d1 = Function(x As Object) 1
        Test1(d1)
        Dim d2 = Function(x As Integer) 1
        Test1(d2)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
System.Func`2[System.Object,System.Int32]
System.Func`2[System.Int32,System.Int32]
]]>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution3()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
    Sub Test2(x As Func(Of Object, Integer))
        System.Console.WriteLine(x)
    End Sub

    Sub Test2(x As Func(Of Integer, Integer))
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim d1 = Function(x As String) 1
        Test2(d1)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30519: Overload resolution failed because no accessible 'Test2' can be called without a narrowing conversion:
    'Public Sub Test2(x As System.Func(Of Object, Integer))': Argument matching parameter 'x' narrows from 'Function <generated method>(x As String) As Integer' to 'System.Func(Of Object, Integer)'.
    'Public Sub Test2(x As System.Func(Of Integer, Integer))': Argument matching parameter 'x' narrows from 'Function <generated method>(x As String) As Integer' to 'System.Func(Of Integer, Integer)'.
        Test2(d1)
        ~~~~~
]]></expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30518: Overload resolution failed because no accessible 'Test2' can be called with these arguments:
    'Public Sub Test2(x As System.Func(Of Object, Integer))': Option Strict On disallows implicit conversions from 'Function <generated method>(x As String) As Integer' to 'System.Func(Of Object, Integer)'.
    'Public Sub Test2(x As System.Func(Of Integer, Integer))': Option Strict On disallows implicit conversions from 'Function <generated method>(x As String) As Integer' to 'System.Func(Of Integer, Integer)'.
        Test2(d1)
        ~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution4()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program

    Sub Test3(x As Func(Of Object))
        System.Console.WriteLine(x)
    End Sub

    Sub Test4(x As Func(Of Object))
        System.Console.WriteLine(x)
    End Sub

    Sub Test3(x As Func(Of Integer))
        System.Console.WriteLine(x)
    End Sub

    Sub Test5(x As Func(Of Integer))
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim d1 = Function() "a"
        Test3(d1)
        Test4(d1)
        Test5(d1)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC42016: Implicit conversion from 'Function <generated method>() As String' to 'System.Func(Of Integer)'.
        Test5(d1)
              ~~
BC42016: Implicit conversion from 'String' to 'Integer'.
        Test5(d1)
              ~~
]]></expected>)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Object]
System.Func`1[System.Object]
System.Func`1[System.Int32]
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution5()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Option Strict On        

Imports System        

Module Program

    Sub Test6(x As Object)
        System.Console.WriteLine(x)
    End Sub

    Sub Test7(x As Object)
        System.Console.WriteLine(x)
    End Sub

    Sub Test6(x As Func(Of Object))
        System.Console.WriteLine(x)
    End Sub

    Sub Test8(x As Func(Of Object))
        System.Console.WriteLine(x)
    End Sub

    Sub Test9(x As Func(Of String))
        System.Console.WriteLine(x)
    End Sub

    Sub Test10(x As Func(Of String))
        System.Console.WriteLine(x)
    End Sub

    Sub Test9(x As Func(Of Object))
        System.Console.WriteLine(x)
    End Sub

    Sub Test11(x As Func(Of String))
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim d1 = Function() "a"
        Test6(d1)
        Test7(d1)
        Test8(d1)

        Test9(d1)
        Test10(d1)
        Test11(d1)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Object]
VB$AnonymousDelegate_0`1[System.String]
System.Func`1[System.Object]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution6()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
    Sub Test13(x As Func(Of Integer, Integer))
        System.Console.WriteLine(x)
    End Sub

    Sub Test13(x As Action)
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim x12 = Function() Integer.MaxValue
        Test13(x12)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)
                CompileAndVerify(compilation, <![CDATA[
System.Action
]]>)

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
            Next

        End Sub

        <Fact()>
        Public Sub OverloadResolution7()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
    Sub Test14(x As Func(Of Integer, Integer))
        System.Console.WriteLine(x)
    End Sub

    Sub Test14(x As Func(Of Byte))
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim d1 = Function() 1
        Test14(d1)
        Test14(AddressOf d1.Invoke)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            CompileAndVerify(compilation, <![CDATA[
System.Func`2[System.Int32,System.Int32]
System.Func`2[System.Int32,System.Int32]
]]>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30518: Overload resolution failed because no accessible 'Test14' can be called with these arguments:
    'Public Sub Test14(x As System.Func(Of Integer, Integer))': Option Strict On disallows implicit conversions from 'Function <generated method>() As Integer' to 'System.Func(Of Integer, Integer)'.
    'Public Sub Test14(x As System.Func(Of Byte))': Option Strict On disallows implicit conversions from 'Function <generated method>() As Integer' to 'System.Func(Of Byte)'.
        Test14(d1)
        ~~~~~~
BC30518: Overload resolution failed because no accessible 'Test14' can be called with these arguments:
    'Public Sub Test14(x As System.Func(Of Integer, Integer))': Option Strict On does not allow narrowing in implicit type conversions between method 'Public Overridable Function Invoke() As Integer' and delegate 'Delegate Function System.Func(Of Integer, Integer)(arg As Integer) As Integer'.
    'Public Sub Test14(x As System.Func(Of Byte))': Option Strict On does not allow narrowing in implicit type conversions between method 'Public Overridable Function Invoke() As Integer' and delegate 'Delegate Function System.Func(Of Byte)() As Byte'.
        Test14(AddressOf d1.Invoke)
        ~~~~~~
]]></expected>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution8()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program

    Sub Test1(x As Func(Of Action(Of SByte)))
        System.Console.WriteLine(x)
    End Sub

    Sub Test1(x As Func(Of Action(Of Integer)))
        System.Console.WriteLine(x)
    End Sub

    Sub Test1(x As Func(Of Object))
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim d = Sub(x As Short)
                End Sub

        Test1(Function() d)
        Test1(Function()
                  Return d
              End Function)
  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Action`1[System.SByte]]
System.Func`1[System.Action`1[System.SByte]]
]]>)
        End Sub

        <Fact()>
        Public Sub OverloadResolution9()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program

    Sub Test1(x As Func(Of Action(Of SByte)))
        System.Console.WriteLine(x)
    End Sub

    Sub Test1(x As Func(Of Action(Of Integer)))
        System.Console.WriteLine(x)
    End Sub

    Sub Test1(x As Func(Of Object))
        System.Console.WriteLine(x)
    End Sub

  Sub Main()
        Dim d = Sub(x As Short)
                End Sub

        Test1(Function() AddressOf d.Invoke)
        Test1(Function()
                  Return AddressOf d.Invoke
              End Function)

        Test1(Function() Sub(x As Short)
                         End Sub)
        Test1(Function() '2
                  Return Sub(x As Short)
                         End Sub
              End Function)

  End Sub
End Module
    </file>
</compilation>


            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As System.Func(Of System.Action(Of SByte)))': Not most specific.
    'Public Sub Test1(x As System.Func(Of System.Action(Of Integer)))': Not most specific.
        Test1(Function() AddressOf d.Invoke)
        ~~~~~
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As System.Func(Of System.Action(Of SByte)))': Not most specific.
    'Public Sub Test1(x As System.Func(Of System.Action(Of Integer)))': Not most specific.
        Test1(Function()
        ~~~~~
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As System.Func(Of System.Action(Of SByte)))': Not most specific.
    'Public Sub Test1(x As System.Func(Of System.Action(Of Integer)))': Not most specific.
        Test1(Function() Sub(x As Short)
        ~~~~~
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As System.Func(Of System.Action(Of SByte)))': Not most specific.
    'Public Sub Test1(x As System.Func(Of System.Action(Of Integer)))': Not most specific.
        Test1(Function() '2
        ~~~~~
</expected>)
        End Sub

    End Class

End Namespace

