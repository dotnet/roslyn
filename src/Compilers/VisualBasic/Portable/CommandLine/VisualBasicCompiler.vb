' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend MustInherit Class VisualBasicCompiler
        Inherits CommonCompiler

        Friend Const ResponseFileName As String = "vbc.rsp"
        Friend Const VbcCommandLinePrefix = "vbc : " 'Common prefix String For VB diagnostic output with no location.
        Private Shared s_responseFileName As String
        Private ReadOnly _responseFile As String
        Private ReadOnly _diagnosticFormatter As CommandLineDiagnosticFormatter
        Private _additionalTextFiles As ImmutableArray(Of AdditionalTextFile)

        Protected Sub New(parser As VisualBasicCommandLineParser, responseFile As String, args As String(), clientDirectory As String, baseDirectory As String, sdkDirectory As String, additionalReferenceDirectories As String, analyzerLoader As IAnalyzerAssemblyLoader)
            MyBase.New(parser, responseFile, args, clientDirectory, baseDirectory, sdkDirectory, additionalReferenceDirectories, analyzerLoader)

            _diagnosticFormatter = New CommandLineDiagnosticFormatter(baseDirectory, AddressOf GetAdditionalTextFiles)
            _additionalTextFiles = Nothing
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
            Dim content = ReadFileContent(file, fileReadDiagnostics)

            If content Is Nothing Then
                ReportErrors(fileReadDiagnostics, consoleOutput, errorLogger)
                fileReadDiagnostics.Clear()
                hadErrors = True
                Return Nothing
            End If

            Dim tree = VisualBasicSyntaxTree.ParseText(content, If(file.IsScript, scriptParseOptions, parseOptions), file.Path)

            ' prepopulate line tables.
            ' we will need line tables anyways and it is better to Not wait until we are in emit
            ' where things run sequentially.
            Dim isHiddenDummy As Boolean
            tree.GetMappedLineSpanAndVisibility(Nothing, isHiddenDummy)

            Return tree
        End Function

        Public Overrides Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger, errorLogger As ErrorLogger) As Compilation
            Dim parseOptions = Arguments.ParseOptions

            ' We compute script parse options once so we don't have to do it repeatedly in
            ' case there are many script files.
            Dim scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script)

            Dim hadErrors As Boolean = False

            Dim sourceFiles As ImmutableArray(Of CommandLineSourceFile) = Arguments.SourceFiles
            Dim trees(sourceFiles.Length - 1) As SyntaxTree

            If Arguments.CompilationOptions.ConcurrentBuild Then
                Parallel.For(0, sourceFiles.Length,
                   UICultureUtilities.WithCurrentUICulture(Of Integer)(
                        Sub(i As Integer)
                            ' NOTE: order of trees is important!!
                            trees(i) = ParseFile(consoleOutput, parseOptions, scriptParseOptions, hadErrors, sourceFiles(i), errorLogger)
                        End Sub))
            Else
                For i = 0 To sourceFiles.Length - 1
                    ' NOTE: order of trees is important!!
                    trees(i) = ParseFile(consoleOutput, parseOptions, scriptParseOptions, hadErrors, sourceFiles(i), errorLogger)
                Next
            End If

            ' If there were any errors while trying to read files, then exit.
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

            If ReportErrors(diagnostics, consoleOutput, errorLogger) Then
                Return Nothing
            End If

            If Arguments.OutputLevel = OutputLevel.Verbose Then
                PrintReferences(resolvedReferences, consoleOutput)
            End If

            Dim strongNameProvider = New LoggingStrongNameProvider(Arguments.KeyFileSearchPaths, touchedFilesLogger)
            Dim xmlFileResolver = New LoggingXmlFileResolver(Arguments.BaseDirectory, touchedFilesLogger)

            ' TODO: support for #load search paths
            Dim sourceFileResolver = New LoggingSourceFileResolver(ImmutableArray(Of String).Empty, Arguments.BaseDirectory, Arguments.PathMap, touchedFilesLogger)

            Dim result = VisualBasicCompilation.Create(
                 Arguments.CompilationName,
                 trees,
                 resolvedReferences,
                 Arguments.CompilationOptions.
                     WithMetadataReferenceResolver(referenceDirectiveResolver).
                     WithAssemblyIdentityComparer(assemblyIdentityComparer).
                     WithStrongNameProvider(strongNameProvider).
                     WithXmlReferenceResolver(xmlFileResolver).
                     WithSourceReferenceResolver(sourceFileResolver))

            Return result
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

        Protected Overrides Sub PrintError(Diagnostic As DiagnosticInfo, consoleOutput As TextWriter)
            consoleOutput.Write(VisualBasicCompiler.VbcCommandLinePrefix)
            consoleOutput.WriteLine(Diagnostic.ToString(Culture))
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
            consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_LogoLine1, Culture), GetToolName(), GetAssemblyFileVersion())
            consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_LogoLine2, Culture))
            consoleOutput.WriteLine()
        End Sub

        Friend Overrides Function GetToolName() As String
            Return ErrorFactory.IdToString(ERRID.IDS_ToolName, Culture)
        End Function

        ''' <summary>
        ''' Print Commandline help message (up to 80 English characters per line)
        ''' </summary>
        ''' <param name="consoleOutput"></param>
        Public Overrides Sub PrintHelp(consoleOutput As TextWriter)
            consoleOutput.WriteLine(ErrorFactory.IdToString(ERRID.IDS_VBCHelp, Culture))
        End Sub

        Protected Overrides Function TryGetCompilerDiagnosticCode(diagnosticId As String, ByRef code As UInteger) As Boolean
            Return CommonCompiler.TryGetCompilerDiagnosticCode(diagnosticId, "BC", code)
        End Function

        Protected Overrides Sub ResolveAnalyzersAndGeneratorsFromArguments(
            diagnostics As List(Of DiagnosticInfo),
            messageProvider As CommonMessageProvider,
            ByRef analyzers As ImmutableArray(Of DiagnosticAnalyzer),
            ByRef generators As ImmutableArray(Of SourceGenerator))
            Arguments.ResolveAnalyzersAndGeneratorsFromArguments(LanguageNames.VisualBasic, diagnostics, messageProvider, AssemblyLoader, analyzers, generators)
        End Sub
    End Class
End Namespace

