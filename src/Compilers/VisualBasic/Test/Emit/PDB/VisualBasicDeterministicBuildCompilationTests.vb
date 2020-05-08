' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports System.Security.Cryptography
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Debugging
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.PDB

Public Class VisualBasicDeterministicBuildCompilationTests
    Inherits BasicTestBase

    ' Provide non default options for to test that they are being serialized
    ' to the pdb correctly. It needs to produce a compilation to be emitted, but otherwise
    ' everything should be non-default if possible. Diagnostic settings are ignored
    ' because they won't be serialized. 
    Private ReadOnly VisualBasicParseOptions As VisualBasicParseOptions = New VisualBasicParseOptions(
            LanguageVersion.VisualBasic16,
            documentationMode:=DocumentationMode.Diagnose,
            preprocessorSymbols:={New KeyValuePair(Of String, Object)("PreOne", "True"), New KeyValuePair(Of String, Object)("PreTwo", "Test")})

    Private ReadOnly VisualBasicCompilationOptions As VisualBasicCompilationOptions = New VisualBasicCompilationOptions(
            OutputKind.ConsoleApplication,
            globalImports:=GlobalImport.Parse("System", "System.Threading"),
            rootNamespace:="RootNamespace",
            optionStrict:=OptionStrict.On,
            optionInfer:=False,
            optionExplicit:=False,
            optionCompareText:=True,
            parseOptions:=VisualBasicParseOptions,
            embedVbCoreRuntime:=True,
            optimizationLevel:=OptimizationLevel.Release,
            checkOverflow:=False,
            deterministic:=True)

    Private ReadOnly EmitOptions As EmitOptions = New EmitOptions(
            debugInformationFormat:=DebugInformationFormat.Embedded,
            pdbChecksumAlgorithm:=HashAlgorithmName.SHA256)


    Private Sub VerifyCompilationOptions(originalOptions As VisualBasicCompilationOptions, compilationOptionsBlobReader As BlobReader, Optional compilerVersion As String = Nothing)
        Dim pdbOptions = DeterministicBuildCompilationTestHelpers.ParseCompilationOptions(compilationOptionsBlobReader)

        If (compilerVersion = Nothing) Then
            compilerVersion = DeterministicBuildCompilationTestHelpers.GetCurrentCompilerVersion()
        End If


        ' See VisualBasicCompilation.SerializeForPdb for options that are added
        Assert.Equal(compilerVersion.ToString(), pdbOptions("compilerversion"))
        Assert.Equal(originalOptions.CheckOverflow.ToString(), pdbOptions("checked"))
        Assert.Equal(originalOptions.OptionStrict.ToString(), pdbOptions("optionstrict"))

        Dim preprocessorStrings = originalOptions.ParseOptions.PreprocessorSymbols.Select(Function(p)
                                                                                              If (p.Value Is Nothing) Then
                                                                                                  Return p.Key
                                                                                              End If

                                                                                              Return p.Key + "=" + p.Value.ToString()
                                                                                          End Function)
        Assert.Equal(String.Join(",", preprocessorStrings), pdbOptions("define"))

    End Sub

    Private Sub TestDeterministicCompilationVB(code As String, encoding As Encoding, ParamArray metadataReferences() As TestMetadataReferenceInfo)
        Dim syntaxTree = Parse(code, "a.vb", VisualBasicParseOptions, encoding)

        Dim originalCompilation = CreateCompilation(
                syntaxTree,
                references:=metadataReferences.SelectAsArray(Of MetadataReference)(Function(r) r.MetadataReference),
                options:=VisualBasicCompilationOptions)

        Dim peBlob = originalCompilation.EmitToArray(EmitOptions)

        Using peReader As PEReader = New PEReader(peBlob)
            Dim entries = peReader.ReadDebugDirectory()

            AssertEx.Equal({DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.Reproducible, DebugDirectoryEntryType.EmbeddedPortablePdb}, entries.Select(Of DebugDirectoryEntryType)(Function(e) e.Type))

            Dim codeView = entries(0)
            Dim checksum = entries(1)
            Dim reproducible = entries(2)
            Dim embedded = entries(3)


            Using embeddedPdb As MetadataReaderProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded)
                Dim pdbReader = embeddedPdb.GetMetadataReader()

                Dim metadataReferenceReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.MetadataReferenceInfo, pdbReader)
                Dim compilationOptionsReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.CompilationOptions, pdbReader)

                VerifyCompilationOptions(VisualBasicCompilationOptions, compilationOptionsReader)
                DeterministicBuildCompilationTestHelpers.VerifyReferenceInfo(metadataReferences, metadataReferenceReader)
            End Using
        End Using
    End Sub

    <Fact>
    Public Sub PortablePdb_DeterministicCompilation1()
        Dim source = "
Imports System

Module mainModule
    Sub Main()
        Console.WriteLine()
    End Sub
End Module"

        Dim referenceSource =
            <compilation>
                <file name="b.vb">
                    Public Class SomeClass
                    End Class

                    Public Class SomeOtherClass
                    End Class
                </file>
            </compilation>

        Dim referenceCompilation = CreateCompilation(referenceSource, options:=TestOptions.DebugDll)
        Using reference As TestMetadataReferenceInfo = TestMetadataReferenceInfo.Create(referenceCompilation, "abcd.dll", EmitOptions.Default)
            TestDeterministicCompilationVB(source, Encoding.UTF7, reference)
        End Using
    End Sub
End Class
