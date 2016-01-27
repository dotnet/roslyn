' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
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
            Dim assembly = CreateCompilation(identity, {}, {}).Assembly
            Assert.Same(identity, GetMissingAssemblyIdentities(ERRID.ERR_UnreferencedAssemblyEvent3, assembly).Single())
        End Sub

        <Fact>
        Public Sub ErrorsWithAssemblyMultipleSymbolArguments()
            Dim identity1 = New AssemblyIdentity(GetUniqueName())
            Dim assembly1 = CreateCompilation(identity1, {}, {}).Assembly
            Dim identity2 = New AssemblyIdentity(GetUniqueName())
            Dim assembly2 = CreateCompilation(identity2, {}, {}).Assembly
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
            Dim libRef = CreateCompilationWithMscorlib({libSource}, {}, TestOptions.DebugDll, assemblyName:="Lib").EmitToImageReference()
            Dim comp = CreateCompilationWithMscorlib({source}, {libRef}, TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef})
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

            Dim ilRef = CompileIL(il, appendDefaultHeader:=False)
            Dim comp = CreateCompilationWithMscorlib({vb}, {ilRef}, TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
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
            Dim comp = CreateCompilationWithMscorlib({source}, {SystemRef, SystemCoreRef, SystemXmlRef, SystemXmlLinqRef}, TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef})
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
        End Sub

        <Fact>
        Public Sub ERR_MissingRuntimeAssembly()
            Const source = "
Public Class C
    Public Sub M(o As Object)
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef})
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
        End Sub

        <WorkItem(1114866, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub ERR_UndefinedType1()
            Dim source = "
Class C
    Sub M()
    End Sub
End Class
"

            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)

            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.UI")
            Assert.True(runtimeAssemblies.Any())

            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies))
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
        End Sub

        <WorkItem(1151888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1151888")>
        <Fact>
        Public Sub ERR_NameNotMember2()
            Const source = "
Imports System.Linq

Public Class C
    Public Sub M(array As Integer())
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, {SystemCoreRef}, TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef})
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
        End Sub


        <WorkItem(1124725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1124725")>
        <WorkItem(597, "GitHub")>
        <Fact>
        Public Sub PseudoVariableType()
            Const source = "
Public Class C
    Public Sub M()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, {}, TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp, {CSharpRef})
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim aliases = ImmutableArray.Create(ExceptionAlias("Microsoft.CSharp.RuntimeBinder.RuntimeBinderException, Microsoft.CSharp, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b03f5f7f11d50a3a", stowed:=True))

            Const expectedError = "error BC30002: Type 'System.Void' is not defined."
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
        End Sub

        <WorkItem(1114866, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub NotYetLoadedWinMds()
            Dim source = "
Class C
    Shared Sub M(f As Windows.Storage.StorageFolder)
    End Sub
End Class
"

            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)
            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage")
            Assert.True(runtimeAssemblies.Any())

            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies))
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
        End Sub

        ''' <remarks>
        ''' Windows.UI.Xaml is the only (win8) winmd with more than two parts.
        ''' </remarks>
        <WorkItem(1114866, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub NotYetLoadedWinMds_MultipleParts()
            Dim source = "
Class C
    Shared Sub M(c As Windows.UI.Colors)
    End Sub
End Class
"

            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)
            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.UI")
            Assert.True(runtimeAssemblies.Any())

            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies))
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
        End Sub

        <WorkItem(1114866, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1114866")>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub NotYetLoadedWinMds_GetType()
            Dim source = "
Class C
    Shared Sub M(f As Windows.Storage.StorageFolder)
    End Sub
End Class
"

            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef}.Concat(WinRtRefs), TestOptions.DebugDll)

            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage")
            Assert.True(runtimeAssemblies.Any())

            Dim runtime = CreateRuntimeInstance(comp, {MscorlibRef}.Concat(runtimeAssemblies))
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
        End Sub

        <WorkItem(1154988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1154988")>
        <Fact>
        Public Sub CompileWithRetrySameErrorReported()
            Dim source = " 
Class C 
    Sub M() 
    End Sub 
End Class 
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(comp)
            Dim context = CreateMethodContext(runtime, "C.M")

            Dim missingModule = runtime.Modules.First()
            Dim missingIdentity = missingModule.GetMetadataReader().ReadAssemblyIdentityOrThrow()

            Dim numRetries = 0
            Dim errorMessage As String = Nothing
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
                errorMessage)

            Assert.Equal(2, numRetries) ' Ensure that we actually retried and that we bailed out on the second retry if the same identity was seen in the diagnostics.
            Assert.Equal($"error BC30652: Reference required to assembly '{missingIdentity}' containing the type 'MissingType'. Add one to your project.", errorMessage)
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

            Dim compilation = CreateCompilationWithMscorlib({source}, references:={SystemCoreRef})

            Dim runtime = CreateRuntimeInstance(compilation, {MscorlibRef})
            Dim context = CreateMethodContext(runtime, "C.Main")

            Dim systemCore = SystemCoreRef.ToModuleInstance()
            Dim fakeSystemLinq = CreateCompilationWithMscorlib({""}, options:=TestOptions.ReleaseDll, assemblyName:="System.Linq").ToModuleInstance()

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
        End Sub

        Private Shared Function GetMissingAssemblyIdentities(code As ERRID, ParamArray arguments() As Object) As ImmutableArray(Of AssemblyIdentity)
            Return GetMissingAssemblyIdentities(code, arguments, globalNamespace:=Nothing)
        End Function

        Private Shared Function GetMissingAssemblyIdentities(code As ERRID, arguments() As Object, globalNamespace As NamespaceSymbol) As ImmutableArray(Of AssemblyIdentity)
            Return EvaluationContext.GetMissingAssemblyIdentitiesHelper(code, arguments, globalNamespace, linqLibrary:=EvaluationContextBase.SystemCoreIdentity)
        End Function

    End Class
End Namespace
