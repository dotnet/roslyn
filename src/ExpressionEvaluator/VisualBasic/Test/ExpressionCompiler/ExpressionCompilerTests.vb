' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.DiaSymReader
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit
Imports CommonResources = Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests.Resources

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class ExpressionCompilerTests
        Inherits ExpressionCompilerTestBase

        Private Const s_simpleSource = "
Class C
    Shared Sub M()
    End Sub
End Class
"

        ''' <summary>
        ''' Each assembly should have a unique MVID and assembly name.
        ''' </summary>
        <WorkItem(1029280, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029280")>
        <Fact>
        Public Sub UniqueModuleVersionId()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)

            Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
            Dim moduleVersionId As Guid = Nothing
            Dim symReader As ISymUnmanagedReader = Nothing
            Dim methodToken = 0
            Dim localSignatureToken = 0
            GetContextState(runtime, "C.M", blocks, moduleVersionId, symReader, methodToken, localSignatureToken)
            Const methodVersion = 1

            Dim ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader)
            Dim context = EvaluationContext.CreateMethodContext(
                Nothing,
                blocks,
                MakeDummyLazyAssemblyReaders(),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken)

            Dim errorMessage As String = Nothing
            Dim result = context.CompileExpression("1", errorMessage)
            Dim mvid1 = result.Assembly.GetModuleVersionId()
            Dim name1 = result.Assembly.GetAssemblyName()
            Assert.NotEqual(mvid1, Guid.Empty)

            context = EvaluationContext.CreateMethodContext(
                New VisualBasicMetadataContext(blocks, context),
                blocks,
                MakeDummyLazyAssemblyReaders(),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion,
                ilOffset,
                localSignatureToken)

            result = context.CompileExpression("2", errorMessage)
            Dim mvid2 = result.Assembly.GetModuleVersionId()
            Dim name2 = result.Assembly.GetAssemblyName()
            Assert.NotEqual(mvid2, Guid.Empty)
            Assert.NotEqual(mvid2, mvid1)
            Assert.NotEqual(name2.FullName, name1.FullName)
        End Sub

        <Fact>
        Public Sub ParseError()
            Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim errorMessage As String = Nothing
            Dim result = context.CompileExpression("M(", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Null(result)
            Assert.Equal("error BC30201: Expression expected.", errorMessage)
        End Sub

        ''' <summary>
        ''' Diagnostics should be formatted with the CurrentUICulture.
        ''' </summary>
        <WorkItem(941599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/941599")>
        <Fact>
        Public Sub FormatterCultureInfo()
            Dim previousCulture = Thread.CurrentThread.CurrentCulture
            Dim previousUICulture = Thread.CurrentThread.CurrentUICulture
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR")
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE")
            Try
                Dim comp = CreateCompilationWithMscorlib({s_simpleSource}, options:=TestOptions.DebugDll)
                Dim runtime = CreateRuntimeInstance(comp)
                Dim context = CreateMethodContext(
                    runtime,
                    "C.M")
                Dim resultProperties As ResultProperties = Nothing
                Dim errorMessage As String = Nothing
                Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
                Dim result = context.CompileExpression(
                    "M(",
                    DkmEvaluationFlags.TreatAsExpression,
                    NoAliases,
                    CustomDiagnosticFormatter.Instance,
                    resultProperties,
                    errorMessage,
                    missingAssemblyIdentities,
                    preferredUICulture:=Nothing,
                    testData:=Nothing)
                Assert.Empty(missingAssemblyIdentities)
                Assert.Null(result)
                Assert.Equal("LCID=1031, Code=30201", errorMessage)
            Finally
                Thread.CurrentThread.CurrentUICulture = previousUICulture
                Thread.CurrentThread.CurrentCulture = previousCulture
            End Try
        End Sub

        <Fact>
        Public Sub BindingError()
            Const source = "
Class C
    Shared Sub M(o As Object)
        Dim a As Object() = {}
        For Each x In a
            M(x)
        Next
        For Each y In a
#ExternalSource(""test"", 999)
            M(y)
#End ExternalSource
        Next
    End Sub
End Class
"
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                atLineNumber:=999,
                expr:="If(y, x)",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)
        End Sub

        <Fact>
        Public Sub EmitError()
            Const source = "
Class C
    Shared Sub M(o As Object)
    End Sub
End Class
"
            Dim longName = New String("P"c, 1100)
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:=String.Format("New With {{ .{0} = o }}", longName),
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal(String.Format("error BC37220: Name '${0}' exceeds the maximum length allowed in metadata.", longName), errorMessage)
        End Sub

        <Fact>
        Public Sub NoSymbols()
            Const source = "
Class C
    Shared Function F(o As Object) As Object
        Return o
    End Function
    Shared Sub M(x As Integer)
        Dim y = x + 1
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, debugFormat:=Nothing)
            For Each moduleInstance In runtime.Modules
                Assert.Null(moduleInstance.SymReader)
            Next

            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")

            ' Local reference.
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("F(y)", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC30451: 'y' is not declared. It may be inaccessible due to its protection level.", errorMessage)

            ' No local reference.
            testData = New CompilationTestData()
            result = context.CompileExpression("F(x)", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            ' Unlike C#, VB doesn't create a temp local to store the return value.
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  box        ""Integer""
  IL_0006:  call       ""Function C.F(Object) As Object""
  IL_000b:  ret
}")
        End Sub

        ''' <summary>
        ''' Reuse Compilation if references match, and reuse entire
        ''' EvaluationContext if references and local scopes match.
        ''' </summary>
        <Fact>
        Public Sub ReuseEvaluationContext()
            Const sourceA =
"Public Interface I
End Interface"
            Const sourceB =
"Class C
    Shared Sub F(o As I)
        Dim x As Object = 1
        If o Is Nothing Then
            Dim y As Object = 2
            y = x
        Else
            Dim z As Object
        End If
        x = 3
    End Sub
    Shared Sub G()
    End Sub
End Class"
            Dim compA = CreateCompilationWithMscorlib({sourceA}, options:=TestOptions.DebugDll)
            Dim referenceA = compA.EmitToImageReference()
            Dim compB = CreateCompilationWithMscorlib({sourceB}, options:=TestOptions.DebugDll, references:={referenceA})
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            compB.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Const methodVersion = 1

            Dim previous As VisualBasicMetadataContext = Nothing
            Dim startOffset = 0
            Dim endOffset = 0
            Dim runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, SymReaderFactory.CreateReader(pdbBytes))
            Dim typeBlocks As ImmutableArray(Of MetadataBlock) = Nothing
            Dim methodBlocks As ImmutableArray(Of MetadataBlock) = Nothing
            Dim moduleVersionId As Guid = Nothing
            Dim symReader As ISymUnmanagedReader = Nothing
            Dim typeToken = 0
            Dim methodToken = 0
            Dim localSignatureToken = 0
            GetContextState(runtime, "C", typeBlocks, moduleVersionId, symReader, typeToken, localSignatureToken)
            GetContextState(runtime, "C.F", methodBlocks, moduleVersionId, symReader, methodToken, localSignatureToken)

            ' Get non-empty scopes.
            Dim scopes = symReader.GetScopes(methodToken, methodVersion, EvaluationContext.IsLocalScopeEndInclusive).WhereAsArray(Function(s) s.Locals.Length > 0)
            Assert.True(scopes.Length >= 3)
            Dim outerScope = scopes.First(Function(s) s.Locals.Contains("x"))

            startOffset = outerScope.StartOffset
            endOffset = outerScope.EndOffset

            ' At start of outer scope.
            Dim context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, CType(startOffset, UInteger), localSignatureToken)
            Assert.Equal(Nothing, previous)
            previous = New VisualBasicMetadataContext(methodBlocks, context)

            ' At end of outer scope - not reused because of the nested scope.
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, CType(endOffset, UInteger), localSignatureToken)
            Assert.NotEqual(context, previous.EvaluationContext) ' Not required, just documentary.

            ' At type context.
            context = EvaluationContext.CreateTypeContext(previous, typeBlocks, moduleVersionId, typeToken)
            Assert.NotEqual(context, previous.EvaluationContext)
            Assert.Null(context.MethodContextReuseConstraints)
            Assert.Equal(context.Compilation, previous.Compilation)

            ' Step through entire method.
            Dim previousScope As Scope = Nothing
            previous = New VisualBasicMetadataContext(typeBlocks, context)
            For offset = startOffset To endOffset - 1
                Dim scope = scopes.GetInnermostScope(offset)
                Dim constraints = previous.EvaluationContext.MethodContextReuseConstraints
                If constraints.HasValue Then
                    Assert.Equal(scope Is previousScope, constraints.GetValueOrDefault().AreSatisfied(moduleVersionId, methodToken, methodVersion, offset))
                End If

                context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, CType(offset, UInteger), localSignatureToken)
                If scope Is previousScope Then
                    Assert.Equal(context, previous.EvaluationContext)
                Else
                    ' Different scope. Should reuse compilation.
                    Assert.NotEqual(context, previous.EvaluationContext)
                    If previous.EvaluationContext IsNot Nothing Then
                        Assert.NotEqual(context.MethodContextReuseConstraints, previous.EvaluationContext.MethodContextReuseConstraints)
                        Assert.Equal(context.Compilation, previous.Compilation)
                    End If
                End If
                previousScope = scope
                previous = New VisualBasicMetadataContext(methodBlocks, context)
            Next

            ' With different references.
            Dim fewerReferences = references.Remove(referenceA)
            Assert.Equal(fewerReferences.Length, references.Length - 1)
            runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), fewerReferences, exeBytes, SymReaderFactory.CreateReader(pdbBytes))
            methodBlocks = Nothing
            moduleVersionId = Nothing
            symReader = Nothing
            methodToken = 0
            localSignatureToken = 0
            GetContextState(runtime, "C.F", methodBlocks, moduleVersionId, symReader, methodToken, localSignatureToken)

            ' Different references. No reuse.
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, CType(endOffset - 1, UInteger), localSignatureToken)
            Assert.NotEqual(context, previous.EvaluationContext)
            Assert.True(previous.EvaluationContext.MethodContextReuseConstraints.Value.AreSatisfied(moduleVersionId, methodToken, methodVersion, endOffset - 1))
            Assert.NotEqual(context.Compilation, previous.Compilation)
            previous = New VisualBasicMetadataContext(methodBlocks, context)

            ' Different method. Should reuse Compilation.
            GetContextState(runtime, "C.G", methodBlocks, moduleVersionId, symReader, methodToken, localSignatureToken)
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, ilOffset:=0, localSignatureToken:=localSignatureToken)
            Assert.NotEqual(context, previous.EvaluationContext)
            Assert.False(previous.EvaluationContext.MethodContextReuseConstraints.Value.AreSatisfied(moduleVersionId, methodToken, methodVersion, 0))
            Assert.Equal(context.Compilation, previous.Compilation)

            ' No EvaluationContext. Should reuse Compilation
            previous = New VisualBasicMetadataContext(previous.MetadataBlocks, previous.Compilation)
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, ilOffset:=0, localSignatureToken:=localSignatureToken)
            Assert.Null(previous.EvaluationContext)
            Assert.NotNull(context)
            Assert.Equal(context.Compilation, previous.Compilation)
        End Sub

        <Fact>
        Public Sub EvaluateLocal()
            Const source = "
Class C
    Shared Sub M()
        Dim x As Integer = 1
        Dim y As Integer = 2
    End Sub
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="x")
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //x
  Integer V_1) //y
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="y")
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //x
  Integer V_1) //y
  IL_0000:  ldloc.1
  IL_0001:  ret
}")
        End Sub

        <Fact>
        Public Sub FormatSpecifiers()
            Const source = "
