' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
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
    Implements IEnumerable(Of Object())

    Private Sub VerifyCompilationOptions(originalOptions As VisualBasicCompilationOptions, compilationOptionsBlobReader As BlobReader, emitOptions As EmitOptions, compilation As VisualBasicCompilation)
        Dim pdbOptions = DeterministicBuildCompilationTestHelpers.ParseCompilationOptions(compilationOptionsBlobReader)

        DeterministicBuildCompilationTestHelpers.AssertCommonOptions(emitOptions, originalOptions, compilation, pdbOptions)

        ' See VisualBasicCompilation.SerializeForPdb for options that are added
        pdbOptions.VerifyPdbOption("checked", originalOptions.CheckOverflow)
        pdbOptions.VerifyPdbOption("strict", originalOptions.OptionStrict)

        Assert.Equal(originalOptions.ParseOptions.LanguageVersion.MapSpecifiedToEffectiveVersion().ToDisplayString(), pdbOptions("language-version"))

        pdbOptions.VerifyPdbOption(
            "define",
            originalOptions.ParseOptions.PreprocessorSymbols,
            isDefault:=Function(v) v.IsEmpty,
            toString:=Function(v) String.Join(",", v.Select(Function(p) If(p.Value IsNot Nothing, $"{p.Key}=""{p.Value}""", p.Key))))
    End Sub

    Private Sub TestDeterministicCompilationVB(syntaxTrees As SyntaxTree(), compilationOptions As VisualBasicCompilationOptions, emitOptions As EmitOptions, ParamArray metadataReferences() As TestMetadataReferenceInfo)

        Dim tf = TargetFramework.NetStandard20
        Dim originalCompilation = CreateCompilation(
                syntaxTrees,
                references:=metadataReferences.SelectAsArray(Of MetadataReference)(Function(r) r.MetadataReference),
                options:=compilationOptions,
                targetFramework:=tf)

        Dim peBlob = originalCompilation.EmitToArray(emitOptions)

        Using peReader As PEReader = New PEReader(peBlob)
            Dim entries = peReader.ReadDebugDirectory()

            AssertEx.Equal({DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.Reproducible, DebugDirectoryEntryType.EmbeddedPortablePdb}, entries.Select(Of DebugDirectoryEntryType)(Function(e) e.Type))

            Dim codeView = entries(0)
            Dim checksum = entries(1)
            Dim reproducible = entries(2)
            Dim embedded = entries(3)

            Using embeddedPdb As MetadataReaderProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded)
                Dim pdbReader = embeddedPdb.GetMetadataReader()

                Dim metadataReferenceReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.CompilationMetadataReferences, pdbReader)
                Dim compilationOptionsReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.CompilationOptions, pdbReader)

                VerifyCompilationOptions(compilationOptions, compilationOptionsReader, emitOptions, originalCompilation)
                DeterministicBuildCompilationTestHelpers.VerifyReferenceInfo(metadataReferences, tf, metadataReferenceReader)
            End Using
        End Using
    End Sub

    <Theory>
    <ClassData(GetType(VisualBasicDeterministicBuildCompilationTests))>
    Public Sub PortablePdb_DeterministicCompilation(compilationOptions As VisualBasicCompilationOptions, emitOptions As EmitOptions)
        Dim sourceOne = Parse("
Class C1
End Class", fileName:="one.vb", options:=compilationOptions.ParseOptions, encoding:=Encoding.UTF8)

        Dim sourceTwo = Parse("
Class C2
End Class", fileName:="two.vb", options:=compilationOptions.ParseOptions, encoding:=New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))

        Dim referenceSourceOne =
            <compilation>
                <file name="b.vb">
                    Public Class SomeClass
                    End Class

                    Public Class SomeOtherClass
                    End Class
                </file>
            </compilation>

        Dim referenceSourceTwo =
    <compilation>
        <file name="b.vb">
                    Public Class SomeClass
                    End Class

                    Public Class SomeOtherClass
                    End Class
                </file>
    </compilation>

        Dim referenceOneCompilation = CreateCompilation(referenceSourceOne, options:=TestOptions.DebugDll)
        Dim referenceTwoCompilation = CreateCompilation(referenceSourceTwo, options:=TestOptions.DebugDll)

        Using referenceOne As TestMetadataReferenceInfo = TestMetadataReferenceInfo.Create(referenceOneCompilation, "abcd.dll", EmitOptions.Default)
            Using referenceTwo As TestMetadataReferenceInfo = TestMetadataReferenceInfo.Create(referenceTwoCompilation, "efgh.dll", EmitOptions.Default)
                TestDeterministicCompilationVB({sourceOne, sourceTwo}, compilationOptions, emitOptions, referenceOne, referenceTwo)
            End Using
        End Using
    End Sub

    <ConditionalTheory(GetType(DesktopOnly))>
    <ClassData(GetType(VisualBasicDeterministicBuildCompilationTests))>
    Public Sub PortablePdb_DeterministicCompilationWithSJIS(compilationOptions As VisualBasicCompilationOptions, emitOptions As EmitOptions)
        Dim sourceOne = Parse("
Class C1
End Class", fileName:="one.vb", options:=compilationOptions.ParseOptions, encoding:=Encoding.UTF8)

        Dim sourceTwo = Parse("
Class C2
End Class", fileName:="two.vb", options:=compilationOptions.ParseOptions, encoding:=New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))

        Dim sourceThree = Parse("
Class C3
End Class", fileName:="three.vb", options:=compilationOptions.ParseOptions, encoding:=Encoding.GetEncoding(932)) ' SJIS encoding

        Dim referenceSourceOne =
            <compilation>
                <file name="b.vb">
                    Public Class SomeClass
                    End Class

                    Public Class SomeOtherClass
                    End Class
                </file>
            </compilation>

        Dim referenceSourceTwo =
    <compilation>
        <file name="b.vb">
                    Public Class SomeClass
                    End Class

                    Public Class SomeOtherClass
                    End Class
                </file>
    </compilation>

        Dim referenceOneCompilation = CreateCompilation(referenceSourceOne, options:=TestOptions.DebugDll)
        Dim referenceTwoCompilation = CreateCompilation(referenceSourceTwo, options:=TestOptions.DebugDll)

        Using referenceOne As TestMetadataReferenceInfo = TestMetadataReferenceInfo.Create(referenceOneCompilation, "abcd.dll", EmitOptions.Default)
            Using referenceTwo As TestMetadataReferenceInfo = TestMetadataReferenceInfo.Create(referenceTwoCompilation, "efgh.dll", EmitOptions.Default)
                TestDeterministicCompilationVB({sourceOne, sourceTwo, sourceThree}, compilationOptions, emitOptions, referenceOne, referenceTwo)
            End Using
        End Using
    End Sub

    Public Iterator Function GetEnumerator() As IEnumerator(Of Object()) Implements IEnumerable(Of Object()).GetEnumerator
        For Each compilationOptions As VisualBasicCompilationOptions In GetCompilationOptions()
            For Each emitOptions As EmitOptions In GetEmitOptions()
                Yield {compilationOptions, emitOptions}
            Next
        Next
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function

    Private Iterator Function GetEmitOptions() As IEnumerable(Of EmitOptions)
        Dim emitOptions = New EmitOptions(debugInformationFormat:=DebugInformationFormat.Embedded)
        Yield emitOptions
        Yield emitOptions.WithDefaultSourceFileEncoding(Encoding.UTF8)
    End Function

    Private Iterator Function GetCompilationOptions() As IEnumerable(Of VisualBasicCompilationOptions)
        For Each parseOption As VisualBasicParseOptions In GetParseOptions()
            ' Provide non default options for to test that they are being serialized
            ' to the pdb correctly. It needs to produce a compilation to be emitted, but otherwise
            ' everything should be non-default if possible. Diagnostic settings are ignored
            ' because they won't be serialized. 

            ' Use constructor that requires all arguments. If New arguments are added, it's possible they need to be
            ' included in the pdb serialization And added to tests here
            Dim defaultOptions = New VisualBasicCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                moduleName:=Nothing,
                mainTypeName:=Nothing,
                scriptClassName:=WellKnownMemberNames.DefaultScriptClassName,
                globalImports:={GlobalImport.Parse("System")},
                rootNamespace:=Nothing,
                optionStrict:=OptionStrict.Off,
                optionInfer:=True,
                optionExplicit:=True,
                optionCompareText:=False,
                parseOptions:=parseOption,
                embedVbCoreRuntime:=False,
                optimizationLevel:=OptimizationLevel.Debug,
                checkOverflow:=True,
                cryptoKeyContainer:=Nothing,
                cryptoKeyFile:=Nothing,
                cryptoPublicKey:=Nothing,
                delaySign:=Nothing,
                platform:=Platform.AnyCpu,
                generalDiagnosticOption:=ReportDiagnostic.Default,
                specificDiagnosticOptions:=Nothing,
                concurrentBuild:=True,
                deterministic:=True,
                xmlReferenceResolver:=Nothing,
                sourceReferenceResolver:=Nothing,
                metadataReferenceResolver:=Nothing,
                assemblyIdentityComparer:=Nothing,
                strongNameProvider:=Nothing,
                publicSign:=False,
                reportSuppressedDiagnostics:=False,
                metadataImportOptions:=MetadataImportOptions.Public)

            Yield defaultOptions
            Yield defaultOptions.WithOptimizationLevel(OptimizationLevel.Release)
            Yield defaultOptions.WithDebugPlusMode(True)
            Yield defaultOptions.WithOptimizationLevel(OptimizationLevel.Release).WithDebugPlusMode(True)
        Next
    End Function

    Private Iterator Function GetParseOptions() As IEnumerable(Of VisualBasicParseOptions)
        Dim parseOptions As New VisualBasicParseOptions()

        Yield parseOptions
        Yield parseOptions.WithLanguageVersion(LanguageVersion.VisualBasic15_3)
        ' https://github.com/dotnet/roslyn/issues/44802 tracks
        ' enabling preprocessor symbol validation for VB
        ' Yield parseOptions.WithPreprocessorSymbols({New KeyValuePair(Of String, Object)("TestPre", True), New KeyValuePair(Of String, Object)("TestPreTwo", True)})
    End Function
End Class
