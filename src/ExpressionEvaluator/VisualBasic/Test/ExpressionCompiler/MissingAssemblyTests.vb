' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.DiaSymReader
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public Class MissingAssemblyTests
        Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub ErrorsWithAssemblyIdentityArguments()
            Dim identity = New AssemblyIdentity(GetUniqueName())
            Assert.Same(identity, GetMissingAssemblyIdentities(ERRID.ERR_UnreferencedAssembly3, identity).Single())
        End Sub

        <Fact>
        Public Sub ErrorsWithAssemblySymbolArguments()
            Dim identity = New AssemblyIdentity(GetUniqueName())
            Dim assembly = CreateEmptyCompilation(identity, source:=Nothing).Assembly
            Assert.Same(identity, GetMissingAssemblyIdentities(ERRID.ERR_UnreferencedAssemblyEvent3, assembly).Single())
        End Sub

        <Fact>
        Public Sub ErrorsWithAssemblyMultipleSymbolArguments()
            Dim identity1 = New AssemblyIdentity(GetUniqueName())
            Dim assembly1 = CreateEmptyCompilation(identity1, source:=Nothing).Assembly
            Dim identity2 = New AssemblyIdentity(GetUniqueName())
            Dim assembly2 = CreateEmptyCompilation(identity2, source:=Nothing).Assembly
            Assert.Same(identity2, GetMissingAssemblyIdentities(ERRID.ERR_ForwardedTypeUnavailable3, "dummy", assembly1, assembly2).Single())
            Assert.Same(identity1, GetMissingAssemblyIdentities(ERRID.ERR_ForwardedTypeUnavailable3, "dummy", assembly2, assembly1).Single())
            Assert.True(GetMissingAssemblyIdentities(ERRID.ERR_ForwardedTypeUnavailable3, "dummy", assembly1).IsDefault)
            Assert.True(GetMissingAssemblyIdentities(ERRID.ERR_ForwardedTypeUnavailable3, "dummy", "dummy", "dummy", assembly1).IsDefault)
        End Sub

        <Fact>
        Public Sub ErrorsRequiringSystemCore()
            AssertEx.SetEqual(GetMissingAssemblyIdentities(ERRID.ERR_XmlFeaturesNotAvailable),
                              EvaluationContextBase.SystemIdentity,
                              EvaluationContextBase.SystemCoreIdentity,
                              EvaluationContextBase.SystemXmlIdentity,
                              EvaluationContextBase.SystemXmlLinqIdentity)
            Assert.Equal(EvaluationContextBase.SystemCoreIdentity, GetMissingAssemblyIdentities(ERRID.ERR_NameNotMember2, "dummy", "dummy").Single())
        End Sub

        <Fact>
        Public Sub ErrorsRequiringVbCore()
            Assert.Equal(EvaluationContextBase.MicrosoftVisualBasicIdentity, GetMissingAssemblyIdentities(ERRID.ERR_MissingRuntimeHelper).Single())
        End Sub

        <Fact>
        Public Sub MultipleAssemblyArguments()
            Dim identity1 = New AssemblyIdentity(GetUniqueName())
            Dim identity2 = New AssemblyIdentity(GetUniqueName())
            Assert.Same(identity1, GetMissingAssemblyIdentities(ERRID.ERR_UnreferencedAssembly3, identity1, identity2).Single())
            Assert.Same(identity2, GetMissingAssemblyIdentities(ERRID.ERR_UnreferencedAssembly3, identity2, identity1).Single())
        End Sub

        <Fact>
        Public Sub NoAssemblyArguments()
            Assert.True(GetMissingAssemblyIdentities(ERRID.ERR_UnreferencedAssembly3).IsDefault)
            Assert.True(GetMissingAssemblyIdentities(ERRID.ERR_UnreferencedAssembly3, "Not an assembly").IsDefault)
        End Sub

        <Fact>
        Public Sub ERR_UnreferencedAssembly3()
            Const libSource = "
Public Class Missing
End Class
"
            Const source = "
Public Class C
    Public Sub M(parameter As Missing)
    End Sub
