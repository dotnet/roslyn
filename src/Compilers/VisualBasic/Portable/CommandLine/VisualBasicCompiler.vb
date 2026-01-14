' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend MustInherit Class VisualBasicCompiler
        Inherits CommonCompiler

        Friend Const ResponseFileName As String = "vbc.rsp"
        Friend Const VbcCommandLinePrefix = "vbc : " 'Common prefix String For VB diagnostic output with no location.
        Private ReadOnly _diagnosticFormatter As CommandLineDiagnosticFormatter
        Private ReadOnly _tempDirectory As String
        Private _additionalTextFiles As ImmutableArray(Of AdditionalTextFile)

        Protected Sub New(parser As VisualBasicCommandLineParser, responseFile As String, args As String(), buildPaths As BuildPaths, additionalReferenceDirectories As String, analyzerLoader As IAnalyzerAssemblyLoader, Optional driverCache As GeneratorDriverCache = Nothing, Optional fileSystem As ICommonCompilerFileSystem = Nothing)
            MyBase.New(parser, responseFile, args, buildPaths, additionalReferenceDirectories, analyzerLoader, driverCache, fileSystem)

            _diagnosticFormatter = New CommandLineDiagnosticFormatter(buildPaths.WorkingDirectory, AddressOf GetAdditionalTextFiles)
            _additionalTextFiles = Nothing
            _tempDirectory = buildPaths.TempDirectory

            Debug.Assert(Arguments.OutputFileName IsNot Nothing OrElse Arguments.Errors.Length > 0 OrElse parser.IsScriptCommandLineParser)
        End Sub

        Private Function GetAdditionalTextFiles() As ImmutableArray(Of AdditionalTextFile)
            Debug.Assert(Not _additionalTextFiles.IsDefault, "GetAdditionalTextFiles called before ResolveAdditionalFilesFromArguments")
            Return _additionalTextFiles
        End Function

        Protected Overrides Function ResolveAdditionalFilesFromArguments(diagnostics As List(Of DiagnosticInfo), messageProvider As CommonMessageProvider, touchedFilesLogger As TouchedFileLogger) As ImmutableArray(Of AdditionalTextFile)
            _additionalTextFiles = MyBase.ResolveAdditionalFilesFromArguments(diagnostics, messageProvider, touchedFilesLogger)
            Return _additionalTextFiles
        End Function

        Friend Overloads ReadOnly Property Arguments As VisualBasicCommandLineArguments
            Get
                Return DirectCast(MyBase.Arguments, VisualBasicCommandLineArguments)
            End Get
        End Property

        Public Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return _diagnosticFormatter
            End Get
        End Property

        Private Function ParseFile(consoleOutput As TextWriter,
                                   parseOptions As VisualBasicParseOptions,
                                   scriptParseOptions As VisualBasicParseOptions,
                                   ByRef hadErrors As Boolean,
                                   file As CommandLineSourceFile,
                                   errorLogger As ErrorLogger) As SyntaxTree

            Dim fileReadDiagnostics As New List(Of DiagnosticInfo)()
            Dim content = TryReadFileContent(file, fileReadDiagnostics)

            If content Is Nothing Then
                ReportDiagnostics(fileReadDiagnostics, consoleOutput, errorLogger, compilation:=Nothing)
                fileReadDiagnostics.Clear()
                hadErrors = True
                Return Nothing
            End If

            Dim tree = VisualBasicSyntaxTree.ParseText(
                content,
                If(file.IsScript, scriptParseOptions, parseOptions),
                file.Path)

            ' prepopulate line tables.
            ' we will need line tables anyways and it is better to Not wait until we are in emit
            ' where things run sequentially.
            Dim isHiddenDummy As Boolean
            tree.GetMappedLineSpanAndVisibility(Nothing, isHiddenDummy)

            Return tree
        End Function

        Public Overrides Function CreateCompilation(consoleOutput As TextWriter,
                                                    touchedFilesLogger As TouchedFileLogger,
                                                    errorLogger As ErrorLogger,
                                                    analyzerConfigOptions As ImmutableArray(Of AnalyzerConfigOptionsResult),
                                                    globalAnalyzerConfigOptions As AnalyzerConfigOptionsResult) As Compilation
            Dim parseOptions = Arguments.ParseOptions

            ' We compute script parse options once so we don't have to do it repeatedly in
            ' case there are many script files.
            Dim scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script)

            Dim hadErrors As Boolean = False

            Dim sourceFiles As ImmutableArray(Of CommandLineSourceFile) = Arguments.SourceFiles
            Dim trees(sourceFiles.Length - 1) As SyntaxTree

            If Arguments.CompilationOptions.ConcurrentBuild Then
                RoslynParallel.For(
                    0,
                    sourceFiles.Length,
                    UICultureUtilities.WithCurrentUICulture(Of Integer)(
                        Sub(i As Integer)
                            ' NOTE: order of trees is important!!
                            trees(i) = ParseFile(
                                consoleOutput,
                                parseOptions,
                                scriptParseOptions,
                                hadErrors,
                                sourceFiles(i),
                                errorLogger)
                        End Sub),
                    CancellationToken.None)
            Else
                For i = 0 To sourceFiles.Length - 1
                    ' NOTE: order of trees is important!!
                    trees(i) = ParseFile(
                        consoleOutput,
                        parseOptions,
                        scriptParseOptions,
                        hadErrors,
                        sourceFiles(i),
                        errorLogger)
                Next
            End If

            If hadErrors Then
                Return Nothing
            End If

            If Arguments.TouchedFilesPath IsNot Nothing Then
                For Each file In sourceFiles
                    touchedFilesLogger.AddRead(file.Path)
                Next
            End If

            Dim diagnostics = New List(Of DiagnosticInfo)()

            Dim assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default

            Dim referenceDirectiveResolver As MetadataReferenceResolver = Nothing
            Dim resolvedReferences = ResolveMetadataReferences(diagnostics, touchedFilesLogger, referenceDirectiveResolver)

            If ReportDiagnostics(diagnostics, consoleOutput, errorLogger, compilation:=Nothing) Then
                Return Nothing
            End If

            If Arguments.OutputLevel = OutputLevel.Verbose Then
                PrintReferences(resolvedReferences, consoleOutput)
            End If

            Dim xmlFileResolver = New LoggingXmlFileResolver(Arguments.BaseDirectory, touchedFilesLogger)

            ' TODO: support for #load search paths
            Dim sourceFileResolver = New LoggingSourceFileResolver(ImmutableArray(Of String).Empty, Arguments.BaseDirectory, Arguments.PathMap, touchedFilesLogger)

            Dim loggingFileSystem = New LoggingStrongNameFileSystem(touchedFilesLogger, _tempDirectory)
            Dim syntaxTreeOptions = New CompilerSyntaxTreeOptionsProvider(trees, analyzerConfigOptions, globalAnalyzerConfigOptions)

            Return VisualBasicCompilation.Create(
                 Arguments.CompilationName,
                 trees,
                 resolvedReferences,
                 Arguments.CompilationOptions.
                     WithMetadataReferenceResolver(referenceDirectiveResolver).
                     WithAssemblyIdentityComparer(assemblyIdentityComparer).
                     WithXmlReferenceResolver(xmlFileResolver).
                     WithStrongNameProvider(Arguments.GetStrongNameProvider(loggingFileSystem)).
                     WithSourceReferenceResolver(sourceFileResolver).
                     WithSyntaxTreeOptionsProvider(syntaxTreeOptions))
        End Function

        Protected Overrides Function GetOutputFileName(compilation As Compilation, cancellationToken As CancellationToken) As String
            ' The only case this is Nothing is when there are errors during parsing in which case this should never get called
            Debug.Assert(Arguments.OutputFileName IsNot Nothing)
            Return Arguments.OutputFileName
        End Function

        Private Sub PrintReferences(resolvedReferences As List(Of MetadataReference), consoleOutput As TextWriter)
            For Each reference In resolvedReferences
                If reference.Properties.Kind = MetadataImageKind.Module Then
                    consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_MSG_ADDMODULE, Culture), reference.Display)
                ElseIf reference.Properties.EmbedInteropTypes Then
                    consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_MSG_ADDLINKREFERENCE, Culture), reference.Display)
                Else
                    consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_MSG_ADDREFERENCE, Culture), reference.Display)
                End If
            Next

            consoleOutput.WriteLine()
        End Sub

        Friend Overrides Function SuppressDefaultResponseFile(args As IEnumerable(Of String)) As Boolean
            For Each arg In args
                Select Case arg.ToLowerInvariant
                    Case "/noconfig", "-noconfig", "/nostdlib", "-nostdlib"
                        Return True
                End Select
            Next
            Return False
        End Function

        ''' <summary>
        ''' Print compiler logo
        ''' </summary>
        ''' <param name="consoleOutput"></param>
        Public Overrides Sub PrintLogo(consoleOutput As TextWriter)
            consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_LogoLine1, Culture), GetToolName(), GetCompilerVersion())
            consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_LogoLine2, Culture))
            consoleOutput.WriteLine()
        End Sub

        Friend Overrides Function GetToolName() As String
            Return ErrorFactory.IdToString(ERRID.IDS_ToolName, Culture)
        End Function

        Friend Overrides ReadOnly Property Type As Type
            Get
                ' We do not use Me.GetType() so that we don't break mock subtypes
                Return GetType(VisualBasicCompiler)
            End Get
        End Property

        ''' <summary>
        ''' Print Commandline help message (up to 80 English characters per line)
        ''' </summary>
        ''' <param name="consoleOutput"></param>
        Public Overrides Sub PrintHelp(consoleOutput As TextWriter)
            consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_VBCHelp, Culture))
        End Sub

        Public Overrides Sub PrintLangVersions(consoleOutput As TextWriter)
            consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_LangVersions, Culture))
            Dim defaultVersion = LanguageVersion.Default.MapSpecifiedToEffectiveVersion()
            Dim latestVersion = LanguageVersion.Latest.MapSpecifiedToEffectiveVersion()
            For Each v As LanguageVersion In System.Enum.GetValues(GetType(LanguageVersion))
                If v = defaultVersion Then
                    consoleOutput.WriteLine($"{v.ToDisplayString()} (default)")
                ElseIf v = latestVersion Then
                    consoleOutput.WriteLine($"{v.ToDisplayString()} (latest)")
                Else
                    consoleOutput.WriteLine(v.ToDisplayString())
                End If
            Next
            consoleOutput.WriteLine()
        End Sub

        Protected Overrides Function TryGetCompilerDiagnosticCode(diagnosticId As String, ByRef code As UInteger) As Boolean
            Return CommonCompiler.TryGetCompilerDiagnosticCode(diagnosticId, "BC", code)
        End Function

        Protected Overrides Sub ResolveAnalyzersFromArguments(
            diagnostics As List(Of DiagnosticInfo),
            messageProvider As CommonMessageProvider,
            compilationOptions As CompilationOptions,
            skipAnalyzers As Boolean,
            ByRef analyzers As ImmutableArray(Of DiagnosticAnalyzer),
            ByRef generators As ImmutableArray(Of ISourceGenerator))

            Arguments.ResolveAnalyzersFromArguments(LanguageNames.VisualBasic, diagnostics, messageProvider, AssemblyLoader, compilationOptions, skipAnalyzers, analyzers, generators)
        End Sub

        Protected Overrides Sub ResolveEmbeddedFilesFromExternalSourceDirectives(
            tree As SyntaxTree,
            resolver As SourceReferenceResolver,
            embeddedFiles As OrderedSet(Of String),
            diagnostics As DiagnosticBag)

            For Each directive As ExternalSourceDirectiveTriviaSyntax In tree.GetRoot().GetDirectives(
                Function(d) d.Kind() = SyntaxKind.ExternalSourceDirectiveTrivia)

                If directive.ExternalSource.IsMissing Then
                    Continue For
                End If

                Dim path = CStr(directive.ExternalSource.Value)
                If path Is Nothing Then
                    Continue For
                End If

                Dim resolvedPath = resolver.ResolveReference(path, tree.FilePath)
                If resolvedPath Is Nothing Then
                    diagnostics.Add(
                        MessageProvider.CreateDiagnostic(
                            MessageProvider.ERR_FileNotFound,
                            directive.ExternalSource.GetLocation(),
                            path))

                    Continue For
                End If

                embeddedFiles.Add(resolvedPath)
            Next
        End Sub

        Private Protected Overrides Function CreateGeneratorDriver(baseDirectory As String, parseOptions As ParseOptions, generators As ImmutableArray(Of ISourceGenerator), analyzerConfigOptionsProvider As AnalyzerConfigOptionsProvider, additionalTexts As ImmutableArray(Of AdditionalText), checksumAlgorithm As SourceHashAlgorithm) As GeneratorDriver
            Return VisualBasicGeneratorDriver.Create(generators, additionalTexts, DirectCast(parseOptions, VisualBasicParseOptions), analyzerConfigOptionsProvider,
                                                     driverOptions:=New GeneratorDriverOptions(disabledOutputs:=IncrementalGeneratorOutputKind.Host, baseDirectory:=baseDirectory) With {.ChecksumAlgorithm = checksumAlgorithm})
        End Function

        Private Protected Overrides Sub DiagnoseBadAccesses(consoleOutput As TextWriter, errorLogger As ErrorLogger, compilation As Compilation, diagnostics As ImmutableArray(Of Diagnostic))
            Dim newDiagnostics = DiagnosticBag.GetInstance()

            For Each diag In diagnostics
                Dim symbol As Symbol
                Select Case diag.Code
                    Case ERRID.ERR_InaccessibleSymbol2,
                         ERRID.ERR_InaccessibleMember3,
                         ERRID.ERR_InAccessibleCoClass3,
                         ERRID.ERR_CannotOverrideInAccessibleMember,
                         ERRID.ERR_InaccessibleReturnTypeOfMember2

                        Dim symbolDiagnostic = DirectCast(DirectCast(diag, DiagnosticWithInfo).Info, BadSymbolDiagnostic)
                        symbol = symbolDiagnostic.BadSymbol

                    Case Else
                        Continue For
                End Select

                newDiagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_SymbolDefinedInAssembly, symbol, symbol.ContainingAssembly), diag.Location))
            Next

            ReportDiagnostics(newDiagnostics.ToReadOnlyAndFree(), consoleOutput, errorLogger, compilation)
        End Sub

    End Class
End Namespace
