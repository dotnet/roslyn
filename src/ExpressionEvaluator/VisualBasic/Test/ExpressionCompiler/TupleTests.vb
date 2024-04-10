' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.IO
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class TupleTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub Literal()
            Const source =
"Class C
    Shared Sub M()
        Dim o As (Integer, Integer)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, references:={ValueTupleRef, SystemRuntimeFacadeRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp, {MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    Dim result = context.CompileExpression(
                        "(A:=1, B:=2)",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        errorMessage,
                        testData)
                    Assert.Null(errorMessage)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = result.GetCustomTypeInfo(typeInfo)
                    Assert.NotNull(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal({"A", "B"}, tupleElementNames)
                    Dim methodData = testData.GetMethodData("<>x.<>m0")
                    Dim method = methodData.Method
                    Assert.True(method.ReturnType.IsTupleType)
                    CheckAttribute(result.Assembly, method, AttributeDescription.TupleElementNamesAttribute, expected:=True)
                    methodData.VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //o
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0007:  ret
}")
                End Sub)
        End Sub

        <Fact>
        Public Sub DuplicateValueTupleBetweenMscorlibAndLibrary()
            Const versionTemplate = "<Assembly: System.Reflection.AssemblyVersion(""{0}.0.0.0"")>"

            Const corlib_vb = "
Namespace System
    Public Class [Object]
    End Class
    Public Structure Void
    End Structure
    Public Class ValueType
    End Class
    Public Structure IntPtr
    End Structure
    Public Structure Int32
    End Structure
    Public Class [String]
    End Class
    Public Class Attribute
    End Class
End Namespace

Namespace System.Reflection
    Public Class AssemblyVersionAttribute
        Inherits Attribute

        Public Sub New(version As String)
        End Sub
    End Class
End Namespace
"

            Dim corlibWithoutVT = CreateEmptyCompilation({String.Format(versionTemplate, "1") + corlib_vb}, options:=TestOptions.DebugDll, assemblyName:="corlib")
            corlibWithoutVT.AssertTheseDiagnostics()
            Dim corlibWithoutVTRef = corlibWithoutVT.EmitToImageReference()

            Const valuetuple_vb As String = "
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
        End Sub
    End Structure
End Namespace
"

            Dim corlibWithVT = CreateEmptyCompilation({String.Format(versionTemplate, "2") + corlib_vb + valuetuple_vb}, options:=TestOptions.DebugDll, assemblyName:="corlib")
            corlibWithVT.AssertTheseDiagnostics()

            Const source As String =
"Class C
    Shared Function M() As (Integer, Integer)
        Dim o = (1, 2)
        Return o
    End Function
End Class"

            Dim app = CreateEmptyCompilation(source + valuetuple_vb, references:={corlibWithoutVTRef}, options:=TestOptions.DebugDll)
            app.AssertTheseDiagnostics()

            Dim runtime = CreateRuntimeInstance({app.ToModuleInstance(), corlibWithVT.ToModuleInstance()})
            ' Create EE context with app assembly (including ValueTuple) and a more recent corlib (also including ValueTuple)
            Dim evalContext = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()

            Dim compileResult = evalContext.CompileExpression("(1, 2)", errorMessage, testData)
            Assert.Null(errorMessage)

            Using block As ModuleMetadata = ModuleMetadata.CreateFromStream(New MemoryStream(compileResult.Assembly))
                Dim reader = block.MetadataReader

                Dim appRef = app.Assembly.Identity.Name
                AssertEx.SetEqual({"corlib 2.0", appRef + " 0.0"}, reader.DumpAssemblyReferences())

                AssertEx.SetEqual({"Object, System, AssemblyReference:corlib",
                    "ValueTuple`2, System, AssemblyReference:" + appRef}, ' ValueTuple comes from app, not corlib
                    reader.DumpTypeReferences())
            End Using
        End Sub

        <Fact>
        Public Sub TupleElementNamesAttribute_NotAvailable()
            Const source =
"Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
        Public Sub New(_1 As T1, _2 As T2)
            Item1 = _1
            Item2 = _2
        End Sub
    End Structure
End Namespace
Class C
    Shared Sub M()
        Dim o As (Integer, Integer)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    Dim result = context.CompileExpression(
                        "(A:=1, B:=2)",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        errorMessage,
                        testData)
                    Assert.Null(errorMessage)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = result.GetCustomTypeInfo(typeInfo)
                    Assert.Null(typeInfo)
                    Dim methodData = testData.GetMethodData("<>x.<>m0")
                    Dim method = methodData.Method
                    Assert.True(method.ReturnType.IsTupleType)
                    CheckAttribute(result.Assembly, method, AttributeDescription.TupleElementNamesAttribute, expected:=False)
                    methodData.VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //o
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  newobj     ""Sub System.ValueTuple(Of Integer, Integer)..ctor(Integer, Integer)""
  IL_0007:  ret
}")
                End Sub)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13948")>
        Public Sub Local()
            Const source =