Class C
    Shared Function F(x As String, y As String) As Object
        Return x
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.F")
            Dim errorMessage As String = Nothing

            ' No format specifiers.
            Dim result = context.CompileExpression("x", errorMessage)
            CheckFormatSpecifiers(result)

            ' Format specifiers on expression.
            result = context.CompileExpression("x,", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,,", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x y", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'y' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x yy zz", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'yy' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,,y", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,yy,zz,ww", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            CheckFormatSpecifiers(result, "yy", "zz", "ww")
            result = context.CompileExpression("x, y z", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'z' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x, y  ,  z  ", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            CheckFormatSpecifiers(result, "y", "z")
            result = context.CompileExpression("x, y, z,", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,y,z;w", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'z;w' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x, y;, z", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'y;' is not a valid format specifier", errorMessage)

            ' Format specifiers after comment (ignored).
            result = context.CompileExpression("x ' ,f", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            CheckFormatSpecifiers(result)

            ' Format specifiers on assignment value.
            result = context.CompileAssignment("x", "Nothing, y", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Null(result)
            Assert.Equal("error BC30035: Syntax error.", errorMessage)

            ' Format specifiers, no expression.
            result = context.CompileExpression(",f", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC30201: Expression expected.", errorMessage)
        End Sub

        Private Shared Sub CheckFormatSpecifiers(result As CompileResult, ParamArray formatSpecifiers As String())
            Assert.NotNull(result.Assembly)
            If formatSpecifiers.Length = 0 Then
                Assert.Null(result.FormatSpecifiers)
            Else
                AssertEx.Equal(formatSpecifiers, result.FormatSpecifiers)
            End If
        End Sub

        ''' <summary>
        ''' Locals in the generated method should account for temporary slots
        ''' in the original method.  Also, some temporaries may not be included
        ''' in any scope.
        ''' </summary>
        <Fact>
        Public Sub IncludeTemporarySlots()
            Const source = "
Class C
    Shared Function F(a As Integer()) As String
        SyncLock New C()
#ExternalSource(""test"", 999)
            Dim s As String = a(0).ToString()
            Return s
#End ExternalSource
        End SyncLock
    End Function
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.F", atLineNumber:=999)
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("a(0)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (String V_0, //F
                Object V_1,
                Boolean V_2,
                String V_3) //s
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  ldelem.i4
  IL_0003:  ret
}")
        End Sub

        <Fact>
        Public Sub EvaluateMe()
            Const source = "
Class A
    Friend Overridable Function F() As String
        Return Nothing
    End Function

    Friend G As String

    Friend Overridable ReadOnly Property P As String
        Get
            Return Nothing
        End Get
    End Property
End Class

Class B
    Inherits A

    Friend Overrides Function F() As String
        Return Nothing
    End Function

    Friend Shadows G As String

    Friend Overrides ReadOnly Property P As String
        Get
            Return Nothing
        End Get
    End Property

    Overloads Shared Function F(f1 As System.Func(Of String)) As String
        Return Nothing
    End Function

    Sub M()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "B.M")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            Dim result = context.CompileExpression("If(Me.F(), If(Me.G, Me.P))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""Function B.F() As String""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001a
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""B.G As String""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  pop
  IL_0014:  ldarg.0
  IL_0015:  callvirt   ""Function B.get_P() As String""
  IL_001a:  ret
}
")

            testData = New CompilationTestData()
            result = context.CompileExpression("F(AddressOf Me.F)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""Function B.F() As String""
  IL_0008:  newobj     ""Sub System.Func(Of String)..ctor(Object, System.IntPtr)""
  IL_000d:  call       ""Function B.F(System.Func(Of String)) As String""
  IL_0012:  ret
}
")

            testData = New CompilationTestData()
            result = context.CompileExpression("F(new System.Func(Of String)(AddressOf Me.F))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldvirtftn  ""Function B.F() As String""
  IL_0008:  newobj     ""Sub System.Func(Of String)..ctor(Object, System.IntPtr)""
  IL_000d:  call       ""Function B.F(System.Func(Of String)) As String""
  IL_0012:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateMyClass()
            Const source = "
Class A
    Friend Overridable Function F() As String
        Return Nothing
    End Function

    Friend G As String

    Friend Overridable ReadOnly Property P As String
        Get
            Return Nothing
        End Get
    End Property
End Class

Class B
    Inherits A

    Friend Overrides Function F() As String
        Return Nothing
    End Function

    Friend Shadows G As String

    Friend Overrides ReadOnly Property P As String
        Get
            Return Nothing
        End Get
    End Property

    Overloads Shared Function F(f1 As System.Func(Of String)) As String
        Return Nothing
    End Function

    Sub M()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "B.M")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            Dim result = context.CompileExpression("If(MyClass.F(), If(MyClass.G, MyClass.P))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""Function B.F() As String""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001a
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""B.G As String""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  pop
  IL_0014:  ldarg.0
  IL_0015:  call       ""Function B.get_P() As String""
  IL_001a:  ret
}
")

            testData = New CompilationTestData()
            result = context.CompileExpression("F(AddressOf MyClass.F)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""Function B.F() As String""
  IL_0007:  newobj     ""Sub System.Func(Of String)..ctor(Object, System.IntPtr)""
  IL_000c:  call       ""Function B.F(System.Func(Of String)) As String""
  IL_0011:  ret
}
")

            testData = New CompilationTestData()
            result = context.CompileExpression("F(new System.Func(Of String)(AddressOf MyClass.F))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""Function B.F() As String""
  IL_0007:  newobj     ""Sub System.Func(Of String)..ctor(Object, System.IntPtr)""
  IL_000c:  call       ""Function B.F(System.Func(Of String)) As String""
  IL_0011:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateMyBase()
            Const source = "
Class A
    Friend Overridable Function F() As String
        Return Nothing
    End Function

    Friend G As String

    Friend Overridable ReadOnly Property P As String
        Get
            Return Nothing
        End Get
    End Property
End Class

Class B
    Inherits A

    Friend Overrides Function F() As String
        Return Nothing
    End Function

    Friend Shadows G As String

    Friend Overrides ReadOnly Property P As String
        Get
            Return Nothing
        End Get
    End Property

    Overloads Shared Function F(f1 As System.Func(Of String)) As String
        Return Nothing
    End Function

    Sub M()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "B.M")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            Dim result = context.CompileExpression("If(MyBase.F(), If(MyBase.G, MyBase.P))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""Function A.F() As String""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001a
  IL_0009:  pop
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""A.G As String""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  pop
  IL_0014:  ldarg.0
  IL_0015:  call       ""Function A.get_P() As String""
  IL_001a:  ret
}
")

            testData = New CompilationTestData()
            result = context.CompileExpression("F(AddressOf MyBase.F)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""Function A.F() As String""
  IL_0007:  newobj     ""Sub System.Func(Of String)..ctor(Object, System.IntPtr)""
  IL_000c:  call       ""Function B.F(System.Func(Of String)) As String""
  IL_0011:  ret
}
")

            testData = New CompilationTestData()
            result = context.CompileExpression("F(new System.Func(Of String)(AddressOf MyBase.F))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""Function A.F() As String""
  IL_0007:  newobj     ""Sub System.Func(Of String)..ctor(Object, System.IntPtr)""
  IL_000c:  call       ""Function B.F(System.Func(Of String)) As String""
  IL_0011:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateStructureMe()
            Const source = "
Structure S
    Shared Function F(x As Object, y As Object) As Object
        Return Nothing
    End Function

    Private x As Object
    
    Sub M()
    End Sub
End Structure
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="S.M",
                expr:="F(Me, Me.x)")
            Dim methodData = testData.GetMethodData("<>x.<>m0(ByRef S)")
            Dim parameter = DirectCast(methodData.Method, MethodSymbol).Parameters.Single()
            Assert.True(parameter.IsByRef)
            methodData.VerifyIL("
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""S""
  IL_0006:  box        ""S""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""S.x As Object""
  IL_0011:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0016:  call       ""Function S.F(Object, Object) As Object""
  IL_001b:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateSharedMethodParameters()
            Const source = "
Class C
    Shared Function F(x As Integer, y As Integer) As Object
        Return x + y
    End Function
    Shared Sub M(x As Integer, y As Integer)
    End Sub
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="F(y, x)")
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  call       ""Function C.F(Integer, Integer) As Object""
  IL_0007:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateInstanceMethodParametersAndLocals()
            Const source = "
Class C
    Function F(x As Integer) As Object
        Return x
    End Function

    Sub M(x As Integer)
        Dim y As Integer = 1
    End Sub
End Class
"

            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="F(x + y)")
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       10 (0xa)
  .maxstack  3
  .locals init (Integer V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldloc.0
  IL_0003:  add.ovf
  IL_0004:  callvirt   ""Function C.F(Integer) As Object""
  IL_0009:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateLocals()
            Dim source = "
Class C
    Shared Sub M()
        Dim x As Integer = 1
        If x < 0 Then
            dim y As Integer = 2
        Else
#ExternalSource(""test"", 999)
            dim z As Integer = 3
#End ExternalSource
        End if
    End Sub
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                atLineNumber:=999,
                expr:="x + z")
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (Integer V_0, //x
  Boolean V_1,
  Integer V_2,
  Integer V_3) //z
  IL_0000:  ldloc.0
  IL_0001:  ldloc.3
  IL_0002:  add.ovf
  IL_0003:  ret
}")
        End Sub

        <Fact>
        Public Sub EvaluateForEachLocal()
            Const source = "
Class C
    Shared Function F(args As Object()) As Boolean
        If args Is Nothing Then
            Return True
        End If
        For Each o In args
#ExternalSource(""test"", 999)
            System.Console.WriteLine() ' Force non-hidden sequence point.
#End ExternalSource
        Next
        Return False
    End Function
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.F",
                atLineNumber:=999,
                expr:="o")
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (Boolean V_0, //F
                Boolean V_1,
                Object() V_2,
                Integer V_3,
                Object V_4, //o
                Boolean V_5)
  IL_0000:  ldloc.s    V_4
  IL_0002:  ret
}")
        End Sub

        ''' <summary>
        ''' Generated "Me" parameter should not conflict with existing "[Me]" parameter.
        ''' </summary>
        <Fact>
        Public Sub ParameterNamedMe()
            Const source = "
Class C
    Function M([Me] As C) As Object
        Return Nothing
    End Function
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="[Me].M(Me)")
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (Object V_0) //M
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""Function C.M(C) As Object""
  IL_0007:  ret
}")
        End Sub

        ''' <summary>
        ''' Generated "Me" parameter should not conflict with existing "[Me]" local.
        ''' </summary>
        <Fact>
        Public Sub LocalNamedMe()
            Const source = "
Class C
    Function M(o As Object) As Object
        Dim [Me] = Me
        Return Nothing
    End Function
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="[Me].M(Me)")
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  2
  .locals init (Object V_0, //M
  C V_1) //Me
  IL_0000:  ldloc.1
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""Function C.M(Object) As Object""
  IL_0007:  ret
}")
        End Sub

        <Fact>
        Public Sub ByRefParameter()
            Const source = "
Class C
    Shared Function M(<System.Runtime.InteropServices.OutAttribute> ByRef x As Object) As Object
        Dim y As Object
        x = Nothing
        Return Nothing
    End Function
End Class
"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="M(y)")
            Dim parameter = testData.GetMethodData("<>x.<>m0(ByRef Object)").Method.Parameters.Single()
            Assert.Equal(RefKind.Ref, parameter.RefKind)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (Object V_0, //M
                Object V_1) //y
  IL_0000:  ldloca.s   V_1
  IL_0002:  call       ""Function C.M(ByRef Object) As Object""
  IL_0007:  ret
}")
        End Sub

        ''' <summary>
        ''' Method defined in IL where PDB does not contain VB custom metadata.
        ''' </summary>
        <Fact>
        Public Sub LocalType_FromIL()
            Const il = "
.class public C
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .field public object F;
  .method public static void M()
  {
    .locals init ([0] class C c1)
    ret
  }
}
"

            Dim exeBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            EmitILToArray(il, appendDefaultHeader:=True, includePdb:=True, assemblyBytes:=exeBytes, pdbBytes:=pdbBytes)

            Dim runtime = CreateRuntimeInstance(
                    assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName(),
                    references:=ImmutableArray.Create(MscorlibRef),
                    exeBytes:=exeBytes.ToArray(),
                    symReader:=SymReaderFactory.CreateReader(pdbBytes))

            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("c1.F", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C V_0) //c1
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C.F As Object""
  IL_0006:  ret
}
")
        End Sub

        ''' <summary>
        ''' Allow locals with optional custom modifiers.
        ''' </summary>
        ''' <remarks>
        ''' The custom modifiers are not copied to the corresponding local
        ''' in the generated method since there is no need.
        ''' </remarks>
        <WorkItem(884627, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/884627")>
        <Fact>
        Public Sub LocalType_CustomModifiers()
            Const il = "
.class public C
{
  .method public specialname rtspecialname instance void .ctor()
  {
    ret
  }
  .field public object F;
  .method public static void M()
  {
    .locals init ([0] class C modopt(int32) modopt(object) c1)
    ret
  }
}
"

            Dim exeBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            EmitILToArray(il, appendDefaultHeader:=True, includePdb:=True, assemblyBytes:=exeBytes, pdbBytes:=pdbBytes)

            Dim runtime = CreateRuntimeInstance(
                    assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName(),
                    references:=ImmutableArray.Create(MscorlibRef),
                    exeBytes:=exeBytes.ToArray(),
                    symReader:=SymReaderFactory.CreateReader(pdbBytes))

            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("c1.F", errorMessage, testData)
            Assert.Null(errorMessage)

            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Dim locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder()
            Dim local = locals.Single()
            Assert.Equal("C", local.Type.ToString())
            Assert.Equal(0, local.CustomModifiers.Length) ' Custom modifiers are not copied
            methodData.VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (C V_0) //c1
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""C.F As Object""
  IL_0006:  ret
}
")
        End Sub

        <WorkItem(1012956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1012956")>
        <Fact>
        Public Sub LocalType_ByRefOrPinned()
            Const il = "
.class private auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .method private hidebysig static void  M(string s, int32[] a) cil managed
  {
    // Code size       73 (0x49)
    .maxstack  2
    .locals init ([0] string pinned s,
                  [1] int32& pinned f,
                  [2] int32& i)
    ret
  }
}
"

            Dim exeBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            EmitILToArray(il, appendDefaultHeader:=True, includePdb:=True, assemblyBytes:=exeBytes, pdbBytes:=pdbBytes)

            Dim runtime = CreateRuntimeInstance(
                    assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName(),
                    references:=ImmutableArray.Create(MscorlibRef),
                    exeBytes:=exeBytes.ToArray(),
                    symReader:=SymReaderFactory.CreateReader(pdbBytes))

            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim errorMessage As String = Nothing

            Dim testData = New CompilationTestData()
            context.CompileExpression("s", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (pinned String V_0, //s
  pinned Integer& V_1, //f
  Integer& V_2) //i
  IL_0000:  ldloc.0
  IL_0001:  conv.i
  IL_0002:  ret
}")

            testData = New CompilationTestData()
            context.CompileAssignment("s", """hello""", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (pinned String V_0, //s
                pinned Integer& V_1, //f
                Integer& V_2) //i
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.0
  IL_0006:  ret
}")

            testData = New CompilationTestData()
            context.CompileExpression("f", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (pinned String V_0, //s
  pinned Integer& V_1, //f
  Integer& V_2) //i
  IL_0000:  ldloc.1
  IL_0001:  conv.i
  IL_0002:  ldind.i4
  IL_0003:  ret
}")

            testData = New CompilationTestData()
            context.CompileAssignment("f", "1", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        5 (0x5)
  .maxstack  2
  .locals init (pinned String V_0, //s
  pinned Integer& V_1, //f
  Integer& V_2) //i
  IL_0000:  ldloc.1
  IL_0001:  conv.i
  IL_0002:  ldc.i4.1
  IL_0003:  stind.i4
  IL_0004:  ret
}")

            testData = New CompilationTestData()
            context.CompileExpression("i", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (pinned String V_0, //s
  pinned Integer& V_1, //f
  Integer& V_2) //i
  IL_0000:  ldloc.2
  IL_0001:  ldind.i4
  IL_0002:  ret
}")

            testData = New CompilationTestData()
            context.CompileAssignment("i", "1", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (pinned String V_0, //s
  pinned Integer& V_1, //f
  Integer& V_2) //i
  IL_0000:  ldloc.2
  IL_0001:  ldc.i4.1
  IL_0002:  stind.i4
  IL_0003:  ret
}")
        End Sub

        <WorkItem(1034549, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1034549")>
        <Fact>
        Public Sub AssignLocal()
            Const source =
"Class C
    Shared Sub M()
        Dim x = 0
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileAssignment("x", "1", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        3 (0x3)
  .maxstack  1
  .locals init (Integer V_0) //x
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ret
}")
        End Sub

        <Fact>
        Public Sub AssignInstanceMethodParametersAndLocals()
            Const source = "
Class C
    Private a As Object()
    
    Shared Function F(x As Integer) As Integer
        Return x
    End Function

    Sub M(x As Integer)
        Dim y As Integer
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileAssignment("Me.a(F(x))", "Me.a(y)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       27 (0x1b)
  .maxstack  4
  .locals init (Integer V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.a As Object()""
  IL_0006:  ldarg.1
  IL_0007:  call       ""Function C.F(Integer) As Integer""
  IL_000c:  ldarg.0
  IL_000d:  ldfld      ""C.a As Object()""
  IL_0012:  ldloc.0
  IL_0013:  ldelem.ref
  IL_0014:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0019:  stelem.ref
  IL_001a:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateNothing()
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                s_simpleSource,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="Nothing",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal(DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags)
            Dim method = testData.GetMethodData("<>x.<>m0").Method
            Assert.Equal(SpecialType.System_Object, method.ReturnType.SpecialType)
            Assert.False(method.ReturnsVoid)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}
")
        End Sub

        <Fact>
        Public Sub MayHaveSideEffects()
            Const source =
"Imports System
Imports System.Diagnostics.Contracts
Imports Microsoft.VisualBasic
Class C
    Function F() As Object
        Return 1
    End Function
    <Pure>
    Function G() As Object
        Return 2
    End Function
    Property P As Object
    Shared Function H() As Object
        Return 3
    End Function
    Default Public ReadOnly Property Item(ByVal str As String) As String
        Get
            Return Format(""No!"")
        End Get
    End Property
    Shared Sub M(o As C, i As Integer, a As Action, obj As Object)
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime({Parse(source)}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            CheckResultProperties(context, "o.F()", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            ' Calls to methods are reported as having side effects, even if
            ' the method is marked <Pure>. This matches the native EE.
            CheckResultProperties(context, "o.G()", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "o.P", DkmClrCompilationResultFlags.None)
            CheckResultProperties(context, "If(a, Sub() i = 1)", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "If(a, Sub() i += 2)", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "If(a, Sub() i *= 3)", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "If(a, Sub() i -= 4)", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "New C() With {.P = 1}", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "New C() With {.P = H()}", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "obj(""Test"")", DkmClrCompilationResultFlags.PotentialSideEffect)
            CheckResultProperties(context, "obj.Item(""Test"")", DkmClrCompilationResultFlags.PotentialSideEffect)
        End Sub

        <Fact>
        Public Sub IsAssignable()
            Const source = "
Imports System

Class C
    Public F As Integer
    Public ReadOnly RF As Integer
    Public Const CF As Integer = 1

    Public Event E()

    Public Custom Event CE As Action
        AddHandler(value As Action)
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event

    Public ReadOnly Property RP As Integer
        Get
            Return 0
        End Get
    End Property
    Public WriteOnly Property WP As Integer
        Set(value As Integer)
        End Set
    End Property
    Public Property RWP As Integer

    Public ReadOnly Property RP(x As Integer) As Integer
        Get
            Return 0
        End Get
    End Property
    Public WriteOnly Property WP(x As Integer) As Integer
        Set(value As Integer)
        End Set
    End Property
    Public Property RWP(x As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property

    Public Function M() As Integer
        Return 0
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                    runtime,
                    methodName:="C.M")

            CheckResultProperties(context, "F", DkmClrCompilationResultFlags.None)
            CheckResultProperties(context, "RF", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "CF", DkmClrCompilationResultFlags.ReadOnlyResult)

            ' Note: flags are always None in error cases.

            CheckResultProperties(context, "E", DkmClrCompilationResultFlags.None, "error BC32022: 'Public Event E As C.EEventHandler' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.")
            CheckResultProperties(context, "CE", DkmClrCompilationResultFlags.None, "error BC32022: 'Public Event CE As Action' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.")

            CheckResultProperties(context, "RP", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "WP", DkmClrCompilationResultFlags.None, "error BC30524: Property 'WP' is 'WriteOnly'.")
            CheckResultProperties(context, "RWP", DkmClrCompilationResultFlags.None)

            CheckResultProperties(context, "RP(1)", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "WP(1)", DkmClrCompilationResultFlags.None, "error BC30524: Property 'WP' is 'WriteOnly'.")
            CheckResultProperties(context, "RWP(1)", DkmClrCompilationResultFlags.None)

            CheckResultProperties(context, "M()", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)

            CheckResultProperties(context, "Nothing", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "1", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "AddressOf M", DkmClrCompilationResultFlags.None, "error BC30491: Expression does not produce a value.")
            CheckResultProperties(context, "GetType(C)", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "New C()", DkmClrCompilationResultFlags.ReadOnlyResult)
        End Sub

        <Fact>
        Public Sub IsAssignable_Array()
            Const source = "
Imports System

Class C
    Public ReadOnly RF As Integer()

    Private Readonly _rp As Integer()
    Public ReadOnly Property RP As Integer()
        Get
            Return _rp
        End Get
    End Property

    Private ReadOnly _m As Integer()
    Public Function M() As Integer()
        Return _m
    End Function
End Class
"
            Dim compilation0 = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                    runtime,
                    methodName:="C.M")

            CheckResultProperties(context, "RF", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "RF(0)", DkmClrCompilationResultFlags.None)

            CheckResultProperties(context, "RP", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "RP(0)", DkmClrCompilationResultFlags.None)

            CheckResultProperties(context, "M()", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "M()(0)", DkmClrCompilationResultFlags.PotentialSideEffect)
        End Sub

        Private Shared Sub CheckResultProperties(context As EvaluationContext, expr As String, expectedFlags As DkmClrCompilationResultFlags, Optional expectedErrorMessage As String = Nothing)
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression(expr, resultProperties, errorMessage, testData)
            Assert.Equal(expectedErrorMessage, errorMessage)
            Assert.NotEqual(expectedErrorMessage Is Nothing, result Is Nothing)
            Assert.Equal(expectedFlags, resultProperties.Flags)
        End Sub

        ''' <summary>
        ''' Set BooleanResult for bool expressions.
        ''' </summary>
        <Fact>
        Public Sub EvaluateBooleanExpression()
            Dim source =
"Class C
    Shared Function F() As Boolean
        Return False
    End Function
    Shared Sub M(x As Boolean, y As Boolean?)
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            context.CompileExpression("x", resultProperties, errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult)
            context.CompileExpression("y", resultProperties, errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.None)
            context.CompileExpression("y.Value", resultProperties, errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult Or DkmClrCompilationResultFlags.ReadOnlyResult)
            context.CompileExpression("Not y", resultProperties, errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.ReadOnlyResult)
            context.CompileExpression("False", resultProperties, errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult Or DkmClrCompilationResultFlags.ReadOnlyResult)
            context.CompileExpression("F()", resultProperties, errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.BoolResult Or DkmClrCompilationResultFlags.ReadOnlyResult Or DkmClrCompilationResultFlags.PotentialSideEffect)
        End Sub

        <Fact>
        Public Sub EvaluateNonRValueExpression()
            Const source = "
Class C
    WriteOnly Property P As Object
        Set
        End Set
    End Property
    Sub M()
    End Sub
End Class
"
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="P",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30524: Property 'P' is 'WriteOnly'.", errorMessage)
        End Sub

        ''' <summary>
        ''' Expression that does not return a value.
        ''' </summary>
        <Fact>
        Public Sub EvaluateVoidExpression()
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                s_simpleSource,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="C.M()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult, resultProperties.Flags)
            Dim method = testData.GetMethodData("<>x.<>m0").Method
            Assert.Equal(SpecialType.System_Void, method.ReturnType.SpecialType)
            Assert.True(method.ReturnsVoid)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        6 (0x6)
  .maxstack  0
  IL_0000:  call       ""Sub C.M()""
  IL_0005:  ret
}")
        End Sub

        <Fact, WorkItem(1112400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112400")>
        Public Sub EvaluateMethodGroup()
            Dim errorMessage As String = Nothing

            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                s_simpleSource,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="AddressOf C.M",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30491: Expression does not produce a value.", errorMessage)

            Dim source = "
Class C
    Shared Function F() As Boolean
        Return True
    End Function
End Class"
            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.F",
                expr:="C.F",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            Assert.Equal(DkmEvaluationResultAccessType.Public, resultProperties.AccessType)
            Assert.Equal(DkmEvaluationResultCategory.Method, resultProperties.Category)
            Assert.Equal(DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult Or DkmClrCompilationResultFlags.BoolResult, resultProperties.Flags)
            Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, resultProperties.ModifierFlags)
            Assert.Equal(DkmEvaluationResultStorageType.Static, resultProperties.StorageType)
        End Sub

        <Fact>
        Public Sub EvaluatePropertyGroup()
            Dim source = "
Class C
    Property P As Boolean
End Class
"
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing

            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.get_P",
                expr:="P",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            Assert.Equal(DkmEvaluationResultAccessType.Public, resultProperties.AccessType)
            Assert.Equal(DkmEvaluationResultCategory.Property, resultProperties.Category)
            Assert.Equal(DkmClrCompilationResultFlags.BoolResult, resultProperties.Flags)
            Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, resultProperties.ModifierFlags)
            Assert.Equal(DkmEvaluationResultStorageType.None, resultProperties.StorageType)

            testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.set_P",
                expr:="P()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            Assert.Equal(DkmEvaluationResultAccessType.Public, resultProperties.AccessType)
            Assert.Equal(DkmEvaluationResultCategory.Property, resultProperties.Category)
            Assert.Equal(DkmClrCompilationResultFlags.BoolResult, resultProperties.Flags)
            Assert.Equal(DkmEvaluationResultTypeModifierFlags.None, resultProperties.ModifierFlags)
            Assert.Equal(DkmEvaluationResultStorageType.None, resultProperties.StorageType)
        End Sub

        <WorkItem(964, "https://github.com/dotnet/roslyn/issues/964")>
        <Fact>
        Public Sub EvaluateXmlMemberAccess()
            Dim source =
"Class C
    Shared Sub M(x As System.Xml.Linq.XElement)
        Dim y = x.@<y>
    End Sub
End Class"
            Dim allReferences = GetAllXmlReferences()
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                options:=TestOptions.DebugDll,
                references:=allReferences)
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Dim runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                allReferences,
                exeBytes,
                Nothing)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("x.@a", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""a""
  IL_0006:  ldstr      """"
  IL_000b:  call       ""Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName""
  IL_0010:  call       ""Function My.InternalXmlHelper.get_AttributeValue(System.Xml.Linq.XElement, System.Xml.Linq.XName) As String""
  IL_0015:  ret
}")
        End Sub

        <WorkItem(964, "https://github.com/dotnet/roslyn/issues/964")>
        <Fact>
        Public Sub InternalXmlHelper_RootNamespace()
            Dim source =
"Class C
    Shared Sub M(x As System.Xml.Linq.XElement)
        Dim y = x.@<y>
    End Sub
End Class"
            Dim allReferences = GetAllXmlReferences()
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                options:=TestOptions.DebugDll.WithRootNamespace("Root"),
                references:=allReferences)
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Dim runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                allReferences,
                exeBytes,
                SymReaderFactory.CreateReader(pdbBytes)) ' Need SymReader to find root namespace.
            Dim context = CreateMethodContext(runtime, "Root.C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("x.@a", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       22 (0x16)
  .maxstack  3
  .locals init (String V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""a""
  IL_0006:  ldstr      """"
  IL_000b:  call       ""Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName""
  IL_0010:  call       ""Function Root.My.InternalXmlHelper.get_AttributeValue(System.Xml.Linq.XElement, System.Xml.Linq.XName) As String""
  IL_0015:  ret
}")
        End Sub

        <WorkItem(964, "https://github.com/dotnet/roslyn/issues/964")>
        <Fact>
        Public Sub InternalXmlHelper_AddedModules()
            Dim sourceTemplate =
"Class C{0}
    Shared Sub M(x As System.Xml.Linq.XElement)
        Dim y = x.@<y>
    End Sub
End Class"
            Dim xmlReferences = GetAllXmlReferences()
            Dim moduleOptions = New VisualBasicCompilationOptions(OutputKind.NetModule, optimizationLevel:=OptimizationLevel.Debug).WithExtendedCustomDebugInformation(True)

            Dim tree1 = VisualBasicSyntaxTree.ParseText(String.Format(sourceTemplate, 1))
            Dim tree2 = VisualBasicSyntaxTree.ParseText(String.Format(sourceTemplate, 2))
            Dim ref1 = CreateCompilationWithReferences(tree1, xmlReferences, moduleOptions, assemblyName:="Module1").EmitToImageReference()
            Dim ref2 = CreateCompilationWithReferences(tree2, xmlReferences, moduleOptions, assemblyName:="Module2").EmitToImageReference()

            Dim tree = VisualBasicSyntaxTree.ParseText(String.Format(sourceTemplate, ""))
            Dim compReferences = xmlReferences.Concat(ImmutableArray.Create(ref1, ref2))
            Dim comp = CreateCompilationWithReferences(tree, compReferences, TestOptions.DebugDll, assemblyName:="Test")

            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Dim runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                compReferences,
                exeBytes,
                Nothing)
            Dim context = CreateMethodContext(runtime, "C1.M") ' In Module1
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("x.@a", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       22 (0x16)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldstr      ""a""
  IL_0006:  ldstr      """"
  IL_000b:  call       ""Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName""
  IL_0010:  call       ""Function My.InternalXmlHelper.get_AttributeValue(System.Xml.Linq.XElement, System.Xml.Linq.XName) As String""
  IL_0015:  ret
}")
        End Sub

        <Fact>
        Public Sub AssignByRefParameter()
            Const source =
"Class C
    Shared Sub M1(ByRef x As Integer)
        x = 1
    End Sub
    Shared Sub M2(Of T)(ByRef y As T)
        y = Nothing
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                references:={SystemCoreRef},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M1")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileAssignment("x", "2", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.2
  IL_0002:  stind.i4
  IL_0003:  ret
}
")
            context = CreateMethodContext(
                runtime,
                methodName:="C.M2")
            testData = New CompilationTestData()
            context.CompileAssignment("y", "Nothing", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0(Of T)").VerifyIL("
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  initobj    ""T""
  IL_0007:  ret
}
")
        End Sub

        <Fact>
        Public Sub EvaluateNamespace()
            Const source = "
Namespace N
    Class C
        Shared Sub M()
        End Sub
    End Class
End Namespace
"
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="N.C.M",
                expr:="N",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30112: 'N' is a namespace and cannot be used as an expression.", errorMessage)
        End Sub

        <Fact>
        Public Sub EvaluateType()
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                s_simpleSource,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="C",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30109: 'C' is a class type and cannot be used as an expression.", errorMessage)
        End Sub

        <WorkItem(986227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/986227")>
        <Fact>
        Public Sub RewriteCatchLocal()
            Const source =
"Imports System
Class E(Of T)
    Inherits Exception
End Class
Class C(Of T)
    Shared Sub M()
        Microsoft.VisualBasic.VBMath.Randomize() ' Make sure Microsoft.VisualBasic has been loaded.
        dim z as Integer = 0
    End Sub
End Class"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:=
"DirectCast(Function()
    Dim e1 As E(Of T) = Nothing
    Try
        e1 = Nothing
    Catch e2 As E(Of T)
        e1 = e2
    End Try
    Return e1
End Function, Func(Of E(Of T)))()")
            Dim methodData = testData.GetMethodData("<>x(Of T)._Closure$__._Lambda$__0-0")
            Dim method = DirectCast(methodData.Method, MethodSymbol)
            Dim containingType = method.ContainingType
            Dim returnType = DirectCast(method.ReturnType, NamedTypeSymbol)
            ' Return type E(Of T) with type argument T from <>x(Of T).
            Assert.Equal(returnType.TypeArguments(0).ContainingSymbol, containingType.ContainingType)
            Dim locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder()
            Assert.Equal(locals.Length, 2)
            ' All locals of type E(Of T) with type argument T from <>x(Of T).
            For Each local In locals
                Dim localType = DirectCast(local.Type, NamedTypeSymbol)
                Dim typeArg = localType.TypeArguments(0)
                Assert.Equal(typeArg.ContainingSymbol, containingType.ContainingType)
            Next
            methodData.VerifyIL(
"{
  // Code size       24 (0x18)
  .maxstack  2
  .locals init (E(Of T) V_0, //e1
                E(Of T) V_1) //e2
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldnull
    IL_0003:  stloc.0
    IL_0004:  leave.s    IL_0016
  }
  catch E(Of T)
  {
    IL_0006:  dup
    IL_0007:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
    IL_000c:  stloc.1
    IL_000d:  ldloc.1
    IL_000e:  stloc.0
    IL_000f:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
    IL_0014:  leave.s    IL_0016
  }
  IL_0016:  ldloc.0
  IL_0017:  ret
}")
        End Sub

        <WorkItem(986227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/986227")>
        <Fact>
        Public Sub RewriteSequenceTemps()
            Const source =
"Class C
    Private F As Object
    Shared Sub M(Of T As {C, New})()
        Dim o As T
    End Sub
End Class"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="New T() With {.F = 1}")
            Dim methodData = testData.GetMethodData("<>x.<>m0(Of T)")
            Dim method = DirectCast(methodData.Method, MethodSymbol)
            Dim returnType = method.ReturnType
            Assert.Equal(returnType.TypeKind, TypeKind.TypeParameter)
            Assert.Equal(returnType.ContainingSymbol, method)

            Dim locals = methodData.ILBuilder.LocalSlotManager.LocalsInOrder()
            Assert.Equal(method, DirectCast(locals.Single().Type, TypeSymbol).ContainingSymbol)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       23 (0x17)
  .maxstack  3
  .locals init (T V_0) //o
  IL_0000:  call       ""Function System.Activator.CreateInstance(Of T)() As T""
  IL_0005:  dup
  IL_0006:  box        ""T""
  IL_000b:  ldc.i4.1
  IL_000c:  box        ""Integer""
  IL_0011:  stfld      ""C.F As Object""
  IL_0016:  ret
}")
        End Sub

        <Fact>
        Public Sub GenericMethod()
            Const source =
"Class A(Of T)
    Class B(Of U, V As U)
        Shared Sub M1(Of W, X As A(Of W).B(Of Object, U()))()
        End Sub
        Shared Sub M2()
        End Sub
    End Class
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())

            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="A.B.M1")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("If(GetType(V), GetType(X))", errorMessage, testData)
            Dim methodData = testData.GetMethodData("<>x(Of T, U, V).<>m0(Of W, X)")
            Dim actualIL = methodData.GetMethodIL()
            Dim expectedIL = "
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldtoken    ""V""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0018
  IL_000d:  pop
  IL_000e:  ldtoken    ""X""
  IL_0013:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0018:  ret
}
"
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL)
            Assert.Equal(Cci.CallingConvention.Generic, (DirectCast(methodData.Method, Cci.IMethodDefinition)).CallingConvention)

            context = CreateMethodContext(
                runtime,
                methodName:="A.B.M2")
            testData = New CompilationTestData()
            context.CompileExpression("If(GetType(T), GetType(U))", errorMessage, testData)
            methodData = testData.GetMethodData("<>x(Of T, U, V).<>m0")
            Assert.Equal(Cci.CallingConvention.Default, (DirectCast(methodData.Method, Cci.IMethodDefinition)).CallingConvention)
        End Sub

        <Fact>
        Public Sub EvaluateCapturedLocalsOutsideLambda()
            Const source =
"Class A
    Friend Overridable Function F(o As Object) As Object
        Return 1
    End Function
End Class
Class B
    Inherits A
    Friend Overrides Function F(o As Object) As Object
        Return 2
    End Function
    Shared Overloads Sub F(_f As System.Func(Of Object))
        _f()
    End Sub
    Sub M(Of T As {A, New})(x As Object)
        F(Function() Me.F(x))
        If x IsNot Nothing Then
#ExternalSource(""test"", 999)
            Dim y = New T()
            Dim z = 1
            F(Function() MyBase.F(y))
#End ExternalSource
        Else
            Dim w = 2
            F(Function() w)
        End If
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="B.M",
                atLineNumber:=999)
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("Me.F(y)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0(Of T)").VerifyIL(
"{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (B._Closure$__3-0(Of T) V_0, //$VB$Closure_0
                Boolean V_1,
                B._Closure$__3-1(Of T) V_2, //$VB$Closure_1
                Integer V_3, //z
                B._Closure$__3-2(Of T) V_4)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""B._Closure$__3-0(Of T).$VB$Me As B""
  IL_0006:  ldloc.2
  IL_0007:  ldfld      ""B._Closure$__3-1(Of T).$VB$Local_y As T""
  IL_000c:  box        ""T""
  IL_0011:  callvirt   ""Function B.F(Object) As Object""
  IL_0016:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression("MyClass.F(y)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0(Of T)").VerifyIL(
"{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (B._Closure$__3-0(Of T) V_0, //$VB$Closure_0
                Boolean V_1,
                B._Closure$__3-1(Of T) V_2, //$VB$Closure_1
                Integer V_3, //z
                B._Closure$__3-2(Of T) V_4)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""B._Closure$__3-0(Of T).$VB$Me As B""
  IL_0006:  ldloc.2
  IL_0007:  ldfld      ""B._Closure$__3-1(Of T).$VB$Local_y As T""
  IL_000c:  box        ""T""
  IL_0011:  call       ""Function B.F(Object) As Object""
  IL_0016:  ret
}")
            testData = New CompilationTestData()
            context.CompileExpression("MyBase.F(x)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0(Of T)").VerifyIL(
"{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (B._Closure$__3-0(Of T) V_0, //$VB$Closure_0
                Boolean V_1,
                B._Closure$__3-1(Of T) V_2, //$VB$Closure_1
                Integer V_3, //z
                B._Closure$__3-2(Of T) V_4)
  IL_0000:  ldloc.0
  IL_0001:  ldfld      ""B._Closure$__3-0(Of T).$VB$Me As B""
  IL_0006:  ldloc.0
  IL_0007:  ldfld      ""B._Closure$__3-0(Of T).$VB$Local_x As Object""
  IL_000c:  call       ""Function System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(Object) As Object""
  IL_0011:  call       ""Function A.F(Object) As Object""
  IL_0016:  ret
}")
        End Sub

        ''' <summary>
        ''' Generate PrivateImplementationDetails class
        ''' for initializer expressions.
        ''' </summary>
        <Fact>
        Public Sub EvaluateInitializerExpression()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                references:={SystemCoreRef},
                options:=TestOptions.DebugDll.WithModuleName("MODULE"),
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("{ 1, 2, 3, 4, 5 }", errorMessage, testData)
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(methodData.Method.ReturnType.ToDisplayString(), "Integer()")
            methodData.VerifyIL(
"{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""Integer""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails>.1036C5F8EF306104BD582D73E555F4DAE8EECB24""
  IL_000c:  call       ""Sub System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0011:  ret
}")
        End Sub

        <Fact>
        Public Sub ExpressionTree()
            Const source =
"Imports System
Imports System.Linq.Expressions
Class C
    Shared Function F(e As Expression(Of Func(Of Object))) As Object
        Return e.Compile()()
    End Function
    Shared Sub M(o As Integer)
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                references:={SystemCoreRef},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("F(Function() o + 1)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size      100 (0x64)
  .maxstack  3
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      ""<>x._Closure$__0-0.$VB$Local_o As Integer""
  IL_000c:  ldtoken    ""<>x._Closure$__0-0""
  IL_0011:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0016:  call       ""Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression""
  IL_001b:  ldtoken    ""<>x._Closure$__0-0.$VB$Local_o As Integer""
  IL_0020:  call       ""Function System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle) As System.Reflection.FieldInfo""
  IL_0025:  call       ""Function System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo) As System.Linq.Expressions.MemberExpression""
  IL_002a:  ldc.i4.1
  IL_002b:  box        ""Integer""
  IL_0030:  ldtoken    ""Integer""
  IL_0035:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_003a:  call       ""Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression""
  IL_003f:  call       ""Function System.Linq.Expressions.Expression.AddChecked(System.Linq.Expressions.Expression, System.Linq.Expressions.Expression) As System.Linq.Expressions.BinaryExpression""
  IL_0044:  ldtoken    ""Object""
  IL_0049:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_004e:  call       ""Function System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, System.Type) As System.Linq.Expressions.UnaryExpression""
  IL_0053:  ldc.i4.0
  IL_0054:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0059:  call       ""Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of Object))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of Object))""
  IL_005e:  call       ""Function C.F(System.Linq.Expressions.Expression(Of System.Func(Of Object))) As Object""
  IL_0063:  ret
}")
        End Sub

        ''' <summary>
        ''' DiagnosticsPass must be run on evaluation method.
        ''' </summary>
        <Fact>
        Public Sub DiagnosticsPass()
            Const source =
"Imports System
Imports System.Linq.Expressions
Class C
    Shared Function F(e As Expression(Of Func(Of Object))) As Object
        Return e.Compile()()
    End Function
    Shared Sub M()
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                references:={SystemCoreRef},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("F(Function()
                        Return Nothing
                    End Function)", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Equal(errorMessage, "error BC36675: Statement lambdas cannot be converted to expression trees.")
        End Sub

        <WorkItem(1096605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1096605")>
        <Fact>
        Public Sub EvaluateAsync()
            Const source =
"Imports System
Imports System.Threading.Tasks
Class C
    Async Function F() As Task(Of Object)
        Return Nothing
    End Function
    Sub G(f As Func(Of Task(Of Object)))
    End Sub
    Sub M()
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("G(Async Function() Await F())", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (<>x._Closure$__0-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C""
  IL_000d:  ldloc.0
  IL_000e:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""Function <>x._Closure$__0-0._Lambda$__0() As System.Threading.Tasks.Task(Of Object)""
  IL_001a:  newobj     ""Sub System.Func(Of System.Threading.Tasks.Task(Of Object))..ctor(Object, System.IntPtr)""
  IL_001f:  callvirt   ""Sub C.G(System.Func(Of System.Threading.Tasks.Task(Of Object)))""
  IL_0024:  ret
}")
        End Sub

        ''' <summary>
        ''' Unnamed temporaries at the end of the local
        ''' signature should be preserved.
        ''' </summary>
        <Fact()>
        Public Sub TrailingUnnamedTemporaries()
            Const source =
"Class C
    Private F As Object
    Shared Function M(c As Object()) As Boolean
        For Each o in c
            If o IsNot Nothing Then Return True
        Next
        Return False
    End Function
End Class"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="New C() With {.F = 1}")
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       18 (0x12)
  .maxstack  3
  .locals init (Boolean V_0, //M
                Object() V_1,
                Integer V_2,
                Object V_3,
                Boolean V_4,
                Boolean V_5)
  IL_0000:  newobj     ""Sub C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  box        ""Integer""
  IL_000c:  stfld      ""C.F As Object""
  IL_0011:  ret
}")
        End Sub

        <WorkItem(958448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958448")>
        <Fact>
        Public Sub ConditionalAttribute()
            Const source =
"Imports System.Diagnostics
Class C
    Shared Sub M(x As Integer)
    End Sub
    <Conditional(""D"")>
    Shared Sub F(o As Object)
    End Sub
End Class"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="F(x + 1)")
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  box        ""Integer""
  IL_0008:  call       ""Sub C.F(Object)""
  IL_000d:  ret
}")
        End Sub

        <WorkItem(958448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/958448")>
        <Fact>
        Public Sub ConditionalAttribute_CollectionInitializer()
            Const source =
"Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Class C
    Implements IEnumerable
    Private c As New List(Of Object)()
    <Conditional(""D"")>
    Sub Add(o As Object)
        c.Add(o)
    End Sub
    Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return c.GetEnumerator()
    End Function
    Shared Sub M()
    End Sub
End Class"
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="New C() From {1, 2}")
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  newobj     ""Sub C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  box        ""Integer""
  IL_000c:  callvirt   ""Sub C.Add(Object)""
  IL_0011:  dup
  IL_0012:  ldc.i4.2
  IL_0013:  box        ""Integer""
  IL_0018:  callvirt   ""Sub C.Add(Object)""
  IL_001d:  ret   
}")
        End Sub

        <WorkItem(994485, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/994485")>
        <Fact>
        Public Sub Repro994485()
            Const source = "
Imports System

Enum E
    A
End Enum

Class C
    Function M(e As E?) As Action
        Dim a As Action = Sub() e.ToString()
        Dim ee As E = e.Value
        Return a
    End Function
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")

            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("e.HasValue", errorMessage, testData)
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.SpecialType, SpecialType.System_Boolean)
            methodData.VerifyIL(
"{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (C._Closure$__1-0 V_0, //$VB$Closure_0
                System.Action V_1, //M
                System.Action V_2, //a
                E V_3) //ee
  IL_0000:  ldloc.0
  IL_0001:  ldflda     ""C._Closure$__1-0.$VB$Local_e As E?""
  IL_0006:  call       ""Function E?.get_HasValue() As Boolean""
  IL_000b:  ret
}")
        End Sub

        <Fact>
        Public Sub NestedGenericTypes()
            Const source = "
Class C(Of T)
    Class D(Of U)
        Sub M(u1 As U, t1 As T, type1 As System.Type, type2 As System.Type)
        End Sub
    End Class
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.D.M",
                expr:="M(u1, t1, GetType(U), GetType(T))",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Null(errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)

            testData.GetMethodData("<>x(Of T, U).<>m0").VerifyIL(
"{
  // Code size       29 (0x1d)
  .maxstack  5
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldtoken    ""U""
  IL_0008:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000d:  ldtoken    ""T""
  IL_0012:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0017:  callvirt   ""Sub C(Of T).D(Of U).M(U, T, System.Type, System.Type)""
  IL_001c:  ret
}")
        End Sub

        <Fact>
        Public Sub NestedGenericTypes_GenericMethod()
            Const source = "
Class C(Of T)
    Class D(Of U)
        Sub M(Of V)(v1 As V, u1 As U, t1 As T, type1 As System.Type, type2 As System.Type, type3 As System.Type)
        End Sub
    End Class
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.D.M",
                expr:="M(v1, u1, t1, GetType(V), GetType(U), GetType(T))",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Null(errorMessage)
            Assert.Equal(resultProperties.Flags, DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)

            testData.GetMethodData("<>x(Of T, U).<>m0(Of V)").VerifyIL(
"{
  // Code size       40 (0x28)
  .maxstack  7
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldarg.3
  IL_0004:  ldtoken    ""V""
  IL_0009:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000e:  ldtoken    ""U""
  IL_0013:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0018:  ldtoken    ""T""
  IL_001d:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0022:  callvirt   ""Sub C(Of T).D(Of U).M(Of V)(V, U, T, System.Type, System.Type, System.Type)""
  IL_0027:  ret
}")
        End Sub

        <WorkItem(1000946, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1000946")>
        <Fact>
        Public Sub MyBaseExpression()
            Const source = "
Class Base
End Class

Class Derived
    Inherits Base
    
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="Derived.M",
                expr:="MyBase",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC32027: 'MyBase' must be followed by '.' and an identifier.", errorMessage)
        End Sub

        <Fact>
        Public Sub NonCapturingLambda()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="(New System.Func(Of Integer, Integer)(Function(x) x + x))(1)",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       43 (0x2b)
  .maxstack  2
  IL_0000:  ldsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of Integer, Integer)""
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of Integer, Integer)""
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     ""<>x._Closure$__.$I As <>x._Closure$__""
  IL_0013:  ldftn      ""Function <>x._Closure$__._Lambda$__0-0(Integer) As Integer""
  IL_0019:  newobj     ""Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)""
  IL_001e:  dup
  IL_001f:  stsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of Integer, Integer)""
  IL_0024:  ldc.i4.1
  IL_0025:  callvirt   ""Function System.Func(Of Integer, Integer).Invoke(Integer) As Integer""
  IL_002a:  ret
}
")

            testData.GetMethodData("<>x._Closure$__._Lambda$__0-0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}")
        End Sub

        <Fact>
        Public Sub CapturingLambda_Parameter()
            Const source = "
