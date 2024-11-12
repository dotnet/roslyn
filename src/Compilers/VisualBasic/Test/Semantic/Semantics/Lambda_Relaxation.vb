' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Basic.Reference.Assemblies
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class Lambda_Relaxation
        Inherits BasicTestBase

        <Fact()>
        Public Sub ArgumentIsVbOrBoxWidening()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Integer) = Sub(x As Object) 'BIND1:"Sub(x As Object)"
                                                  System.Console.WriteLine(x)
                                              End Sub
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            If True Then
                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Action(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWidening", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Integer)"
  IL_0019:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0024:  ldc.i4.2
  IL_0025:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_002a:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Object)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Object).Invoke(Object)"
  IL_002f:  ret
}
    ]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, expectedOutput:="2")
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompileAndVerify(compilation, expectedOutput:="2")
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)
        End Sub

        <Fact()>
        Public Sub ArgumentIsVbOrBoxWidening2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Integer)
        x1 = CType(Sub(x As Object) 'BIND1:"Sub(x As Object)"
                       System.Console.WriteLine(x)
                   End Sub, System.Action(Of Integer))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Null(typeInfo.ConvertedType)

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Identity", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Integer)"
  IL_0019:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0024:  ldc.i4.2
  IL_0025:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_002a:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Object)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Object).Invoke(Object)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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
            Next
        End Sub

        <Fact()>
        Public Sub ArgumentIsVbOrBoxWidening3()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Integer)
        x1 = DirectCast(Sub(x As Object) 'BIND1:"Sub(x As Object)"
                            System.Console.WriteLine(x)
                        End Sub, System.Action(Of Integer))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Null(typeInfo.ConvertedType)

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Identity", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Integer)"
  IL_0019:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0024:  ldc.i4.2
  IL_0025:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_002a:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Object)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Object).Invoke(Object)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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
            Next
        End Sub

        <Fact()>
        Public Sub ArgumentIsVbOrBoxWidening4()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Integer)
        x1 = TryCast(Sub(x As Object) 'BIND1:"Sub(x As Object)"
                         System.Console.WriteLine(x)
                     End Sub, System.Action(Of Integer))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Null(typeInfo.ConvertedType)

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Identity", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Integer)"
  IL_0019:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Integer)"
  IL_0024:  ldc.i4.2
  IL_0025:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_002a:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Object)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Object).Invoke(Object)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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
            Next
        End Sub

        <Fact()>
        Public Sub ArgumentIsNarrowing()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Object) = Sub(x As Integer) 'BIND1:"Sub(x As Integer)"
                                                 System.Console.WriteLine(x)
                                             End Sub
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Action(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Object)"
  IL_0019:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0024:  ldc.i4.2
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_002f:  ret
}
    ]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
            <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Integer)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
  IL_002f:  ret
}
    ]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
            <![CDATA[
    {
      // Code size        7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.1
      IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
      IL_0006:  ret
    }
    ]]>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Action(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="2")
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Object' to 'Integer'.
        Dim x1 As System.Action(Of Object) = Sub(x As Integer) 'BIND1:"Sub(x As Integer)"
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Action(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Dim x1 As System.Action(Of Object) = Sub(x As Integer) 'BIND1:"Sub(x As Integer)"
                                             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(543114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543114")>
        <Fact()>
        Public Sub ArgumentIsNarrowing2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Object) = CType(Sub(x As Integer)
                                                       System.Console.WriteLine(x)
                                                   End Sub, System.Action(Of Object))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Object)"
  IL_0019:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0024:  ldc.i4.2
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Integer)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
                <![CDATA[
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0006:  ret
}
]]>)
            Next
        End Sub

        <WorkItem(543114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543114")>
        <Fact()>
        Public Sub ArgumentIsNarrowing3()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Object) = DirectCast(Sub(x As Integer)
                                                       System.Console.WriteLine(x)
                                                   End Sub, System.Action(Of Object))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Object)"
  IL_0019:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0024:  ldc.i4.2
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Integer)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
                <![CDATA[
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0006:  ret
}
]]>)
            Next
        End Sub

        <WorkItem(543114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543114")>
        <Fact()>
        Public Sub ArgumentIsNarrowing4()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim x1 As System.Action(Of Object) = TryCast(Sub(x As Integer)
                                                       System.Console.WriteLine(x)
                                                   End Sub, System.Action(Of Object))
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(Object)"
  IL_0019:  newobj     "Sub System.Action(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action(Of Object)"
  IL_0024:  ldc.i4.2
  IL_0025:  box        "Integer"
  IL_002a:  callvirt   "Sub System.Action(Of Object).Invoke(Object)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(Integer)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_002a:  callvirt   "Sub VB$AnonymousDelegate_0(Of Integer).Invoke(Integer)"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
                <![CDATA[
{
    // Code size        7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.1
    IL_0001:  call       "Sub System.Console.WriteLine(Integer)"
    IL_0006:  ret
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
        Dim x1 As Action(Of System.Collections.Generic.IEnumerable(Of Integer)) = Sub(x As System.Collections.IEnumerable) 'BIND1:"Sub(x As System.Collections.IEnumerable)"
                                                                                      System.Console.WriteLine(x)
                                                                                  End Sub
        x1(New Integer() {})
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Action(Of System.Collections.Generic.IEnumerable(Of System.Int32))", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWidening", conv.Kind.ToString())
                Assert.True(conv.Exists)

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="System.Int32[]")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(System.Collections.IEnumerable)"
  IL_0019:  newobj     "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As System.Action(Of System.Collections.Generic.IEnumerable(Of Integer))"
  IL_0024:  ldc.i4.0
  IL_0025:  newarr     "Integer"
  IL_002a:  callvirt   "Sub System.Action(Of System.Collections.Generic.IEnumerable(Of Integer)).Invoke(System.Collections.Generic.IEnumerable(Of Integer))"
  IL_002f:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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
        Dim x1 As Action(Of Integer) = Sub(x As System.Guid) 'BIND1:"Sub(x As System.Guid)"
                                       End Sub
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))
                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Action(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())
                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                Assert.False(conv.Exists)

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action(Of Integer)'.
        Dim x1 As Action(Of Integer) = Sub(x As System.Guid) 'BIND1:"Sub(x As System.Guid)"
                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
            Next
        End Sub

        <Fact()>
        Public Sub ArgumentConversionError2()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim x1 As Action(Of Integer) = ((Sub(x As System.Guid) 'BIND1:"Sub(x As System.Guid)"
                                       End Sub))
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))
                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Action(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                Assert.False(conv.Exists)

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36670: Nested sub does not have a signature that is compatible with delegate 'Action(Of Integer)'.
        Dim x1 As Action(Of Integer) = ((Sub(x As System.Guid) 'BIND1:"Sub(x As System.Guid)"
                                         ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
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
        Dim x1 As Action = Function() 1 'BIND1:"Function()"

        Dim x2 As Action = Function() 'BIND2:"Function()"
                               System.Console.WriteLine("x2")
                               Return 2
                           End Function

        x1()
        x2()
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then

                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Action", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWideningDropReturnOrArgs", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                If True Then
                    Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                    Dim typeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Action", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node2.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWideningDropReturnOrArgs", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="x2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       85 (0x55)
  .maxstack  2
  .locals init (System.Action V_0) //x1
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Action"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1()"
  IL_0019:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Action"
  IL_0024:  stloc.0
  IL_0025:  ldsfld     "Program._Closure$__.$IR0-2 As System.Action"
  IL_002a:  brfalse.s  IL_0033
  IL_002c:  ldsfld     "Program._Closure$__.$IR0-2 As System.Action"
  IL_0031:  br.s       IL_0049
  IL_0033:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0038:  ldftn      "Sub Program._Closure$__._Lambda$__R0-2()"
  IL_003e:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0043:  dup
  IL_0044:  stsfld     "Program._Closure$__.$IR0-2 As System.Action"
  IL_0049:  ldloc.0
  IL_004a:  callvirt   "Sub System.Action.Invoke()"
  IL_004f:  callvirt   "Sub System.Action.Invoke()"
  IL_0054:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-0() As Integer"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0029:  pop
  IL_002a:  ret
}
        ]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
        ]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-2",
                <![CDATA[
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-1() As Integer"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_0024:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0029:  pop
  IL_002a:  ret
}
        ]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-1",
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
            Next
        End Sub

        <Fact()>
        Public Sub ReturnValueIsDropped2_ReturnTypeInferenceError()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim x1 As Action = Function() AddressOf Main 'BIND1:"Function()"

        Dim x2 As Action = Function() 'BIND2:"Function()"
                               Return AddressOf Main
                           End Function
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))
                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Action", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If
                If True Then
                    Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                    Dim typeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Action", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node2.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30581: 'AddressOf' expression cannot be converted to 'Object' because 'Object' is not a delegate type.
        Dim x1 As Action = Function() AddressOf Main 'BIND1:"Function()"
                                      ~~~~~~~~~~~~~~