"class C
{
    static void M()
    {
        (int A, int B) o = (1, 2);
    }
}"
            Dim comp = CreateCSharpCompilation(source, referencedAssemblies:={MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef})
            WithRuntimeInstance(comp, {MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(1, locals.Count)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = locals(0).GetCustomTypeInfo(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal({"A", "B"}, tupleElementNames)
                    Dim method = testData.Methods.Single().Value.Method
                    CheckAttribute(assembly, method, AttributeDescription.TupleElementNamesAttribute, expected:=True)
                    Assert.True(method.ReturnType.IsTupleType)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "o", expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (System.ValueTuple(Of Integer, Integer) V_0) //o
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13948")>
        Public Sub Constant()
            Const source =
"class A<T>
{
     internal class B<U>
    {
    }
}
class C
{
    static (object, object) F;
    static void M()
    {
        const A<(int, int A)>.B<(object B, object)>[] c = null;
    }
}"
            Dim comp = CreateCSharpCompilation(source, referencedAssemblies:={MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef})
            WithRuntimeInstance(comp, {MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim testData = New CompilationTestData()
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
                    Assert.Equal(1, locals.Count)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = locals(0).GetCustomTypeInfo(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal({Nothing, "A", "B", Nothing}, tupleElementNames)
                    Dim method = DirectCast(testData.Methods.Single().Value.Method, MethodSymbol)
                    CheckAttribute(assembly, method, AttributeDescription.TupleElementNamesAttribute, expected:=True)
                    Dim returnType = method.ReturnType
                    Assert.False(returnType.IsTupleType)
                    Assert.True(returnType.ContainsTuple())
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "c", expectedFlags:=DkmClrCompilationResultFlags.ReadOnlyResult, expectedILOpt:=
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13803")>
        Public Sub LongTupleLocalElement_NoNames()
            Const source =
"Class C
    Shared Sub M()
        Dim x = (1, 2, 3, 4, 5, 6, 7, 8)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, references:={SystemRuntimeFacadeRef, ValueTupleRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                {MscorlibRef, SystemCoreRef, SystemRuntimeFacadeRef, ValueTupleRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression(
                        "x.Item4 + x.Item8",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        errorMessage,
                        testData)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer)) V_0) //x
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer)).Item4 As Integer""
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer)).Rest As System.ValueTuple(Of Integer)""
  IL_000c:  ldfld      ""System.ValueTuple(Of Integer).Item1 As Integer""
  IL_0011:  add.ovf
  IL_0012:  ret
}")
                End Sub)
        End Sub

        <Fact>
        Public Sub LongTupleLocalElement_Names()
            Const source =