End Class
"
            Dim libRef = CreateCompilationWithMscorlib40({libSource}, {}, TestOptions.DebugDll, assemblyName:="Lib").EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib40({source}, {libRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp, {MscorlibRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Const expectedError = "error BC30652: Reference required to assembly 'Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' containing the type 'Missing'. Add one to your project."
                    Dim expectedMissingAssemblyIdentity = New AssemblyIdentity("Lib")

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

                    context.CompileExpression(
                        "parameter",
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

                End Sub)
        End Sub

        <Fact>
        Public Sub ERR_ForwardedTypeUnavailable3()
            Const il = "
.assembly extern mscorlib { }
.assembly extern pe2 { }
.assembly pe1 { }

.class extern forwarder Forwarded
{
  .assembly extern pe2
}

.class public auto ansi beforefieldinit Dummy
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
"
            Const vb = "
Class C
    Shared Sub M(d As Dummy)
    End Sub
End Class
"

            Dim ilRef = CompileIL(il, prependDefaultHeader:=False)
            Dim comp = CreateCompilationWithMscorlib40({vb}, {ilRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim expectedMissingAssemblyIdentity As AssemblyIdentity = New AssemblyIdentity("pe2")
                    Dim expectedError = $"error BC31424: Type 'Forwarded' in assembly '{context.Compilation.Assembly.Identity}' has been forwarded to assembly '{expectedMissingAssemblyIdentity}'. Either a reference to '{expectedMissingAssemblyIdentity}' is missing from your project or the type 'Forwarded' is missing from assembly '{expectedMissingAssemblyIdentity}'."

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

                    context.CompileExpression(
                        "New Forwarded()",
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
                End Sub)
        End Sub

        <Fact>
        Public Sub ERR_XmlFeaturesNotAvailable()
            Const source = "
Public Class C
    Public Sub M()
        dim x = <element/>
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib40({source}, {SystemRef, SystemCoreRef, SystemXmlRef, SystemXmlLinqRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp, {MscorlibRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Const expectedError = "error BC31190: XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core or other assemblies declaring System.Linq.Enumerable, System.Xml.Linq.XElement, System.Xml.Linq.XName, System.Xml.Linq.XAttribute and System.Xml.Linq.XNamespace types."

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

                    context.CompileExpression(
                        "<value/>",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)
                    Assert.Equal(expectedError, actualError)
                    AssertEx.SetEqual(actualMissingAssemblyIdentities,
                              EvaluationContextBase.SystemIdentity,
                              EvaluationContextBase.SystemCoreIdentity,
                              EvaluationContextBase.SystemXmlIdentity,
                              EvaluationContextBase.SystemXmlLinqIdentity)
                End Sub)
        End Sub

        <Fact>
        Public Sub ERR_MissingRuntimeAssembly()
            Const source = "
Public Class C
    Public Sub M(o As Object)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp, {MscorlibRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Const expectedError = "error BC35000: Requested operation is not available because the runtime library function 'Microsoft.VisualBasic.CompilerServices.Operators.CompareObjectEqual' is not defined."
                    Dim expectedMissingAssemblyIdentity = EvaluationContextBase.MicrosoftVisualBasicIdentity

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

                    context.CompileExpression(
                        "o = o",
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
                End Sub)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub ERR_UndefinedType1()
            Dim source = "
Class C
    Sub M()
    End Sub
End Class
"

            Dim comp = CreateEmptyCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)

            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.UI")
            Assert.True(runtimeAssemblies.Any())

            WithRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies),
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim globalNamespace = context.Compilation.GlobalNamespace

                    Dim expectedIdentity = New AssemblyIdentity("Windows.Storage", contentType:=AssemblyContentType.WindowsRuntime)

                    Dim actualIdentity = GetMissingAssemblyIdentities(ERRID.ERR_UndefinedType1, {"Windows.Storage"}, globalNamespace).Single()
                    Assert.Equal(expectedIdentity, actualIdentity)

                    actualIdentity = GetMissingAssemblyIdentities(ERRID.ERR_UndefinedType1, {"Global.Windows.Storage"}, globalNamespace).Single()
                    Assert.Equal(expectedIdentity, actualIdentity)

                    actualIdentity = GetMissingAssemblyIdentities(ERRID.ERR_UndefinedType1, {"Global.Windows.Storage.Additional"}, globalNamespace).Single()
                    Assert.Equal(expectedIdentity, actualIdentity)

                    expectedIdentity = New AssemblyIdentity("Windows.UI.Xaml", contentType:=AssemblyContentType.WindowsRuntime)

                    actualIdentity = GetMissingAssemblyIdentities(ERRID.ERR_UndefinedType1, {"Windows.UI.Xaml"}, globalNamespace).Single()
                    Assert.Equal(expectedIdentity, actualIdentity)

                    Assert.True(GetMissingAssemblyIdentities(ERRID.ERR_UndefinedType1, {"Windows.UI.Xaml(Of T)"}, globalNamespace).IsDefault)
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1151888")>
        Public Sub ERR_NameNotMember2()
            Const source = "
Imports System.Linq

Public Class C
    Public Sub M(array As Integer())
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib40({source}, {SystemCoreRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp, {MscorlibRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Const expectedErrorTemplate = "error BC30456: '{0}' is not a member of 'Integer()'."
                    Dim expectedMissingAssemblyIdentity = EvaluationContextBase.SystemCoreIdentity

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

                    context.CompileExpression(
                        "array.Count()",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)
                    Assert.Equal(String.Format(expectedErrorTemplate, "Count"), actualError)
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single())

                    context.CompileExpression(
                        "array.NoSuchMethod()",
                        DkmEvaluationFlags.TreatAsExpression,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)
                    Assert.Equal(String.Format(expectedErrorTemplate, "NoSuchMethod"), actualError)
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single())
                End Sub)
        End Sub

        <Fact, WorkItem(597, "GitHub")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1124725")>
        Public Sub PseudoVariableType()
            Const source = "
Public Class C
    Public Sub M()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib40({source}, {}, TestOptions.DebugDll)
            WithRuntimeInstance(comp, {CSharpRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim aliases = ImmutableArray.Create(ExceptionAlias("Microsoft.CSharp.RuntimeBinder.RuntimeBinderException, Microsoft.CSharp, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a", stowed:=True))

                    Const expectedError = "error BC30652: Reference required to assembly " &
                                          "'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' " &
                                          "containing the type 'Exception'. Add one to your project."
                    Dim expectedMissingAssemblyIdentity = comp.Assembly.CorLibrary.Identity

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing

                    context.CompileExpression(
                        "$stowedexception",
                        DkmEvaluationFlags.TreatAsExpression,
                        aliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)

                    Assert.Equal(expectedError, actualError)
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single())
                End Sub)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub NotYetLoadedWinMds()
            Dim source = "
Class C
    Shared Sub M(f As Windows.Storage.StorageFolder)
    End Sub
End Class
"

            Dim comp = CreateEmptyCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)
            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage")
            Assert.True(runtimeAssemblies.Any())

            WithRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies),
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Const expectedError = "error BC30456: 'UI' is not a member of 'Windows'."
                    Dim expectedMissingAssemblyIdentity = New AssemblyIdentity("Windows.UI", contentType:=System.Reflection.AssemblyContentType.WindowsRuntime)

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
                    context.CompileExpression(
                        "Windows.UI.Colors",
                        DkmEvaluationFlags.None,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)
                    Assert.Equal(expectedError, actualError)
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single())
                End Sub)
        End Sub

        ''' <remarks>
        ''' Windows.UI.Xaml is the only (win8) winmd with more than two parts.
        ''' </remarks>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub NotYetLoadedWinMds_MultipleParts()
            Dim source = "
Class C
    Shared Sub M(c As Windows.UI.Colors)
    End Sub
End Class
"

            Dim comp = CreateEmptyCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)
            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.UI")
            Assert.True(runtimeAssemblies.Any())

            WithRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies),
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Const expectedError = "error BC30456: 'Xaml' is not a member of 'Windows.UI'."
                    Dim expectedMissingAssemblyIdentity = New AssemblyIdentity("Windows.UI.Xaml", contentType:=System.Reflection.AssemblyContentType.WindowsRuntime)

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
                    context.CompileExpression(
                        "Windows.[UI].Xaml.Application",
                        DkmEvaluationFlags.None,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)
                    Assert.Equal(expectedError, actualError)
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single())
                End Sub)
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub NotYetLoadedWinMds_GetType()
            Dim source = "
Class C
    Shared Sub M(f As Windows.Storage.StorageFolder)
    End Sub
End Class
"

            Dim comp = CreateEmptyCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)

            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage")
            Assert.True(runtimeAssemblies.Any())

            WithRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies),
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Const expectedError = "error BC30002: Type 'Windows.UI.Colors' is not defined."
                    Dim expectedMissingAssemblyIdentity = New AssemblyIdentity("Windows.UI", contentType:=System.Reflection.AssemblyContentType.WindowsRuntime)

                    Dim resultProperties As ResultProperties = Nothing
                    Dim actualError As String = Nothing
                    Dim actualMissingAssemblyIdentities As ImmutableArray(Of AssemblyIdentity) = Nothing
                    context.CompileExpression(
                        "GetType([Windows].UI.Colors)",
                        DkmEvaluationFlags.None,
                        NoAliases,
                        DebuggerDiagnosticFormatter.Instance,
                        resultProperties,
                        actualError,
                        actualMissingAssemblyIdentities,
                        EnsureEnglishUICulture.PreferredOrNull,
                        testData:=Nothing)
                    Assert.Equal(expectedError, actualError)
                    Assert.Equal(expectedMissingAssemblyIdentity, actualMissingAssemblyIdentities.Single())
                End Sub)
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1154988")>
        Public Sub CompileWithRetrySameErrorReported()
            Dim source = " 
Class C 
    Sub M() 
    End Sub 
End Class 
"
            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.M")

                    Dim missingModule = runtime.Modules.First()
                    Dim missingIdentity = missingModule.GetMetadataReader().ReadAssemblyIdentityOrThrow()

                    Dim numRetries = 0
                    Dim errorMessage As String = Nothing
                    Dim compileResult As CompileResult = Nothing
                    ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                        runtime.Modules.Select(Function(m) m.MetadataBlock).ToImmutableArray(),
                        context,
                        Function(unused, diagnostics)
                            numRetries += 1
                            Assert.InRange(numRetries, 0, 2) ' We don't want to loop forever... 
                            diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_UnreferencedAssembly3, missingIdentity, "MissingType"), Location.None))
                            Return Nothing
                        End Function,
                        Function(assemblyIdentity, ByRef uSize)
                            uSize = CUInt(missingModule.MetadataLength)
                            Return missingModule.MetadataAddress
                        End Function,
                        compileResult,
                        errorMessage)

                    Assert.Equal(2, numRetries) ' Ensure that we actually retried and that we bailed out on the second retry if the same identity was seen in the diagnostics.
                    Assert.Equal($"error BC30652: { String.Format(VBResources.ERR_UnreferencedAssembly3, missingIdentity, "MissingType")}", errorMessage)
                End Sub)
        End Sub

        <Fact>
        Public Sub TryDifferentLinqLibraryOnRetry()
            Dim source = "
Imports System.Linq
Class C
    Shared Sub Main(args() As String)
    End Sub
End Class
Class UseLinq
    Dim b = Enumerable.Any(Of Integer)(Nothing)
End Class"

            Dim comp = CreateCompilationWithMscorlib40({source}, references:={SystemCoreRef})
            WithRuntimeInstance(comp, {MscorlibRef},
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.Main")

                    Dim systemCore = SystemCoreRef.ToModuleInstance()
                    Dim fakeSystemLinq = CreateCompilationWithMscorlib40({""}, options:=TestOptions.ReleaseDll, assemblyName:="System.Linq").ToModuleInstance()

                    Dim errorMessage As String = Nothing
                    Dim testData As CompilationTestData = Nothing
                    Dim retryCount = 0
                    Dim compileResult = ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                        runtime.Modules.Select(Function(m) m.MetadataBlock).ToImmutableArray(),
                        "args.Where(Function(a) a.Length > 0)",
                        ImmutableArray(Of [Alias]).Empty,
                        Function(_1, _2) context, ' ignore new blocks and just keep using the same failed context...
                        Function(assemblyIdentity As AssemblyIdentity, ByRef uSize As UInteger)
                            retryCount += 1
                            Dim block As MetadataBlock
                            Select Case retryCount
                                Case 1
                                    Assert.Equal(EvaluationContextBase.SystemLinqIdentity, assemblyIdentity)
                                    block = fakeSystemLinq.MetadataBlock
                                Case 2
                                    Assert.Equal(EvaluationContextBase.SystemCoreIdentity, assemblyIdentity)
                                    block = systemCore.MetadataBlock
                                Case Else
                                    Throw ExceptionUtilities.Unreachable
                            End Select
                            uSize = CUInt(block.Size)
                            Return block.Pointer
                        End Function,
                        errorMessage:=errorMessage,
                        testData:=testData)

                    Assert.Equal(2, retryCount)
                End Sub)
        End Sub

        <Fact>
        Public Sub TupleNoSystemRuntimeWithVB15()
            Const source =
"Class C
    Shared Sub M()
        Dim x = 1
        Dim y = (x, 2)
        Dim z = (3, 4, (5, 6))
    End Sub
End Class"
            TupleContextNoSystemRuntime(
                source,
                "C.M",
                "y",
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //x
                System.ValueTuple(Of Integer, Integer) V_1, //y
                System.ValueTuple(Of Integer, Integer, System.ValueTuple(Of Integer, Integer)) V_2) //z
  IL_0000:  ldloc.1
  IL_0001:  ret
}")
        End Sub

        <Fact>
        Public Sub TupleNoSystemRuntimeWithVB15_3()
            Const source =
"Class C
    Shared Sub M()
        Dim x = 1
        Dim y = (x, 2)
        Dim z = (3, 4, (5, 6))
    End Sub
End Class"
            TupleContextNoSystemRuntime(
                source,
                "C.M",
                "y",
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //x
                System.ValueTuple(Of Integer, Integer) V_1, //y
                System.ValueTuple(Of Integer, Integer, System.ValueTuple(Of Integer, Integer)) V_2) //z
  IL_0000:  ldloc.1
  IL_0001:  ret
}", LanguageVersion.VisualBasic15_3)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16879")>
        Public Sub NonTupleNoSystemRuntime()
            Const source =
"Class C
    Shared Sub M()
        Dim x = 1
        Dim y = (x, 2)
        Dim z = (3, 4, (5, 6))
    End Sub
End Class"
            TupleContextNoSystemRuntime(
                source,
                "C.M",
                "x",
"{
  // Code size        2 (0x2)
  .maxstack  1
  .locals init (Integer V_0, //x
                System.ValueTuple(Of Integer, Integer) V_1, //y
                System.ValueTuple(Of Integer, Integer, System.ValueTuple(Of Integer, Integer)) V_2) //z
  IL_0000:  ldloc.0
  IL_0001:  ret
}")
        End Sub

        Private Shared Sub TupleContextNoSystemRuntime(source As String, methodName As String, expression As String, expectedIL As String,
                                                       Optional languageVersion As LanguageVersion = LanguageVersion.VisualBasic15)
            Dim comp = CreateEmptyCompilation({source}, references:={Net461.References.mscorlib, Net461.References.SystemRuntime, ValueTupleLegacyRef}, options:=TestOptions.DebugDll,
                                                     parseOptions:=TestOptions.Regular.WithLanguageVersion(languageVersion))
            Using systemRuntime = Net461.References.SystemRuntime.ToModuleInstance()
                WithRuntimeInstance(comp, {Net461.References.mscorlib, ValueTupleLegacyRef},
                    Sub(runtime)
                        Dim methodBlocks As ImmutableArray(Of MetadataBlock) = Nothing
                        Dim moduleId As ModuleId = Nothing
                        Dim symReader As ISymUnmanagedReader = Nothing
                        Dim typeToken = 0
                        Dim methodToken = 0
                        Dim localSignatureToken = 0
                        GetContextState(runtime, "C.M", methodBlocks, moduleId, symReader, methodToken, localSignatureToken)
                        Dim errorMessage As String = Nothing
                        Dim testData As CompilationTestData = Nothing
                        Dim retryCount = 0
                        Dim compileResult = ExpressionCompilerTestHelpers.CompileExpressionWithRetry(
                            runtime.Modules.Select(Function(m) m.MetadataBlock).ToImmutableArray(),
                            expression,
                            ImmutableArray(Of [Alias]).Empty,
                            Function(b, u) EvaluationContext.CreateMethodContext(b.ToCompilation(), MakeDummyLazyAssemblyReaders(), symReader, moduleId, methodToken, methodVersion:=1, ilOffset:=0, localSignatureToken:=localSignatureToken),
                            Function(assemblyIdentity As AssemblyIdentity, ByRef uSize As UInteger)
                                retryCount += 1
                                Assert.Equal("System.Runtime", assemblyIdentity.Name)
                                Dim block = systemRuntime.MetadataBlock
                                uSize = CUInt(block.Size)
                                Return block.Pointer
                            End Function,
                            errorMessage:=errorMessage,
                            testData:=testData)
                        Assert.Equal(1, retryCount)
                        testData.GetMethodData("<>x.<>m0").VerifyIL(expectedIL)
                    End Sub)
            End Using
        End Sub

        Private Shared Function GetMissingAssemblyIdentities(code As ERRID, ParamArray arguments() As Object) As ImmutableArray(Of AssemblyIdentity)
            Return GetMissingAssemblyIdentities(code, arguments, globalNamespace:=Nothing)
        End Function

        Private Shared Function GetMissingAssemblyIdentities(code As ERRID, arguments() As Object, globalNamespace As NamespaceSymbol) As ImmutableArray(Of AssemblyIdentity)
            Return EvaluationContext.GetMissingAssemblyIdentitiesHelper(code, arguments, globalNamespace, linqLibrary:=EvaluationContextBase.SystemCoreIdentity)
        End Function

    End Class
End Namespace