BC36751: Cannot infer a return type.  Consider adding an 'As' clause to specify the return type.
        Dim x2 As Action = Function() 'BIND2:"Function()"
                           ~~~~~~~~~~
</expected>)
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
        Dim x1 As Func(Of Integer) = Sub() 'BIND1:"Sub()"
                                     End Sub
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))
                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)

                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                Assert.False(conv.Exists)

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36670: Nested sub does not have a signature that is compatible with delegate 'Func(Of Integer)'.
        Dim x1 As Func(Of Integer) = Sub() 'BIND1:"Sub()"
                                     ~~~~~~~~~~~~~~~~~~~~~
</expected>)
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
        Dim x1 As Func(Of Integer) = Function() CObj(1) 'BIND1:"Function()"

        Dim x2 As Func(Of Integer) = Function() As Object 'BIND2:"Function() As Object"
                                         Return 2
                                     End Function

        System.Console.WriteLine(x1())
        System.Console.WriteLine(x2())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If
                If True Then
                    Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node2.Parent)
                    Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       95 (0x5f)
  .maxstack  2
  .locals init (System.Func(Of Integer) V_0) //x1
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As System.Func(Of Integer)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As System.Func(Of Integer)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-0() As Integer"
  IL_0019:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As System.Func(Of Integer)"
  IL_0024:  stloc.0
  IL_0025:  ldsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Integer)"
  IL_002a:  brfalse.s  IL_0033
  IL_002c:  ldsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Integer)"
  IL_0031:  br.s       IL_0049
  IL_0033:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0038:  ldftn      "Function Program._Closure$__._Lambda$__R0-1() As Integer"
  IL_003e:  newobj     "Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0043:  dup
  IL_0044:  stsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Integer)"
  IL_0049:  ldloc.0
  IL_004a:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_004f:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0054:  callvirt   "Function System.Func(Of Integer).Invoke() As Integer"
  IL_0059:  call       "Sub System.Console.WriteLine(Integer)"
  IL_005e:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
            <![CDATA[
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  box        "Integer"
  IL_0006:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_000b:  ret
}
        ]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
            <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-1() As Object"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_0024:  callvirt   "Function VB$AnonymousDelegate_0(Of Object).Invoke() As Object"
  IL_0029:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToInteger(Object) As Integer"
  IL_002e:  ret
}
        ]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__0-1",
            <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  box        "Integer"
  IL_0006:  ret
}
        ]]>)
            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If
                If True Then
                    Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node2.Parent)
                    Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If
            End If

            CompileAndVerify(compilation, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Object' to 'Integer'.
        Dim x1 As Func(Of Integer) = Function() CObj(1) 'BIND1:"Function()"
                                                ~~~~~~~
BC42016: Implicit conversion from 'Object' to 'Integer'.
        Dim x2 As Func(Of Integer) = Function() As Object 'BIND2:"Function() As Object"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If
                If True Then
                    Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node2.Parent)
                    Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If
            End If

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Dim x1 As Func(Of Integer) = Function() CObj(1) 'BIND1:"Function()"
                                                ~~~~~~~
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
        Dim x2 As Func(Of Integer) = Function() As Object 'BIND2:"Function() As Object"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub ReturnIsIsVbOrBoxNarrowing1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim x1 As Func(Of Object) = Function() As Integer 'BIND1:"Function() As Integer"
                                        Return 2
                                    End Function

        System.Console.WriteLine(x1())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Object)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWidening", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="2")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       52 (0x34)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Object)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Object)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__R0-1() As Object"
  IL_0019:  newobj     "Sub System.Func(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Object)"
  IL_0024:  callvirt   "Function System.Func(Of Object).Invoke() As Object"
  IL_0029:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_002e:  call       "Sub System.Console.WriteLine(Object)"
  IL_0033:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-0() As Integer"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0029:  box        "Integer"
  IL_002e:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  ret
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
        Dim x1 As Func(Of Collections.IEnumerable) = Function() As Collections.Generic.IEnumerable(Of Integer) 'BIND1:"Function() As Collections.Generic.IEnumerable(Of Integer)"
                                                         Return New Integer() {}
                                                     End Function

        System.Console.WriteLine(x1())
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Collections.IEnumerable)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWidening", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="System.Int32[]")

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As System.Func(Of System.Collections.IEnumerable)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As System.Func(Of System.Collections.IEnumerable)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-0() As System.Collections.Generic.IEnumerable(Of Integer)"
  IL_0019:  newobj     "Sub System.Func(Of System.Collections.IEnumerable)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As System.Func(Of System.Collections.IEnumerable)"
  IL_0024:  callvirt   "Function System.Func(Of System.Collections.IEnumerable).Invoke() As System.Collections.IEnumerable"
  IL_0029:  call       "Sub System.Console.WriteLine(Object)"
  IL_002e:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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
        Dim x1 As Func(Of Guid) = Function() As Integer 'BIND1:"Function() As Integer"
                                      Return 1
                                  End Function
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Guid)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36532: Nested function does not have the same signature as delegate 'Func(Of Guid)'.
        Dim x1 As Func(Of Guid) = Function() As Integer 'BIND1:"Function() As Integer"
                                  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
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
        Dim x1 As Func(Of Integer, Integer) = Function() 1 'BIND1:"Function()"

        Dim x2 As Func(Of Integer, Integer) = Function() As Integer 'BIND2:"Function() As Integer"
                                                  Return 2
                                              End Function

        System.Console.WriteLine(x1(12))
        System.Console.WriteLine(x2(13))
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWideningDropReturnOrArgs", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If
                If True Then
                    Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node2.Parent)
                    Assert.Equal("Widening, Lambda, DelegateRelaxationLevelWideningDropReturnOrArgs", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine)

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       99 (0x63)
  .maxstack  3
  .locals init (System.Func(Of Integer, Integer) V_0) //x1
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Integer, Integer)"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Integer, Integer)"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__R0-1(Integer) As Integer"
  IL_0019:  newobj     "Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As System.Func(Of Integer, Integer)"
  IL_0024:  stloc.0
  IL_0025:  ldsfld     "Program._Closure$__.$IR0-2 As System.Func(Of Integer, Integer)"
  IL_002a:  brfalse.s  IL_0033
  IL_002c:  ldsfld     "Program._Closure$__.$IR0-2 As System.Func(Of Integer, Integer)"
  IL_0031:  br.s       IL_0049
  IL_0033:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0038:  ldftn      "Function Program._Closure$__._Lambda$__R0-2(Integer) As Integer"
  IL_003e:  newobj     "Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)"
  IL_0043:  dup
  IL_0044:  stsfld     "Program._Closure$__.$IR0-2 As System.Func(Of Integer, Integer)"
  IL_0049:  ldloc.0
  IL_004a:  ldc.i4.s   12
  IL_004c:  callvirt   "Function System.Func(Of Integer, Integer).Invoke(Integer) As Integer"
  IL_0051:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0056:  ldc.i4.s   13
  IL_0058:  callvirt   "Function System.Func(Of Integer, Integer).Invoke(Integer) As Integer"
  IL_005d:  call       "Sub System.Console.WriteLine(Integer)"
  IL_0062:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
                <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-0() As Integer"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0029:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__R0-2",
                <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Function Program._Closure$__._Lambda$__0-1() As Integer"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Integer)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-1 As <generated method>"
  IL_0024:  callvirt   "Function VB$AnonymousDelegate_0(Of Integer).Invoke() As Integer"
  IL_0029:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-1",
                <![CDATA[
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  ret
}
]]>)
            Next
        End Sub

        <Fact()>
        Public Sub ParameterCountMismatch()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Module Program
  Sub Main()
        Dim x1 As Func(Of Integer) = Function(y As Integer) 1 'BIND1:"Function(y As Integer)"

        Dim x2 As Func(Of Integer) = Function(y As Integer) As Integer 'BIND2:"Function(y As Integer) As Integer"
                                         Return 2
                                     End Function

        Dim x3 As Func(Of Integer, Integer, Integer) = Function(y) 1 'BIND3:"Function(y)"

        Dim x4 As Func(Of Integer, Integer, Integer) = Function(y) As Integer 'BIND4:"Function(y) As Integer"
                                                           Return 2
                                                       End Function
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If
                If True Then
                    Dim node2 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 2)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node2.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node2.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If
                If True Then
                    Dim node3 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 3)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node3.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32, System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node3.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If
                If True Then
                    Dim node4 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 4)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node4.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32, System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node4.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer)'.
        Dim x1 As Func(Of Integer) = Function(y As Integer) 1 'BIND1:"Function(y As Integer)"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer)'.
        Dim x2 As Func(Of Integer) = Function(y As Integer) As Integer 'BIND2:"Function(y As Integer) As Integer"
                                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer, Integer)'.
        Dim x3 As Func(Of Integer, Integer, Integer) = Function(y) 1 'BIND3:"Function(y)"
                                                       ~~~~~~~~~~~~~
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer, Integer)'.
        Dim x4 As Func(Of Integer, Integer, Integer) = Function(y) As Integer 'BIND4:"Function(y) As Integer"
                                                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
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
        Dim x1 As Func(Of Integer, Integer) = Function(ByRef y As Integer) 1 'BIND1:"Function(ByRef y As Integer)"
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("System.Func(Of System.Int32, System.Int32)", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Lambda, DelegateRelaxationLevelInvalid", conv.Kind.ToString())
                    Assert.False(conv.Exists)
                End If

                CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC36532: Nested function does not have the same signature as delegate 'Func(Of Integer, Integer)'.
        Dim x1 As Func(Of Integer, Integer) = Function(ByRef y As Integer) 1 'BIND1:"Function(ByRef y As Integer)"
                                              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
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
        Dim x1 As d1 = Sub(ByRef y As Object) 'BIND1:"Sub(ByRef y As Object)"
                           System.Console.WriteLine(y)
                           y = "2"
                       End Sub

        Dim x2 As String = "1"
        x1(x2)
        System.Console.WriteLine(x2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("d1", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       56 (0x38)
  .maxstack  2
  .locals init (String V_0) //x2
  IL_0000:  ldsfld     "Program._Closure$__.$IR0-1 As d1"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$IR0-1 As d1"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__R0-1(ByRef String)"
  IL_0019:  newobj     "Sub d1..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$IR0-1 As d1"
  IL_0024:  ldstr      "1"
  IL_0029:  stloc.0
  IL_002a:  ldloca.s   V_0
  IL_002c:  callvirt   "Sub d1.Invoke(ByRef String)"
  IL_0031:  ldloc.0
  IL_0032:  call       "Sub System.Console.WriteLine(String)"
  IL_0037:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__R0-1",
            <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (Object V_0)
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(ByRef Object)"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0(Of Object)..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As <generated method>"
  IL_0024:  ldarg.1
  IL_0025:  ldind.ref
  IL_0026:  stloc.0
  IL_0027:  ldloca.s   V_0
  IL_0029:  callvirt   "Sub VB$AnonymousDelegate_0(Of Object).Invoke(ByRef Object)"
  IL_002e:  ldarg.1
  IL_002f:  ldloc.0
  IL_0030:  call       "Function Microsoft.VisualBasic.CompilerServices.Conversions.ToString(Object) As String"
  IL_0035:  stind.ref
  IL_0036:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("d1", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompileAndVerify(compilation, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC41999: Implicit conversion from 'Object' to 'String' in copying the value of 'ByRef' parameter 'y' back to the matching argument.
        Dim x1 As d1 = Sub(ByRef y As Object) 'BIND1:"Sub(ByRef y As Object)"
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            If True Then
                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                Assert.Null(typeInfo.Type)
                Assert.Equal("d1", typeInfo.ConvertedType.ToTestDisplayString())

                Dim conv = semanticModel.GetConversion(node1.Parent)
                Assert.Equal("Narrowing, Lambda, DelegateRelaxationLevelNarrowing", conv.Kind.ToString())
                Assert.True(conv.Exists)
            End If

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32029: Option Strict On disallows narrowing from type 'Object' to type 'String' in copying the value of 'ByRef' parameter 'y' back to the matching argument.
        Dim x1 As d1 = Sub(ByRef y As Object) 'BIND1:"Sub(ByRef y As Object)"
                       ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <Fact()>
        Public Sub NoRelaxation()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Delegate Sub d1(ByRef a As Object)

Module Program
  Sub Main()
        Dim x1 As d1 = Sub(ByRef y As Object) 'BIND1:"Sub(ByRef y As Object)"
                           System.Console.WriteLine(y)
                           y = "2"
                       End Sub

        Dim x2 As Object = "1"
        x1(x2)
        System.Console.WriteLine(x2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            For Each optStrict In {OptionStrict.Off, OptionStrict.On, OptionStrict.Custom}
                compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(optStrict))

                Assert.Equal(optStrict, compilation.Options.OptionStrict)

                Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
                Dim semanticModel = compilation.GetSemanticModel(tree)
                If True Then
                    Dim node1 As LambdaHeaderSyntax = CompilationUtils.FindBindingText(Of LambdaHeaderSyntax)(compilation, "a.vb", 1)

                    Dim typeInfo As TypeInfo = semanticModel.GetTypeInfo(DirectCast(node1.Parent, LambdaExpressionSyntax))

                    Assert.Null(typeInfo.Type)
                    Assert.Equal("d1", typeInfo.ConvertedType.ToTestDisplayString())

                    Dim conv = semanticModel.GetConversion(node1.Parent)
                    Assert.Equal("Widening, Lambda", conv.Kind.ToString())
                    Assert.True(conv.Exists)
                End If

                Dim verifier = CompileAndVerify(compilation, expectedOutput:="1" & Environment.NewLine & "2" & Environment.NewLine)

                CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

                verifier.VerifyIL("Program.Main",
                <![CDATA[
{
  // Code size       61 (0x3d)
  .maxstack  2
  .locals init (Object V_0) //x2
  IL_0000:  ldsfld     "Program._Closure$__.$I0-0 As d1"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I0-0 As d1"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__0-0(ByRef Object)"
  IL_0019:  newobj     "Sub d1..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I0-0 As d1"
  IL_0024:  ldstr      "1"
  IL_0029:  stloc.0
  IL_002a:  ldloca.s   V_0
  IL_002c:  callvirt   "Sub d1.Invoke(ByRef Object)"
  IL_0031:  ldloc.0
  IL_0032:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0037:  call       "Sub System.Console.WriteLine(Object)"
  IL_003c:  ret
}
]]>)

                verifier.VerifyIL("Program._Closure$__._Lambda$__0-0",
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

            Next
        End Sub

        <Fact()>
        Public Sub RestrictedTypes1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Delegate Sub d5(a1 As Object, a2 As ArgIterator)

Class Program
  Sub Main()
        Dim x1 As d5 = Sub(y1 As Object, y2 As ArgIterator)
                       End Sub

        Dim x2 As d5 = Sub(y1 As String, y2 As ArgIterator)
                       End Sub
  End Sub

    Sub Test123(y2 As ArgIterator)
        Dim x1 As Action(Of Object) = Function(y1 As Object) As ArgIterator
                                          Return y2 '1
                                      End Function '1

        Dim x2 As Action(Of Object) = Function(y1 As Object) As ArgIterator
                                      End Function '2

        Dim x3 As Action(Of Object) = Function(y1 As Object)
                                          Return y2 '3
                                      End Function '3

        Dim x4 As d6 = Function() Nothing
                       
    End Sub

End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(compilationDef,
            <![CDATA[
.class public auto ansi sealed d6
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(object TargetObject,
                               native int TargetMethod) runtime managed
  {
  } // end of method d6::.ctor

  .method public newslot strict virtual instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(int32 x,
                      class [mscorlib]System.AsyncCallback DelegateCallback,
                      object DelegateAsyncState) runtime managed
  {
  } // end of method d6::BeginInvoke

  .method public newslot strict virtual instance valuetype [mscorlib]System.ArgIterator 
          EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
  {
  } // end of method d6::EndInvoke

  .method public newslot strict virtual instance valuetype [mscorlib]System.ArgIterator 
          Invoke(int32 x) runtime managed
  {
  } // end of method d6::Invoke

} // end of class d6
]]>)

            'Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, OptionsExe)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
    <![CDATA[
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x2 As d5 = Sub(y1 As String, y2 As ArgIterator)
                                               ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x1 As Action(Of Object) = Function(y1 As Object) As ArgIterator
                                                                ~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x2 As Action(Of Object) = Function(y1 As Object) As ArgIterator
                                                                ~~~~~~~~~~~
BC42105: Function '<anonymous method>' doesn't return a value on all code paths. A null reference exception could occur at run time when the result is used.
                                      End Function '2
                                      ~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x3 As Action(Of Object) = Function(y1 As Object)
                                      ~~~~~~~~~~~~~~~~~~~~~~
BC31396: 'ArgIterator' cannot be made nullable, and cannot be used as the data type of an array element, field, anonymous type member, type argument, 'ByRef' parameter, or return statement.
        Dim x4 As d6 = Function() Nothing
                       ~~~~~~~~~~
]]>
</expected>)
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
10:     Test1(Function() Function(a As Object) 1)

20:     Test1(Function()
                  Return Function(a As Object) 1
              End Function)

30:     Test1(Function()
                  Return Function(a As Object)
                             Return 1
                         End Function
              End Function)

40:     Test1(Function()
                  Return Function(a As Object) As Integer
                             Return 1
                         End Function
              End Function)

50:     Test1(Function() As Func(Of Object, Integer)
                  Return Function(a As Object)
                             Return 1
                         End Function
              End Function)

        Test1(Function() AddressOf Test2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As Func(Of Func(Of Object, Integer)))': Not most specific.
    'Public Sub Test1(x As Func(Of Func(Of Integer, Integer)))': Not most specific.
10:     Test1(Function() Function(a As Object) 1)
        ~~~~~
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As Func(Of Func(Of Object, Integer)))': Not most specific.
    'Public Sub Test1(x As Func(Of Func(Of Integer, Integer)))': Not most specific.
20:     Test1(Function()
        ~~~~~
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As Func(Of Func(Of Object, Integer)))': Not most specific.
    'Public Sub Test1(x As Func(Of Func(Of Integer, Integer)))': Not most specific.
30:     Test1(Function()
        ~~~~~
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As Func(Of Func(Of Object, Integer)))': Not most specific.
    'Public Sub Test1(x As Func(Of Func(Of Integer, Integer)))': Not most specific.
40:     Test1(Function()
        ~~~~~
BC30521: Overload resolution failed because no accessible 'Test1' is most specific for these arguments:
    'Public Sub Test1(x As Func(Of Func(Of Object, Integer)))': Not most specific.
    'Public Sub Test1(x As Func(Of Func(Of Integer, Integer)))': Not most specific.
        Test1(Function() AddressOf Test2)
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
        Test1(Function(x As Object) 1)
        Test1(Function(x As Integer) 1)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

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
        Test2(Function(x As String) 1)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'Test2' can be called without a narrowing conversion:
    'Public Sub Test2(x As Func(Of Object, Integer))': Argument matching parameter 'x' narrows to 'Func(Of Object, Integer)'.
    'Public Sub Test2(x As Func(Of Integer, Integer))': Argument matching parameter 'x' narrows to 'Func(Of Integer, Integer)'.
        Test2(Function(x As String) 1)
        ~~~~~
</expected>)

            compilation = compilation.WithOptions(TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))
            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'Test2' can be called with these arguments:
    'Public Sub Test2(x As Func(Of Object, Integer))': Option Strict On disallows implicit conversions from 'Object' to 'String'.
    'Public Sub Test2(x As Func(Of Integer, Integer))': Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        Test2(Function(x As String) 1)
        ~~~~~
</expected>)
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
        Test3(Function() "a")
        Test4(Function() "a")
        Test5(Function() "a")

        Test3(Function()
                  Return "b"
              End Function)
        Test4(Function()
                  Return "b"
              End Function)
        Test5(Function()
                  Return "b"
              End Function)

        Test3(Function() As String
                  Return "b"
              End Function)
        Test4(Function() As String
                  Return "b"
              End Function)
        Test5(Function() As String
                  Return "b"
              End Function)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'String' to 'Integer'.
        Test5(Function() "a")
                         ~~~
BC42016: Implicit conversion from 'String' to 'Integer'.
                  Return "b"
                         ~~~
BC42016: Implicit conversion from 'String' to 'Integer'.
        Test5(Function() As String
              ~~~~~~~~~~~~~~~~~~~~~
</expected>)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Object]
System.Func`1[System.Object]
System.Func`1[System.Int32]
System.Func`1[System.Object]
System.Func`1[System.Object]
System.Func`1[System.Int32]
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
        Test6(Function() "a")
        Test7(Function() "a")
        Test8(Function() "a")

        Test6(Function()
                  Return "b"
              End Function)
        Test7(Function()
                  Return "b"
              End Function)
        Test8(Function()
                  Return "b"
              End Function)

        Test6(Function() As String
                  Return "b"
              End Function)
        Test7(Function() As String
                  Return "b"
              End Function)
        Test8(Function() As String
                  Return "b"
              End Function)


        Test9(Function() "a")
        Test10(Function() "a")
        Test11(Function() "a")

        Test9(Function()
                  Return "b"
              End Function)
        Test10(Function()
                   Return "b"
               End Function)
        Test11(Function()
                   Return "b"
               End Function)

        Test9(Function() As String
                  Return "c"
              End Function)
        Test10(Function() As String
                   Return "c"
               End Function)
        Test11(Function() As String
                   Return "c"
               End Function)

  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            CompileAndVerify(compilation, <![CDATA[
System.Func`1[System.Object]
VB$AnonymousDelegate_0`1[System.String]
System.Func`1[System.Object]
System.Func`1[System.Object]
VB$AnonymousDelegate_0`1[System.String]
System.Func`1[System.Object]
System.Func`1[System.Object]
VB$AnonymousDelegate_0`1[System.String]
System.Func`1[System.Object]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
System.Func`1[System.String]
]]>)
        End Sub

        <Fact()>
        Public Sub ArgumentIsVbOrBoxWidening5()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Module Program
  Sub Main()
        Dim val As Integer = 1
        Dim x1 As System.Action(Of Integer) = Sub(x As Object)
                                                  System.Console.WriteLine("{0}{1}",val,x)
                                              End Sub
        x1(2)
  End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            Assert.Equal(OptionStrict.Off, compilation.Options.OptionStrict)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:="12")

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)

            verifier.VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  newobj     "Sub Program._Closure$__0-0..ctor()"
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  stfld      "Program._Closure$__0-0.$VB$Local_val As Integer"
  IL_000c:  ldftn      "Sub Program._Closure$__0-0._Lambda$__R1(Integer)"
  IL_0012:  newobj     "Sub System.Action(Of Integer)..ctor(Object, System.IntPtr)"
  IL_0017:  ldc.i4.2
  IL_0018:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_001d:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__0-0._Lambda$__R1",
            <![CDATA[
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  box        "Integer"
  IL_0007:  call       "Sub Program._Closure$__0-0._Lambda$__0(Object)"
  IL_000c:  ret
}
]]>)

            verifier.VerifyIL("Program._Closure$__0-0._Lambda$__0",
            <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  3
  IL_0000:  ldstr      "{0}{1}"
  IL_0005:  ldarg.0
  IL_0006:  ldfld      "Program._Closure$__0-0.$VB$Local_val As Integer"
  IL_000b:  box        "Integer"
  IL_0010:  ldarg.1
  IL_0011:  call       "Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object"
  IL_0016:  call       "Sub System.Console.WriteLine(String, Object, Object)"
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub CustomModifiers1()

            Dim compilationDef =
<compilation name="LambdaTests1">
    <file name="a.vb">
Imports System        

Class Program
    Shared Sub Main()
        Dim d As TestCustomModifiers
        Dim a(-1) As Integer

        d = AddressOf F1
        d(a)

        d = Function(x)
                System.Console.WriteLine("L1")
                Return x
            End Function
        d(a)

        d = Function(x As Integer()) As Integer()
                System.Console.WriteLine("L2")
                Return x
            End Function
        d(a)
    End Sub

    Shared Function F1(x As Integer()) As Integer()
        System.Console.WriteLine("F1")
        Return x
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CreateCompilationWithCustomILSource(compilationDef,
            <![CDATA[
.class public auto ansi sealed TestCustomModifiers
       extends [mscorlib]System.MulticastDelegate
{
  .method public specialname rtspecialname 
          instance void  .ctor(object TargetObject,
                               native int TargetMethod) runtime managed
  {
  } // end of method TestCustomModifiers::.ctor

  .method public newslot strict virtual instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x,
                      class [mscorlib]System.AsyncCallback DelegateCallback,
                      object DelegateAsyncState) runtime managed
  {
  } // end of method TestCustomModifiers::BeginInvoke

  .method public newslot strict virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 
          EndInvoke(class [mscorlib]System.IAsyncResult DelegateAsyncResult) runtime managed
  {
  } // end of method TestCustomModifiers::EndInvoke

  .method public newslot strict virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) 
          Invoke(int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst) [] modopt([mscorlib]System.Runtime.CompilerServices.IsConst) x) runtime managed
  {
  } // end of method TestCustomModifiers::Invoke

} // end of class TestCustomModifiers
]]>.Value, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
</expected>)

            CompileAndVerify(compilation, expectedOutput:="F1" & Environment.NewLine & "L1" & Environment.NewLine & "L2")
        End Sub

        <WorkItem(543647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543647")>
        <Fact()>
        Public Sub BaseMethodParamIntegerDelegateParamShort()

            Dim compilationDef =
<compilation name="Test">
    <file name="a.vb">
Imports System

Delegate Function DelShort(ByVal s As Short) As String

MustInherit Class BaseClass
    Overridable Function MethodThatIsVirtual(ByVal i As Integer) As String
        Return "Base Method"
    End Function
End Class


Class DerivedClass
    Inherits BaseClass
    Overrides Function MethodThatIsVirtual(ByVal i As Integer) As String
        Return "Derived Method"
    End Function

    Function TC2() As String
        Dim dS As DelShort = AddressOf MyBase.MethodThatIsVirtual
        Return dS(1)
    End Function
End Class

Module Program
    Sub Main(args As String())
        Dim d = New DerivedClass()
        Console.WriteLine(d.TC2())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation, expectedOutput:="Base Method")
        End Sub

        <WorkItem(531532, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531532")>
        <Fact()>
        Public Sub Bug18258()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Runtime.CompilerServices

Friend Module Program
    <Extension>
    Sub Bar(Of T)(x As T)
        Dim d As New Action(Of Action(Of T))(Sub(y)
                                             End Sub)
        d.Invoke(AddressOf x.Bar)
    End Sub
    Sub Main()
    End Sub
End Module
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(compilationDef, {Net40.References.SystemCore}, TestOptions.ReleaseDll)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected></expected>)
            Dim verifier = CompileAndVerify(compilation)
        End Sub

        <WorkItem(1096576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1096576")>
        <Fact()>
        Public Sub Bug1096576()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Class Thing
    Sub Goo()
    End Sub
 
    Public t As New Thing
    Public tcb As AsyncCallback = AddressOf t.Goo
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(compilationDef, options:=TestOptions.DebugDll)

            Dim verifier = CompileAndVerify(compilation).VerifyDiagnostics()
        End Sub

    End Class
End Namespace