Class C
    Sub M(p As Integer)
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="(New System.Func(Of Integer, Integer)(Function(x) x + p))(1)",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.1
  IL_0007:  stfld      ""<>x._Closure$__0-0.$VB$Local_p As Integer""
  IL_000c:  ldftn      ""Function <>x._Closure$__0-0._Lambda$__0(Integer) As Integer""
  IL_0012:  newobj     ""Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)""
  IL_0017:  ldc.i4.1
  IL_0018:  callvirt   ""Function System.Func(Of Integer, Integer).Invoke(Integer) As Integer""
  IL_001d:  ret
}")

            testData.GetMethodData("<>x._Closure$__0-0._Lambda$__0").VerifyIL(
"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""<>x._Closure$__0-0.$VB$Local_p As Integer""
  IL_0007:  add.ovf
  IL_0008:  ret
}")
        End Sub

        <Fact>
        Public Sub CapturingLambda_HoistedParameter()
            Const source = "
Imports System.Collections.Generic

Class C
    Iterator Function M(p As Integer) As IEnumerable(Of Integer)
        N(p)
        Yield p
        N(p)
    End Function

    Sub N(p As Integer)
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.VB$StateMachine_1_M.MoveNext",
                expr:="(New System.Func(Of Integer, Integer)(Function(x) x + p))(1)",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       30 (0x1e)
  .maxstack  3
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C.VB$StateMachine_1_M""
  IL_000c:  ldftn      ""Function <>x._Closure$__0-0._Lambda$__0(Integer) As Integer""
  IL_0012:  newobj     ""Sub System.Func(Of Integer, Integer)..ctor(Object, System.IntPtr)""
  IL_0017:  ldc.i4.1
  IL_0018:  callvirt   ""Function System.Func(Of Integer, Integer).Invoke(Integer) As Integer""
  IL_001d:  ret
}")

            testData.GetMethodData("<>x._Closure$__0-0._Lambda$__0").VerifyIL(
"{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C.VB$StateMachine_1_M""
  IL_0007:  ldfld      ""C.VB$StateMachine_1_M.$VB$Local_p As Integer""
  IL_000c:  add.ovf
  IL_000d:  ret
}")
        End Sub

        <Fact>
        Public Sub UntypedLambda()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="Function(x) 1",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL(
"
{
  // Code size       36 (0x24)
  .maxstack  2
  IL_0000:  ldsfld     ""<>x._Closure$__.$I0-0 As <generated method>""
  IL_0005:  brfalse.s  IL_000d
  IL_0007:  ldsfld     ""<>x._Closure$__.$I0-0 As <generated method>""
  IL_000c:  ret
  IL_000d:  ldsfld     ""<>x._Closure$__.$I As <>x._Closure$__""
  IL_0012:  ldftn      ""Function <>x._Closure$__._Lambda$__0-0(Object) As Integer""
  IL_0018:  newobj     ""Sub VB$AnonymousDelegate_0(Of Object, Integer)..ctor(Object, System.IntPtr)""
  IL_001d:  dup
  IL_001e:  stsfld     ""<>x._Closure$__.$I0-0 As <generated method>""
  IL_0023:  ret
}")
            testData.GetMethodData("<>x._Closure$__._Lambda$__0-0").VerifyIL(
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}")
        End Sub

        <Fact>
        Public Sub ClassMyBaseCall()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="MyBase.ToString()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""Function Object.ToString() As String""
  IL_0006:  ret
}
")
        End Sub

        <Fact>
        Public Sub StructureMyBaseCall()
            Const source = "
