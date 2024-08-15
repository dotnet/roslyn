' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection.PortableExecutable
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.TestHelpers
Imports Roslyn.Test.Utilities.TestMetadata
Imports Basic.Reference.Assemblies
Imports CS = Microsoft.CodeAnalysis.CSharp

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CompilationAPITests
        Inherits BasicTestBase

        Private Function WithDiagnosticOptions(
            tree As SyntaxTree,
            ParamArray options As (String, ReportDiagnostic)()) As VisualBasicCompilationOptions

            Return TestOptions.DebugDll.
                WithSyntaxTreeOptionsProvider(New TestSyntaxTreeOptionsProvider(tree, options))
        End Function

        <Fact>
        Public Sub PerTreeVsGlobalSuppress()
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")
            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
            Dim comp = CreateCompilationWithMscorlib45({tree}, options:=options)
            comp.AssertNoDiagnostics()

            options = options.WithSyntaxTreeOptionsProvider(
                New TestSyntaxTreeOptionsProvider(tree, ("BC42024", ReportDiagnostic.Warn)))
            comp = CreateCompilationWithMscorlib45({tree}, options:=options)
            ' Global options override syntax tree options. This is the opposite of C# behavior
            comp.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub PerTreeDiagnosticOptionsParseWarnings()
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")

            Dim comp = CreateCompilation({tree}, options:=TestOptions.DebugDll)
            comp.AssertTheseDiagnostics(
                <errors>
BC42024: Unused local variable: 'x'.
        Dim x As Integer
            ~
                </errors>)

            Dim options = WithDiagnosticOptions(tree, ("BC42024", ReportDiagnostic.Suppress))
            Dim comp2 = CreateCompilation({tree}, options:=options)
            comp2.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub PerTreeDiagnosticOptionsVsPragma()
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Class C
     Sub M()
#Disable Warning BC42024
        Dim x As Integer
#Enable Warning BC42024
    End Sub
