' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit
Imports Resources = Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests.Resources

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class WinMdTests
        Inherits ExpressionCompilerTestBase

        ''' <summary>
        ''' Handle runtime assemblies rather than Windows.winmd
        ''' (compile-time assembly) since those are the assemblies
        ''' loaded in the debuggee.
        ''' </summary>
        <WorkItem(981104)>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub Win8RuntimeAssemblies()
            Const source =
"Class C
    Shared Sub M(f As Windows.Storage.StorageFolder, p As Windows.Foundation.Collections.PropertySet)
    End Sub
End Class"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll, references:=WinRtRefs)
            Dim runtimeAssemblies = ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage", "Windows.Foundation.Collections")
            Assert.True(runtimeAssemblies.Length >= 2)
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Dim runtime = CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                ImmutableArray.Create(MscorlibRef).Concat(runtimeAssemblies), ' no reference to Windows.winmd
                exeBytes,
                New SymReader(pdbBytes))
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("If(p Is Nothing, f, Nothing)", errorMessage, testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0005
  IL_0003:  ldnull
  IL_0004:  ret
  IL_0005:  ldarg.0
  IL_0006:  ret
}")
        End Sub

        <Fact>
        Public Sub Win8OnWin8()
            CompileTimeAndRuntimeAssemblies(
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows_Data)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows_Storage)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                "Windows.Storage")
        End Sub

        <Fact>
        Public Sub Win8OnWin10()
            CompileTimeAndRuntimeAssemblies(
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.Windows)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Data)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Storage)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_3(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                "Windows.Storage")
        End Sub

        <Fact>
        Public Sub Win10OnWin10()
            CompileTimeAndRuntimeAssemblies(
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Data)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows_Storage)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                ImmutableArray.Create(
                    MscorlibRef,
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.Windows)).GetReference(),
                    AssemblyMetadata.CreateFromImage(ToVersion1_4(Resources.LibraryA)).GetReference(),
                    AssemblyMetadata.CreateFromImage(Resources.LibraryB).GetReference()),
                "Windows")
        End Sub

        Private Sub CompileTimeAndRuntimeAssemblies(
            compileReferences As ImmutableArray(Of MetadataReference),
            runtimeReferences As ImmutableArray(Of MetadataReference),
            storageAssemblyName As String)
            Const source =
"Class C
    Shared Sub M(a As LibraryA.A, b As LibraryB.B, t As Windows.Data.Text.TextSegment, f As Windows.Storage.StorageFolder)
    End Sub
End Class"
            Dim runtime = CreateRuntime(source, compileReferences, runtimeReferences)
            Dim context = CreateMethodContext(runtime, "C.M")
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression("If(a, If(b, If(t, f)))", errorMessage, testData)
            Assert.Null(errorMessage)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0010
  IL_0004:  pop
  IL_0005:  ldarg.1
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0010
  IL_0009:  pop
  IL_000a:  ldarg.2
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0010
  IL_000e:  pop
  IL_000f:  ldarg.3
  IL_0010:  ret
}")
            testData = New CompilationTestData()
            Dim result = context.CompileExpression("f", errorMessage, testData)
            Assert.Null(errorMessage)
            Dim methodData = testData.GetMethodData("<>x.<>m0")
            methodData.VerifyIL(
"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.3
  IL_0001:  ret
}")
            ' Check return type is from runtime assembly.
            Dim assemblyReference = AssemblyMetadata.CreateFromImage(result.Assembly).GetReference()
            Dim comp = VisualBasicCompilation.Create(
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName(),
                references:=runtimeReferences.Concat(ImmutableArray.Create(Of MetadataReference)(assemblyReference)))
            Dim assembly = ImmutableArray.CreateRange(result.Assembly)
            Using metadata = ModuleMetadata.CreateFromImage(ImmutableArray.CreateRange(assembly))
                Dim reader = metadata.MetadataReader
                Dim typeDef = reader.GetTypeDef("<>x")
                Dim methodHandle = reader.GetMethodDefHandle(typeDef, "<>m0")
                Dim [module] = DirectCast(comp.GetMember("<>x").ContainingModule, PEModuleSymbol)
                Dim metadataDecoder = New MetadataDecoder([module])
                Dim signatureHeader As SignatureHeader = Nothing
                Dim metadataException As BadImageFormatException = Nothing
                Dim parameters = metadataDecoder.GetSignatureForMethod(methodHandle, signatureHeader, metadataException)
                Assert.Equal(parameters.Length, 5)
                Dim actualReturnType = parameters(0).Type
                Assert.Equal(actualReturnType.TypeKind, TypeKind.Class) ' not error
                Dim expectedReturnType = comp.GetMember("Windows.Storage.StorageFolder")
                Assert.Equal(expectedReturnType, actualReturnType)
                Assert.Equal(storageAssemblyName, actualReturnType.ContainingAssembly.Name)
            End Using
        End Sub

        ''' <summary>
        ''' Assembly-qualified name containing "ContentType=WindowsRuntime",
        ''' and referencing runtime assembly.
        ''' </summary>
        <WorkItem(1116143)>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub AssemblyQualifiedName()
            Const source =
"Class C
    Shared Sub M(f As Windows.Storage.StorageFolder, p As Windows.Foundation.Collections.PropertySet)
    End Sub
End Class"
            Dim runtime = CreateRuntime(
                source,
                ImmutableArray.CreateRange(WinRtRefs),
                ImmutableArray.Create(MscorlibRef).Concat(ExpressionCompilerTestHelpers.GetRuntimeWinMds("Windows.Storage", "Windows.Foundation.Collections")))
            Dim context = CreateMethodContext(
                runtime,
                "C.M")
            Dim aliases = ImmutableArray.Create(
                VariableAlias("s", "Windows.Storage.StorageFolder, Windows.Storage, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"),
                VariableAlias("d", "Windows.Foundation.DateTime, Windows.Foundation, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"))
            Dim errorMessage As String = Nothing
            Dim testData = New CompilationTestData()
            context.CompileExpression(
                "If(DirectCast(s.Attributes, Object), d.UniversalTime)",
                DkmEvaluationFlags.TreatAsExpression,
                aliases,
                errorMessage,
                testData)
            testData.GetMethodData("<>x.<>m0").VerifyIL(
"{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_000a:  castclass  ""Windows.Storage.StorageFolder""
  IL_000f:  callvirt   ""Function Windows.Storage.StorageFolder.get_Attributes() As Windows.Storage.FileAttributes""
  IL_0014:  box        ""Windows.Storage.FileAttributes""
  IL_0019:  dup
  IL_001a:  brtrue.s   IL_0036
  IL_001c:  pop
  IL_001d:  ldstr      ""d""
  IL_0022:  call       ""Function Microsoft.VisualStudio.Debugger.Clr.IntrinsicMethods.GetObjectByAlias(String) As Object""
  IL_0027:  unbox.any  ""Windows.Foundation.DateTime""
  IL_002c:  ldfld      ""Windows.Foundation.DateTime.UniversalTime As Long""
  IL_0031:  box        ""Long""
  IL_0036:  ret
}")
        End Sub

        Private Function CreateRuntime(
            source As String,
            compileReferences As ImmutableArray(Of MetadataReference),
            runtimeReferences As ImmutableArray(Of MetadataReference)) As RuntimeInstance

            Dim comp = CreateCompilationWithMscorlib(
                {source},
                options:=TestOptions.DebugDll,
                assemblyName:=ExpressionCompilerUtilities.GenerateUniqueName(),
                references:=compileReferences)
            Dim exeBytes As Byte() = Nothing
            Dim pdbBytes As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            comp.EmitAndGetReferences(exeBytes, pdbBytes, references)
            Return CreateRuntimeInstance(
                ExpressionCompilerUtilities.GenerateUniqueName(),
                runtimeReferences.AddIntrinsicAssembly(),
                exeBytes,
                New SymReader(pdbBytes))
        End Function

        Private Shared Function ToVersion1_3(bytes As Byte()) As Byte()
            Return ExpressionCompilerTestHelpers.ToVersion1_3(bytes)
        End Function

        Private Shared Function ToVersion1_4(bytes As Byte()) As Byte()
            Return ExpressionCompilerTestHelpers.ToVersion1_4(bytes)
        End Function

    End Class

End Namespace