Structure S
    Sub M()
    End Sub
End Structure
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="S.M",
                expr:="MyBase.ToString()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30044: 'MyBase' is not valid within a structure.", errorMessage)
        End Sub

        <Fact>
        Public Sub ModuleMyBaseCall()
            Const source = "
Module S
    Sub M()
    End Sub
End Module
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="S.M",
                expr:="MyBase.ToString()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC32001: 'MyBase' is not valid within a Module.", errorMessage)
        End Sub

        <WorkItem(1010922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1010922")>
        <Fact>
        Public Sub IntegerOverflow()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"

            Dim checkedComp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(checkedComp)
            Dim context = CreateMethodContext(runtime, methodName:="C.M")

            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("2147483647 + 1", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC30439: Constant expression not representable in type 'Integer'.", errorMessage)

            ' As in dev12, the global "unchecked" option is not respected at debug time.
            Dim uncheckedComp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll.WithOverflowChecks(False))
            runtime = CreateRuntimeInstance(uncheckedComp)
            context = CreateMethodContext(runtime, methodName:="C.M")

            errorMessage = Nothing
            context.CompileExpression("2147483647 + 1", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC30439: Constant expression not representable in type 'Integer'.", errorMessage)
        End Sub

        <WorkItem(1012956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1012956")>
        <Fact>
        Public Sub AssignmentConversion()
            Const source = "
Class C
    Sub M(u As UInteger)
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.M")

            Dim errorMessage As String = Nothing
            context.CompileAssignment("u", "2147483647 + 1", errorMessage, formatter:=DebuggerDiagnosticFormatter.Instance)
            Assert.Equal("error BC30439: Constant expression not representable in type 'Integer'.", errorMessage)
        End Sub

        <WorkItem(1016530, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1016530")>
        <Fact>
        Public Sub EvaluateStatement()
            Dim source = "
Class C
    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="Throw New System.Exception()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)

            Assert.Equal("error BC30201: Expression expected.", errorMessage)
        End Sub

        <WorkItem(1015887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")>
        <Fact>
        Public Sub DateTimeFieldConstant()
            Dim source = "
Class C
    Const D = #01/02/2010#

    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="D",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       15 (0xf)
  .maxstack  1
  IL_0000:  ldc.i8     0x8cc5955a94ec000
  IL_0009:  newobj     ""Sub Date..ctor(Long)""
  IL_000e:  ret
}
")
        End Sub

        <WorkItem(1015887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015887")>
        <Fact>
        Public Sub DecimalFieldConstant()
            Dim source = "
Class C
    Const D = 3.14D

    Sub M()
    End Sub
End Class
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="D",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       15 (0xf)
  .maxstack  5
  IL_0000:  ldc.i4     0x13a
  IL_0005:  ldc.i4.0
  IL_0006:  ldc.i4.0
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.2
  IL_0009:  newobj     ""Sub Decimal..ctor(Integer, Integer, Integer, Boolean, Byte)""
  IL_000e:  ret
}
")
        End Sub

        <WorkItem(1028808, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1028808")>
        <Fact>
        Public Sub StaticLambdaInDisplayClass()
            ' Note:  I don't think the VB compiler ever generated code like this, but
            '        it doesn't hurt to make sure we do the right thing if it did...
            Dim source =
".class private auto ansi C
       extends [mscorlib]System.Object
{
  .class auto ansi nested assembly '_Closure$__1'
         extends [mscorlib]System.Object
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .field public class C $VB$Local_c
    .field private static class [mscorlib]System.Action`1<int32> '_ClosureCache$__4'
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      ret
    }

    .method private hidebysig static void 
            '_Lambda$__2'(int32 x) cil managed
    {
      ret
    }
  }

  // Need some static method 'Test' with 'x' in scope.
  .method private hidebysig static void 
          Test(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ret
  }
}"
            Dim exeBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            EmitILToArray(source, appendDefaultHeader:=True, includePdb:=True, assemblyBytes:=exeBytes, pdbBytes:=pdbBytes)

            Dim runtime = CreateRuntimeInstance(
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName(),
                references:=ImmutableArray.Create(MscorlibRef),
                exeBytes:=exeBytes.ToArray(),
                symReader:=SymReaderFactory.CreateReader(pdbBytes))

            Dim context = CreateMethodContext(
                runtime,
                methodName:="C._Closure$__1._Lambda$__2")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("x", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
")
        End Sub

        <WorkItem(1030236, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1030236")>
        <Fact>
        Public Sub ExtensionMethodInContainingType()
            Dim source = "
Imports System.Runtime.CompilerServices
Module Module1
    Sub Main()
        Dim s = ""Extend!""
        Dim r = s.Method
    End Sub
    <Extension>
    Private Function Method(s As String) As Integer
        Return 1
    End Function
End Module
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="Module1.Main",
                expr:="s.Method()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (String V_0, //s
                Integer V_1) //r
  IL_0000:  ldloc.0
  IL_0001:  call       ""Function Module1.Method(String) As Integer""
  IL_0006:  ret
}
")
        End Sub

        <WorkItem(1030236, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1030236")>
        <Fact>
        Public Sub ExtensionMethodInContainingNamespace()
            Dim source = "
Imports System.Runtime.CompilerServices
Module Module1
    Sub Main()
        Dim s = ""Extend!""
        Dim r = s.Method
    End Sub
End Module
Module Module2
    <Extension>
    Friend Function Method(s As String) As Integer
        Return 1
    End Function
End Module
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="Module1.Main",
                expr:="s.Method()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (String V_0, //s
                Integer V_1) //r
  IL_0000:  ldloc.0
  IL_0001:  call       ""Function Module2.Method(String) As Integer""
  IL_0006:  ret
}
")
        End Sub

        <WorkItem(1030236, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1030236")>
        <Fact>
        Public Sub ExtensionMethodInImportedNamespace()
            Dim source = "
Imports System.Runtime.CompilerServices
Imports NS1
Module Module1
    Sub Main()
        Dim s = ""Extend!""
        Dim r = s.Method
    End Sub
End Module
Namespace NS1
    Module Module2
        <Extension>
        Public Function Method(s As String) As Integer
            Return 1
        End Function
    End Module
End Namespace
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="Module1.Main",
                expr:="s.Method()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (String V_0, //s
                Integer V_1) //r
  IL_0000:  ldloc.0
  IL_0001:  call       ""Function NS1.Module2.Method(String) As Integer""
  IL_0006:  ret
}
")
        End Sub

        <WorkItem(1030236, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1030236")>
        <Fact>
        Public Sub InaccessibleExtensionMethod() ' EE will be able to access this extension method anyway...
            Dim source = "
Imports System.Runtime.CompilerServices
Module Module1
    Sub Main()
        Dim s = ""Extend!""
    End Sub
End Module
Module Module2
    <Extension>
    Private Function Method(s As String) As Integer
        Return 1
    End Function
End Module
"
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="Module1.Main",
                expr:="s.Method()",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)

            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (String V_0) //s
  IL_0000:  ldloc.0
  IL_0001:  call       ""Function Module2.Method(String) As Integer""
  IL_0006:  ret
}
")
        End Sub

        <WorkItem(1042918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1042918")>
        <WorkItem(964, "https://github.com/dotnet/roslyn/issues/964")>
        <Fact>
        Public Sub ConditionalAccessExpressionType()
            Dim source =
"Class C
    Function F() As Integer
        Return 0
    End Function
    Function G() As C
        Return Nothing
    End Function
    Private X As System.Xml.Linq.XElement
    Sub M()
        Dim dummy = Me?.X.@a ' Ensure InternalXmlHelper is emitted.
    End Sub
End Class"
            Dim allReferences = GetAllXmlReferences()
            Dim comp = CreateCompilationWithReferences(
                MakeSources(source),
                options:=TestOptions.DebugDll,
                references:=allReferences)
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Dim runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                allReferences,
                exeBytes,
                Nothing)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("Me?.F()", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")
            methodData.VerifyIL(
"{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (String V_0,
                Integer? V_1)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    ""Integer?""
  IL_000b:  ldloc.1
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  call       ""Function C.F() As Integer""
  IL_0013:  newobj     ""Sub Integer?..ctor(Integer)""
  IL_0018:  ret
}")

            testData = New CompilationTestData()
            result = context.CompileExpression("(Me?.F())", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")

            testData = New CompilationTestData()
            result = context.CompileExpression("Me?.X.@a", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Null(errorMessage)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.SpecialType, SpecialType.System_String)
            methodData.VerifyIL(
"{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0005
  IL_0003:  ldnull
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""C.X As System.Xml.Linq.XElement""
  IL_000b:  ldstr      ""a""
  IL_0010:  ldstr      """"
  IL_0015:  call       ""Function System.Xml.Linq.XName.Get(String, String) As System.Xml.Linq.XName""
  IL_001a:  call       ""Function My.InternalXmlHelper.get_AttributeValue(System.Xml.Linq.XElement, System.Xml.Linq.XName) As String""
  IL_001f:  ret
}")

            testData = New CompilationTestData()
            result = context.CompileExpression("(New C())?.G()?.F()", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")

            testData = New CompilationTestData()
            result = context.CompileExpression("(New C())?.G().F()", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")

            testData = New CompilationTestData()
            result = context.CompileExpression("G()?.M()", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.True(DirectCast(methodData.Method, MethodSymbol).IsSub)
            methodData.VerifyIL(
"{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (String V_0)
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""Function C.G() As C""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_000b
  IL_0009:  pop
  IL_000a:  ret
  IL_000b:  call       ""Sub C.M()""
  IL_0010:  ret
}")

            testData = New CompilationTestData()
            result = context.CompileExpression("(G()?.M())", errorMessage, testData, DebuggerDiagnosticFormatter.Instance)
            Assert.Equal(errorMessage, "error BC30491: Expression does not produce a value.")
        End Sub

        <WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")>
        <Fact>
        Public Sub IteratorParameters()
            Const source = "
Class C
    Iterator Function F(x As Integer) As System.Collections.IEnumerable
        Yield x
        Yield Me ' Until iterators always capture 'Me', do it explicitly.
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_1_F.MoveNext")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("x", errorMessage, testData)
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(SpecialType.System_Int32, methodData.Method.ReturnType.SpecialType)
            methodData.VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$VB$Local_x As Integer""
  IL_0006:  ret
}
")
        End Sub

        <WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")>
        <Fact>
        Public Sub IteratorGenericLocal()
            Const source = "
Class C(Of T)
    Iterator Function F(x As Integer) As System.Collections.IEnumerable
        Dim t1 As T = Nothing
        Yield t1
        t1.ToString()
        Yield Me ' Until iterators always capture 'Me', do it explicitly.
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_1_F.MoveNext")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("x", errorMessage, testData)
            Dim methodData = testData.GetMethodData("<>x(Of T).<>m0")
            Assert.Equal(SpecialType.System_Int32, methodData.Method.ReturnType.SpecialType)
            methodData.VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C(Of T).VB$StateMachine_1_F.$VB$Local_x As Integer""
  IL_0006:  ret
}
")
        End Sub

        <Fact>
        Public Sub SharedDelegate_Class()
            Const source = "
Delegate Sub D()

Class C
    Shared Sub F()
    End Sub
    
    Shared Sub G(d1 As D)
    End Sub
    
    Shared Sub M()
    End Sub
End Class
"

            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="G(AddressOf F)")
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""Sub C.F()""
  IL_0007:  newobj     ""Sub D..ctor(Object, System.IntPtr)""
  IL_000c:  call       ""Sub C.G(D)""
  IL_0011:  ret
}
")
        End Sub

        <Fact>
        Public Sub SharedDelegate_Structure()
            Const source = "
Delegate Sub D()

Structure S
    Shared Sub F()
    End Sub
    
    Shared Sub G(d1 As D)
    End Sub
    
    Shared Sub M()
    End Sub
End Structure
"

            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="S.M",
                expr:="G(AddressOf F)")
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""Sub S.F()""
  IL_0007:  newobj     ""Sub D..ctor(Object, System.IntPtr)""
  IL_000c:  call       ""Sub S.G(D)""
  IL_0011:  ret
}
")
        End Sub

        <WorkItem(1079749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079749")>
        <Fact>
        Public Sub RangeVariableError()
            Const source =
"Class C
    Shared Sub M()
    End Sub
End Class"
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="From c in ""ABC"" Select c",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC36593: Expression of type 'String' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.", errorMessage)
        End Sub

        <WorkItem(1079762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079762")>
        <Fact>
        Public Sub Bug1079762()
            Const source = "
Class C
    Shared Sub F(f1 As System.Func(Of Object, Boolean), o As Object)
        f1(o)
    End Sub

    Shared Sub M(x As Object, y As Object)
        F(Function(z) z IsNot Nothing AndAlso x IsNot Nothing, 3)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C._Closure$__2-0._Lambda$__0")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("z", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Boolean V_0)
  IL_0000:  ldarg.1
  IL_0001:  ret
}
")
            testData = New CompilationTestData()
            context.CompileExpression("y", errorMessage, testData)
            Assert.Equal("error BC30451: 'y' is not declared. It may be inaccessible due to its protection level.", errorMessage)
        End Sub

        <WorkItem(1014763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1014763")>
        <Fact>
        Public Sub NonStateMachineTypeParameter()
            Const source = "
Imports System.Collections.Generic

Class C
    Shared Function I(Of T)(array As T()) As IEnumerable(Of T)
        return array
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.I")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("GetType(T)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (System.Collections.Generic.IEnumerable(Of T) V_0) //I
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ret
}
")
        End Sub

        <WorkItem(1014763, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1014763")>
        <Fact>
        Public Sub StateMachineTypeParameter()
            Const source = "
Imports System.Collections.Generic

Class C
    Shared Iterator Function I(Of T)(array As T()) As IEnumerable(Of T)
        For Each a in array
            Yield a
        Next
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_1_I.MoveNext")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("GetType(T)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x(Of T).<>m0").VerifyIL("
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1,
                Boolean V_2)
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ret
}
")
        End Sub

        <WorkItem(1085642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085642")>
        <Fact>
        Public Sub ModuleWithBadImageFormat()
            Dim source = "
Class C
    Dim F As Integer = 1
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            Dim exeBytes() As Byte = Nothing
            Dim pdbBytes() As Byte = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Dim exeReference = AssemblyMetadata.CreateFromImage(exeBytes).GetReference(display:=Guid.NewGuid().ToString("D"))

            Dim modulesBuilder = ArrayBuilder(Of ModuleInstance).GetInstance()
            Dim corruptMetadata = New ModuleInstance(
                metadataReference:=Nothing,
                moduleMetadata:=Nothing,
                moduleVersionId:=Nothing,
                fullImage:=Nothing,
                metadataOnly:=CommonResources.NoValidTables,
                symReader:=Nothing,
                includeLocalSignatures:=False)

            modulesBuilder.Add(corruptMetadata)
            modulesBuilder.Add(exeReference.ToModuleInstance(exeBytes, SymReaderFactory.CreateReader(pdbBytes)))
            modulesBuilder.AddRange(references.Select(Function(r) r.ToModuleInstance(fullImage:=Nothing, symReader:=Nothing)))
            Dim modules = modulesBuilder.ToImmutableAndFree()

            Using runtime = New RuntimeInstance(modules)
                Dim context = CreateMethodContext(runtime, "C.M")
                Dim resultProperties As ResultProperties
                Dim errorMessage As String = Nothing
                Dim testData = New CompilationTestData()
                ' Verify that we can still evaluate expressions for modules that are not corrupt.
                context.CompileExpression("(new C()).F", resultProperties, errorMessage, testData)
                Assert.Null(errorMessage)
                Assert.Equal(DkmClrCompilationResultFlags.None, resultProperties.Flags)
                testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""Sub C..ctor()""
  IL_0005:  ldfld      ""C.F As Integer""
  IL_000a:  ret
}")
            End Using
        End Sub

        <WorkItem(1089688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089688")>
        <Fact>
        Public Sub MissingType()
            Const libSource = "
Public Class Missing
End Class
"

            Const source = "
Public Class C
    Public field As Missing
    
    Public Sub M(parameter As Missing)
        Dim local As Missing
    End Sub
End Class
"
            Dim libRef = CreateCompilationWithMscorlib({libSource}, assemblyName:="Lib", options:=TestOptions.DebugDll).EmitToImageReference()
            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef, libRef}, TestOptions.DebugDll)

            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim unusedReferences As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, unusedReferences)

            Dim runtime = CreateRuntimeInstance(GetUniqueName(), ImmutableArray.Create(MscorlibRef), exeBytes, SymReaderFactory.CreateReader(pdbBytes))
            Dim context = CreateMethodContext(runtime, "C.M")

            Const expectedError1 = "error BC30652: Reference required to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project."
            Const expectedError2 = "error BC30652: Reference required to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project."
            Dim expectedMissingAssemblyIdentity As New AssemblyIdentity("Lib")

            Dim resultProperties As ResultProperties = Nothing
            Dim actualError As String = Nothing
            Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

            Dim verify As Action(Of String, String) =
                Sub(expr, expectedError)
                    context.CompileExpression(
                        expr,
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)
                    Assert.Equal(expectedError, actualError)
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single())
                End Sub

            verify("M(Nothing)", expectedError2)
            verify("field", expectedError2)
            verify("field.Method", expectedError2)
            verify("parameter", expectedError1)
            verify("parameter.Method", expectedError1)
            verify("local", expectedError1)
            verify("local.Method", expectedError1)

            ' Note that even expressions that don't required the missing type will fail because
            ' the method we synthesize refers to the original locals and parameters.
            verify("0", expectedError1)
        End Sub

        <WorkItem(1090458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090458")>
        <Fact>
        Public Sub ObsoleteAttribute()
            Const source = "
Imports System
Imports System.Diagnostics

Class C
    Shared Sub Main()
        Dim c As New C()
    End Sub

    <Obsolete(""Hello"", True)>
    Property P() As Integer
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.Main")
            Dim errorMessage As String = Nothing
            context.CompileExpression("c.P", errorMessage)
            Assert.Null(errorMessage)
        End Sub

        <WorkItem(1090458, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1090458")>
        <Fact>
        Public Sub DeprecatedAttribute()
            Const source = "
Imports System
Imports Windows.Foundation.Metadata

Class C
    Shared Sub Main()
        Dim c As New C()
    End Sub

    <Deprecated(""Hello"", DeprecationType.Remove, 1)>
    Property P() As Integer
End Class

Namespace Windows.Foundation.Metadata
    <AttributeUsage(
        AttributeTargets.Class Or
        AttributeTargets.Struct Or
        AttributeTargets.Enum Or
        AttributeTargets.Constructor Or
        AttributeTargets.Method Or
        AttributeTargets.Property Or
        AttributeTargets.Field Or
        AttributeTargets.Event Or
        AttributeTargets.Interface Or
        AttributeTargets.Delegate, AllowMultiple:=True)>
    Public NotInheritable Class DeprecatedAttribute : Inherits Attribute
        Public Sub New(message As String, dtype As DeprecationType, version As UInteger)
        End Sub

        Public Sub New(message As String, dtype As DeprecationType, version As UInteger, contract As Type)
        End Sub
    End Class

    Public Enum DeprecationType
        Deprecate = 0
        Remove = 1
    End Enum
End Namespace
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.Main")
            Dim errorMessage As String = Nothing
            context.CompileExpression("c.P", errorMessage)
            Assert.Null(errorMessage)
        End Sub

        <WorkItem(1089591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1089591")>
        <Fact>
        Public Sub BadPdb_MissingMethod()
            Const source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source})

            Dim exeBytes As Byte() = Nothing
            Dim unusedPdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, unusedPdbBytes, references)

            Dim symReader As ISymUnmanagedReader = New MockSymUnmanagedReader(ImmutableDictionary(Of Integer, MethodDebugInfoBytes).Empty)

            Dim runtime = CreateRuntimeInstance("assemblyName", references, exeBytes, symReader)
            Dim evalContext = CreateMethodContext(runtime, "C.Main")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            evalContext.CompileExpression("1", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
")
        End Sub

        <WorkItem(1108133, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1108133")>
        <Fact>
        Public Sub SymUnmanagedReaderNotImplemented()
            Const source = "
Public Class C
    Public Shared Sub Main()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source})

            Dim exeBytes As Byte() = Nothing
            Dim unusedPdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, unusedPdbBytes, references)

            Dim runtime = CreateRuntimeInstance("assemblyName", references, exeBytes, NotImplementedSymUnmanagedReader.Instance)
            Dim evalContext = CreateMethodContext(runtime, "C.Main")
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            evalContext.CompileExpression("1", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}
")
        End Sub

        <WorkItem(1450, "https://github.com/dotnet/roslyn/issues/1450")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/1450")>
        Public Sub WithExpression()
            Const source =
"Structure S
    Friend F As T
End Structure
Structure T
End Structure
Class C
    Shared Sub F(o As System.Action)
    End Sub
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
"F(Sub()
    Dim o = New S()
    With o
        .F = New T()
        With .F
        End With
    End With
End Sub)",
                DkmEvaluationFlags.None,
                NoAliases,
                DebuggerDiagnosticFormatter.Instance,
                resultProperties,
                errorMessage,
                missingAssemblyIdentities,
                EnsureEnglishUICulture.PreferredOrNull,
                testData)
            Assert.Empty(missingAssemblyIdentities)
            testData.GetMethodData("<>x._Closure$__._Lambda$__0-1").VerifyIL(
"{
...
}")
        End Sub

        <WorkItem(1115543, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1115543")>
        <Fact>
        Public Sub MethodTypeParameterInLambda()
            Const source = "
Class C(Of T)
    Sub M(Of U)()
        Dim lambda =
            Function(u1 As U) As Integer
                Return u1.GetHashCode()
            End Function
        Dim result = lambda(Nothing)
    End Sub
End Class"
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation)
            Dim context = CreateMethodContext(runtime, "C._Closure$__1._Lambda$__1-0")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("GetType(T)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ret
}")

            ' As in dev12, there is no way to bind "U" to the method type parameter.
            ' In particular, there is no way to map back from the lambda to the method
            ' that declares it to determine the source name of the type parameter
            ' (which is also not deducible from the mangled form).
            testData = New CompilationTestData()
            result = context.CompileExpression("GetType(U)", errorMessage, testData)
            Assert.Equal("error BC30002: Type 'U' is not defined.", errorMessage)

            ' Sufficiently well-informed users can reference the type parameter using
            ' its mangled name.
            testData = New CompilationTestData()
            result = context.CompileExpression("GetType($CLS0)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x(Of T, $CLS0).<>m0").VerifyIL("
{
  // Code size       11 (0xb)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldtoken    ""$CLS0""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ret
}")
        End Sub

        <WorkItem(1112496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112496")>
        <Fact>
        Public Sub EvaluateLocalInAsyncLambda()
            Const source = "
Imports System.Threading.Tasks
Module Module1
    Sub Main()
        Dim GetIntegerAsync =
            Async Function() As Task(Of Integer)
                Dim i = 42
                Return i
            End Function
        Dim result = GetIntegerAsync()
    End Sub
End Module"
            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation)
            Dim context = CreateMethodContext(runtime, "Module1._Closure$__.VB$StateMachine___Lambda$__0-0.MoveNext")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("i", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Module1._Closure$__.VB$StateMachine___Lambda$__0-0.$VB$ResumableLocal_i$0 As Integer""
  IL_0006:  ret
}")
        End Sub

        <Fact>
        Public Sub GetTypeOpenGenericType()
            Dim source = "
Imports System

Class C
    Sub M()
    End Sub
End Class"
            Dim compilation = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("GetType(Action(Of ))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldtoken    ""System.Action(Of T)""
  IL_0005:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000a:  ret
}")
        End Sub

        <WorkItem(1068138, "DevDiv")>
        <Fact>
        Public Sub GetSymAttributeByVersion()
            Const source1 = "
Public Class C
    Public Shared Sub M()
        Dim x As Integer = 1
    End Sub
End Class
"

            Const source2 = "
Public Class C
    Public Shared Sub M()
        Dim x As Integer = 1
        Dim y as String = ""a""
    End Sub
End Class
"

            Dim comp1 = CreateCompilationWithMscorlib({source1}, options:=TestOptions.DebugDll)
            Dim comp2 = CreateCompilationWithMscorlib({source2}, options:=TestOptions.DebugDll)

            Using _
                peStream1Unused As New MemoryStream(),
                peStream2 As New MemoryStream(),
                pdbStream1 As New MemoryStream(),
                pdbStream2 As New MemoryStream()

                Assert.True(comp1.Emit(peStream1Unused, pdbStream1).Success)
                Assert.True(comp2.Emit(peStream2, pdbStream2).Success)

                pdbStream1.Position = 0
                pdbStream2.Position = 0
                peStream2.Position = 0

                Dim symReader = SymReaderFactory.CreateReader(pdbStream1)
                symReader.UpdateSymbolStore(pdbStream2)

                Dim runtime = CreateRuntimeInstance(
                    GetUniqueName(),
                    ImmutableArray.Create(MscorlibRef, ExpressionCompilerTestHelpers.IntrinsicAssemblyReference),
                    peStream2.ToArray(),
                    symReader)

                Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
                Dim moduleVersionId As Guid = Nothing
                Dim symReader2 As ISymUnmanagedReader = Nothing
                Dim methodToken As Integer = Nothing
                Dim localSignatureToken As Integer = Nothing
                GetContextState(runtime, "C.M", blocks, moduleVersionId, symReader2, methodToken, localSignatureToken)

                Assert.Same(symReader, symReader2)

                AssertEx.SetEqual(symReader.GetLocalNames(methodToken, methodVersion:=1), "x")
                AssertEx.SetEqual(symReader.GetLocalNames(methodToken, methodVersion:=2), "x", "y")

                Dim context1 = EvaluationContext.CreateMethodContext(
                    Nothing,
                    blocks,
                    MakeDummyLazyAssemblyReaders(),
                    symReader,
                    moduleVersionId,
                    methodToken:=methodToken,
                    methodVersion:=1,
                    ilOffset:=0,
                    localSignatureToken:=localSignatureToken)

                Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
                Dim typeName As String = Nothing
                context1.CompileGetLocals(
                    locals,
                    argumentsOnly:=False,
                    typeName:=typeName,
                    testData:=Nothing)
                AssertEx.SetEqual(locals.Select(Function(l) l.LocalName), "x")

                Dim context2 = EvaluationContext.CreateMethodContext(
                    Nothing,
                    blocks,
                    MakeDummyLazyAssemblyReaders(),
                    symReader,
                    moduleVersionId,
                    methodToken:=methodToken,
                    methodVersion:=2,
                    ilOffset:=0,
                    localSignatureToken:=localSignatureToken)

                locals.Clear()
                context2.CompileGetLocals(
                    locals,
                    argumentsOnly:=False,
                    typeName:=typeName,
                    testData:=Nothing)
                AssertEx.SetEqual(locals.Select(Function(l) l.LocalName), "x", "y")
            End Using
        End Sub

        ''' <summary>
        ''' Ignore accessibility in lambda rewriter.
        ''' </summary>
        <Fact>
        Public Sub LambdaRewriterIgnoreAccessibility()
            Const source =
"Imports System.Linq
Class C
    Shared Sub M()
        Dim q = {New C()}.AsQueryable()
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib(
                {source},
                references:={SystemCoreRef},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("q.Where(Function(c) True)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       64 (0x40)
  .maxstack  6
  .locals init (System.Linq.IQueryable(Of C) V_0, //q
                System.Linq.Expressions.ParameterExpression V_1)
  IL_0000:  ldloc.0
  IL_0001:  ldtoken    ""C""
  IL_0006:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_000b:  ldstr      ""c""
  IL_0010:  call       ""Function System.Linq.Expressions.Expression.Parameter(System.Type, String) As System.Linq.Expressions.ParameterExpression""
  IL_0015:  stloc.1
  IL_0016:  ldc.i4.1
  IL_0017:  box        ""Boolean""
  IL_001c:  ldtoken    ""Boolean""
  IL_0021:  call       ""Function System.Type.GetTypeFromHandle(System.RuntimeTypeHandle) As System.Type""
  IL_0026:  call       ""Function System.Linq.Expressions.Expression.Constant(Object, System.Type) As System.Linq.Expressions.ConstantExpression""
  IL_002b:  ldc.i4.1
  IL_002c:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0031:  dup
  IL_0032:  ldc.i4.0
  IL_0033:  ldloc.1
  IL_0034:  stelem.ref
  IL_0035:  call       ""Function System.Linq.Expressions.Expression.Lambda(Of System.Func(Of C, Boolean))(System.Linq.Expressions.Expression, ParamArray System.Linq.Expressions.ParameterExpression()) As System.Linq.Expressions.Expression(Of System.Func(Of C, Boolean))""
  IL_003a:  call       ""Function System.Linq.Queryable.Where(Of C)(System.Linq.IQueryable(Of C), System.Linq.Expressions.Expression(Of System.Func(Of C, Boolean))) As System.Linq.IQueryable(Of C)""
  IL_003f:  ret
}")
        End Sub

        ''' <summary>
        ''' Ignore accessibility in async rewriter.
        ''' </summary>
        <WorkItem(1813, "https://github.com/dotnet/roslyn/issues/1813")>
        <Fact>
        Public Sub AsyncRewriterIgnoreAccessibility()
            Const source =
"Imports System
Imports System.Threading.Tasks
Class C
End Class
Module M
    Sub F(Of T)(f As Func(Of Task(Of T)))
    End Sub
    Sub M()
    End Sub
End Module"
            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(runtime, methodName:="M.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("F(Async Function() New C())", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       42 (0x2a)
  .maxstack  2
  IL_0000:  ldsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of System.Threading.Tasks.Task(Of C))""
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of System.Threading.Tasks.Task(Of C))""
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     ""<>x._Closure$__.$I As <>x._Closure$__""
  IL_0013:  ldftn      ""Function <>x._Closure$__._Lambda$__0-0() As System.Threading.Tasks.Task(Of C)""
  IL_0019:  newobj     ""Sub System.Func(Of System.Threading.Tasks.Task(Of C))..ctor(Object, System.IntPtr)""
  IL_001e:  dup
  IL_001f:  stsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of System.Threading.Tasks.Task(Of C))""
  IL_0024:  call       ""Sub M.F(Of C)(System.Func(Of System.Threading.Tasks.Task(Of C)))""
  IL_0029:  ret
}")
        End Sub

        <WorkItem(1145125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1145125")>
        <Fact>
        Public Sub LocalInLambda()
            Dim source = "
Imports System
Class C
    Sub M(f As Func(Of Integer))
        Dim x = 42
    End Sub
End Class"

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source), options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("M(Function() x)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (Integer V_0, //x
                <>x._Closure$__0-0 V_1) //$VB$Closure_0
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  ldloc.0
  IL_0008:  stfld      ""<>x._Closure$__0-0.$VB$Local_x As Integer""
  IL_000d:  ldarg.0
  IL_000e:  ldloc.1
  IL_000f:  ldftn      ""Function <>x._Closure$__0-0._Lambda$__0() As Integer""
  IL_0015:  newobj     ""Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)""
  IL_001a:  callvirt   ""Sub C.M(System.Func(Of Integer))""
  IL_001f:  ret
}")
        End Sub

        <WorkItem(1145125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1145125")>
        <Fact>
        Public Sub CapturedLocalInLambda()
            Dim source = "
Imports System
Class C
    Sub M(f As Func(Of Integer))
        Dim x = 42
        M(Function() x)
    End Sub
End Class"

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source))
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("M(Function() x)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (C._Closure$__1-0 V_0, //$VB$Closure_0
                <>x._Closure$__0-0 V_1) //$VB$Closure_0
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  stloc.1
  IL_0006:  ldloc.1
  IL_0007:  ldloc.0
  IL_0008:  stfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Closure_0 As C._Closure$__1-0""
  IL_000d:  ldarg.0
  IL_000e:  ldloc.1
  IL_000f:  ldftn      ""Function <>x._Closure$__0-0._Lambda$__0() As Integer""
  IL_0015:  newobj     ""Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)""
  IL_001a:  callvirt   ""Sub C.M(System.Func(Of Integer))""
  IL_001f:  ret
}")
        End Sub

        <WorkItem(1145125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1145125")>
        <Fact>
        Public Sub CapturedParameterAndLocalInLambda()
            Dim source = "
Imports System
Class C
    Sub M(x As Integer)
        F(Function() x)
        If True Then
            Dim y = 42.0
            F(Function() y)
        End If
    End Sub
    Function F(p As Func(Of Integer)) As Integer
        Return p()
    End Function
End Class"

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source))
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M", atLineNumber:=6)

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("F(Function() x + y)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (C._Closure$__1-0 V_0, //$VB$Closure_0
                C._Closure$__1-1 V_1, //$VB$Closure_1
                <>x._Closure$__0-0 V_2) //$VB$Closure_0
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  ldloc.0
  IL_0008:  stfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Closure_0 As C._Closure$__1-0""
  IL_000d:  ldloc.2
  IL_000e:  ldloc.1
  IL_000f:  stfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Closure_1 As C._Closure$__1-1""
  IL_0014:  ldarg.0
  IL_0015:  ldloc.2
  IL_0016:  ldftn      ""Function <>x._Closure$__0-0._Lambda$__0() As Integer""
  IL_001c:  newobj     ""Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)""
  IL_0021:  callvirt   ""Function C.F(System.Func(Of Integer)) As Integer""
  IL_0026:  ret
}")
            testData.GetMethodData("<>x._Closure$__0-0._Lambda$__0").VerifyIL("
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Closure_0 As C._Closure$__1-0""
  IL_0006:  ldfld      ""C._Closure$__1-0.$VB$Local_x As Integer""
  IL_000b:  conv.r8
  IL_000c:  ldarg.0
  IL_000d:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Closure_1 As C._Closure$__1-1""
  IL_0012:  ldfld      ""C._Closure$__1-1.$VB$Local_y As Double""
  IL_0017:  add
  IL_0018:  call       ""Function System.Math.Round(Double) As Double""
  IL_001d:  conv.ovf.i4
  IL_001e:  ret
}")
        End Sub

        <WorkItem(1145125, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1145125")>
        <Fact>
        Public Sub CapturedParameterAndLocalInNestedLambda()
            Dim source = "
Imports System
Class C
    Sub M(x As Integer)
        F(Function() x)
        If True Then
            Dim y = 42.0
            F(Function()
                  Dim z = 2600
                  Return F(Function() x + y + z)
              End Function)
        End If
    End Sub
    Function F(p As Func(Of Integer)) As Integer
        Return p()
    End Function
End Class"

            Dim comp = CreateCompilationWithMscorlib45AndVBRuntime(MakeSources(source))
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C._Closure$__1-2._Lambda$__2")

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("F(Function() x + y + z)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (<>x._Closure$__0-0 V_0) //$VB$Closure_0
  IL_0000:  newobj     ""Sub <>x._Closure$__0-0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C._Closure$__1-2""
  IL_000d:  ldloc.0
  IL_000e:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C._Closure$__1-2""
  IL_0013:  ldfld      ""C._Closure$__1-2.$VB$NonLocal_$VB$Closure_3 As C._Closure$__1-1""
  IL_0018:  ldfld      ""C._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As C._Closure$__1-0""
  IL_001d:  ldfld      ""C._Closure$__1-0.$VB$Me As C""
  IL_0022:  ldloc.0
  IL_0023:  ldftn      ""Function <>x._Closure$__0-0._Lambda$__0() As Integer""
  IL_0029:  newobj     ""Sub System.Func(Of Integer)..ctor(Object, System.IntPtr)""
  IL_002e:  callvirt   ""Function C.F(System.Func(Of Integer)) As Integer""
  IL_0033:  ret
}")
            testData.GetMethodData("<>x._Closure$__0-0._Lambda$__0").VerifyIL("
{
  // Code size       59 (0x3b)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C._Closure$__1-2""
  IL_0006:  ldfld      ""C._Closure$__1-2.$VB$NonLocal_$VB$Closure_3 As C._Closure$__1-1""
  IL_000b:  ldfld      ""C._Closure$__1-1.$VB$NonLocal_$VB$Closure_2 As C._Closure$__1-0""
  IL_0010:  ldfld      ""C._Closure$__1-0.$VB$Local_x As Integer""
  IL_0015:  conv.r8
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C._Closure$__1-2""
  IL_001c:  ldfld      ""C._Closure$__1-2.$VB$NonLocal_$VB$Closure_3 As C._Closure$__1-1""
  IL_0021:  ldfld      ""C._Closure$__1-1.$VB$Local_y As Double""
  IL_0026:  add
  IL_0027:  ldarg.0
  IL_0028:  ldfld      ""<>x._Closure$__0-0.$VB$Local_$VB$Me As C._Closure$__1-2""
  IL_002d:  ldfld      ""C._Closure$__1-2.$VB$Local_z As Integer""
  IL_0032:  conv.r8
  IL_0033:  add
  IL_0034:  call       ""Function System.Math.Round(Double) As Double""
  IL_0039:  conv.ovf.i4
  IL_003a:  ret
}")
        End Sub

        <Fact>
        Public Sub NullAnonymousTypeInstance()
            Const source =
"Class C
    Shared Sub Main()
    End Sub
End Class"
            Dim testData = Evaluate(source, OutputKind.ConsoleApplication, "C.Main", "If(False, New With {.P = 1}, Nothing)")
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Dim returnType = DirectCast(methodData.Method.ReturnType, NamedTypeSymbol)
            Assert.True(returnType.IsAnonymousType)
            methodData.VerifyIL(
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldnull
  IL_0001:  ret
}")
        End Sub

        ''' <summary>
        ''' DkmClrInstructionAddress.ILOffset is set to UInteger.MaxValue
        ''' if the instruction does not map to an IL offset.
        ''' </summary>
        <WorkItem(1185315, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1185315")>
        <Fact>
        Public Sub NoILOffset()
            Const source =
"Class C
    Shared Sub M(x As Integer)
        Dim y As Integer
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)

            Dim blocks As ImmutableArray(Of MetadataBlock) = Nothing
            Dim moduleVersionId As Guid = Nothing
            Dim symReader As ISymUnmanagedReader = Nothing
            Dim methodToken = 0
            Dim localSignatureToken = 0
            GetContextState(runtime, "C.M", blocks, moduleVersionId, symReader, methodToken, localSignatureToken)

            Dim ilOffset = ExpressionCompilerTestHelpers.GetOffset(methodToken, symReader)
            Dim context = EvaluationContext.CreateMethodContext(
                Nothing,
                blocks,
                MakeDummyLazyAssemblyReaders(),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion:=1,
                ilOffset:=ExpressionCompilerTestHelpers.NoILOffset,
                localSignatureToken:=localSignatureToken)

            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("x + y", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (Integer V_0) //y
  IL_0000:  ldarg.0
  IL_0001:  ldloc.0
  IL_0002:  add.ovf
  IL_0003:  ret
}")

            ' Verify the context is re-used for ILOffset == 0.
            Dim previous = context
            context = EvaluationContext.CreateMethodContext(
                New VisualBasicMetadataContext(blocks, previous),
                blocks,
                MakeDummyLazyAssemblyReaders(),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion:=1,
                ilOffset:=0,
                localSignatureToken:=localSignatureToken)
            Assert.Same(previous, context)

            ' Verify the context is re-used for NoILOffset.
            previous = context
            context = EvaluationContext.CreateMethodContext(
                New VisualBasicMetadataContext(blocks, previous),
                blocks,
                MakeDummyLazyAssemblyReaders(),
                symReader,
                moduleVersionId,
                methodToken,
                methodVersion:=1,
                ilOffset:=ExpressionCompilerTestHelpers.NoILOffset,
                localSignatureToken:=localSignatureToken)
            Assert.Same(previous, context)
        End Sub

        <WorkItem(3939, "https://github.com/dotnet/roslyn/issues/3939")>
        <Fact>
        Public Sub NameofInstanceInSharedContext()
            Const source = "
Class C
    Private X As Integer
    Shared Function M() As String
        Return Nameof(X)
    End Function
End Class
"
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="Nameof(X)",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (String V_0) //M
  IL_0000:  ldstr      ""X""
  IL_0005:  ret
}
")
        End Sub

        <WorkItem(3939, "https://github.com/dotnet/roslyn/issues/3939")>
        <Fact>
        Public Sub NameofInstanceInSharedContext_ExplicitMe()
            Const source = "
Class C
    Private X As Integer
    Shared Function M() As String
        Return Nameof(X)
    End Function
End Class
"
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                source,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="Nameof(Me.X)",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("error BC30043: 'Me' is valid only within an instance method.", errorMessage)
        End Sub

        <Fact>
        Public Sub ImportsInAsyncLambda()
            Const source =
"Imports System.Linq
Class C
    Shared Sub M()
        Dim f As System.Action =
            Async Sub()
                Dim c = {1, 2, 3}
                c.Select(Function(i) i)
            End Sub
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib45AndVBRuntime({Parse(source)}, options:=TestOptions.DebugDll, references:={SystemCoreRef})
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(runtime, "C._Closure$__.VB$StateMachine___Lambda$__1-0.MoveNext")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("c.Where(Function(n) n > 0)", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (Integer V_0,
                System.Exception V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__.VB$StateMachine___Lambda$__1-0.$VB$ResumableLocal_c$0 As Integer()""
  IL_0006:  ldsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of Integer, Boolean)""
  IL_000b:  brfalse.s  IL_0014
  IL_000d:  ldsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of Integer, Boolean)""
  IL_0012:  br.s       IL_002a
  IL_0014:  ldsfld     ""<>x._Closure$__.$I As <>x._Closure$__""
  IL_0019:  ldftn      ""Function <>x._Closure$__._Lambda$__0-0(Integer) As Boolean""
  IL_001f:  newobj     ""Sub System.Func(Of Integer, Boolean)..ctor(Object, System.IntPtr)""
  IL_0024:  dup
  IL_0025:  stsfld     ""<>x._Closure$__.$I0-0 As System.Func(Of Integer, Boolean)""
  IL_002a:  call       ""Function System.Linq.Enumerable.Where(Of Integer)(System.Collections.Generic.IEnumerable(Of Integer), System.Func(Of Integer, Boolean)) As System.Collections.Generic.IEnumerable(Of Integer)""
  IL_002f:  ret
}")
        End Sub

    End Class

End Namespace