End Class")

            Dim comp = CreateCompilation({tree}, options:=TestOptions.DebugDll)
            comp.AssertNoDiagnostics()

            Dim options = WithDiagnosticOptions(tree, ("BC42024", ReportDiagnostic.Warn))
            Dim comp2 = CreateCompilation({tree}, options:=options)
            ' Pragma should have precedence over per-tree options
            comp2.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub PerTreeDiagnosticOptionsVsSpecificOptions()
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")

            Dim options = TestOptions.DebugDll.WithSpecificDiagnosticOptions(
                CreateImmutableDictionary(("BC42024", ReportDiagnostic.Suppress)))
            Dim comp = CreateCompilationWithMscorlib45({tree}, options:=options)
            comp.AssertNoDiagnostics()

            options = options.WithSyntaxTreeOptionsProvider(
                New TestSyntaxTreeOptionsProvider(tree, ("BC42024", ReportDiagnostic.Error)))
            Dim comp2 = CreateCompilationWithMscorlib45({tree}, options:=options)
            ' Specific diagnostic options should have precedence over tree options
            comp2.AssertNoDiagnostics()
        End Sub

        <Fact>
        Public Sub DifferentDiagnosticOptionsForTrees()
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")
            Dim newTree = SyntaxFactory.ParseSyntaxTree("
Class D
     Sub M()
        Dim y As Integer
    End Sub
End Class")

            Dim options = TestOptions.DebugDll.WithSyntaxTreeOptionsProvider(
                New TestSyntaxTreeOptionsProvider(
                    (tree, {("BC42024", ReportDiagnostic.Suppress)}),
                    (newTree, {("BC4024", ReportDiagnostic.Error)})))
            Dim comp = CreateCompilationWithMscorlib45({tree, newTree}, options:=options)
            comp.AssertTheseDiagnostics(
                <errors>
BC42024: Unused local variable: 'y'.
        Dim y As Integer
            ~ 
                </errors>)
        End Sub

        <Fact>
        Public Sub TreeOptionsComparerRespected()
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Class C
     Sub M()
        Dim x As Integer
    End Sub
End Class")
            ' Default provider is case insensitive
            Dim options = WithDiagnosticOptions(tree, ("bc42024", ReportDiagnostic.Suppress))

            Dim comp = CreateCompilation(tree, options:=options)
            comp.AssertNoDiagnostics()

            options = options.WithSyntaxTreeOptionsProvider(
                New TestSyntaxTreeOptionsProvider(
                    StringComparer.Ordinal,
                    Nothing,
                    (tree, {("bc42024", ReportDiagnostic.Suppress)}))
            )

            comp = CreateCompilation(tree, options:=options)
            comp.AssertTheseDiagnostics(
                <errors>
BC42024: Unused local variable: 'x'.
        Dim x As Integer
            ~
                </errors>)
        End Sub

        <WorkItem(8360, "https://github.com/dotnet/roslyn/issues/8360")>
        <WorkItem(9153, "https://github.com/dotnet/roslyn/issues/9153")>
        <Fact>
        Public Sub PublicSignWithRelativeKeyPath()
            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                    WithPublicSign(True).WithCryptoKeyFile("test.snk")
            AssertTheseDiagnostics(VisualBasicCompilation.Create("test", options:=options),
<errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
BC37257: Option 'CryptoKeyFile' must be an absolute path.
</errors>)
        End Sub

        <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        <Fact>
        Public Sub PublicSignWithEmptyKeyPath()
            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                    WithPublicSign(True).WithCryptoKeyFile("")
            AssertTheseDiagnostics(VisualBasicCompilation.Create("test", options:=options),
<errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
</errors>)
        End Sub

        <WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")>
        <Fact>
        Public Sub PublicSignWithEmptyKeyPath2()
            Dim options = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).
                    WithPublicSign(True).WithCryptoKeyFile("""""")
            AssertTheseDiagnostics(VisualBasicCompilation.Create("test", options:=options),
<errors>
BC37254: Public sign was specified and requires a public key, but no public key was specified
BC37257: Option 'CryptoKeyFile' must be an absolute path.
</errors>)
        End Sub

        <Fact>
        Public Sub LocalizableErrorArgumentToStringDoesntStackOverflow()
            ' Error ID is arbitrary
            Dim arg = New LocalizableErrorArgument(ERRID.IDS_ProjectSettingsLocationName)
            Assert.NotNull(arg.ToString())
        End Sub

        <WorkItem(538778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538778")>
        <WorkItem(537623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537623")>
        <WorkItem(233669, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=233669")>
        <Fact>
        Public Sub CompilationName()
            ' report an error, rather then silently ignoring the directory
            ' (see cli partition II 22.30) 
            VisualBasicCompilation.Create("C:/goo/Test.exe").AssertTheseEmitDiagnostics(
                <expected>
BC30420: 'Sub Main' was not found in 'C:/goo/Test.exe'.
BC37283: Invalid assembly name: Name contains invalid characters.
                </expected>)
            VisualBasicCompilation.Create("C:\goo\Test.exe", options:=TestOptions.ReleaseDll).AssertTheseDeclarationDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name contains invalid characters.
                </expected>)
            VisualBasicCompilation.Create("\goo/Test.exe", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name contains invalid characters.
                </expected>)
            VisualBasicCompilation.Create("C:Test.exe", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name contains invalid characters.
                </expected>)
            VisualBasicCompilation.Create("Te" & ChrW(0) & "st.exe", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name contains invalid characters.
                </expected>)
            VisualBasicCompilation.Create("  " & vbTab & "  ", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name cannot start with whitespace.
                </expected>)
            VisualBasicCompilation.Create(ChrW(&HD800), options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name contains invalid characters.
                </expected>)
            VisualBasicCompilation.Create("", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name cannot be empty.
                </expected>)
            VisualBasicCompilation.Create(" a", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics(
                <expected>
BC37283: Invalid assembly name: Name cannot start with whitespace.
                </expected>)
            VisualBasicCompilation.Create("\u2000a", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics( ' // U+2000 is whitespace
                <expected>
BC37283: Invalid assembly name: Name contains invalid characters.
                </expected>)

            ' other characters than directory separators are ok:
            VisualBasicCompilation.Create(";,*?<>#!@&", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics()
            VisualBasicCompilation.Create("goo", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics()
            VisualBasicCompilation.Create(".goo", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics()
            VisualBasicCompilation.Create("goo ", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics() ' can end with whitespace
            VisualBasicCompilation.Create("....", options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics()
            VisualBasicCompilation.Create(Nothing, options:=TestOptions.ReleaseDll).AssertTheseEmitDiagnostics()
        End Sub

        <Fact>
        Public Sub CreateAPITest()
            Dim listSyntaxTree = New List(Of SyntaxTree)
            Dim listRef = New List(Of MetadataReference)

            Dim s1 = "using Goo"
            Dim t1 As SyntaxTree = VisualBasicSyntaxTree.ParseText(s1)
            listSyntaxTree.Add(t1)

            ' System.dll
            listRef.Add(NetFramework.System)
            Dim ops = TestOptions.ReleaseExe

            ' Create Compilation with Option is not Nothing
            Dim comp = VisualBasicCompilation.Create("Compilation", listSyntaxTree, listRef, ops)
            Assert.Equal(ops, comp.Options)
            Assert.NotNull(comp.SyntaxTrees)
            Assert.NotNull(comp.References)
            Assert.Equal(1, comp.SyntaxTrees.Count)
            Assert.Equal(1, comp.References.Count)

            ' Create Compilation with PreProcessorSymbols of Option is empty
            Dim ops1 = TestOptions.ReleaseExe.WithGlobalImports(GlobalImport.Parse({"System", "Microsoft.VisualBasic"})).WithRootNamespace("")
            ' Create Compilation with Assembly name contains invalid char
            Dim asmname = "楽聖いち にÅÅ€"
            comp = VisualBasicCompilation.Create(asmname, listSyntaxTree, listRef, ops1)
            Assert.Equal(asmname, comp.Assembly.Name)
            Assert.Equal(asmname + ".exe", comp.SourceModule.Name)

            Dim compOpt = VisualBasicCompilation.Create("Compilation", Nothing, Nothing, Nothing)
            Assert.NotNull(compOpt.Options)

            ' Not Implemented code
            ' comp = comp.ChangeOptions(options:=null)
            ' Assert.Equal(CompilationOptions.Default, comp.Options)
            ' comp = comp.ChangeOptions(ops1)
            ' Assert.Equal(ops1, comp.Options)
            ' comp = comp.ChangeOptions(comp1.Options)
            ' ssert.Equal(comp1.Options, comp.Options)
            ' comp = comp.ChangeOptions(CompilationOptions.Default)
            ' Assert.Equal(CompilationOptions.Default, comp.Options)
        End Sub

        <Fact>
        Public Sub GetSpecialType()
            Dim comp = DirectCast(VisualBasicCompilation.Create("compilation", Nothing, Nothing, Nothing), Compilation)
            ' Get Special Type by enum
            Dim ntSmb = comp.GetSpecialType(typeId:=SpecialType.Count)
            Assert.Equal(SpecialType.Count, ntSmb.SpecialType)
            ' Get Special Type by integer
            ntSmb = comp.GetSpecialType(CType(31, SpecialType))
            Assert.Equal(31, CType(ntSmb.SpecialType, Integer))
        End Sub

        <Fact>
        Public Sub GetTypeByMetadataName()
            Dim comp = VisualBasicCompilation.Create("compilation", Nothing, Nothing, Nothing)
            ' Get Type Name And Arity
            Assert.Null(comp.GetTypeByMetadataName("`1"))
            Assert.Null(comp.GetTypeByMetadataName("中文`1"))

            ' Throw exception when the parameter of GetTypeByNameAndArity is NULL 
            'Assert.Throws(Of Exception)(
            '    Sub()
            '        comp.GetTypeByNameAndArity(fullName:=Nothing, arity:=1)
            '    End Sub)

            ' Throw exception when the parameter of GetTypeByNameAndArity is less than 0 
            'Assert.Throws(Of Exception)(
            '    Sub()
            '        comp.GetTypeByNameAndArity(String.Empty, -4)
            '    End Sub)

            Dim compilationDef =
<compilation name="compilation">
    <file name="a.vb">
Namespace A.B  
    Class C  
        Class D  
        Class E 
        End Class
        End Class
    End Class

    Class G(Of T) 
        Class Q(Of S1,S2) 
        ENd Class
    End Class

    Class G(Of T1,T2) 
    End Class
End Namespace

       Class C  
         Class D  
           Class E 
           End Class
         End Class
       End Class

    </file>
</compilation>

            comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(compilationDef)

            'IsCaseSensitive
            Assert.Equal(Of Boolean)(False, comp.IsCaseSensitive)

            Assert.Equal("D", comp.GetTypeByMetadataName("C+D").Name)
            Assert.Equal("E", comp.GetTypeByMetadataName("C+D+E").Name)

            Assert.Null(comp.GetTypeByMetadataName(""))
            Assert.Null(comp.GetTypeByMetadataName("+"))
            Assert.Null(comp.GetTypeByMetadataName("++"))
            Assert.Equal("C", comp.GetTypeByMetadataName("A.B.C").Name)
            Assert.Equal("D", comp.GetTypeByMetadataName("A.B.C+D").Name)
            Assert.Null(comp.GetTypeByMetadataName("A.B.C+F"))
            Assert.Equal("E", comp.GetTypeByMetadataName("A.B.C+D+E").Name)
            Assert.Null(comp.GetTypeByMetadataName("A.B.C+D+E+F"))
            Assert.Equal(1, comp.GetTypeByMetadataName("A.B.G`1").Arity)
            Assert.Equal(2, comp.GetTypeByMetadataName("A.B.G`1+Q`2").Arity)
            Assert.Equal(2, comp.GetTypeByMetadataName("A.B.G`2").Arity)

            Assert.Null(comp.GetTypeByMetadataName("c"))
            Assert.Null(comp.GetTypeByMetadataName("A.b.C"))
            Assert.Null(comp.GetTypeByMetadataName("C+d"))

            Assert.Equal(SpecialType.System_Array, comp.GetTypeByMetadataName("System.Array").SpecialType)
            Assert.Null(comp.Assembly.GetTypeByMetadataName("System.Array"))
            Assert.Equal("E", comp.Assembly.GetTypeByMetadataName("A.B.C+D+E").Name)
        End Sub

        <Fact>
        Public Sub EmitToMemoryStreams()
            Dim comp = VisualBasicCompilation.Create("Compilation", options:=TestOptions.ReleaseDll)

            Using output = New MemoryStream()
                Using outputPdb = New MemoryStream()
                    Using outputxml = New MemoryStream()
                        Dim result = comp.Emit(output, outputPdb, Nothing)
                        Assert.True(result.Success)
                        result = comp.Emit(output, outputPdb)
                        Assert.True(result.Success)
                        result = comp.Emit(peStream:=output, pdbStream:=outputPdb, xmlDocumentationStream:=Nothing, cancellationToken:=Nothing)
                        Assert.True(result.Success)
                        result = comp.Emit(peStream:=output, pdbStream:=outputPdb, cancellationToken:=Nothing)
                        Assert.True(result.Success)
                        result = comp.Emit(output, outputPdb)
                        Assert.True(result.Success)
                        result = comp.Emit(output, outputPdb)
                        Assert.True(result.Success)
                        result = comp.Emit(output, outputPdb, outputxml)
                        Assert.True(result.Success)
                        result = comp.Emit(output, Nothing, Nothing, Nothing)
                        Assert.True(result.Success)
                        result = comp.Emit(output)
                        Assert.True(result.Success)
                        result = comp.Emit(output, Nothing, outputxml)
                        Assert.True(result.Success)
                        result = comp.Emit(output, xmlDocumentationStream:=outputxml)
                        Assert.True(result.Success)
                        result = comp.Emit(output, Nothing, outputxml)
                        Assert.True(result.Success)
                        result = comp.Emit(output, xmlDocumentationStream:=outputxml)
                        Assert.True(result.Success)
                    End Using
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub Emit_BadArgs()
            Dim comp = VisualBasicCompilation.Create("Compilation", options:=TestOptions.ReleaseDll)

            Assert.Throws(Of ArgumentNullException)("peStream", Sub() comp.Emit(peStream:=Nothing))
            Assert.Throws(Of ArgumentException)("peStream", Sub() comp.Emit(peStream:=New TestStream(canRead:=True, canWrite:=False, canSeek:=True)))
            Assert.Throws(Of ArgumentException)("pdbStream", Sub() comp.Emit(peStream:=New MemoryStream(), pdbStream:=New TestStream(canRead:=True, canWrite:=False, canSeek:=True)))
            Assert.Throws(Of ArgumentException)("pdbStream", Sub() comp.Emit(peStream:=New MemoryStream(), pdbStream:=New MemoryStream(), options:=EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded)))

            Assert.Throws(Of ArgumentException)("sourceLinkStream", Sub() comp.Emit(
                peStream:=New MemoryStream(),
                pdbStream:=New MemoryStream(),
                options:=EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb),
                sourceLinkStream:=New TestStream(canRead:=False, canWrite:=True, canSeek:=True)))

            Assert.Throws(Of ArgumentException)("embeddedTexts", Sub() comp.Emit(
                peStream:=New MemoryStream(),
                pdbStream:=Nothing,
                options:=Nothing,
                embeddedTexts:={EmbeddedText.FromStream("_", New MemoryStream())}))

            Assert.Throws(Of ArgumentException)("embeddedTexts", Sub() comp.Emit(
                peStream:=New MemoryStream(),
                pdbStream:=Nothing,
                options:=EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb),
                embeddedTexts:={EmbeddedText.FromStream("_", New MemoryStream())}))

            Assert.Throws(Of ArgumentException)("win32Resources", Sub() comp.Emit(
                peStream:=New MemoryStream(),
                win32Resources:=New TestStream(canRead:=True, canWrite:=False, canSeek:=False)))

            Assert.Throws(Of ArgumentException)("win32Resources", Sub() comp.Emit(
                peStream:=New MemoryStream(),
                win32Resources:=New TestStream(canRead:=False, canWrite:=False, canSeek:=True)))

            ' we don't report an error when we can't write to the XML doc stream:
            Assert.True(comp.Emit(
                peStream:=New MemoryStream(),
                pdbStream:=New MemoryStream(),
                xmlDocumentationStream:=New TestStream(canRead:=True, canWrite:=False, canSeek:=True)).Success)
        End Sub

        <Fact>
        Public Sub EmitOptionsDiagnostics()
            Dim c = CreateCompilationWithMscorlib40({"class C {}"})
            Dim stream = New MemoryStream()

            Dim options = New EmitOptions(
                debugInformationFormat:=CType(-1, DebugInformationFormat),
                outputNameOverride:=" ",
                fileAlignment:=513,
                subsystemVersion:=SubsystemVersion.Create(1000000, -1000000),
                pdbChecksumAlgorithm:=New HashAlgorithmName("invalid hash algorithm name"))

            Dim result = c.Emit(stream, options:=options)

            result.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_InvalidDebugInformationFormat).WithArguments("-1"),
                Diagnostic(ERRID.ERR_InvalidOutputName).WithArguments("Name cannot start with whitespace."),
                Diagnostic(ERRID.ERR_InvalidFileAlignment).WithArguments("513"),
                Diagnostic(ERRID.ERR_InvalidSubsystemVersion).WithArguments("1000000.-1000000"),
                Diagnostic(ERRID.ERR_InvalidHashAlgorithmName).WithArguments("invalid hash algorithm name"))

            Assert.False(result.Success)
        End Sub

        <Fact>
        Sub EmitOptions_PdbChecksumAndDeterminism()
            Dim options = New EmitOptions(pdbChecksumAlgorithm:=New HashAlgorithmName())
            Dim diagnosticBag = New DiagnosticBag()

            options.ValidateOptions(diagnosticBag, MessageProvider.Instance, isDeterministic:=True)
            diagnosticBag.Verify(
                Diagnostic(ERRID.ERR_InvalidHashAlgorithmName).WithArguments(""))

            diagnosticBag.Clear()

            options.ValidateOptions(diagnosticBag, MessageProvider.Instance, isDeterministic:=False)
            diagnosticBag.Verify()
        End Sub

        <Fact>
        Public Sub ReferenceAPITest()
            ' Create Compilation takes two args
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim ref1 = NetFramework.mscorlib
            Dim ref2 = NetFramework.System
            Dim ref3 = New TestMetadataReference(fullPath:="c:\xml.bms")
            Dim ref4 = New TestMetadataReference(fullPath:="c:\aaa.dll")

            ' Add a new empty item 
            comp = comp.AddReferences(Enumerable.Empty(Of MetadataReference)())
            Assert.Equal(0, comp.References.Count)

            ' Add a new valid item 
            comp = comp.AddReferences(ref1)
            Dim assemblySmb = comp.GetReferencedAssemblySymbol(ref1)
            Assert.NotNull(assemblySmb)
            Assert.Equal("mscorlib", assemblySmb.Name, StringComparer.OrdinalIgnoreCase)
            Assert.Equal(1, comp.References.Count)
            Assert.Equal(MetadataImageKind.Assembly, comp.References(0).Properties.Kind)
            Assert.Same(ref1, comp.References(0))

            ' Replace an existing item with another valid item 
            comp = comp.ReplaceReference(ref1, ref2)
            Assert.Equal(1, comp.References.Count)
            Assert.Equal(MetadataImageKind.Assembly, comp.References(0).Properties.Kind)
            Assert.Equal(ref2, comp.References(0))

            ' Remove an existing item 
            comp = comp.RemoveReferences(ref2)
            Assert.Equal(0, comp.References.Count)

            'WithReferences 
            Dim hs1 As New HashSet(Of MetadataReference) From {ref1, ref2, ref3}
            Dim compCollection1 = VisualBasicCompilation.Create("Compilation")
            Assert.Equal(Of Integer)(0, Enumerable.Count(Of MetadataReference)(compCollection1.References))
            Dim c2 As Compilation = compCollection1.WithReferences(hs1)
            Assert.Equal(Of Integer)(3, Enumerable.Count(Of MetadataReference)(c2.References))

            'WithReferences 
            Dim compCollection2 = VisualBasicCompilation.Create("Compilation")
            Assert.Equal(Of Integer)(0, Enumerable.Count(Of MetadataReference)(compCollection2.References))
            Dim c3 As Compilation = compCollection1.WithReferences(ref1, ref2, ref3)
            Assert.Equal(Of Integer)(3, Enumerable.Count(Of MetadataReference)(c3.References))

            'ReferencedAssemblyNames
            Dim RefAsm_Names As IEnumerable(Of AssemblyIdentity) = c2.ReferencedAssemblyNames
            Assert.Equal(Of Integer)(2, Enumerable.Count(Of AssemblyIdentity)(RefAsm_Names))
            Dim ListNames As New List(Of String)
            Dim I As AssemblyIdentity
            For Each I In RefAsm_Names
                ListNames.Add(I.Name)
            Next
            Assert.Contains(Of String)("mscorlib", ListNames)
            Assert.Contains(Of String)("System", ListNames)

            'RemoveAllReferences
            c2 = c2.RemoveAllReferences
            Assert.Equal(Of Integer)(0, Enumerable.Count(Of MetadataReference)(c2.References))

            ' Overload with Hashset
            Dim hs = New HashSet(Of MetadataReference)() From {ref1, ref2, ref3}
            Dim compCollection = VisualBasicCompilation.Create("Compilation", references:=hs)
            compCollection = compCollection.AddReferences(ref1, ref2, ref3, ref4).RemoveReferences(hs)
            Assert.Equal(1, compCollection.References.Count)
            compCollection = compCollection.AddReferences(hs).RemoveReferences(ref1, ref2, ref3, ref4)
            Assert.Equal(0, compCollection.References.Count)

            ' Overload with Collection
            Dim col = New ObjectModel.Collection(Of MetadataReference)() From {ref1, ref2, ref3}
            compCollection = VisualBasicCompilation.Create("Compilation", references:=col)
            compCollection = compCollection.AddReferences(col).RemoveReferences(ref1, ref2, ref3)
            Assert.Equal(0, compCollection.References.Count)
            compCollection = compCollection.AddReferences(ref1, ref2, ref3).RemoveReferences(col)
            Assert.Equal(0, comp.References.Count)

            ' Overload with ConcurrentStack
            Dim stack = New Concurrent.ConcurrentStack(Of MetadataReference)
            stack.Push(ref1)
            stack.Push(ref2)
            stack.Push(ref3)
            compCollection = VisualBasicCompilation.Create("Compilation", references:=stack)
            compCollection = compCollection.AddReferences(stack).RemoveReferences(ref1, ref3, ref2)
            Assert.Equal(0, compCollection.References.Count)
            compCollection = compCollection.AddReferences(ref2, ref1, ref3).RemoveReferences(stack)
            Assert.Equal(0, compCollection.References.Count)

            ' Overload with ConcurrentQueue
            Dim queue = New Concurrent.ConcurrentQueue(Of MetadataReference)
            queue.Enqueue(ref1)
            queue.Enqueue(ref2)
            queue.Enqueue(ref3)
            compCollection = VisualBasicCompilation.Create("Compilation", references:=queue)
            compCollection = compCollection.AddReferences(queue).RemoveReferences(ref3, ref2, ref1)
            Assert.Equal(0, compCollection.References.Count)
            compCollection = compCollection.AddReferences(ref2, ref1, ref3).RemoveReferences(queue)
            Assert.Equal(0, compCollection.References.Count)
        End Sub

        <WorkItem(537826, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537826")>
        <Fact>
        Public Sub SyntreeAPITest()

            Dim s1 = "using System.Linq;"
            Dim s2 = <![CDATA[Class goo
                        sub main
                            Public Operator
                         End Operator
                    End Class
                ]]>.Value
            Dim s3 = "Imports s$ = System.Text"
            Dim s4 = <text>
                Module Module1
                    Sub Goo()
                        for i = 0 to 100
                        next
                    end sub
               End Module
                </text>.Value
            Dim t1 = VisualBasicSyntaxTree.ParseText(s4)
            Dim withErrorTree = VisualBasicSyntaxTree.ParseText(s2)
            Dim withErrorTree1 = VisualBasicSyntaxTree.ParseText(s3)
            Dim withErrorTreeCS = VisualBasicSyntaxTree.ParseText(s1)
            Dim withExpressionRootTree = SyntaxFactory.ParseExpression("0").SyntaxTree

            ' Create compilation takes three args
            Dim comp = VisualBasicCompilation.Create("Compilation",
                                                     {t1},
                                                     {MscorlibRef, MsvbRef},
                                                     TestOptions.ReleaseDll)
            Dim tree = comp.SyntaxTrees.AsEnumerable()
            comp.VerifyDiagnostics()

            ' Add syntaxtree with error
            comp = comp.AddSyntaxTrees(withErrorTreeCS)
            Assert.Equal(2, comp.GetDiagnostics().Length())

            ' Remove syntaxtree without error
            comp = comp.RemoveSyntaxTrees(tree)
            Assert.Equal(2, comp.GetDiagnostics(cancellationToken:=CancellationToken.None).Length())

            ' Remove syntaxtree with error
            comp = comp.RemoveSyntaxTrees(withErrorTreeCS)
            Assert.Equal(0, comp.GetDiagnostics().Length())
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Length())

            ' Get valid binding
            Dim bind = comp.GetSemanticModel(syntaxTree:=t1)
            Assert.NotNull(bind)

            ' Get Binding with tree is not exist
            bind = comp.GetSemanticModel(withErrorTree)
            Assert.NotNull(bind)

            ' Add syntaxtree which is CS language
            comp = comp.AddSyntaxTrees(withErrorTreeCS)
            Assert.Equal(2, comp.GetDiagnostics().Length())

            comp = comp.RemoveSyntaxTrees(withErrorTreeCS)
            Assert.Equal(0, comp.GetDiagnostics().Length())

            comp = comp.AddSyntaxTrees(t1, withErrorTree, withErrorTree1, withErrorTreeCS)
            comp = comp.RemoveSyntaxTrees(t1, withErrorTree, withErrorTree1, withErrorTreeCS)

            ' Add a new empty item
            comp = comp.AddSyntaxTrees(Enumerable.Empty(Of SyntaxTree))
            Assert.Equal(0, comp.SyntaxTrees.Length)

            ' Add a new valid item
            comp = comp.AddSyntaxTrees(t1)
            Assert.Equal(1, comp.SyntaxTrees.Length)

            comp = comp.AddSyntaxTrees(VisualBasicSyntaxTree.ParseText(s4))
            Assert.Equal(2, comp.SyntaxTrees.Length)

            ' Replace an existing item with another valid item 
            comp = comp.ReplaceSyntaxTree(t1, VisualBasicSyntaxTree.ParseText(s4))
            Assert.Equal(2, comp.SyntaxTrees.Length)

            ' Replace an existing item with same item 
            comp = comp.AddSyntaxTrees(t1).ReplaceSyntaxTree(t1, t1)
            Assert.Equal(3, comp.SyntaxTrees.Length)

            ' Replace with existing and verify that it throws
            Assert.Throws(Of ArgumentException)(Sub() comp.ReplaceSyntaxTree(t1, comp.SyntaxTrees(0)))

            Assert.Throws(Of ArgumentException)(Sub() comp.AddSyntaxTrees(t1))

            ' SyntaxTrees have reference equality. This removal should fail.
            Assert.Throws(Of ArgumentException)(Sub() comp = comp.RemoveSyntaxTrees(VisualBasicSyntaxTree.ParseText(s4)))
            Assert.Equal(3, comp.SyntaxTrees.Length)

            ' Remove non-existing item
            Assert.Throws(Of ArgumentException)(Sub() comp = comp.RemoveSyntaxTrees(withErrorTree))
            Assert.Equal(3, comp.SyntaxTrees.Length)

            Dim t4 = VisualBasicSyntaxTree.ParseText("Using System;")
            Dim t5 = VisualBasicSyntaxTree.ParseText("Usingsssssssssssss System;")
            Dim t6 = VisualBasicSyntaxTree.ParseText("Import System")

            ' Overload with Hashset
            Dim hs = New HashSet(Of SyntaxTree) From {t4, t5, t6}
            Dim compCollection = VisualBasicCompilation.Create("Compilation", syntaxTrees:=hs)
            compCollection = compCollection.RemoveSyntaxTrees(hs)
            Assert.Equal(0, compCollection.SyntaxTrees.Length)
            compCollection = compCollection.AddSyntaxTrees(hs).RemoveSyntaxTrees(t4, t5, t6)
            Assert.Equal(0, compCollection.SyntaxTrees.Length)

            ' Overload with Collection
            Dim col = New ObjectModel.Collection(Of SyntaxTree) From {t4, t5, t6}
            compCollection = VisualBasicCompilation.Create("Compilation", syntaxTrees:=col)
            compCollection = compCollection.RemoveSyntaxTrees(t4, t5, t6)
            Assert.Equal(0, compCollection.SyntaxTrees.Length)
            Assert.Throws(Of ArgumentException)(Sub() compCollection = compCollection.AddSyntaxTrees(t4, t5).RemoveSyntaxTrees(col))
            Assert.Equal(0, compCollection.SyntaxTrees.Length)

            ' Overload with ConcurrentStack
            Dim stack = New Concurrent.ConcurrentStack(Of SyntaxTree)
            stack.Push(t4)
            stack.Push(t5)
            stack.Push(t6)
            compCollection = VisualBasicCompilation.Create("Compilation", syntaxTrees:=stack)
            compCollection = compCollection.RemoveSyntaxTrees(t4, t6, t5)
            Assert.Equal(0, compCollection.SyntaxTrees.Length)
            Assert.Throws(Of ArgumentException)(Sub() compCollection = compCollection.AddSyntaxTrees(t4, t6).RemoveSyntaxTrees(stack))
            Assert.Equal(0, compCollection.SyntaxTrees.Length)

            ' Overload with ConcurrentQueue
            Dim queue = New Concurrent.ConcurrentQueue(Of SyntaxTree)
            queue.Enqueue(t4)
            queue.Enqueue(t5)
            queue.Enqueue(t6)
            compCollection = VisualBasicCompilation.Create("Compilation", syntaxTrees:=queue)
            compCollection = compCollection.RemoveSyntaxTrees(t4, t6, t5)
            Assert.Equal(0, compCollection.SyntaxTrees.Length)
            Assert.Throws(Of ArgumentException)(Sub() compCollection = compCollection.AddSyntaxTrees(t4, t6).RemoveSyntaxTrees(queue))
            Assert.Equal(0, compCollection.SyntaxTrees.Length)

            ' VisualBasicCompilation.Create with syntaxtree with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.False(withExpressionRootTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?")
            Assert.Throws(Of ArgumentException)(Sub() VisualBasicCompilation.Create("Compilation", syntaxTrees:={withExpressionRootTree}))

            ' AddSyntaxTrees with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.Throws(Of ArgumentException)(Sub() comp.AddSyntaxTrees(withExpressionRootTree))

            ' ReplaceSyntaxTrees syntaxtree with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.Throws(Of ArgumentException)(Sub() comp.ReplaceSyntaxTree(comp.SyntaxTrees(0), withExpressionRootTree))
        End Sub

        <Fact>
        Public Sub ChainedOperations()

            Dim s1 = "using System.Linq;"
            Dim s2 = ""
            Dim s3 = "Import System"
            Dim t1 = VisualBasicSyntaxTree.ParseText(s1)
            Dim t2 = VisualBasicSyntaxTree.ParseText(s2)
            Dim t3 = VisualBasicSyntaxTree.ParseText(s3)

            Dim listSyntaxTree = New List(Of SyntaxTree)
            listSyntaxTree.Add(t1)
            listSyntaxTree.Add(t2)

            ' Remove second SyntaxTree
            Dim comp = VisualBasicCompilation.Create("Compilation")
            comp = comp.AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(t2)
            Assert.Equal(1, comp.SyntaxTrees.Length)

            'ContainsSyntaxTree
            Dim b1 As Boolean = comp.ContainsSyntaxTree(t2)
            Assert.Equal(Of Boolean)(False, b1)
            comp = comp.AddSyntaxTrees({t2})
            b1 = comp.ContainsSyntaxTree(t2)
            Assert.Equal(Of Boolean)(True, b1)

            Dim xt As SyntaxTree = Nothing
            Assert.Equal(Of Boolean)(False, comp.ContainsSyntaxTree(xt))

            comp = comp.RemoveSyntaxTrees({t2})
            Assert.Equal(1, comp.SyntaxTrees.Length)
            comp = comp.AddSyntaxTrees({t2})
            Assert.Equal(2, comp.SyntaxTrees.Length)

            'RemoveAllSyntaxTrees
            comp = comp.RemoveAllSyntaxTrees
            Assert.Equal(0, comp.SyntaxTrees.Length)
            comp = VisualBasicCompilation.Create("Compilation").AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees({t2})
            Assert.Equal(Of Integer)(1, comp.SyntaxTrees.Length)
            Assert.Equal(Of String)("Object", comp.ObjectType.Name)

            ' Remove mid SyntaxTree
            listSyntaxTree.Add(t3)
            comp = comp.RemoveSyntaxTrees(t1).AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(t2)
            Assert.Equal(2, comp.SyntaxTrees.Length)

            ' remove list
            listSyntaxTree.Remove(t2)
            comp = comp.AddSyntaxTrees().RemoveSyntaxTrees(listSyntaxTree)
            comp = comp.AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(listSyntaxTree)
            Assert.Equal(0, comp.SyntaxTrees.Length)

            listSyntaxTree.Clear()
            listSyntaxTree.Add(t1)
            ' Chained operation count > 2
            comp = comp.AddSyntaxTrees(listSyntaxTree).AddReferences().ReplaceSyntaxTree(t1, t2)
            Assert.Equal(1, comp.SyntaxTrees.Length)
            Assert.Equal(0, comp.References.Count)

            ' Create compilation with args is disordered
            Dim comp1 = VisualBasicCompilation.Create("Compilation")
            Dim Err = "c:\file_that_does_not_exist"
            Dim ref1 = NetFramework.mscorlib
            Dim listRef = New List(Of MetadataReference)
            ' this is NOT testing Roslyn
            listRef.Add(ref1)
            listRef.Add(ref1)

            ' Remove with no args
            comp1 = comp1.AddReferences(listRef).AddSyntaxTrees(listSyntaxTree).RemoveReferences().RemoveSyntaxTrees()
            'should have only added one reference since ref1.Equals(ref1) and Equal references are added only once.
            Assert.Equal(1, comp1.References.Count)
            Assert.Equal(1, comp1.SyntaxTrees.Length)

        End Sub

        <WorkItem(713356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713356")>
        <Fact()>
        Public Sub MissedModuleA()
            Dim netModule1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="missing1">
    <file name="a.vb">
Class C1
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseModule)
            netModule1.VerifyDiagnostics()

            Dim netModule2 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="missing2">
        <file name="a.vb">
Class C2
    Public Shared Sub M()
        Dim a As New C1()
    End Sub
End Class
    </file>
    </compilation>, references:={netModule1.EmitToImageReference()}, options:=TestOptions.ReleaseModule)
            netModule2.VerifyDiagnostics()

            Dim assembly = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="missing">
    <file name="a.vb">
Class C3
    Public Shared Sub Main(args() As String)
        Dim a As New C2()
    End Sub
End Class
    </file>
</compilation>, references:={netModule2.EmitToImageReference()})
            assembly.VerifyDiagnostics(Diagnostic(ERRID.ERR_MissingNetModuleReference).WithArguments("missing1.netmodule"))

            assembly = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="MissedModuleA">
    <file name="a.vb">
Class C3
    Public Shared Sub Main(args() As String)
        Dim a As New C2()
    End Sub
End Class
    </file>
</compilation>, references:={netModule1.EmitToImageReference(), netModule2.EmitToImageReference()})
            assembly.VerifyDiagnostics()

            ' ILVerify: Assembly or module not found: missing2
            CompileAndVerify(assembly, verify:=Verification.FailsILVerify)
        End Sub

        <WorkItem(713356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713356")>
        <Fact()>
        Public Sub MissedModuleB_OneError()
            Dim netModule1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="a1">
    <file name="a.vb">
Class C1
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseModule)
            CompilationUtils.AssertNoDiagnostics(netModule1)

            Dim netModule2 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="a2">
        <file name="a.vb">
Class C2
    Public Shared Sub M()
        Dim a As New C1()
    End Sub
End Class
    </file>
    </compilation>, references:={netModule1.EmitToImageReference()}, options:=TestOptions.ReleaseModule)
            CompilationUtils.AssertNoDiagnostics(netModule2)

            Dim netModule3 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="a3">
    <file name="a.vb">