"Class C
    Shared Sub M()
        Dim x = (1, 2, Three:=3, Four:=4, 5, 6, 7, Eight:=8)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, references:={SystemRuntimeFacadeRef, ValueTupleRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                {MscorlibRef, SystemCoreRef, SystemRuntimeFacadeRef, ValueTupleRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression(
                        "x.Item8 + x.Eight",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        errorMessage,
                        testData)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer)) V_0) //x
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer)).Rest As System.ValueTuple(Of Integer)""
  IL_0006:  ldfld      ""System.ValueTuple(Of Integer).Item1 As Integer""
  IL_000b:  ldloc.0
  IL_000c:  ldfld      ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer)).Rest As System.ValueTuple(Of Integer)""
  IL_0011:  ldfld      ""System.ValueTuple(Of Integer).Item1 As Integer""
  IL_0016:  add.ovf
  IL_0017:  ret
}")
                End Sub)
        End Sub

        ''' <summary>
        ''' Locals declared in the VB EE do not have an explicit
        ''' type and are statically typed to Object, so tuple
        ''' element names on the value are not preserved.
        ''' </summary>
        <Fact>
        Public Sub DeclareLocal()
            Const source =
"Class C
    Shared Sub M()
        Dim x = (1, 2)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, references:={ValueTupleRef, SystemRuntimeFacadeRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp, references:={MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef},
                validator:=Sub(runtime)
                               Dim context = CreateMethodContext(runtime, "C.M")
                               Dim errorMessage As String = Nothing
                               Dim testData = New CompilationTestData()
                               Dim result = context.CompileExpression(
                                   "y = DirectCast(x, (A As Integer, B As Integer))",
                                   DkmEvaluationFlags.None,
                                   NoAliases,
                                   errorMessage,
                                   testData)
                               Assert.Null(errorMessage)
                               Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                               Dim typeInfoId = result.GetCustomTypeInfo(typeInfo)
                               Assert.Null(typeInfo)
                               Dim methodData = testData.GetMethodData("<>x.<>m0")
                               Dim method = methodData.Method
                               CheckAttribute(result.Assembly, method, AttributeDescription.TupleElementNamesAttribute, expected:=False)
                               methodData.VerifyIL(
           "{
  // Code size       48 (0x30)
  .maxstack  4
  .locals init (System.ValueTuple(Of Integer, Integer) V_0, //x
                System.Guid V_1)
  IL_0000:  ldtoken    ""Object""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ldstr      ""y""
  IL_000f:  ldloca.s   V_1
  IL_0011:  initobj    ""System.Guid""
  IL_0017:  ldloc.1
  IL_0018:  ldnull
  IL_0019:  call       ""Sub Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.CreateVariable(System.Type, String, System.Guid, Byte())""
  IL_001e:  ldstr      ""y""
  IL_0023:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetVariableAddress(Of Object)(String) As Object""
  IL_0028:  ldloc.0
  IL_0029:  box        ""System.ValueTuple(Of Integer, Integer)""
  IL_002e:  stind.ref
  IL_002f:  ret
}")
                           End Sub)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13589")>
        Public Sub [Alias]()
            Const source =
"Class C
    Shared F As (Integer, Integer)
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, references:={ValueTupleRef, SystemRuntimeFacadeRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp, {MscorlibRef, ValueTupleRef, SystemRuntimeFacadeRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                    Dim typeName As String = Nothing
                    Dim aliasElementNames = New ReadOnlyCollection(Of String)({"A", "B", Nothing, "D"})
                    Dim [alias] = New [Alias](
                        DkmClrAliasKind.Variable,
                        "t",
                        "t",
                        "System.ValueTuple`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.ValueTuple`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51]][], System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                        CustomTypeInfo.PayloadTypeId,
                        CustomTypeInfo.Encode(Nothing, aliasElementNames))
                    Dim diagnostics = DiagnosticBag.GetInstance()
                    Dim testData = New CompilationTestData()
                    Dim assembly = context.CompileGetLocals(
                        locals,
                        argumentsOnly:=False,
                        aliases:=ImmutableArray.Create([alias]),
                        diagnostics:=diagnostics,
                        typeName:=typeName,
                        testData:=testData)
                    diagnostics.Verify()
                    diagnostics.Free()
                    Assert.Equal(1, locals.Count)
                    Dim typeInfo As ReadOnlyCollection(Of Byte) = Nothing
                    Dim typeInfoId = locals(0).GetCustomTypeInfo(typeInfo)
                    Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
                    Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
                    CustomTypeInfo.Decode(typeInfoId, typeInfo, dynamicFlags, tupleElementNames)
                    Assert.Equal(aliasElementNames, tupleElementNames)
                    Dim method = testData.Methods.Single().Value.Method
                    CheckAttribute(assembly, method, AttributeDescription.TupleElementNamesAttribute, expected:=True)
                    Dim returnType = DirectCast(method.ReturnType, TypeSymbol)
                    Assert.False(returnType.IsTupleType)
                    Assert.True(DirectCast(returnType, ArrayTypeSymbol).ElementType.IsTupleType)
                    VerifyLocal(testData, typeName, locals(0), "<>m0", "t", expectedILOpt:=
"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldstr      ""t""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""System.ValueTuple(Of Integer, System.ValueTuple(Of Integer, Integer))()""
  IL_000f:  ret
}")
                    locals.Free()
                End Sub)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13803")>
        Public Sub AliasElement_NoNames()
            Const source =
"Class C
    Shared F As (Integer, Integer)
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib40({source}, references:={SystemRuntimeFacadeRef, ValueTupleRef}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                {MscorlibRef, SystemCoreRef, SystemRuntimeFacadeRef, ValueTupleRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")
                    Dim [alias] = New [Alias](
                        DkmClrAliasKind.Variable,
                        "x",
                        "x",
                        "System.ValueTuple`8[" +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                            "[System.ValueTuple`2[" +
                                "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]," +
                                "[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], " +
                            "System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51]], " +
                        "System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51",
                        Guid.Empty,
                        Nothing)
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression(
                        "x.Item4 + x.Item8",
                        DkmEvaluationFlags.TreatAsExpression,
                        ImmutableArray.Create([alias]),
                        errorMessage,
                        testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldstr      ""x""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  unbox.any  ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer, Integer))""
  IL_000f:  ldfld      ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer, Integer)).Item4 As Integer""
  IL_0014:  ldstr      ""x""
  IL_0019:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_001e:  unbox.any  ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer, Integer))""
  IL_0023:  ldfld      ""System.ValueTuple(Of Integer, Integer, Integer, Integer, Integer, Integer, Integer, System.ValueTuple(Of Integer, Integer)).Rest As System.ValueTuple(Of Integer, Integer)""
  IL_0028:  ldfld      ""System.ValueTuple(Of Integer, Integer).Item1 As Integer""
  IL_002d:  add.ovf
  IL_002e:  ret
}")
                End Sub)
        End Sub

    End Class

End Namespace
