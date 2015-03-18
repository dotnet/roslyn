' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Reflection.Metadata
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.Utilities
Imports Xunit
Imports CommonResources = Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests.Resources

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class ExpressionCompilerTests
        Inherits ExpressionCompilerTestBase

        Const SimpleSource = "
Class C
    Shared Sub M()
    End Sub
End Class
"

        ''' <summary>
        ''' Each assembly should have a unique MVID and assembly name.
        ''' </summary>
        <WorkItem(1029280)>
        <Fact>
        Public Sub UniqueModuleVersionId()
            Dim comp = CreateCompilationWithMscorlib({SimpleSource}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim errorMessage As String = Nothing
            Dim result = context.CompileExpression("1", errorMessage)
            Dim mvid1 = result.Assembly.GetModuleVersionId()
            Dim name1 = result.Assembly.GetAssemblyName()
            Assert.NotEqual(mvid1, Guid.Empty)

            context = CreateMethodContext(runtime, "C.M", previous:=New VisualBasicMetadataContext(context))
            result = context.CompileExpression("2", errorMessage)
            Dim mvid2 = result.Assembly.GetModuleVersionId()
            Dim name2 = result.Assembly.GetAssemblyName()
            Assert.NotEqual(mvid2, Guid.Empty)
            Assert.NotEqual(mvid2, mvid1)
            Assert.NotEqual(name2.FullName, name1.FullName)
        End Sub

        <Fact>
        Public Sub ParseError()
            Dim comp = CreateCompilationWithMscorlib({SimpleSource}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim errorMessage As String = Nothing
            Dim result = context.CompileExpression("M(", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Null(result.Assembly)
            Assert.Equal("(1) : error BC30201: Expression expected.", errorMessage)
        End Sub

        ''' <summary>
        ''' Diagnostics should be formatted with the CurrentUICulture.
        ''' </summary>
        <WorkItem(941599)>
        <Fact>
        Public Sub FormatterCultureInfo()
            Dim previousCulture = Thread.CurrentThread.CurrentCulture
            Dim previousUICulture = Thread.CurrentThread.CurrentUICulture
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR")
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE")
            Try
                Dim comp = CreateCompilationWithMscorlib({SimpleSource}, compOptions:=TestOptions.DebugDll)
                Dim runtime = CreateRuntimeInstance(comp)
                Dim context = CreateMethodContext(
                    runtime,
                    "C.M")
                Dim resultProperties As ResultProperties = Nothing
                Dim errorMessage As String = Nothing
                Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
                Dim result = context.CompileExpression(
                    DefaultInspectionContext.Instance,
                    "M(",
                    DkmEvaluationFlags.TreatAsExpression,
                    CustomDiagnosticFormatter.Instance,
                    resultProperties,
                    errorMessage,
                    missingAssemblyIdentities,
                    preferredUICulture:=Nothing,
                    testData:=Nothing)
                Assert.Empty(missingAssemblyIdentities)
                Assert.Null(result.Assembly)
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
            Assert.Equal("(1) : error BC30451: 'x' is not declared. It may be inaccessible due to its protection level.", errorMessage)
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, includeSymbols:=False)
            For Each moduleInstance In runtime.Modules
                Assert.Null(moduleInstance.SymReader)
            Next

            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")

            ' Local reference.
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("F(y)", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("(1) : error BC30451: 'y' is not declared. It may be inaccessible due to its protection level.", errorMessage)

            ' No local reference.
            testData = New CompilationTestData()
            result = context.CompileExpression("F(x)", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
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
            Dim compA = CreateCompilationWithMscorlib({sourceA}, compOptions:=TestOptions.DebugDll)
            Dim referenceA = compA.EmitToImageReference()
            Dim compB = CreateCompilationWithMscorlib({sourceB}, compOptions:=TestOptions.DebugDll, references:={referenceA})
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            compB.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Const methodVersion = 1

            Dim previous As VisualBasicMetadataContext = Nothing
            Dim startOffset = 0
            Dim endOffset = 0
            Dim runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), references, exeBytes, New SymReader(pdbBytes))
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
            Dim context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, startOffset, localSignatureToken)
            Assert.Null(previous)
            previous = new VisualBasicMetadataContext(context)

            ' At end of outer scope - not reused because of the nested scope.
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, endOffset, localSignatureToken)
            Assert.NotEqual(context, previous.EvaluationContext) ' Not required, just documentary.

            ' At type context.
            context = EvaluationContext.CreateTypeContext(previous, methodBlocks, moduleVersionId, typeToken)
            Assert.NotEqual(context, previous.EvaluationContext)
            Assert.Null(context.MethodContextReuseConstraints)
            Assert.Equal(context.Compilation, previous.Compilation)

            ' Step through entire method.
            Dim previousScope As Scope = Nothing
            previous = new VisualBasicMetadataContext(context)
            For offset = startOffset To endOffset - 1
                Dim scope = scopes.GetInnermostScope(offset)
                Dim constraints = previous.EvaluationContext.MethodContextReuseConstraints
                If constraints.HasValue Then
                    Assert.Equal(scope Is previousScope, constraints.GetValueOrDefault().AreSatisfied(moduleVersionId, methodToken, methodVersion, offset))
                End If

                context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, offset, localSignatureToken)
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
                previous = new VisualBasicMetadataContext(context)
            Next

            ' With different references.
            Dim fewerReferences = references.Remove(referenceA)
            Assert.Equal(fewerReferences.Length, references.Length - 1)
            runtime = CreateRuntimeInstance(ExpressionCompilerUtilities.GenerateUniqueName(), fewerReferences, exeBytes, New SymReader(pdbBytes))
            methodBlocks = Nothing
            moduleVersionId = Nothing
            symReader = Nothing
            methodToken = 0
            localSignatureToken = 0
            GetContextState(runtime, "C.F", methodBlocks, moduleVersionId, symReader, methodToken, localSignatureToken)

            ' Different references. No reuse.
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, endOffset - 1, localSignatureToken)
            Assert.NotEqual(context, previous.EvaluationContext)
            Assert.True(previous.EvaluationContext.MethodContextReuseConstraints.Value.AreSatisfied(moduleVersionId, methodToken, methodVersion, endOffset - 1))
            Assert.NotEqual(context.Compilation, previous.Compilation)
            previous = new VisualBasicMetadataContext(context)

            ' Different method. Should reuse Compilation.
            GetContextState(runtime, "C.G", methodBlocks, moduleVersionId, symReader, methodToken, localSignatureToken)
            context = EvaluationContext.CreateMethodContext(previous, methodBlocks, MakeDummyLazyAssemblyReaders(), symReader, moduleVersionId, methodToken, methodVersion, ilOffset:=0, localSignatureToken:=localSignatureToken)
            Assert.NotEqual(context, previous.EvaluationContext)
            Assert.False(previous.EvaluationContext.MethodContextReuseConstraints.Value.AreSatisfied(moduleVersionId, methodToken, methodVersion, 0))
            Assert.Equal(context.Compilation, previous.Compilation)

            ' No EvaluationContext. Should reuse Compilation
            previous = New VisualBasicMetadataContext(previous.MetadataBlocks)
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, includeSymbols:=False)
            Dim context = CreateMethodContext(runtime, methodName:="C.F")
            Dim errorMessage As String = Nothing

            ' No format specifiers.
            Dim result = context.CompileExpression("x", errorMessage)
            CheckFormatSpecifiers(result)

            ' Format specifiers on expression.
            result = context.CompileExpression("x,", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,,", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x y", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'y' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x yy zz", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'yy' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,,y", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,yy,zz,ww", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            CheckFormatSpecifiers(result, "yy", "zz", "ww")
            result = context.CompileExpression("x, y z", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'z' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x, y  ,  z  ", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            CheckFormatSpecifiers(result, "y", "z")
            result = context.CompileExpression("x, y, z,", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: ',' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x,y,z;w", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'z;w' is not a valid format specifier", errorMessage)
            result = context.CompileExpression("x, y;, z", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("error BC37237: 'y;' is not a valid format specifier", errorMessage)

            ' Format specifiers after comment (ignored).
            result = context.CompileExpression("x ' ,f", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            CheckFormatSpecifiers(result)

            ' Format specifiers on assignment value.
            result = context.CompileAssignment("x", "Nothing, y", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Null(result.Assembly)
            Assert.Null(result.FormatSpecifiers)
            Assert.Equal("(1) : error BC30035: Syntax error.", errorMessage)

            ' Format specifiers, no expression.
            result = context.CompileExpression(",f", errorMessage, Nothing, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("(1) : error BC30201: Expression expected.", errorMessage)
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

            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.F", atLineNumber:=999)
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("a(0)", resultProperties, errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        4 (0x4)
  .maxstack  2
  .locals init (String V_0, //F
  Object V_1,
  Boolean V_2,
  String V_3, //s
  Boolean V_4)
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "B.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            Dim result = context.CompileExpression("If(Me.F(), If(Me.G, Me.P))", resultProperties, errorMessage, testData)
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
            result = context.CompileExpression("F(AddressOf Me.F)", resultProperties, errorMessage, testData)
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
            result = context.CompileExpression("F(new System.Func(Of String)(AddressOf Me.F))", resultProperties, errorMessage, testData)
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "B.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            Dim result = context.CompileExpression("If(MyClass.F(), If(MyClass.G, MyClass.P))", resultProperties, errorMessage, testData)
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
            result = context.CompileExpression("F(AddressOf MyClass.F)", resultProperties, errorMessage, testData)
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
            result = context.CompileExpression("F(new System.Func(Of String)(AddressOf MyClass.F))", resultProperties, errorMessage, testData)
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "B.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            Dim result = context.CompileExpression("If(MyBase.F(), If(MyBase.G, MyBase.P))", resultProperties, errorMessage, testData)
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
            result = context.CompileExpression("F(AddressOf MyBase.F)", resultProperties, errorMessage, testData)
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
            result = context.CompileExpression("F(new System.Func(Of String)(AddressOf MyBase.F))", resultProperties, errorMessage, testData)
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
                Object V_4) //o
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
                    symReader:=New SymReader(pdbBytes.ToArray()))

            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("c1.F", resultProperties, errorMessage, testData)
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
        <WorkItem(884627)>
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
                    symReader:=New SymReader(pdbBytes.ToArray()))

            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("c1.F", resultProperties, errorMessage, testData)
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

        <WorkItem(1012956)>
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
                    symReader:=New SymReader(pdbBytes.ToArray()))

            Dim context = CreateMethodContext(runtime, methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing

            Dim testData = New CompilationTestData()
            context.CompileExpression("s", resultProperties, errorMessage, testData)
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
            context.CompileExpression("f", resultProperties, errorMessage, testData)
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
            context.CompileExpression("i", resultProperties, errorMessage, testData)
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

        <WorkItem(1034549)>
        <Fact>
        Public Sub AssignLocal()
            Const source =
"Class C
    Shared Sub M()
        Dim x = 0
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
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

            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
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
                SimpleSource,
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
    Shared Sub M(o As C, i As Integer, a As Action)
    End Sub
End Class"
            Dim compilation0 = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
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
            Dim compilation0 = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                    runtime,
                    methodName:="C.M")

            CheckResultProperties(context, "F", DkmClrCompilationResultFlags.None)
            CheckResultProperties(context, "RF", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "CF", DkmClrCompilationResultFlags.ReadOnlyResult)

            ' Note: flags are always None in error cases.

            CheckResultProperties(context, "E", DkmClrCompilationResultFlags.None, "(1,2): error BC32022: 'Public Event E As C.EEventHandler' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.")
            CheckResultProperties(context, "CE", DkmClrCompilationResultFlags.None, "(1,2): error BC32022: 'Public Event CE As Action' is an event, and cannot be called directly. Use a 'RaiseEvent' statement to raise an event.")

            CheckResultProperties(context, "RP", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "WP", DkmClrCompilationResultFlags.None, "(1,2): error BC30524: Property 'WP' is 'WriteOnly'.")
            CheckResultProperties(context, "RWP", DkmClrCompilationResultFlags.None)

            CheckResultProperties(context, "RP(1)", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "WP(1)", DkmClrCompilationResultFlags.None, "(1,2): error BC30524: Property 'WP' is 'WriteOnly'.")
            CheckResultProperties(context, "RWP(1)", DkmClrCompilationResultFlags.None)

            CheckResultProperties(context, "M()", DkmClrCompilationResultFlags.PotentialSideEffect Or DkmClrCompilationResultFlags.ReadOnlyResult)

            CheckResultProperties(context, "Nothing", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "1", DkmClrCompilationResultFlags.ReadOnlyResult)
            CheckResultProperties(context, "AddressOf M", DkmClrCompilationResultFlags.None, "(1,2): error BC30491: Expression does not produce a value.")
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
            Dim compilation0 = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
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
            Assert.NotEqual(expectedErrorMessage Is Nothing, result.Assembly Is Nothing)
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
                compOptions:=TestOptions.DebugDll,
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
            Assert.Equal("(1) : error BC30524: Property 'P' is 'WriteOnly'.", errorMessage)
        End Sub

        ''' <summary>
        ''' Expression that does not return a value.
        ''' </summary>
        <Fact>
        Public Sub EvaluateVoidExpression()
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                SimpleSource,
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

        <Fact, WorkItem(1112400)>
        Public Sub EvaluateMethodGroup()
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing

            Dim testData = Evaluate(
                SimpleSource,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="AddressOf C.M",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("(1) : error BC30491: Expression does not produce a value.", errorMessage)

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

        <WorkItem(964, "GitHub")>
        <Fact(Skip:="964")>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("x.@a", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
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
                compOptions:=TestOptions.DebugDll,
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
            Assert.Equal("(1) : error BC30112: 'N' is a namespace and cannot be used as an expression.", errorMessage)
        End Sub

        <Fact>
        Public Sub EvaluateType()
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = Evaluate(
                SimpleSource,
                OutputKind.DynamicallyLinkedLibrary,
                methodName:="C.M",
                expr:="C",
                resultProperties:=resultProperties,
                errorMessage:=errorMessage)
            Assert.Equal("(1) : error BC30109: 'C' is a class type and cannot be used as an expression.", errorMessage)
        End Sub

        <WorkItem(986227)>
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
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (E(Of T) V_0, //e1
  E(Of T) V_1) //e2
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
{
  IL_0002:  leave.s    IL_0014
}
  catch E(Of T)
{
  IL_0004:  dup
  IL_0005:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(System.Exception)""
  IL_000a:  stloc.1
  IL_000b:  ldloc.1
  IL_000c:  stloc.0
  IL_000d:  call       ""Sub Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError()""
  IL_0012:  leave.s    IL_0014
}
  IL_0014:  ldloc.0
  IL_0015:  ret
}")
        End Sub

        <WorkItem(986227)>
        <Fact(Skip:="986227")>
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
            ' Both locals of type T from <>m0(Of T): the original local
            ' and the temporary for "New T()" in (New T()).F = 1.
            Assert.Equal(locals.Length, 2)
            For Each local In locals
                Dim localType = DirectCast(local.Type, TypeSymbol)
                Assert.Equal(localType.ContainingSymbol, method)
            Next
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
...
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
                compOptions:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())

            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="A.B.M1")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("If(GetType(V), GetType(X))", resultProperties, errorMessage, testData)
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
            context.CompileExpression("If(GetType(T), GetType(U))", resultProperties, errorMessage, testData)
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
                compOptions:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="B.M",
                atLineNumber:=999)
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("Me.F(y)", resultProperties, errorMessage, testData)
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
            context.CompileExpression("MyClass.F(y)", resultProperties, errorMessage, testData)
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
            context.CompileExpression("MyBase.F(x)", resultProperties, errorMessage, testData)
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
                compOptions:=TestOptions.DebugDll.WithModuleName("MODULE"),
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("{ 1, 2, 3, 4, 5 }", resultProperties, errorMessage, testData)
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(methodData.Method.ReturnType.ToDisplayString(), "Integer()")
            methodData.VerifyIL(
"{
  // Code size       18 (0x12)
  .maxstack  3
  IL_0000:  ldc.i4.5
  IL_0001:  newarr     ""Integer""
  IL_0006:  dup
  IL_0007:  ldtoken    ""<PrivateImplementationDetails><{#Module#}.dll>.__StaticArrayInitTypeSize=20 <PrivateImplementationDetails><{#Module#}.dll>.1036C5F8EF306104BD582D73E555F4DAE8EECB24""
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
                compOptions:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("F(Function() o + 1)", resultProperties, errorMessage, testData)
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
                compOptions:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(compilation0)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("F(Function()
                        Return Nothing
                    End Function)", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal(errorMessage, "(1) : error BC36675: Statement lambdas cannot be converted to expression trees.")
        End Sub

        <WorkItem(1096605)>
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
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("G(Async Function() Await F())", resultProperties, errorMessage, testData)
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
  Boolean V_4)
  IL_0000:  newobj     ""Sub C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  box        ""Integer""
  IL_000c:  stfld      ""C.F As Object""
  IL_0011:  ret
}")
        End Sub

        <WorkItem(958448)>
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

        <WorkItem(958448)>
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

        <WorkItem(994485)>
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

            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(
                runtime,
                methodName:="C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("e.HasValue", resultProperties, errorMessage, testData)
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

        <WorkItem(1000946)>
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
            Assert.Equal("(1) : error BC32027: 'MyBase' must be followed by '.' and an identifier.", errorMessage)
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
  IL_0001:  dup
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
            Assert.Equal("(1) : error BC30044: 'MyBase' is not valid within a structure.", errorMessage)
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
            Assert.Equal("(1) : error BC32001: 'MyBase' is not valid within a Module.", errorMessage)
        End Sub

        <WorkItem(1010922)>
        <Fact>
        Public Sub IntegerOverflow()
            Const source = "
Class C
    Sub M()
    End Sub
End Class
"

            Dim checkedComp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(checkedComp)
            Dim context = CreateMethodContext(runtime, methodName:="C.M")

            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("2147483647 + 1", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("(1) : error BC30439: Constant expression not representable in type 'Integer'.", errorMessage)

            ' As in dev12, the global "unchecked" option is not respected at debug time.
            Dim uncheckedComp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll.WithOverflowChecks(False))
            runtime = CreateRuntimeInstance(uncheckedComp)
            context = CreateMethodContext(runtime, methodName:="C.M")

            errorMessage = Nothing
            context.CompileExpression("2147483647 + 1", resultProperties:=Nothing, [error]:=errorMessage, testData:=Nothing, formatter:=VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("(1) : error BC30439: Constant expression not representable in type 'Integer'.", errorMessage)
        End Sub

        <WorkItem(1012956)>
        <Fact>
        Public Sub AssignmentConversion()
            Const source = "
Class C
    Sub M(u As UInteger)
    End Sub
End Class
"

            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, methodName:="C.M")

            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileAssignment("u", "2147483647 + 1", errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal("(1) : error BC30439: Constant expression not representable in type 'Integer'.", errorMessage)
        End Sub

        <WorkItem(1016530)>
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

            Assert.Equal("(1) : error BC30201: Expression expected.", errorMessage)
        End Sub

        <WorkItem(1015887)>
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

        <WorkItem(1015887)>
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

        <WorkItem(1028808)>
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
                symReader:=New SymReader(pdbBytes.ToArray()))

            Dim context = CreateMethodContext(
                runtime,
                methodName:="C._Closure$__1._Lambda$__2")
            Dim errorMessage As String = Nothing
            Dim resultProperties As ResultProperties = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("x", resultProperties, errorMessage, testData)
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

        <WorkItem(1030236)>
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

        <WorkItem(1030236)>
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

        <WorkItem(1030236)>
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

        <WorkItem(1030236)>
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

        <WorkItem(1042918)>
        <WorkItem(964, "GitHub")>
        <Fact(Skip:="964")>
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

            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("Me?.F()", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")
            methodData.VerifyIL(
"{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (Integer? V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000d
  IL_0003:  ldloca.s   V_0
  IL_0005:  initobj    ""Integer?""
  IL_000b:  ldloc.0
  IL_000c:  ret
  IL_000d:  ldarg.0
  IL_000e:  call       ""Function C.F() As Integer""
  IL_0013:  newobj     ""Sub Integer?..ctor(Integer)""
  IL_0018:  ret
}")

            testData = New CompilationTestData()
            result = context.CompileExpression("(Me?.F())", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")

            testData = New CompilationTestData()
            result = context.CompileExpression("Me?.X.@a", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            Assert.Null(errorMessage)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.SpecialType, SpecialType.System_String)
            methodData.VerifyIL(
"{
  // Code size       32 (0x20)
  .maxstack  3
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
            result = context.CompileExpression("(New C())?.G()?.F()", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")

            testData = New CompilationTestData()
            result = context.CompileExpression("(New C())?.G().F()", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.Equal(DirectCast(methodData.Method, MethodSymbol).ReturnType.ToDisplayString(), "Integer?")

            testData = New CompilationTestData()
            result = context.CompileExpression("G()?.M()", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            methodData = testData.GetMethodData("<>x.<>m0")
            Assert.True(DirectCast(methodData.Method, MethodSymbol).IsSub)
            methodData.VerifyIL(
"{
  // Code size       17 (0x11)
  .maxstack  2
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
            result = context.CompileExpression("(G()?.M())", resultProperties, errorMessage, testData, VisualBasicDiagnosticFormatter.Instance)
            Assert.Equal(errorMessage, "(1) : error BC30491: Expression does not produce a value.")
        End Sub

        <WorkItem(1024137)>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_1_F.MoveNext")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("x", resultProperties, errorMessage, testData)
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

        <WorkItem(1024137)>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_1_F.MoveNext")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("x", resultProperties, errorMessage, testData)
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

        <WorkItem(1079749)>
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
            Assert.Equal("(1) : error BC36593: Expression of type 'String' is not queryable. Make sure you are not missing an assembly reference and/or namespace import for the LINQ provider.", errorMessage)
        End Sub

        <WorkItem(1079762)>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C._Closure$__2-0._Lambda$__0")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("z", resultProperties, errorMessage, testData)
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
            context.CompileExpression("y", resultProperties, errorMessage, testData)
            Assert.Equal("(1,2): error BC30451: 'y' is not declared. It may be inaccessible due to its protection level.", errorMessage)
        End Sub

        <WorkItem(1014763)>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.I")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("GetType(T)", resultProperties, errorMessage, testData)
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

        <WorkItem(1014763)>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_1_I.MoveNext")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData As New CompilationTestData()
            context.CompileExpression("GetType(T)", resultProperties, errorMessage, testData)
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

        <WorkItem(1085642)>
        <Fact>
        Public Sub ModuleWithBadImageFormat()
            Dim source = "
Class C
    Dim F As Integer = 1
    Shared Sub M()
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll, assemblyName:=GetUniqueName())
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
            modulesBuilder.Add(exeReference.ToModuleInstance(exeBytes, New SymReader(pdbBytes)))
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

        <WorkItem(1089688)>
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
            Dim libRef = CreateCompilationWithMscorlib({libSource}, assemblyName:="Lib", compOptions:=TestOptions.DebugDll).EmitToImageReference()
            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef, libRef}, TestOptions.DebugDll)

            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim unusedReferences As ImmutableArray(Of MetadataReference) = Nothing
            Dim result = comp.EmitAndGetReferences(exeBytes, pdbBytes, unusedReferences)
            Assert.True(result)

            Dim runtime = CreateRuntimeInstance(GetUniqueName(), ImmutableArray.Create(MscorlibRef), exeBytes, New SymReader(pdbBytes))
            Dim context = CreateMethodContext(runtime, "C.M")

            Const expectedError1 = "(1,1): error BC30652: Reference required to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project."
            Const expectedError2 = "(1,2): error BC30652: Reference required to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project."
            Dim expectedMissingAssemblyIdentity As New AssemblyIdentity("Lib")

            Dim resultProperties As ResultProperties = Nothing
            Dim actualError As String = Nothing
            Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

            Dim verify As Action(Of String, String) =
                Sub(expr, expectedError)
                    context.CompileExpression(
                    DefaultInspectionContext.Instance,
                    expr,
                    DkmEvaluationFlags.TreatAsExpression,
                    DiagnosticFormatter.Instance,
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

        <WorkItem(1090458)>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.Main")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            context.CompileExpression("c.P", resultProperties, errorMessage)
            Assert.Null(errorMessage)
        End Sub

        <WorkItem(1090458)>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.Main")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            context.CompileExpression("c.P", resultProperties, errorMessage)
            Assert.Null(errorMessage)
        End Sub

        <WorkItem(1089591)>
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
            Dim result = comp.EmitAndGetReferences(exeBytes, unusedPdbBytes, references)
            Assert.True(result)

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

        <WorkItem(1108133)>
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
            Dim result = comp.EmitAndGetReferences(exeBytes, unusedPdbBytes, references)
            Assert.True(result)

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

        <WorkItem(1105859)>
        <Fact(Skip:="1105859")>
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
            Dim comp = CreateCompilationWithMscorlib({source}, compOptions:=TestOptions.DebugDll, assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName())
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim missingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                InspectionContextFactory.Empty,
"F(Sub()
    Dim o = New S()
    With o
        .F = New T()
        With .F
        End With
    End With
End Sub)",
                DkmEvaluationFlags.None,
                DiagnosticFormatter.Instance,
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

        <WorkItem(1115543)>
        <Fact>
        Sub MethodTypeParameterInLambda()
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
            Dim resultProperties As ResultProperties = Nothing
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
            Assert.Equal("(1,10): error BC30002: Type 'U' is not defined.", errorMessage)

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

        <WorkItem(1112496)>
        <Fact(Skip:="1112496")>
        Sub EvaluateLocalInAsyncLambda()
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
            Dim context = CreateMethodContext(runtime, "Module1._Closure$__.VB$StateMachine___Lambda$__0-1.MoveNext")
            Dim resultProperties As ResultProperties = Nothing
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            Dim result = context.CompileExpression("i", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0, //$VB$ResumableLocal_i$0
                Integer V_1,
                System.Threading.Tasks.Task(Of Integer) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Module1._Closure$__.VB$StateMachine___Lambda$__0-1.$VB$ResumableLocal_i$0 As Integer""
  IL_0006:  ret
}")
        End Sub

    End Class

End Namespace