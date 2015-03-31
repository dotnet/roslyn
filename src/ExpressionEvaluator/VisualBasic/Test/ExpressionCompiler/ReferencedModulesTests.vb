' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
Imports Microsoft.DiaSymReader
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ReferencedModulesTests
        Inherits ExpressionCompilerTestBase

        ''' <summary>
        ''' MakeAssemblyReferences should drop unreferenced assemblies.
        ''' </summary>
        <WorkItem(1141029)>
        <Fact>
        Public Sub AssemblyDuplicateReferences()
            Const sourceA =
"Public Class A
End Class"
            Const sourceB =
"Public Class B
    Public F As New A()
End Class"
            Const sourceC =
"Class C
    Public F As New B()
    Shared Sub M()
    End Sub
End Class"
            ' Assembly A, multiple versions, strong name.
            Dim assemblyNameA = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim publicKeyA = ImmutableArray.CreateRange(Of Byte)({&H00, &H24, &H00, &H00, &H04, &H80, &H00, &H00, &H94, &H00, &H00, &H00, &H06, &H02, &H00, &H00, &H00, &H24, &H00, &H00, &H52, &H53, &H41, &H31, &H00, &H04, &H00, &H00, &H01, &H00, &H01, &H00, &HED, &HD3, &H22, &HCB, &H6B, &HF8, &HD4, &HA2, &HFC, &HCC, &H87, &H37, &H04, &H06, &H04, &HCE, &HE7, &HB2, &HA6, &HF8, &H4A, &HEE, &HF3, &H19, &HDF, &H5B, &H95, &HE3, &H7A, &H6A, &H28, &H24, &HA4, &H0A, &H83, &H83, &HBD, &HBA, &HF2, &HF2, &H52, &H20, &HE9, &HAA, &H3B, &HD1, &HDD, &HE4, &H9A, &H9A, &H9C, &HC0, &H30, &H8F, &H01, &H40, &H06, &HE0, &H2B, &H95, &H62, &H89, &H2A, &H34, &H75, &H22, &H68, &H64, &H6E, &H7C, &H2E, &H83, &H50, &H5A, &HCE, &H7B, &H0B, &HE8, &HF8, &H71, &HE6, &HF7, &H73, &H8E, &HEB, &H84, &HD2, &H73, &H5D, &H9D, &HBE, &H5E, &HF5, &H90, &HF9, &HAB, &H0A, &H10, &H7E, &H23, &H48, &HF4, &HAD, &H70, &H2E, &HF7, &HD4, &H51, &HD5, &H8B, &H3A, &HF7, &HCA, &H90, &H4C, &HDC, &H80, &H19, &H26, &H65, &HC9, &H37, &HBD, &H52, &H81, &HF1, &H8B, &HCD})
            Dim compilationAS1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameA, New Version(1, 1, 1, 1), cultureName:="", publicKeyOrToken:=publicKeyA, hasPublicKey:=True),
                {sourceA},
                references:={MscorlibRef},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceAS1 = compilationAS1.EmitToImageReference()
            Dim identityAS1 = referenceAS1.GetAssemblyIdentity()
            Dim compilationAS2 = CreateCompilation(
                New AssemblyIdentity(assemblyNameA, New Version(2, 1, 1, 1), cultureName:="", publicKeyOrToken:=publicKeyA, hasPublicKey:=True),
                {sourceA},
                references:={MscorlibRef},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceAS2 = compilationAS2.EmitToImageReference()
            Dim identityAS2 = referenceAS2.GetAssemblyIdentity()

            ' Assembly B, multiple versions, strong name.
            Dim assemblyNameB = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim publicKeyB = ImmutableArray.CreateRange(Of Byte)({&H00, &H24, &H00, &H00, &H04, &H80, &H00, &H00, &H94, &H00, &H00, &H00, &H06, &H02, &H00, &H00, &H00, &H24, &H00, &H00, &H53, &H52, &H41, &H31, &H00, &H04, &H00, &H00, &H01, &H00, &H01, &H00, &HED, &HD3, &H22, &HCB, &H6B, &HF8, &HD4, &HA2, &HFC, &HCC, &H87, &H37, &H04, &H06, &H04, &HCE, &HE7, &HB2, &HA6, &HF8, &H4A, &HEE, &HF3, &H19, &HDF, &H5B, &H95, &HE3, &H7A, &H6A, &H28, &H24, &HA4, &H0A, &H83, &H83, &HBD, &HBA, &HF2, &HF2, &H52, &H20, &HE9, &HAA, &H3B, &HD1, &HDD, &HE4, &H9A, &H9A, &H9C, &HC0, &H30, &H8F, &H01, &H40, &H06, &HE0, &H2B, &H95, &H62, &H89, &H2A, &H34, &H75, &H22, &H68, &H64, &H6E, &H7C, &H2E, &H83, &H50, &H5A, &HCE, &H7B, &H0B, &HE8, &HF8, &H71, &HE6, &HF7, &H73, &H8E, &HEB, &H84, &HD2, &H73, &H5D, &H9D, &HBE, &H5E, &HF5, &H90, &HF9, &HAB, &H0A, &H10, &H7E, &H23, &H48, &HF4, &HAD, &H70, &H2E, &HF7, &HD4, &H51, &HD5, &H8B, &H3A, &HF7, &HCA, &H90, &H4C, &HDC, &H80, &H19, &H26, &H65, &HC9, &H37, &HBD, &H52, &H81, &HF1, &H8B, &HCD})
            Dim compilationBS1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameB, New Version(1, 1, 1, 1), cultureName:="", publicKeyOrToken:=publicKeyB, hasPublicKey:=True),
                {sourceB},
                references:={MscorlibRef, referenceAS1},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceBS1 = compilationBS1.EmitToImageReference()
            Dim identityBS1 = referenceBS1.GetAssemblyIdentity()
            Dim compilationBS2 = CreateCompilation(
                New AssemblyIdentity(assemblyNameB, New Version(2, 2, 2, 1), cultureName:="", publicKeyOrToken:=publicKeyB, hasPublicKey:=True),
                {sourceB},
                references:={MscorlibRef, referenceAS2},
                options:=TestOptions.DebugDll.WithDelaySign(True))
            Dim referenceBS2 = compilationBS2.EmitToImageReference()
            Dim identityBS2 = referenceBS2.GetAssemblyIdentity()

            ' Assembly C, multiple versions, not strong name.
            Dim assemblyNameC = ExpressionCompilerUtilities.GenerateUniqueName()
            Dim compilationCN1 = CreateCompilation(
                New AssemblyIdentity(assemblyNameC, New Version(1, 1, 1, 1)),
                {sourceC},
                references:={MscorlibRef, referenceBS1},
                options:=TestOptions.DebugDll)
            Dim exeBytesC1 As Byte() = Nothing
            Dim pdbBytesC1 As Byte() = Nothing
            Dim references As ImmutableArray(Of MetadataReference) = Nothing
            compilationCN1.EmitAndGetReferences(exeBytesC1, pdbBytesC1, references)
            Dim compilationCN2 = CreateCompilation(
                New AssemblyIdentity(assemblyNameC, New Version(2, 1, 1, 1)),
                {sourceC},
                references:={MscorlibRef, referenceBS2},
                options:=TestOptions.DebugDll)
            Dim exeBytesC2 As Byte() = Nothing
            Dim pdbBytesC2 As Byte() = Nothing
            compilationCN1.EmitAndGetReferences(exeBytesC2, pdbBytesC2, references)

            ' Duplicate assemblies, target module referencing BS1.
            Using runtime = CreateRuntimeInstance(
                assemblyNameC,
                ImmutableArray.Create(MscorlibRef, referenceAS1, referenceAS2, referenceBS2, referenceBS1, referenceBS2),
                exeBytesC1,
                New SymReader(pdbBytesC1))

                Dim typeBlocks As ImmutableArray(Of MetadataBlock) = Nothing
                Dim methodBlocks As ImmutableArray(Of MetadataBlock) = Nothing
                Dim moduleVersionId As Guid = Nothing
                Dim symReader As ISymUnmanagedReader = Nothing
                Dim typeToken = 0
                Dim methodToken = 0
                Dim localSignatureToken = 0
                GetContextState(runtime, "C", typeBlocks, moduleVersionId, symReader, typeToken, localSignatureToken)
                GetContextState(runtime, "C.M", methodBlocks, moduleVersionId, symReader, methodToken, localSignatureToken)

                ' Compile expression with type context.
                Dim context = EvaluationContext.CreateTypeContext(
                    Nothing,
                    typeBlocks,
                    moduleVersionId,
                    typeToken)
                Dim errorMessage As String = Nothing
                ' A is ambiguous since there were no explicit references to AS1 or AS2.
                Dim testData = New CompilationTestData()
                context.CompileExpression("New A()", errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30554: 'A' is ambiguous.")
                testData = New CompilationTestData()
                ' Ideally, B should be resolved to BS1.
                context.CompileExpression("New B()", errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30554: 'B' is ambiguous.")

                ' Compile expression with method context.
                Dim previous = New VisualBasicMetadataContext(typeBlocks, context)
                context = EvaluationContext.CreateMethodContext(
                    previous,
                    methodBlocks,
                    MakeDummyLazyAssemblyReaders(),
                    symReader,
                    moduleVersionId,
                    methodToken,
                    methodVersion:=1,
                    ilOffset:=0,
                    localSignatureToken:=localSignatureToken)
                Assert.Equal(previous.Compilation, context.Compilation) ' re-use type context compilation
                testData = New CompilationTestData()
                ' Ideally, B should be resolved to BS1.
                context.CompileExpression("New B()", errorMessage, testData)
                Assert.Equal(errorMessage, "(1,6): error BC30554: 'B' is ambiguous.")
            End Using
        End Sub

    End Class

End Namespace