Class C22
    Public Shared Sub M()
        Dim a As New C1()
    End Sub
End Class
    </file>
</compilation>, references:={netModule1.EmitToImageReference()}, options:=TestOptions.ReleaseModule)
            CompilationUtils.AssertNoDiagnostics(netModule3)

            Dim assembly = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="a">
    <file name="a.vb">
Class C3
    Public Shared Sub Main(args() As String)
        Dim a As New C2()
        Dim b As New C22()
    End Sub
End Class
    </file>
</compilation>, references:={netModule2.EmitToImageReference(), netModule3.EmitToImageReference()})

            CompilationUtils.AssertTheseDiagnostics(assembly,
<errors>
BC37221: Reference to 'a1.netmodule' netmodule missing.
</errors>)
        End Sub

        <WorkItem(718500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718500")>
        <WorkItem(716762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716762")>
        <Fact()>
        Public Sub MissedModuleB_NoErrorForUnmanagedModules()
            Dim netModule1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="a1">
    <file name="a.vb">
Imports System.Runtime.InteropServices

Public Class ClassDLLImports
    Declare Function getUserName Lib "advapi32.dll" Alias "GetUserNameA" (
        ByVal lpBuffer As String, ByRef nSize As Integer) As Integer
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseModule)
            CompilationUtils.AssertNoDiagnostics(netModule1)

            Dim assembly = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="a">
        <file name="a.vb">
Class C3
    Public Shared Sub Main(args() As String)
    End Sub
End Class
    </file>
    </compilation>, references:={netModule1.EmitToImageReference(expectedWarnings:={
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports System.Runtime.InteropServices")})})

            assembly.AssertNoDiagnostics()
        End Sub

        <WorkItem(715872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715872")>
        <Fact()>
        Public Sub MissedModuleC()
            Dim netModule1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="a1">
    <file name="a.vb">
Class C1
End Class
    </file>
</compilation>, options:=TestOptions.ReleaseModule)
            CompilationUtils.AssertNoDiagnostics(netModule1)

            Dim netModule2 = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
    <compilation name="a1">
        <file name="a.vb">
Class C2
    Public Shared Sub M()
    End Sub
End Class
    </file>
    </compilation>, references:={netModule1.EmitToImageReference()}, options:=TestOptions.ReleaseModule)
            CompilationUtils.AssertNoDiagnostics(netModule2)

            Dim assembly = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="a">
    <file name="a.vb">
Class C3
    Public Shared Sub Main(args() As String)
        Dim a As New C2()
    End Sub
End Class
    </file>
</compilation>, references:={netModule1.EmitToImageReference(), netModule2.EmitToImageReference()})

            CompilationUtils.AssertTheseDiagnostics(assembly,
<errors>
BC37224: Module 'a1.netmodule' is already defined in this assembly. Each module must have a unique filename.
</errors>)
        End Sub

        <Fact>
        Public Sub MixedRefType()
            ' Create compilation takes three args
            Dim csComp = CS.CSharpCompilation.Create("CompilationVB")
            Dim comp = VisualBasicCompilation.Create("Compilation")
            ' this is NOT right path, 
            ' please don't use VB dll (there is a change to the location in Dev11; you test will fail then)
            csComp = csComp.AddReferences(SystemRef)

            ' Add VB reference to C# compilation
            For Each item In csComp.References
                comp = comp.AddReferences(item)
                comp = comp.ReplaceReference(item, item)
            Next
            Assert.Equal(1, comp.References.Count)

            Dim text1 = "Imports System"
            Dim comp1 = VisualBasicCompilation.Create("Test1", {VisualBasicSyntaxTree.ParseText(text1)})
            Dim comp2 = VisualBasicCompilation.Create("Test2", {VisualBasicSyntaxTree.ParseText(text1)})

            Dim compRef1 = comp1.ToMetadataReference()
            Dim compRef2 = comp2.ToMetadataReference()

            Dim csCompRef = csComp.ToMetadataReference(embedInteropTypes:=True)

            Dim ref1 = NetFramework.mscorlib
            Dim ref2 = NetFramework.System

            ' Add VisualBasicCompilationReference
            comp = VisualBasicCompilation.Create("Test1",
                                         {VisualBasicSyntaxTree.ParseText(text1)},
                                         {compRef1, compRef2})
            Assert.Equal(2, comp.References.Count)
            Assert.Equal(MetadataImageKind.Assembly, comp.References(0).Properties.Kind)
            Assert.Contains(compRef1, comp.References)
            Assert.Contains(compRef2, comp.References)
            Dim smb = comp.GetReferencedAssemblySymbol(compRef1)
            Assert.Equal(smb.Kind, SymbolKind.Assembly)
            Assert.Equal("Test1", smb.Identity.Name, StringComparer.OrdinalIgnoreCase)

            ' Mixed reference type
            comp = comp.AddReferences(ref1)
            Assert.Equal(3, comp.References.Count)
            Assert.Contains(ref1, comp.References)

            ' Replace Compilation reference with Assembly file reference
            comp = comp.ReplaceReference(compRef2, ref2)
            Assert.Equal(3, comp.References.Count)
            Assert.Contains(ref2, comp.References)

            ' Replace Assembly file reference with Compilation reference
            comp = comp.ReplaceReference(ref1, compRef2)
            Assert.Equal(3, comp.References.Count)
            Assert.Contains(compRef2, comp.References)

            Dim modRef1 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference()

            ' Add Module file reference
            comp = comp.AddReferences(modRef1)
            ' Not Implemented
            'Dim modSmb = comp.GetReferencedModuleSymbol(modRef1)

            'Assert.Equal("ModuleCS00.mod", modSmb.Name)
            'Assert.Equal(4, comp.References.Count)
            'Assert.True(comp.References.Contains(modRef1))

            ' Get Referenced Assembly Symbol
            'smb = comp.GetReferencedAssemblySymbol(reference:=modRef1)
            'Assert.Equal(smb.Kind, SymbolKind.Assembly)
            'Assert.True(String.Equals(smb.AssemblyName.Name, "Test1", StringComparison.OrdinalIgnoreCase))

            ' Get Referenced Module Symbol
            'Dim moduleSmb = comp.GetReferencedModuleSymbol(reference:=modRef1)
            'Assert.Equal(moduleSmb.Kind, SymbolKind.NetModule)
            'Assert.True(String.Equals(moduleSmb.Name, "ModuleCS00.mod", StringComparison.OrdinalIgnoreCase))

            ' Not implemented
            ' Get Compilation Namespace
            'Dim nsSmb = comp.GlobalNamespace
            'Dim ns = comp.GetCompilationNamespace(ns:=nsSmb)
            'Assert.Equal(ns.Kind, SymbolKind.Namespace)
            'Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase))

            ' Not implemented
            ' GetCompilationNamespace (Derived Class MergedNamespaceSymbol)
            'Dim merged As NamespaceSymbol = MergedNamespaceSymbol.Create(New NamespaceExtent(New MockAssemblySymbol("Merged")), Nothing, Nothing)
            'ns = comp.GetCompilationNamespace(ns:=merged)
            'Assert.Equal(ns.Kind, SymbolKind.Namespace)
            'Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase))

            ' Not implemented
            ' GetCompilationNamespace (Derived Class PENamespaceSymbol)
            'Dim pensSmb As Metadata.PE.PENamespaceSymbol = CType(nsSmb, Metadata.PE.PENamespaceSymbol)
            'ns = comp.GetCompilationNamespace(ns:=pensSmb)
            'Assert.Equal(ns.Kind, SymbolKind.Namespace)
            'Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase))

            ' Replace Module file reference with compilation reference
            comp = comp.RemoveReferences(compRef1).ReplaceReference(modRef1, compRef1)
            Assert.Equal(3, comp.References.Count)
            ' Check the reference order after replace
            Assert.Equal(MetadataImageKind.Assembly, comp.References(2).Properties.Kind)
            Assert.Equal(compRef1, comp.References(2))

            ' Replace compilation Module file reference with Module file reference
            comp = comp.ReplaceReference(compRef1, modRef1)
            ' Check the reference order after replace
            Assert.Equal(3, comp.References.Count)
            Assert.Equal(MetadataImageKind.Module, comp.References(2).Properties.Kind)
            Assert.Equal(modRef1, comp.References(2))

            ' Add CS compilation ref
            Assert.Throws(Of ArgumentException)(Function() comp.AddReferences(csCompRef))

            For Each item In comp.References
                comp = comp.RemoveReferences(item)
            Next
            Assert.Equal(0, comp.References.Count)

            ' Not Implemented
            ' Dim asmByteRef = MetadataReference.CreateFromImage(New Byte(4) {}, "AssemblyBytesRef1", embedInteropTypes:=True)
            'Dim asmObjectRef = New AssemblyObjectReference(assembly:=System.Reflection.Assembly.GetAssembly(GetType(Object)), embedInteropTypes:=True)
            'comp = comp.AddReferences(asmByteRef, asmObjectRef)
            'Assert.Equal(2, comp.References.Count)
            'Assert.Equal(ReferenceKind.AssemblyBytes, comp.References(0).Kind)
            'Assert.Equal(ReferenceKind.AssemblyObject, comp.References(1).Kind)
            'Assert.Equal(asmByteRef, comp.References(0))
            'Assert.Equal(asmObjectRef, comp.References(1))
            'Assert.True(comp.References(0).EmbedInteropTypes)
            'Assert.True(comp.References(1).EmbedInteropTypes)

        End Sub

        <Fact>
        Public Sub ModuleSuppliedAsAssembly()
            Dim comp = VisualBasicCompilation.Create("Compilation", references:={AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleVB01).GetReference()})
            Assert.Equal(comp.GetDiagnostics().First().Code, ERRID.ERR_MetaDataIsNotAssembly)
        End Sub

        <Fact>
        Public Sub AssemblySuppliedAsModule()
            Dim comp = VisualBasicCompilation.Create("Compilation", references:={ModuleMetadata.CreateFromImage(Net461.Resources.System).GetReference()})
            Assert.Equal(comp.GetDiagnostics().First().Code, ERRID.ERR_MetaDataIsNotModule)
        End Sub

        '' Get nonexistent Referenced Assembly Symbol
        <WorkItem(537637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537637")>
        <Fact>
        Public Sub NegReference1()
            Dim comp = VisualBasicCompilation.Create("Compilation")

            Assert.Null(comp.GetReferencedAssemblySymbol(NetFramework.System))

            Dim modRef1 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleVB01).GetReference()
            Assert.Null(comp.GetReferencedModuleSymbol(modRef1))
        End Sub

        '' Add already existing item 
        <WorkItem(537617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537617")>
        <Fact>
        Public Sub NegReference2()
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim ref1 = NetFramework.System
            Dim ref2 = New TestMetadataReference(fullPath:="c:\a\xml.bms")
            Dim ref3 = ref2
            Dim ref4 = New TestMetadataReference(fullPath:="c:\aaa.dll")

            comp = comp.AddReferences(ref1, ref1)

            Assert.Equal(1, comp.References.Count)
            Assert.Equal(ref1, comp.References(0))

            ' Remove non-existing item 
            Assert.Throws(Of ArgumentException)(Function() comp.RemoveReferences(ref2))

            Dim listRef = New List(Of MetadataReference) From {ref1, ref2, ref3, ref4}

            comp = comp.AddReferences(listRef).AddReferences(ref2).ReplaceReference(ref2, ref2)
            Assert.Equal(3, comp.References.Count)
            comp = comp.RemoveReferences(listRef).AddReferences(ref1)
            Assert.Equal(1, comp.References.Count)
            Assert.Equal(ref1, comp.References(0))
        End Sub

        '' Add a new invalid item 
        <WorkItem(537575, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537575")>
        <Fact>
        Public Sub NegReference3()
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim ref1 = New TestMetadataReference(fullPath:="c:\xml.bms")
            Dim ref2 = NetFramework.System
            comp = comp.AddReferences(ref1)
            Assert.Equal(1, comp.References.Count)

            ' Replace a non-existing item with another invalid item
            Assert.Throws(Of ArgumentException)(Sub()
                                                    comp = comp.ReplaceReference(ref2, ref1)
                                                End Sub)

            Assert.Equal(1, comp.References.Count)
        End Sub

        '' Replace a non-existing item with null
        <WorkItem(537567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537567")>
        <Fact>
        Public Sub NegReference4()
            Dim opt = TestOptions.ReleaseExe
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim ref1 = New TestMetadataReference(fullPath:="c:\xml.bms")

            Assert.Throws(Of ArgumentException)(
               Sub()
                   comp.ReplaceReference(ref1, Nothing)
               End Sub)

            ' Replace null and the arg order of replace is vise 
            Assert.Throws(Of ArgumentNullException)(
               Sub()
                   comp.ReplaceReference(newReference:=ref1, oldReference:=Nothing)
               End Sub)
        End Sub

        '' Replace a non-existing item with another valid item
        <WorkItem(537566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537566")>
        <Fact>
        Public Sub NegReference5()
            Dim comp = VisualBasicCompilation.Create("Compilation")

            Dim ref1 = NetFramework.mscorlib
            Dim ref2 = NetFramework.System
            Assert.Throws(Of ArgumentException)(
                Sub()
                    comp = comp.ReplaceReference(ref1, ref2)
                End Sub)

            Dim s1 = "Imports System.Text"
            Dim t1 = Parse(s1)

            ' Replace a non-existing item with another valid item and disorder the args
            Assert.Throws(Of ArgumentException)(Sub()
                                                    comp.ReplaceSyntaxTree(newTree:=VisualBasicSyntaxTree.ParseText("Imports System"), oldTree:=t1)
                                                End Sub)
            Assert.Equal(0, comp.References.Count)
        End Sub

        '' Throw exception when add Nothing references
        <WorkItem(537618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537618")>
        <Fact>
        Public Sub NegReference6()
            Dim opt = TestOptions.ReleaseExe
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Assert.Throws(Of ArgumentNullException)(Sub() comp = comp.AddReferences(Nothing))
        End Sub

        '' Throw exception when remove Nothing references
        <WorkItem(537621, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537621")>
        <Fact>
        Public Sub NegReference7()
            Dim opt = TestOptions.ReleaseExe
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim RemoveNothingRefEx = Assert.Throws(Of ArgumentNullException)(Sub() comp = comp.RemoveReferences(Nothing))
        End Sub

        '' Add already existing item
        <WorkItem(537576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537576")>
        <Fact>
        Public Sub NegSyntaxTree1()
            Dim opt = TestOptions.ReleaseExe
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim t1 = VisualBasicSyntaxTree.ParseText("Using System;")
            Assert.Throws(Of ArgumentException)(Sub() comp.AddSyntaxTrees(t1, t1))
            Assert.Equal(0, comp.SyntaxTrees.Length)
        End Sub

        ' Throw exception when the parameter of ContainsSyntaxTrees is null
        <WorkItem(527256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527256")>
        <Fact>
        Public Sub NegContainsSyntaxTrees()
            Dim opt = TestOptions.ReleaseExe
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Assert.False(comp.SyntaxTrees.Contains(Nothing))
        End Sub

        ' Throw exception when the parameter of AddReferences is CSharpCompilationReference
        <WorkItem(537778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537778")>
        <Fact>
        Public Sub NegGetSymbol()
            Dim opt = TestOptions.ReleaseExe
            Dim comp = VisualBasicCompilation.Create("Compilation")

            Dim csComp = CS.CSharpCompilation.Create("CompilationCS")
            Dim compRef = csComp.ToMetadataReference()

            Assert.Throws(Of ArgumentException)(Function() comp.AddReferences(compRef))

            '' Throw exception when the parameter of GetReferencedAssemblySymbol is null
            'Assert.Throws(Of ArgumentNullException)(Sub() comp.GetReferencedAssemblySymbol(Nothing))

            '' Throw exception when the parameter of GetReferencedModuleSymbol is null
            'Assert.Throws(Of ArgumentNullException)(
            '   Sub()
            '       comp.GetReferencedModuleSymbol(Nothing)
            '   End Sub)
        End Sub

        '' Throw exception when the parameter of GetSpecialType is 'SpecialType.None' 
        <WorkItem(537784, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537784")>
        <Fact>
        Public Sub NegGetSpecialType()
            Dim comp = VisualBasicCompilation.Create("Compilation")

            Assert.Throws(Of ArgumentOutOfRangeException)(
               Sub()
                   comp.GetSpecialType((SpecialType.None))
               End Sub)

            ' Throw exception when the parameter of GetSpecialType is '0' 
            Assert.Throws(Of ArgumentOutOfRangeException)(
                Sub()
                    comp.GetSpecialType(CType(0, SpecialType))
                End Sub)
            ' Throw exception when the parameter of GetBinding is out of range
            Assert.Throws(Of ArgumentOutOfRangeException)(
               Sub()
                   comp.GetSpecialType(CType(100, SpecialType))
               End Sub)

            ' Throw exception when the parameter of GetCompilationNamespace is null
            Assert.Throws(Of ArgumentNullException)(
               Sub()
                   comp.GetCompilationNamespace(namespaceSymbol:=Nothing)
               End Sub)

            Dim bind = comp.GetSemanticModel(Nothing)
            Assert.NotNull(bind)

        End Sub

        <Fact>
        Public Sub NegSynTree()

            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim s1 = "Imports System.Text"
            Dim tree = VisualBasicSyntaxTree.ParseText(s1)
            ' Throw exception when add Nothing SyntaxTree
            Assert.Throws(Of ArgumentNullException)(Sub() comp.AddSyntaxTrees(Nothing))

            ' Throw exception when Remove Nothing SyntaxTree
            Assert.Throws(Of ArgumentNullException)(Sub() comp.RemoveSyntaxTrees(Nothing))

            ' Replace a tree with nothing (aka removing it)
            comp = comp.AddSyntaxTrees(tree).ReplaceSyntaxTree(tree, Nothing)
            Assert.Equal(0, comp.SyntaxTrees.Count)

            ' Throw exception when remove Nothing SyntaxTree
            Assert.Throws(Of ArgumentNullException)(Sub() comp = comp.ReplaceSyntaxTree(Nothing, tree))

            Dim t1 = CS.SyntaxFactory.ParseSyntaxTree(s1)
            Dim t2 As SyntaxTree = t1
            Dim t3 = t2

            Dim csComp = CS.CSharpCompilation.Create("CompilationVB")
            csComp = csComp.AddSyntaxTrees(t1, CS.SyntaxFactory.ParseSyntaxTree("Imports Goo"))
            ' Throw exception when cast SyntaxTree
            For Each item In csComp.SyntaxTrees
                t3 = item
                Dim invalidCastSynTreeEx = Assert.Throws(Of InvalidCastException)(Sub() comp = comp.AddSyntaxTrees(CType(t3, VisualBasicSyntaxTree)))
                invalidCastSynTreeEx = Assert.Throws(Of InvalidCastException)(Sub() comp = comp.RemoveSyntaxTrees(CType(t3, VisualBasicSyntaxTree)))
                invalidCastSynTreeEx = Assert.Throws(Of InvalidCastException)(Sub() comp = comp.ReplaceSyntaxTree(CType(t3, VisualBasicSyntaxTree), CType(t3, VisualBasicSyntaxTree)))
            Next
        End Sub

        <Fact>
        Public Sub RootNSIllegalIdentifiers()
            AssertTheseDiagnostics(TestOptions.ReleaseExe.WithRootNamespace("[[Global]]").Errors,
<expected>
BC2014: the value '[[Global]]' is invalid for option 'RootNamespace'
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseExe.WithRootNamespace("From()").Errors,
<expected>
BC2014: the value 'From()' is invalid for option 'RootNamespace'
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseExe.WithRootNamespace("x$").Errors,
<expected>
BC2014: the value 'x$' is invalid for option 'RootNamespace'
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseExe.WithRootNamespace("Goo.").Errors,
<expected>
BC2014: the value 'Goo.' is invalid for option 'RootNamespace'
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseExe.WithRootNamespace("_").Errors,
<expected>
BC2014: the value '_' is invalid for option 'RootNamespace'
</expected>)
        End Sub

        <Fact>
        Public Sub AmbiguousNestedTypeSymbolFromMetadata()
            Dim code = "Class A : Class B : End Class : End Class"
            Dim c1 = VisualBasicCompilation.Create("Asm1", syntaxTrees:={VisualBasicSyntaxTree.ParseText(code)})
            Dim c2 = VisualBasicCompilation.Create("Asm2", syntaxTrees:={VisualBasicSyntaxTree.ParseText(code)})
            Dim c3 = VisualBasicCompilation.Create("Asm3", references:={c1.ToMetadataReference(), c2.ToMetadataReference()})

            Assert.Null(c3.GetTypeByMetadataName("A+B"))
        End Sub

        <Fact>
        Public Sub DuplicateNestedTypeSymbol()
            Dim code = "Class A : Class B : End Class : Class B : End Class : End Class"
            Dim c1 = VisualBasicCompilation.Create("Asm1",
                syntaxTrees:={VisualBasicSyntaxTree.ParseText(code)})

            Assert.Equal("A.B", c1.GetTypeByMetadataName("A+B").ToDisplayString())
        End Sub

        <Fact()>
        <WorkItem(543211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543211")>
        Public Sub TreeDiagnosticsShouldNotIncludeEntryPointDiagnostics()
            Dim code1 = "Module M : Sub Main : End Sub : End Module"
            Dim code2 = "  "
            Dim tree1 = VisualBasicSyntaxTree.ParseText(code1)
            Dim tree2 = VisualBasicSyntaxTree.ParseText(code2)
            Dim comp = VisualBasicCompilation.Create(
                "Test",
                syntaxTrees:={tree1, tree2})
            Dim semanticModel2 = comp.GetSemanticModel(tree2)
            Dim diagnostics2 = semanticModel2.GetDiagnostics()

            Assert.Equal(0, diagnostics2.Length())
        End Sub

        <Fact()>
        <WorkItem(543292, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543292")>
        Public Sub CompilationNotSupported()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Assert.Throws(Of NotSupportedException)(Function() compilation.DynamicType)
            Assert.Throws(Of NotSupportedException)(Function() compilation.CreatePointerTypeSymbol(Nothing))
            Assert.Throws(Of NotSupportedException)(Function() compilation.CreateFunctionPointerTypeSymbol(Nothing, Nothing, Nothing, Nothing, Nothing, Nothing))
        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_NoNames()
            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType)

            Dim tupleWithoutNames = comp.CreateTupleTypeSymbol(vt2, Nothing)

            Assert.True(tupleWithoutNames.IsTupleType)
            Assert.Equal("(System.Int32, System.String)", tupleWithoutNames.ToTestDisplayString())
            Assert.True(tupleWithoutNames.TupleElements.All(Function(e) e.IsImplicitlyDeclared))
            Assert.Equal({"System.Int32", "System.String"}, tupleWithoutNames.TupleElements.Select(Function(t) t.Type.ToTestDisplayString()))
            Assert.Equal(CInt(SymbolKind.NamedType), CInt(tupleWithoutNames.Kind))
        End Sub

        <Fact>
        Public Sub CreateTupleTypeSymbol_WithNames()
            Dim comp = VisualBasicCompilation.Create("test", references:={MscorlibRef}) ' no ValueTuple
            Dim intType As TypeSymbol = comp.GetSpecialType(SpecialType.System_Int32)
            Dim stringType As TypeSymbol = comp.GetSpecialType(SpecialType.System_String)
            Dim vt2 = comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).Construct(intType, stringType)

            Dim tupleWithNames = comp.CreateTupleTypeSymbol(vt2, ImmutableArray.Create("Alice", "Bob"))

            Assert.True(tupleWithNames.IsTupleType)
            Assert.Equal("(Alice As System.Int32, Bob As System.String)", tupleWithNames.ToTestDisplayString())
            Assert.Equal({"Alice", "Bob"}, tupleWithNames.TupleElements.SelectAsArray(Function(e) e.Name))
            Assert.Equal({"System.Int32", "System.String"}, tupleWithNames.TupleElements.Select(Function(t) t.Type.ToTestDisplayString()))
            Assert.Equal(SymbolKind.NamedType, tupleWithNames.Kind)
        End Sub

        <Fact()>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateArrayType_DefaultArgs()
            Dim comp = DirectCast(VisualBasicCompilation.Create(""), Compilation)
            Dim elementType = comp.GetSpecialType(SpecialType.System_Object)

            Dim arrayType = comp.CreateArrayTypeSymbol(elementType)
            Assert.Equal(1, arrayType.Rank)
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementNullableAnnotation)

            Assert.Throws(Of ArgumentException)(Function() comp.CreateArrayTypeSymbol(elementType, Nothing))
            Assert.Throws(Of ArgumentException)(Function() comp.CreateArrayTypeSymbol(elementType, 0))

            arrayType = comp.CreateArrayTypeSymbol(elementType, 1, Nothing)
            Assert.Equal(1, arrayType.Rank)
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementNullableAnnotation)

            Assert.Throws(Of ArgumentException)(Function() comp.CreateArrayTypeSymbol(elementType, rank:=Nothing))
            Assert.Throws(Of ArgumentException)(Function() comp.CreateArrayTypeSymbol(elementType, rank:=0))

            arrayType = comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation:=Nothing)
            Assert.Equal(1, arrayType.Rank)
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, arrayType.ElementNullableAnnotation)
        End Sub

        <Fact()>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateArrayType_ElementNullableAnnotation()
            Dim comp = DirectCast(VisualBasicCompilation.Create(""), Compilation)
            Dim elementType = comp.GetSpecialType(SpecialType.System_Object)

            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType).ElementNullableAnnotation)
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation:=Nothing).ElementNullableAnnotation)
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation:=CodeAnalysis.NullableAnnotation.None).ElementNullableAnnotation)
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation:=CodeAnalysis.NullableAnnotation.NotAnnotated).ElementNullableAnnotation)
            Assert.Equal(CodeAnalysis.NullableAnnotation.None, comp.CreateArrayTypeSymbol(elementType, elementNullableAnnotation:=CodeAnalysis.NullableAnnotation.Annotated).ElementNullableAnnotation)
        End Sub

        <Fact()>
        Public Sub CreateAnonymousType_IncorrectLengths()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Assert.Throws(Of ArgumentException)(
                Function()
                    Return compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create(DirectCast(Nothing, ITypeSymbol)),
                        ImmutableArray.Create("m1", "m2"))
                End Function)
        End Sub

        <Fact>
        Public Sub CreateAnonymousType_IncorrectLengths_IsReadOnly()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Assert.Throws(Of ArgumentException)(
                Sub()
                    compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create(DirectCast(compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol),
                                          DirectCast(compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol)),
                    ImmutableArray.Create("m1", "m2"),
                    ImmutableArray.Create(True))
                End Sub)
        End Sub

        <Fact>
        Public Sub CreateAnonymousType_IncorrectLengths_Locations()
            Dim Compilation = VisualBasicCompilation.Create("HelloWorld")
            Assert.Throws(Of ArgumentException)(
                Sub()
                    Compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create(DirectCast(Compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol),
                                              DirectCast(Compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol)),
                        ImmutableArray.Create("m1", "m2"),
                        memberLocations:=ImmutableArray.Create(Location.None))
                End Sub)
        End Sub

        <Fact>
        Public Sub CreateAnonymousType_WritableProperty()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Dim type = compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create(DirectCast(compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol),
                                          DirectCast(compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol)),
                    ImmutableArray.Create("m1", "m2"),
                    ImmutableArray.Create(False, False))
            Assert.True(type.IsAnonymousType)
            Assert.Equal(2, type.GetMembers().OfType(Of IPropertySymbol).Count())
            Assert.Equal("<anonymous type: m1 As Integer, m2 As Integer>", type.ToDisplayString())
            Assert.All(type.GetMembers().OfType(Of IPropertySymbol)().Select(Function(p) p.Locations.FirstOrDefault()),
                Sub(loc) Assert.Equal(loc, Location.None))
        End Sub

        <Fact>
        Public Sub CreateAnonymousType_Locations()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Dim tree = VisualBasicSyntaxTree.ParseText("Class X")
            compilation = compilation.AddSyntaxTrees(tree)

            Dim loc1 = Location.Create(tree, New TextSpan(0, 1))
            Dim loc2 = Location.Create(tree, New TextSpan(1, 1))

            Dim type = compilation.CreateAnonymousTypeSymbol(
                    ImmutableArray.Create(DirectCast(compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol),
                                          DirectCast(compilation.GetSpecialType(SpecialType.System_Int32), ITypeSymbol)),
                    ImmutableArray.Create("m1", "m2"),
                    ImmutableArray.Create(False, False),
                    ImmutableArray.Create(loc1, loc2))
            Assert.True(type.IsAnonymousType)
            Assert.Equal(2, type.GetMembers().OfType(Of IPropertySymbol).Count())
            Assert.Equal(loc1, type.GetMembers("m1").Single().Locations.Single())
            Assert.Equal(loc2, type.GetMembers("m2").Single().Locations.Single())
            Assert.Equal("<anonymous type: m1 As Integer, m2 As Integer>", type.ToDisplayString())
        End Sub

        <Fact()>
        Public Sub CreateAnonymousType_NothingArgument()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Assert.Throws(Of ArgumentNullException)(
                Function()
                    Return compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create(DirectCast(Nothing, ITypeSymbol)),
                        ImmutableArray.Create("m1"))
                End Function)
        End Sub

        <Fact()>
        Public Sub CreateAnonymousType1()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Dim type = compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create(Of ITypeSymbol)(compilation.GetSpecialType(SpecialType.System_Int32)),
                        ImmutableArray.Create("m1"))

            Assert.True(type.IsAnonymousType)
            Assert.Equal(1, type.GetMembers().OfType(Of IPropertySymbol).Count())
            Assert.Equal("<anonymous type: Key m1 As Integer>", type.ToDisplayString())
            Assert.All(type.GetMembers().OfType(Of IPropertySymbol)().Select(Function(p) p.Locations.FirstOrDefault()),
                Sub(loc) Assert.Equal(loc, Location.None))
        End Sub

        <Fact()>
        Public Sub CreateMutableAnonymousType1()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Dim type = compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create(Of ITypeSymbol)(compilation.GetSpecialType(SpecialType.System_Int32)),
                        ImmutableArray.Create("m1"),
                        ImmutableArray.Create(False))

            Assert.True(type.IsAnonymousType)
            Assert.Equal(1, type.GetMembers().OfType(Of IPropertySymbol).Count())
            Assert.Equal("<anonymous type: m1 As Integer>", type.ToDisplayString())
            Assert.All(type.GetMembers().OfType(Of IPropertySymbol)().Select(Function(p) p.Locations.FirstOrDefault()),
                Sub(loc) Assert.Equal(loc, Location.None))
        End Sub

        <Fact()>
        Public Sub CreateAnonymousType2()
            Dim compilation = VisualBasicCompilation.Create("HelloWorld")
            Dim type = compilation.CreateAnonymousTypeSymbol(
                        ImmutableArray.Create(Of ITypeSymbol)(compilation.GetSpecialType(SpecialType.System_Int32), compilation.GetSpecialType(SpecialType.System_Boolean)),
                        ImmutableArray.Create("m1", "m2"))

            Assert.True(type.IsAnonymousType)
            Assert.Equal(2, type.GetMembers().OfType(Of IPropertySymbol).Count())
            Assert.Equal("<anonymous type: Key m1 As Integer, Key m2 As Boolean>", type.ToDisplayString())
            Assert.All(type.GetMembers().OfType(Of IPropertySymbol)().Select(Function(p) p.Locations.FirstOrDefault()),
                Sub(loc) Assert.Equal(loc, Location.None))
        End Sub

        <Fact()>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateAnonymousType_DefaultArgs()
            Dim comp = DirectCast(CreateCompilation(""), Compilation)
            Dim memberTypes = ImmutableArray.Create(Of ITypeSymbol)(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String))
            Dim memberNames = ImmutableArray.Create("P", "Q")

            Dim type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, Nothing)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, Nothing, Nothing)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, Nothing, Nothing, Nothing)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberIsReadOnly:=Nothing)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberLocations:=Nothing)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberNullableAnnotations:=Nothing)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())
        End Sub

        <Fact()>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateAnonymousType_MemberNullableAnnotations_Empty()
            Dim comp = DirectCast(VisualBasicCompilation.Create(""), Compilation)
            Dim type = comp.CreateAnonymousTypeSymbol(ImmutableArray(Of ITypeSymbol).Empty, ImmutableArray(Of String).Empty, memberNullableAnnotations:=ImmutableArray(Of CodeAnalysis.NullableAnnotation).Empty)
            Assert.Equal("<empty anonymous type>", type.ToTestDisplayString())
            AssertEx.Equal(Array.Empty(Of CodeAnalysis.NullableAnnotation)(), GetAnonymousTypeNullableAnnotations(type))
        End Sub

        <Fact()>
        <WorkItem(36047, "https://github.com/dotnet/roslyn/issues/36047")>
        Public Sub CreateAnonymousType_MemberNullableAnnotations()
            Dim comp = DirectCast(CreateCompilation(""), Compilation)
            Dim memberTypes = ImmutableArray.Create(Of ITypeSymbol)(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String))
            Dim memberNames = ImmutableArray.Create("P", "Q")

            Dim type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames)
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())
            AssertEx.Equal({CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None}, GetAnonymousTypeNullableAnnotations(type))

            Assert.Throws(Of ArgumentException)(Function() comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.NotAnnotated)))

            type = comp.CreateAnonymousTypeSymbol(memberTypes, memberNames, memberNullableAnnotations:=ImmutableArray.Create(CodeAnalysis.NullableAnnotation.NotAnnotated, CodeAnalysis.NullableAnnotation.Annotated))
            Assert.Equal("<anonymous type: Key P As System.Object, Key Q As System.String>", type.ToTestDisplayString())
            AssertEx.Equal({CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None}, GetAnonymousTypeNullableAnnotations(type))
        End Sub

        Private Shared Function GetAnonymousTypeNullableAnnotations(type As ITypeSymbol) As ImmutableArray(Of CodeAnalysis.NullableAnnotation)
            Return type.GetMembers().OfType(Of IPropertySymbol)().SelectAsArray(Function(p) p.NullableAnnotation)
        End Function

        <Theory>
        <InlineData(WellKnownMemberNames.AdditionOperatorName, "Public Shared Operator +(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.SubtractionOperatorName, "Public Shared Operator -(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.MultiplyOperatorName, "Public Shared Operator *(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.ModulusOperatorName, "Public Shared Operator Mod(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.IntegerDivisionOperatorName, "Public Shared Operator \(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.LeftShiftOperatorName, "Public Shared Operator <<(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.RightShiftOperatorName, "Public Shared Operator >>(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.ExclusiveOrOperatorName, "Public Shared Operator Xor(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.BitwiseOrOperatorName, "Public Shared Operator Or(left As Integer, right As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.BitwiseAndOperatorName, "Public Shared Operator And(left As Integer, right As Integer) As Integer")>
        Public Sub CreateBuiltinBinaryOperator_Supported(name As String, display As String)
            Dim compilation = CreateCompilation("")
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim op = compilation.CreateBuiltinOperator(name, intType, intType, intType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal(display, result)
        End Sub

        <Theory>
        <InlineData(WellKnownMemberNames.EqualityOperatorName, "Public Shared Operator =(left As Integer, right As Integer) As Boolean")>
        <InlineData(WellKnownMemberNames.InequalityOperatorName, "Public Shared Operator <>(left As Integer, right As Integer) As Boolean")>
        <InlineData(WellKnownMemberNames.LessThanOrEqualOperatorName, "Public Shared Operator <=(left As Integer, right As Integer) As Boolean")>
        <InlineData(WellKnownMemberNames.GreaterThanOrEqualOperatorName, "Public Shared Operator >=(left As Integer, right As Integer) As Boolean")>
        <InlineData(WellKnownMemberNames.LessThanOperatorName, "Public Shared Operator <(left As Integer, right As Integer) As Boolean")>
        <InlineData(WellKnownMemberNames.GreaterThanOperatorName, "Public Shared Operator >(left As Integer, right As Integer) As Boolean")>
        Public Sub CreateBuiltinBinaryOperator_Supported_Bool(name As String, display As String)
            Dim compilation = CreateCompilation("")
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim boolType = compilation.GetSpecialType(SpecialType.System_Boolean)
            Dim op = compilation.CreateBuiltinOperator(name, boolType, intType, intType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal(display, result)
        End Sub

        <Fact>
        Public Sub CreateBuiltinBinaryOperator_Supported_Concatenate()
            Dim compilation = CreateCompilation("")
            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
            Dim op = compilation.CreateBuiltinOperator(WellKnownMemberNames.ConcatenateOperatorName, stringType, stringType, stringType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal("Public Shared Operator &(left As String, right As String) As String", result)
        End Sub

        <Fact>
        Public Sub CreateBuiltinBinaryOperator_Supported_Like()
            Dim compilation = CreateCompilation("")
            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
            Dim boolType = compilation.GetSpecialType(SpecialType.System_Boolean)
            Dim op = compilation.CreateBuiltinOperator(WellKnownMemberNames.LikeOperatorName, boolType, stringType, stringType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal("Public Shared Operator Like(left As String, right As String) As Boolean", result)
        End Sub

        <Theory>
        <InlineData(WellKnownMemberNames.DivisionOperatorName, "Public Shared Operator /(left As Double, right As Double) As Double")>
        <InlineData(WellKnownMemberNames.ExponentOperatorName, "Public Shared Operator ^(left As Double, right As Double) As Double")>
        Public Sub CreateBuiltinBinaryOperator_Supported_Double(name As String, display As String)
            Dim compilation = CreateCompilation("")
            Dim doubleType = compilation.GetSpecialType(SpecialType.System_Double)
            Dim op = compilation.CreateBuiltinOperator(name, doubleType, doubleType, doubleType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal(display, result)
        End Sub

        <Fact>
        Public Sub CreateBuiltinBinaryOperator_BogusErrorType()
            Dim compilation = CreateCompilation("")
            Dim fakeIntType = compilation.CreateErrorTypeSymbol(compilation.CreateErrorNamespaceSymbol(compilation.GlobalNamespace, "System"), "Int32", arity:=0)
            Assert.Throws(Of ArgumentException)(Nothing, Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, fakeIntType, fakeIntType, fakeIntType))
        End Sub

        <Fact>
        Public Sub CreateBuiltinBinaryOperator_RealErrorType()
            Dim compilation = CreateCompilation("", references:={}, targetFramework:=TargetFramework.Empty)
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim op = compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType, intType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal("Public Shared Operator +(left As Integer, right As Integer) As Integer", result)
        End Sub

        <Fact>
        Public Sub CreateBuiltinBinaryOperator_NotSupported()
            Dim compilation = CreateCompilation("")
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim nullableIntType = compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(intType)

            ' c# binary operator name
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.IncrementOperatorName, intType, intType, intType))

            ' unary operator name
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, intType, intType, intType))

            ' nullable type 1
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, nullableIntType, intType, intType))

            ' nullable type 2
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, intType, nullableIntType, intType))

            ' nullable type 3
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, intType, intType, nullableIntType))
        End Sub

        <Fact>
        Public Sub CreateBuiltinBinaryOperator_NullChecks()
            Dim compilation = CreateCompilation("")
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Assert.Throws(Of ArgumentNullException)("returnType", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, Nothing, intType, intType))
            Assert.Throws(Of ArgumentNullException)("leftType", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, Nothing, intType))
            Assert.Throws(Of ArgumentNullException)("rightType", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType, Nothing))
        End Sub

        <Theory>
        <InlineData(WellKnownMemberNames.UnaryPlusOperatorName, "Public Shared Operator +(value As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.UnaryNegationOperatorName, "Public Shared Operator -(value As Integer) As Integer")>
        <InlineData(WellKnownMemberNames.OnesComplementOperatorName, "Public Shared Operator Not(value As Integer) As Integer")>
        Public Sub CreateBuiltinUnaryOperator_Supported(name As String, display As String)
            Dim compilation = CreateCompilation("")
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim op = compilation.CreateBuiltinOperator(name, intType, intType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal(display, result)
        End Sub

        <Fact>
        Public Sub CreateBuiltinUnaryOperator_NotSupported()
            Dim compilation = CreateCompilation("")
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim nullableIntType = compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(intType)

            ' Binary operator name
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, intType))

            ' Nullable type 1
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, nullableIntType, intType))

            ' Nullable type 2
            Assert.Throws(Of ArgumentException)("name", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.AdditionOperatorName, intType, nullableIntType))

            ' op_Implicit
            Assert.Throws(Of ArgumentException)(Nothing, Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.ImplicitConversionName, intType, intType))

            ' op_Explicit
            Assert.Throws(Of ArgumentException)(Nothing, Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.ExplicitConversionName, intType, intType))

            ' op_False
            Assert.Throws(Of ArgumentException)(Nothing, Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.FalseOperatorName, intType, intType))

            ' op_True
            Assert.Throws(Of ArgumentException)(Nothing, Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.TrueOperatorName, intType, intType))
        End Sub

        <Fact>
        Public Sub CreateBuiltinUnaryOperator_BogusErrorType()
            Dim compilation = CreateCompilation("")
            Dim fakeIntType = compilation.CreateErrorTypeSymbol(compilation.CreateErrorNamespaceSymbol(compilation.GlobalNamespace, "System"), "Int32", arity:=0)
            Assert.Throws(Of ArgumentException)(Nothing, Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, fakeIntType, fakeIntType))
        End Sub

        <Fact>
        Public Sub CreateBuiltinUnaryOperator_RealErrorType()
            Dim compilation = CreateCompilation("", references:={}, targetFramework:=TargetFramework.Empty)
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim op = compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, intType, intType)
            Dim result = op.ToDisplayString()
            AssertEx.Equal("Public Shared Operator +(value As Integer) As Integer", result)
        End Sub

        <Fact>
        Public Sub CreateBuiltinUnaryOperator_NullChecks()
            Dim compilation = CreateCompilation("")
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Assert.Throws(Of ArgumentNullException)("returnType", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, Nothing, intType))
            Assert.Throws(Of ArgumentNullException)("operandType", Function() compilation.CreateBuiltinOperator(WellKnownMemberNames.UnaryPlusOperatorName, intType, Nothing))
        End Sub

        <Fact>
        Public Sub CreateBuiltinBinaryOperator_CompareToActualSymbols1()
            Dim Compilation = CreateCompilation("
class C
    sub M()
        dim m = 1 + 1 'BIND:""1 + 1""
    end sub
end class")
            Dim intType = Compilation.GetSpecialType(SpecialType.System_Int32)

            Dim SyntaxTree = Compilation.SyntaxTrees.Single()
            Dim SemanticModel = Compilation.GetSemanticModel(SyntaxTree)

            Dim expr = FindBindingText(Of BinaryExpressionSyntax)(Compilation)
            Dim Symbol = SemanticModel.GetSymbolInfo(expr).Symbol

            Assert.NotNull(Symbol)

            Dim subtractBuiltIn = Compilation.CreateBuiltinOperator(WellKnownMemberNames.SubtractionOperatorName, intType, intType, intType)
            Dim subtractBuiltInChecked = Compilation.CreateBuiltinOperator(WellKnownMemberNames.CheckedSubtractionOperatorName, intType, intType, intType)

            Assert.NotEqual(subtractBuiltIn, Symbol)
            Assert.NotEqual(subtractBuiltInChecked, Symbol)
        End Sub

        <Fact()>
        <WorkItem(36046, "https://github.com/dotnet/roslyn/issues/36046")>
        Public Sub ConstructTypeWithNullability()
            Dim source =
"Class Pair(Of T, U)
End Class"
            Dim comp = DirectCast(CreateCompilation(source), Compilation)
            Dim genericType = DirectCast(comp.GetMember("Pair"), INamedTypeSymbol)
            Dim typeArguments = ImmutableArray.Create(Of ITypeSymbol)(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String))

            Assert.Throws(Of ArgumentException)(Function() genericType.Construct(New ImmutableArray(Of ITypeSymbol), New ImmutableArray(Of CodeAnalysis.NullableAnnotation)))
            Assert.Throws(Of ArgumentException)(Function() genericType.Construct(typeArguments:=Nothing, typeArgumentNullableAnnotations:=Nothing))

            Dim type = genericType.Construct(typeArguments, Nothing)
            Assert.Equal("Pair(Of System.Object, System.String)", type.ToTestDisplayString())
            AssertEx.Equal({CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None}, type.TypeArgumentNullableAnnotations)

            Assert.Throws(Of ArgumentException)(Function() genericType.Construct(typeArguments, ImmutableArray(Of CodeAnalysis.NullableAnnotation).Empty))
            Assert.Throws(Of ArgumentException)(Function() genericType.Construct(ImmutableArray.Create(Of ITypeSymbol)(Nothing, Nothing), Nothing))

            type = genericType.Construct(typeArguments, ImmutableArray.Create(CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated))
            Assert.Equal("Pair(Of System.Object, System.String)", type.ToTestDisplayString())
            AssertEx.Equal({CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None}, type.TypeArgumentNullableAnnotations)

            ' Type arguments from C#.
            comp = CreateCSharpCompilation("")
            typeArguments = ImmutableArray.Create(Of ITypeSymbol)(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String))
            Assert.Throws(Of ArgumentException)(Function() genericType.Construct(typeArguments, Nothing))
        End Sub

        <Fact()>
        <WorkItem(37310, "https://github.com/dotnet/roslyn/issues/37310")>
        Public Sub ConstructMethodWithNullability()
            Dim source =
"Class Program
    Shared Sub M(Of T, U)
    End Sub
End Class"
            Dim comp = DirectCast(CreateCompilation(source), Compilation)
            Dim genericMethod = DirectCast(comp.GetMember("Program.M"), IMethodSymbol)
            Dim typeArguments = ImmutableArray.Create(Of ITypeSymbol)(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String))

            Assert.Throws(Of ArgumentException)(Function() genericMethod.Construct(New ImmutableArray(Of ITypeSymbol), New ImmutableArray(Of CodeAnalysis.NullableAnnotation)))
            Assert.Throws(Of ArgumentException)(Function() genericMethod.Construct(typeArguments:=Nothing, typeArgumentNullableAnnotations:=Nothing))

            Dim type = genericMethod.Construct(typeArguments, Nothing)
            Assert.Equal("Sub Program.M(Of System.Object, System.String)()", type.ToTestDisplayString())
            AssertEx.Equal({CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None}, type.TypeArgumentNullableAnnotations)

            Assert.Throws(Of ArgumentException)(Function() genericMethod.Construct(typeArguments, ImmutableArray(Of CodeAnalysis.NullableAnnotation).Empty))
            Assert.Throws(Of ArgumentException)(Function() genericMethod.Construct(ImmutableArray.Create(Of ITypeSymbol)(Nothing, Nothing), Nothing))

            type = genericMethod.Construct(typeArguments, ImmutableArray.Create(CodeAnalysis.NullableAnnotation.Annotated, CodeAnalysis.NullableAnnotation.NotAnnotated))
            Assert.Equal("Sub Program.M(Of System.Object, System.String)()", type.ToTestDisplayString())
            AssertEx.Equal({CodeAnalysis.NullableAnnotation.None, CodeAnalysis.NullableAnnotation.None}, type.TypeArgumentNullableAnnotations)

            ' Type arguments from C#.
            comp = CreateCSharpCompilation("")
            typeArguments = ImmutableArray.Create(Of ITypeSymbol)(comp.GetSpecialType(SpecialType.System_Object), comp.GetSpecialType(SpecialType.System_String))
            Assert.Throws(Of ArgumentException)(Function() genericMethod.Construct(typeArguments, Nothing))
        End Sub

        <Fact()>
        Public Sub GetEntryPoint_Exe()
            Dim source = <compilation name="Name1">
                             <file name="a.vb"><![CDATA[
Class A
    Shared Sub Main()
    End Sub
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, OutputKind.ConsoleApplication)
            compilation.VerifyDiagnostics()

            Dim mainMethod = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("A").GetMember(Of MethodSymbol)("Main")

            Assert.Equal(mainMethod, compilation.GetEntryPoint(Nothing))

            Dim entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(Nothing)
            Assert.Equal(mainMethod, entryPointAndDiagnostics.MethodSymbol)
            entryPointAndDiagnostics.Diagnostics.Verify()
        End Sub

        <Fact()>
        Public Sub GetEntryPoint_Dll()
            Dim source = <compilation name="Name1">
                             <file name="a.vb"><![CDATA[
Class A
    Shared Sub Main()
    End Sub
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, OutputKind.DynamicallyLinkedLibrary)
            compilation.VerifyDiagnostics()

            Assert.Null(compilation.GetEntryPoint(Nothing))
            Assert.Null(compilation.GetEntryPointAndDiagnostics(Nothing))
        End Sub

        <Fact()>
        Public Sub GetEntryPoint_Module()
            Dim source = <compilation name="Name1">
                             <file name="a.vb"><![CDATA[
Class A
    Shared Sub Main()
    End Sub
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, OutputKind.NetModule)
            compilation.VerifyDiagnostics()

            Assert.Null(compilation.GetEntryPoint(Nothing))
            Assert.Null(compilation.GetEntryPointAndDiagnostics(Nothing))
        End Sub

        <Fact>
        Public Sub CreateCompilationForModule()
            Dim source =
<text>
Class A
    Shared Sub Main()
    End Sub
End Class
</text>.Value

            ' equivalent of vbc with no /moduleassemblyname specified:
            Dim c = VisualBasicCompilation.Create(assemblyName:=Nothing, options:=TestOptions.ReleaseModule, syntaxTrees:={Parse(source)}, references:={MscorlibRef})
            c.VerifyEmitDiagnostics()
            Assert.Null(c.AssemblyName)
            Assert.Equal("?", c.Assembly.Name)
            Assert.Equal("?", c.Assembly.Identity.Name)

            ' no name is allowed for assembly as well, although it isn't useful:
            c = VisualBasicCompilation.Create(assemblyName:=Nothing, options:=TestOptions.ReleaseModule, syntaxTrees:={Parse(source)}, references:={MscorlibRef})
            c.VerifyEmitDiagnostics()
            Assert.Null(c.AssemblyName)
            Assert.Equal("?", c.Assembly.Name)
            Assert.Equal("?", c.Assembly.Identity.Name)

            ' equivalent of vbc with /moduleassemblyname specified:
            c = VisualBasicCompilation.Create(assemblyName:="ModuleAssemblyName", options:=TestOptions.ReleaseModule, syntaxTrees:={Parse(source)}, references:={MscorlibRef})
            c.VerifyDiagnostics()
            Assert.Equal("ModuleAssemblyName", c.AssemblyName)
            Assert.Equal("ModuleAssemblyName", c.Assembly.Name)
            Assert.Equal("ModuleAssemblyName", c.Assembly.Identity.Name)
        End Sub

        <WorkItem(8506, "https://github.com/dotnet/roslyn/issues/8506")>
        <WorkItem(17403, "https://github.com/dotnet/roslyn/issues/17403")>
        <Fact()>
        Public Sub CrossCorlibSystemObjectReturnType_Script()
            ' MinAsyncCorlibRef corlib Is used since it provides just enough corlib type definitions
            ' And Task APIs necessary for script hosting are provided by MinAsyncRef. This ensures that
            ' `System.Object, mscorlib, Version=4.0.0.0` will Not be provided (since it's unversioned).
            '
            ' In the original bug, Xamarin iOS, Android, And Mac Mobile profile corlibs were
            ' realistic cross-compilation targets.

            Dim AssertCompilationCorlib As Action(Of VisualBasicCompilation) =
                Sub(compilation As VisualBasicCompilation)
                    Assert.True(compilation.IsSubmission)

                    Dim taskOfT = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)
                    Dim taskOfObject = taskOfT.Construct(compilation.ObjectType)
                    Dim entryPoint = compilation.GetEntryPoint(Nothing)

                    Assert.Same(compilation.ObjectType.ContainingAssembly, taskOfT.ContainingAssembly)
                    Assert.Same(compilation.ObjectType.ContainingAssembly, taskOfObject.ContainingAssembly)
                    Assert.Equal(taskOfObject, entryPoint.ReturnType)
                End Sub

            Dim firstCompilation = VisualBasicCompilation.CreateScriptCompilation(
                "submission-assembly-1",
                references:={MinAsyncCorlibRef},
                syntaxTree:=Parse("? True", options:=TestOptions.Script)
            ).VerifyDiagnostics()

            AssertCompilationCorlib(firstCompilation)

            Dim secondCompilation = VisualBasicCompilation.CreateScriptCompilation(
                "submission-assembly-2",
                previousScriptCompilation:=firstCompilation,
                syntaxTree:=Parse("? False", options:=TestOptions.Script)
            ).WithScriptCompilationInfo(New VisualBasicScriptCompilationInfo(firstCompilation, Nothing, Nothing)
            ).VerifyDiagnostics()

            AssertCompilationCorlib(secondCompilation)

            Assert.Same(firstCompilation.ObjectType, secondCompilation.ObjectType)

            Assert.Null(New VisualBasicScriptCompilationInfo(Nothing, Nothing, Nothing).WithPreviousScriptCompilation(firstCompilation).ReturnTypeOpt)
        End Sub

        <WorkItem(3719, "https://github.com/dotnet/roslyn/issues/3719")>
        <Fact()>
        Public Sub GetEntryPoint_Script()
            Dim source = <![CDATA[System.Console.WriteLine(1)]]>
            Dim compilation = CreateCompilationWithMscorlib45({VisualBasicSyntaxTree.ParseText(source.Value, options:=TestOptions.Script)}, options:=TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim scriptMethod = compilation.GetMember("Script.<Main>")
            Assert.NotNull(scriptMethod)

            Dim method = compilation.GetEntryPoint(Nothing)
            Assert.Equal(method, scriptMethod)
            Dim entryPoint = compilation.GetEntryPointAndDiagnostics(Nothing)
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod)
        End Sub

        <Fact()>
        Public Sub GetEntryPoint_Script_MainIgnored()
            Dim source = <![CDATA[
    Class A
        Shared Sub Main()
        End Sub
    End Class
    ]]>
            Dim compilation = CreateCompilationWithMscorlib45({VisualBasicSyntaxTree.ParseText(source.Value, options:=TestOptions.Script)}, options:=TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics(Diagnostic(ERRID.WRN_MainIgnored, "Main").WithArguments("Public Shared Sub Main()").WithLocation(3, 20))

            Dim scriptMethod = compilation.GetMember("Script.<Main>")
            Assert.NotNull(scriptMethod)

            Dim entryPoint = compilation.GetEntryPointAndDiagnostics(Nothing)
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod)
            entryPoint.Diagnostics.Verify(Diagnostic(ERRID.WRN_MainIgnored, "Main").WithArguments("Public Shared Sub Main()").WithLocation(3, 20))
        End Sub

        <Fact()>
        Public Sub GetEntryPoint_Submission()
            Dim source = "? 1 + 1"
            Dim compilation = VisualBasicCompilation.CreateScriptCompilation(
                "sub",
                references:={MscorlibRef},
                syntaxTree:=Parse(source, options:=TestOptions.Script))
            compilation.VerifyDiagnostics()

            Dim scriptMethod = compilation.GetMember("Script.<Factory>")
            Assert.NotNull(scriptMethod)

            Dim method = compilation.GetEntryPoint(Nothing)
            Assert.Equal(method, scriptMethod)
            Dim entryPoint = compilation.GetEntryPointAndDiagnostics(Nothing)
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod)
            entryPoint.Diagnostics.Verify()
        End Sub

        <Fact()>
        Public Sub GetEntryPoint_Submission_MainIgnored()
            Dim source = "
    Class A
        Shared Sub Main()
        End Sub
    End Class
"
            Dim compilation = VisualBasicCompilation.CreateScriptCompilation(
                "Sub",
                references:={MscorlibRef},
                syntaxTree:=Parse(source, options:=TestOptions.Script))
            compilation.VerifyDiagnostics(Diagnostic(ERRID.WRN_MainIgnored, "Main").WithArguments("Public Shared Sub Main()").WithLocation(3, 20))

            Dim scriptMethod = compilation.GetMember("Script.<Factory>")
            Assert.NotNull(scriptMethod)

            Dim entryPoint = compilation.GetEntryPointAndDiagnostics(Nothing)
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod)
            entryPoint.Diagnostics.Verify(Diagnostic(ERRID.WRN_MainIgnored, "Main").WithArguments("Public Shared Sub Main()").WithLocation(3, 20))
        End Sub

        <Fact()>
        Public Sub GetEntryPoint_MainType()
            Dim source = <compilation name="Name1">
                             <file name="a.vb"><![CDATA[
Class A
    Shared Sub Main()
    End Sub
End Class

Class B
    Shared Sub Main()
    End Sub
End Class
]]>
                             </file>
                         </compilation>
            Dim compilation = CreateCompilationWithMscorlib40(source, options:=TestOptions.ReleaseExe.WithMainTypeName("B"))
            compilation.VerifyDiagnostics()

            Dim mainMethod = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("B").GetMember(Of MethodSymbol)("Main")

            Assert.Equal(mainMethod, compilation.GetEntryPoint(Nothing))

            Dim entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(Nothing)
            Assert.Equal(mainMethod, entryPointAndDiagnostics.MethodSymbol)
            entryPointAndDiagnostics.Diagnostics.Verify()
        End Sub

        <Fact>
        Public Sub ReferenceManagerReuse_WithOptions()
            Dim c1 = VisualBasicCompilation.Create("c", options:=TestOptions.ReleaseDll)

            Dim c2 = c1.WithOptions(TestOptions.ReleaseExe)
            Assert.True(c1.ReferenceManagerEquals(c2))

            c2 = c1.WithOptions(New VisualBasicCompilationOptions(OutputKind.WindowsApplication))
            Assert.True(c1.ReferenceManagerEquals(c2))

            c2 = c1.WithOptions(TestOptions.ReleaseModule)
            Assert.False(c1.ReferenceManagerEquals(c2))

            c1 = VisualBasicCompilation.Create("c", options:=TestOptions.ReleaseModule)

            c2 = c1.WithOptions(TestOptions.ReleaseExe)
            Assert.False(c1.ReferenceManagerEquals(c2))

            c2 = c1.WithOptions(TestOptions.ReleaseDll)
            Assert.False(c1.ReferenceManagerEquals(c2))

            c2 = c1.WithOptions(New VisualBasicCompilationOptions(OutputKind.WindowsApplication))
            Assert.False(c1.ReferenceManagerEquals(c2))
        End Sub

        <Fact>
        Public Sub ReferenceManagerReuse_WithXmlFileResolver()
            Dim c1 = VisualBasicCompilation.Create("c", options:=TestOptions.ReleaseDll)

            Dim c2 = c1.WithOptions(TestOptions.ReleaseDll.WithXmlReferenceResolver(New XmlFileResolver(Nothing)))
            Assert.False(c1.ReferenceManagerEquals(c2))

            Dim c3 = c1.WithOptions(TestOptions.ReleaseDll.WithXmlReferenceResolver(c1.Options.XmlReferenceResolver))
            Assert.True(c1.ReferenceManagerEquals(c3))
        End Sub

        <Fact>
        Public Sub ReferenceManagerReuse_WithMetadataReferenceResolver()
            Dim c1 = VisualBasicCompilation.Create("c", options:=TestOptions.ReleaseDll)

            Dim c2 = c1.WithOptions(TestOptions.ReleaseDll.WithMetadataReferenceResolver(New TestMetadataReferenceResolver()))
            Assert.False(c1.ReferenceManagerEquals(c2))

            Dim c3 = c1.WithOptions(TestOptions.ReleaseDll.WithMetadataReferenceResolver(c1.Options.MetadataReferenceResolver))
            Assert.True(c1.ReferenceManagerEquals(c3))
        End Sub

        <Fact>
        Public Sub ReferenceManagerReuse_WithName()
            Dim c1 = VisualBasicCompilation.Create("c1")

            Dim c2 = c1.WithAssemblyName("c2")
            Assert.False(c1.ReferenceManagerEquals(c2))

            Dim c3 = c1.WithAssemblyName("c1")
            Assert.True(c1.ReferenceManagerEquals(c3))

            Dim c4 = c1.WithAssemblyName(Nothing)
            Assert.False(c1.ReferenceManagerEquals(c4))

            Dim c5 = c4.WithAssemblyName(Nothing)
            Assert.True(c4.ReferenceManagerEquals(c5))
        End Sub

        <Fact>
        Public Sub ReferenceManagerReuse_WithReferences()
            Dim c1 = VisualBasicCompilation.Create("c1")

            Dim c2 = c1.WithReferences({MscorlibRef})
            Assert.False(c1.ReferenceManagerEquals(c2))

            Dim c3 = c2.WithReferences({MscorlibRef, SystemCoreRef})
            Assert.False(c3.ReferenceManagerEquals(c2))

            c3 = c2.AddReferences(SystemCoreRef)
            Assert.False(c3.ReferenceManagerEquals(c2))

            c3 = c2.RemoveAllReferences()
            Assert.False(c3.ReferenceManagerEquals(c2))

            c3 = c2.ReplaceReference(MscorlibRef, SystemCoreRef)
            Assert.False(c3.ReferenceManagerEquals(c2))

            c3 = c2.RemoveReferences(MscorlibRef)
            Assert.False(c3.ReferenceManagerEquals(c2))
        End Sub

        <Fact>
        Public Sub ReferenceManagerReuse_WithSyntaxTrees()
            Dim ta = Parse("Imports System")
            Dim tb = Parse("Imports System", options:=TestOptions.Script)
            Dim tc = Parse("#r ""bar""  ' error: #r in regular code")
            Dim tr = Parse("#r ""goo""", options:=TestOptions.Script)
            Dim ts = Parse("#r ""bar""", options:=TestOptions.Script)

            Dim a = VisualBasicCompilation.Create("c", syntaxTrees:={ta})

            ' add:

            Dim ab = a.AddSyntaxTrees(tb)
            Assert.True(a.ReferenceManagerEquals(ab))

            Dim ac = a.AddSyntaxTrees(tc)
            Assert.True(a.ReferenceManagerEquals(ac))

            Dim ar = a.AddSyntaxTrees(tr)
            Assert.False(a.ReferenceManagerEquals(ar))

            Dim arc = ar.AddSyntaxTrees(tc)
            Assert.True(ar.ReferenceManagerEquals(arc))

            ' remove:

            Dim ar2 = arc.RemoveSyntaxTrees(tc)
            Assert.True(arc.ReferenceManagerEquals(ar2))

            Dim c = arc.RemoveSyntaxTrees(ta, tr)
            Assert.False(arc.ReferenceManagerEquals(c))

            Dim none1 = c.RemoveSyntaxTrees(tc)
            Assert.True(c.ReferenceManagerEquals(none1))

            Dim none2 = arc.RemoveAllSyntaxTrees()
            Assert.False(arc.ReferenceManagerEquals(none2))

            Dim none3 = ac.RemoveAllSyntaxTrees()
            Assert.True(ac.ReferenceManagerEquals(none3))

            ' replace:

            Dim asc = arc.ReplaceSyntaxTree(tr, ts)
            Assert.False(arc.ReferenceManagerEquals(asc))

            Dim brc = arc.ReplaceSyntaxTree(ta, tb)
            Assert.True(arc.ReferenceManagerEquals(brc))

            Dim abc = arc.ReplaceSyntaxTree(tr, tb)
            Assert.False(arc.ReferenceManagerEquals(abc))

            Dim ars = arc.ReplaceSyntaxTree(tc, ts)
            Assert.False(arc.ReferenceManagerEquals(ars))
        End Sub

        <Fact>
        Public Sub ReferenceManagerReuse_WithScriptCompilationInfo()
            ' Note The following results would change if we optimized sharing more: https://github.com/dotnet/roslyn/issues/43397

            Dim c1 = VisualBasicCompilation.CreateScriptCompilation("c1")
            Assert.NotNull(c1.ScriptCompilationInfo)
            Assert.Null(c1.ScriptCompilationInfo.PreviousScriptCompilation)

            Dim c2 = c1.WithScriptCompilationInfo(Nothing)
            Assert.Null(c2.ScriptCompilationInfo)
            Assert.True(c2.ReferenceManagerEquals(c1))

            Dim c3 = c2.WithScriptCompilationInfo(New VisualBasicScriptCompilationInfo(previousCompilationOpt:=Nothing, returnType:=GetType(Integer), globalsType:=Nothing))
            Assert.NotNull(c3.ScriptCompilationInfo)
            Assert.Null(c3.ScriptCompilationInfo.PreviousScriptCompilation)
            Assert.True(c3.ReferenceManagerEquals(c2))

            Dim c4 = c3.WithScriptCompilationInfo(Nothing)
            Assert.Null(c4.ScriptCompilationInfo)
            Assert.True(c4.ReferenceManagerEquals(c3))

            Dim c5 = c4.WithScriptCompilationInfo(New VisualBasicScriptCompilationInfo(previousCompilationOpt:=c1, returnType:=GetType(Integer), globalsType:=Nothing))
            Assert.False(c5.ReferenceManagerEquals(c4))

            Dim c6 = c5.WithScriptCompilationInfo(New VisualBasicScriptCompilationInfo(previousCompilationOpt:=c1, returnType:=GetType(Boolean), globalsType:=Nothing))
            Assert.True(c6.ReferenceManagerEquals(c5))

            Dim c7 = c6.WithScriptCompilationInfo(New VisualBasicScriptCompilationInfo(previousCompilationOpt:=c2, returnType:=GetType(Boolean), globalsType:=Nothing))
            Assert.False(c7.ReferenceManagerEquals(c6))

            Dim c8 = c7.WithScriptCompilationInfo(New VisualBasicScriptCompilationInfo(previousCompilationOpt:=Nothing, returnType:=GetType(Boolean), globalsType:=Nothing))
            Assert.False(c8.ReferenceManagerEquals(c7))

            Dim c9 = c8.WithScriptCompilationInfo(Nothing)
            Assert.True(c9.ReferenceManagerEquals(c8))
        End Sub

        Private Class EvolvingTestReference
            Inherits PortableExecutableReference

            Private ReadOnly _metadataSequence As IEnumerator(Of Metadata)

            Public QueryCount As Integer

            Public Sub New(metadataSequence As IEnumerable(Of Metadata))
                MyBase.New(MetadataReferenceProperties.Assembly)
                Me._metadataSequence = metadataSequence.GetEnumerator()
            End Sub

            Protected Overrides Function CreateDocumentationProvider() As DocumentationProvider
                Return DocumentationProvider.Default
            End Function

            Protected Overrides Function GetMetadataImpl() As Metadata
                QueryCount = QueryCount + 1
                _metadataSequence.MoveNext()
                Return _metadataSequence.Current
            End Function

            Protected Overrides Function WithPropertiesImpl(properties As MetadataReferenceProperties) As PortableExecutableReference
                Throw New NotImplementedException()
            End Function
        End Class

        <ConditionalFact(GetType(NoIOperationValidation), GetType(NoUsedAssembliesValidation))>
        Public Sub MetadataConsistencyWhileEvolvingCompilation()
            Dim md1 = AssemblyMetadata.CreateFromImage(CreateCompilationWithMscorlib40({"Public Class C : End Class"}, options:=TestOptions.ReleaseDll).EmitToArray())
            Dim md2 = AssemblyMetadata.CreateFromImage(CreateCompilationWithMscorlib40({"Public Class D : End Class"}, options:=TestOptions.ReleaseDll).EmitToArray())
            Dim reference = New EvolvingTestReference({md1, md2})

            Dim references = TargetFrameworkUtil.Mscorlib40References.AddRange({reference, reference})
            Dim c1 = CreateEmptyCompilation({"Public Class Main : Public Shared C As C : End Class"}, references:=references, options:=TestOptions.ReleaseDll)
            Dim c2 = c1.WithAssemblyName("c2")
            Dim c3 = c2.AddSyntaxTrees(Parse("Public Class Main2 : Public Shared A As Integer : End Class"))
            Dim c4 = c3.WithOptions(New VisualBasicCompilationOptions(OutputKind.NetModule))
            Dim c5 = c4.WithReferences({MscorlibRef, reference})

            c3.VerifyDiagnostics()
            c1.VerifyDiagnostics()
            c4.VerifyDiagnostics()
            c2.VerifyDiagnostics()
            Assert.Equal(1, reference.QueryCount)

            c5.VerifyDiagnostics(Diagnostic(ERRID.ERR_UndefinedType1, "C").WithArguments("C").WithLocation(1, 40))

            Assert.Equal(2, reference.QueryCount)
        End Sub

        <Fact>
        Public Sub LinkedNetmoduleMetadataMustProvideFullPEImage()
            Dim moduleBytes = TestResources.MetadataTests.NetModule01.ModuleCS00
            Dim headers = New PEHeaders(New MemoryStream(moduleBytes))

            Dim pinnedPEImage = GCHandle.Alloc(moduleBytes.ToArray(), GCHandleType.Pinned)
            Try
                Using mdModule = ModuleMetadata.CreateFromMetadata(pinnedPEImage.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize)
                    Dim c = VisualBasicCompilation.Create("Goo", references:={MscorlibRef, mdModule.GetReference(display:="ModuleCS00")}, options:=TestOptions.ReleaseDll)
                    c.VerifyDiagnostics(Diagnostic(ERRID.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage).WithArguments("ModuleCS00").WithLocation(1, 1))
                End Using
            Finally
                pinnedPEImage.Free()
            End Try
        End Sub

        <Fact>
        <WorkItem(797640, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797640")>
        Public Sub GetMetadataReferenceAPITest()
            Dim comp = VisualBasicCompilation.Create("Compilation")
            Dim metadata = NetFramework.mscorlib
            comp = comp.AddReferences(metadata)
            Dim assemblySmb = comp.GetReferencedAssemblySymbol(metadata)
            Dim reference = comp.GetMetadataReference(assemblySmb)
            Assert.NotNull(reference)

            Dim comp2 = VisualBasicCompilation.Create("Compilation")
            comp2 = comp.AddReferences(metadata)
            Dim reference2 = comp2.GetMetadataReference(assemblySmb)
            Assert.NotNull(reference2)
        End Sub

        <WorkItem(40466, "https://github.com/dotnet/roslyn/issues/40466")>
        <Fact>
        Public Sub GetMetadataReference_CSharpSymbols()
            Dim comp As Compilation = CreateCompilation("")

            Dim csComp = CreateCSharpCompilation("", referencedAssemblies:=TargetFrameworkUtil.GetReferences(TargetFramework.Standard))
            Dim assembly = csComp.GetBoundReferenceManager().GetReferencedAssemblies().First().Value

            Assert.Null(comp.GetMetadataReference(DirectCast(assembly.GetISymbol(), IAssemblySymbol)))
            Assert.Null(comp.GetMetadataReference(csComp.Assembly))
            Assert.Null(comp.GetMetadataReference(Nothing))
        End Sub

        <Fact()>
        Public Sub EqualityOfMergedNamespaces()
            Dim moduleComp = CompilationUtils.CreateEmptyCompilation(
<compilation>
    <file name="a.vb">
Namespace NS1
    Namespace NS3
        Interface T1
        End Interface
    End Namespace
End Namespace

Namespace NS2
    Namespace NS3
        Interface T2
        End Interface
    End Namespace
End Namespace
    </file>
</compilation>, options:=TestOptions.ReleaseModule)

            Dim compilation = CompilationUtils.CreateEmptyCompilationWithReferences(
<compilation>
    <file name="a.vb">
Namespace NS1
    Namespace NS3
        Interface T3
        End Interface
    End Namespace
End Namespace

Namespace NS2
    Namespace NS3
        Interface T4
        End Interface
    End Namespace
End Namespace
    </file>
</compilation>, {moduleComp.EmitToImageReference()})

            TestEqualityRecursive(compilation.GlobalNamespace,
                                  compilation.GlobalNamespace,
                                  NamespaceKind.Compilation,
                                  Function(ns) compilation.GetCompilationNamespace(ns))

            TestEqualityRecursive(compilation.Assembly.GlobalNamespace,
                                  compilation.Assembly.GlobalNamespace,
                                  NamespaceKind.Assembly,
                                  Function(ns) compilation.Assembly.GetAssemblyNamespace(ns))
        End Sub

        Private Shared Sub TestEqualityRecursive(testNs1 As NamespaceSymbol,
                                                 testNs2 As NamespaceSymbol,
                                                 kind As NamespaceKind,
                                                 factory As Func(Of NamespaceSymbol, NamespaceSymbol))
            Assert.Equal(kind, testNs1.NamespaceKind)
            Assert.Same(testNs1, testNs2)

            Dim children1 = testNs1.GetMembers().OrderBy(Function(m) m.Name).ToArray()
            Dim children2 = testNs2.GetMembers().OrderBy(Function(m) m.Name).ToArray()

            For i = 0 To children1.Count - 1
                Assert.Same(children1(i), children2(i))

                If children1(i).Kind = SymbolKind.Namespace Then
                    TestEqualityRecursive(DirectCast(testNs1.GetMembers(children1(i).Name).Single(), NamespaceSymbol),
                                      DirectCast(testNs1.GetMembers(children1(i).Name).Single(), NamespaceSymbol),
                                      kind,
                                      factory)
                End If
            Next

            Assert.Same(testNs1, factory(testNs1))

            For Each constituent In testNs1.ConstituentNamespaces
                Assert.Same(testNs1, factory(constituent))
            Next
        End Sub

        <Fact>
        Public Sub ConsistentParseOptions()
            Dim tree1 = SyntaxFactory.ParseSyntaxTree("", VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))
            Dim tree2 = SyntaxFactory.ParseSyntaxTree("", VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12))
            Dim tree3 = SyntaxFactory.ParseSyntaxTree("", VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic11))

            Dim assemblyName = GetUniqueName()
            Dim CompilationOptions = New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            VisualBasicCompilation.Create(assemblyName, {tree1, tree2}, {MscorlibRef}, CompilationOptions)
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.Create(assemblyName, {tree1, tree3}, {MscorlibRef}, CompilationOptions))
        End Sub

        <Fact>
        Public Sub SubmissionCompilation_Errors()
            Dim genericParameter = GetType(List(Of)).GetGenericArguments()(0)
            Dim open = GetType(Dictionary(Of,)).MakeGenericType(GetType(Integer), genericParameter)
            Dim ptr = GetType(Integer).MakePointerType()
            Dim byRefType = GetType(Integer).MakeByRefType()

            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", returnType:=genericParameter))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", returnType:=open))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", returnType:=GetType(Void)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", returnType:=byRefType))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", globalsType:=genericParameter))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", globalsType:=open))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", globalsType:=GetType(Void)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", globalsType:=GetType(Integer)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", globalsType:=ptr))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", globalsType:=byRefType))

            Dim s0 = VisualBasicCompilation.CreateScriptCompilation("a0", globalsType:=GetType(List(Of Integer)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a1", previousScriptCompilation:=s0, globalsType:=GetType(List(Of Boolean))))

            ' invalid options
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseExe))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeApplication)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsApplication)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithCryptoKeyContainer("goo")))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithCryptoKeyFile("goo.snk")))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithDelaySign(True)))
            Assert.Throws(Of ArgumentException)(Function() VisualBasicCompilation.CreateScriptCompilation("a", options:=TestOptions.ReleaseDll.WithDelaySign(False)))
        End Sub

        <Fact>
        Public Sub HasSubmissionResult()
            Assert.False(VisualBasicCompilation.CreateScriptCompilation("sub").HasSubmissionResult())

            Assert.True(CreateSubmission("?1", parseOptions:=TestOptions.Script).HasSubmissionResult())

            Assert.False(CreateSubmission("1", parseOptions:=TestOptions.Script).HasSubmissionResult())
            ' TODO (https://github.com/dotnet/roslyn/issues/4763): '?' should be optional
            ' TestSubmissionResult(CreateSubmission("1", parseOptions:=TestOptions.Interactive), expectedType:=SpecialType.System_Int32, expectedHasValue:=True)

            ' TODO (https://github.com/dotnet/roslyn/issues/4766): ReturnType should not be ignored
            ' TestSubmissionResult(CreateSubmission("?1", parseOptions:=TestOptions.Interactive, returnType:=GetType(Double)), expectedType:=SpecialType.System_Double, expectedHasValue:=True)

            Assert.False(CreateSubmission("
Sub Goo() 
End Sub
").HasSubmissionResult())

            Assert.False(CreateSubmission("Imports System", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.False(CreateSubmission("Dim i As Integer", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.False(CreateSubmission("System.Console.WriteLine()", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.True(CreateSubmission("?System.Console.WriteLine()", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.True(CreateSubmission("System.Console.ReadLine()", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.True(CreateSubmission("?System.Console.ReadLine()", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.True(CreateSubmission("?Nothing", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.True(CreateSubmission("?AddressOf System.Console.WriteLine", parseOptions:=TestOptions.Script).HasSubmissionResult())
            Assert.True(CreateSubmission("?Function(x) x", parseOptions:=TestOptions.Script).HasSubmissionResult())
        End Sub

        ''' <summary>
        ''' Previous submission has to have no errors.
        ''' </summary>
        <Fact>
        Public Sub PreviousSubmissionWithError()
            Dim s0 = CreateSubmission("Dim a As X = 1")

            s0.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_UndefinedType1, "X").WithArguments("X"))

            Assert.Throws(Of InvalidOperationException)(Function() CreateSubmission("?a + 1", previous:=s0))
        End Sub

        <Fact>
        <WorkItem(13925, "https://github.com/dotnet/roslyn/issues/13925")>
        Public Sub RemoveAllSyntaxTreesAndEmbeddedTrees_01()
            Dim compilation1 = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Public Module C  
    Sub Main()
        System.Console.WriteLine(1)
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(True), references:={SystemRef})

            compilation1.VerifyDiagnostics()

            Dim compilation2 = compilation1.RemoveAllSyntaxTrees()
            compilation2 = compilation2.AddSyntaxTrees(compilation1.SyntaxTrees)

            compilation2.VerifyDiagnostics()
            CompileAndVerify(compilation2, expectedOutput:="1")
        End Sub

        <Fact>
        <WorkItem(13925, "https://github.com/dotnet/roslyn/issues/13925")>
        Public Sub RemoveAllSyntaxTreesAndEmbeddedTrees_02()
            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Module C  
    Sub Main()
        System.Console.WriteLine(1)
    End Sub
End Module
    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(True))

            compilation1.VerifyDiagnostics()

            Dim compilation2 = compilation1.RemoveAllSyntaxTrees()
            compilation2 = compilation2.AddSyntaxTrees(compilation1.SyntaxTrees)

            compilation2.VerifyDiagnostics()
            CompileAndVerify(compilation2, expectedOutput:="1")
        End Sub

        <Fact, WorkItem(50696, "https://github.com/dotnet/roslyn/issues/50696")>
        Public Sub GetWellKnownType()

            Dim corlib = "
Namespace System
    Public Class [Object]
    End Class
    Public Class ValueType
    End Class
    Public Structure Void
    End Structure
End Namespace
"

            Dim tuple = "
Namespace System
    Public Structure ValueTuple(Of T1, T2)
        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub
    End Structure
End Namespace
"

            Dim corlibWithoutValueTupleRef = CreateEmptyCompilation(corlib, assemblyName:="corlibWithoutValueTupleRef").EmitToImageReference()
            Dim corlibWithValueTupleRef = CreateEmptyCompilation(corlib + tuple, assemblyName:="corlibWithValueTupleRef").EmitToImageReference()

            Dim libWithIsExternalInitRef = CreateEmptyCompilation(tuple, references:={corlibWithoutValueTupleRef}, assemblyName:="libWithIsExternalInit").EmitToImageReference()
            Dim libWithIsExternalInitRef2 = CreateEmptyCompilation(tuple, references:={corlibWithoutValueTupleRef}, assemblyName:="libWithIsExternalInit2").EmitToImageReference()

            If True Then
                ' type in source
                Dim comp = CreateEmptyCompilation(tuple, references:={corlibWithoutValueTupleRef}, assemblyName:="source")
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "source")
            End If

            If True Then
                ' type in library
                Dim comp = CreateEmptyCompilation("", references:={corlibWithoutValueTupleRef, libWithIsExternalInitRef}, assemblyName:="source")
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "libWithIsExternalInit")
            End If

            If True Then
                ' type in corlib and in source
                Dim comp = CreateEmptyCompilation(tuple, references:={corlibWithValueTupleRef}, assemblyName:="source")
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "source")
            End If

            If True Then
                ' type in corlib, in library and in source
                Dim comp = CreateEmptyCompilation(tuple, references:={corlibWithValueTupleRef, libWithIsExternalInitRef}, assemblyName:="source")
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "source")
            End If

            If True Then
                ' type in corlib and in two libraries
                Dim comp = CreateEmptyCompilation("", references:={corlibWithValueTupleRef, libWithIsExternalInitRef, libWithIsExternalInitRef2})
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "corlibWithValueTupleRef")
            End If

            If True Then
                ' type in corlib and in two libraries (corlib in middle)
                Dim comp = CreateEmptyCompilation("", references:={libWithIsExternalInitRef, corlibWithValueTupleRef, libWithIsExternalInitRef2})
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "corlibWithValueTupleRef")
            End If

            If True Then
                ' type in corlib and in two libraries (corlib last)
                Dim comp = CreateEmptyCompilation("", references:={libWithIsExternalInitRef, libWithIsExternalInitRef2, corlibWithValueTupleRef})
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "corlibWithValueTupleRef")
            End If

            If True Then
                ' type in corlib and in two libraries, but flag is set
                Dim comp = CreateEmptyCompilation("", references:={corlibWithValueTupleRef, libWithIsExternalInitRef, libWithIsExternalInitRef2},
                    options:=TestOptions.DebugDll.WithIgnoreCorLibraryDuplicatedTypes(True))
                AssertNoDeclarationDiagnostics(comp)
                Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).IsErrorType)
            End If

            If True Then
                ' type in two libraries
                Dim comp = CreateEmptyCompilation("", references:={libWithIsExternalInitRef, libWithIsExternalInitRef2})
                AssertNoDeclarationDiagnostics(comp)
                Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).IsErrorType)
            End If

            If True Then
                ' type in two libraries, but flag is set
                Dim comp = CreateEmptyCompilation("", references:={libWithIsExternalInitRef, libWithIsExternalInitRef2},
                    options:=TestOptions.DebugDll.WithIgnoreCorLibraryDuplicatedTypes(True))
                AssertNoDeclarationDiagnostics(comp)
                Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).IsErrorType)
            End If

            If True Then
                ' type in corlib and in a library
                Dim comp = CreateEmptyCompilation("", references:={corlibWithValueTupleRef, libWithIsExternalInitRef})
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "corlibWithValueTupleRef")
            End If

            If True Then
                ' type in corlib and in a library (reverse order)
                Dim comp = CreateEmptyCompilation("", references:={corlibWithValueTupleRef, libWithIsExternalInitRef})
                AssertNoDeclarationDiagnostics(comp)
                GetWellKnownType_Verify(comp, "corlibWithValueTupleRef")
            End If

            If True Then
                ' type in corlib and in a library, but flag is set
                Dim comp = CreateEmptyCompilation("", references:={corlibWithValueTupleRef, libWithIsExternalInitRef},
                    options:=TestOptions.DebugDll.WithIgnoreCorLibraryDuplicatedTypes(True))
                AssertNoDeclarationDiagnostics(comp)
                Assert.Equal("libWithIsExternalInit", comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).ContainingAssembly.Name)
                Assert.Equal("corlibWithValueTupleRef", comp.GetTypeByMetadataName("System.ValueTuple`2").ContainingAssembly.Name)
            End If
        End Sub

        Private Shared Sub GetWellKnownType_Verify(comp As VisualBasicCompilation, expectedAssemblyName As String)
            Assert.Equal(expectedAssemblyName, comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).ContainingAssembly.Name)
            Assert.Equal(expectedAssemblyName, comp.GetTypeByMetadataName("System.ValueTuple`2").ContainingAssembly.Name)
        End Sub

    End Class
End Namespace
