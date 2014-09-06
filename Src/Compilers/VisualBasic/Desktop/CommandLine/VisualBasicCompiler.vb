' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Instrumentation

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend MustInherit Class VisualBasicCompiler
        Inherits CommonCompiler

        Friend Const VbcCommandLinePrefix = "vbc : " 'Common prefix String For VB diagnostic output with no location.
        Private Shared p_responseFileName As String
        Private ReadOnly m_diagnosticFormatter As CommandLineDiagnosticFormatter

        Protected Sub New(parser As VisualBasicCommandLineParser, responseFile As String, args As String(), baseDirectory As String, additionalReferencePaths As String)
            MyBase.New(parser, responseFile, args, baseDirectory, additionalReferencePaths)

            m_diagnosticFormatter = New CommandLineDiagnosticFormatter(baseDirectory)
        End Sub

        Protected Shared ReadOnly Property BasicResponseFileName As String
            Get
                If String.IsNullOrEmpty(p_responseFileName) Then
                    p_responseFileName = Path.Combine(ResponseFileDirectory, "vbc.rsp")
                End If
                Return p_responseFileName
            End Get
        End Property

        Friend Overloads ReadOnly Property Arguments As VisualBasicCommandLineArguments
            Get
                Return DirectCast(MyBase.Arguments, VisualBasicCommandLineArguments)
            End Get
        End Property

        Public Overrides ReadOnly Property DiagnosticFormatter As DiagnosticFormatter
            Get
                Return m_diagnosticFormatter
            End Get
        End Property

        Private Function ParseFile(consoleOutput As TextWriter,
                                   parseOptions As VisualBasicParseOptions,
                                   scriptParseOptions As VisualBasicParseOptions,
                                   ByRef hadErrors As Boolean,
                                   file As CommandLineSourceFile) As SyntaxTree

            Dim fileReadDiagnostics As New List(Of DiagnosticInfo)()
            Dim content = ReadFileContent(file, fileReadDiagnostics, Arguments.Encoding)

            If content Is Nothing Then
                PrintErrors(fileReadDiagnostics, consoleOutput)
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

        Protected Overrides Function CreateCompilation(consoleOutput As TextWriter, touchedFilesLogger As TouchedFileLogger) As Compilation
            Dim parseOptions = Arguments.ParseOptions
            Dim scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script)

            Dim hadErrors As Boolean = False

            Dim sourceFiles As ImmutableArray(Of CommandLineSourceFile) = Arguments.SourceFiles
            Dim trees(sourceFiles.Length - 1) As SyntaxTree

            If Arguments.CompilationOptions.ConcurrentBuild Then
                Parallel.For(0, sourceFiles.Length,
                             Sub(i As Integer)
                                 ' NOTE: order of trees is important!!
                                 trees(i) = ParseFile(consoleOutput, parseOptions, scriptParseOptions, hadErrors, sourceFiles(i))
                             End Sub)
            Else
                For i = 0 To sourceFiles.Length - 1
                    ' NOTE: order of trees is important!!
                    trees(i) = ParseFile(consoleOutput, parseOptions, scriptParseOptions, hadErrors, sourceFiles(i))
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
            Dim metadataProvider As MetadataFileReferenceProvider = GetMetadataProvider()

            Dim externalReferenceResolver = GetExternalMetadataResolver(touchedFilesLogger)
            Dim resolvedReferences = ResolveMetadataReferences(externalReferenceResolver, metadataProvider, diagnostics, assemblyIdentityComparer, touchedFilesLogger, referenceDirectiveResolver)

            If PrintErrors(diagnostics, consoleOutput) Then
                Return Nothing
            End If

            If Arguments.OutputLevel = OutputLevel.Verbose Then
                PrintReferences(resolvedReferences, consoleOutput)
            End If

            Dim strongNameProvider = New LoggingStrongNameProvider(Arguments.KeyFileSearchPaths, touchedFilesLogger)
            Dim xmlFileResolver = New LoggingXmlFileResolver(Arguments.BaseDirectory, touchedFilesLogger)

            ' TODO: support for #load search paths
            Dim sourceFileResolver = New LoggingSourceFileResolver(ImmutableArray(Of String).Empty, Arguments.BaseDirectory, touchedFilesLogger)

            Dim result = VisualBasicCompilation.Create(
                 Arguments.CompilationName,
                 trees,
                 resolvedReferences,
                 Arguments.CompilationOptions.
                     WithMetadataReferenceResolver(referenceDirectiveResolver).
                     WithMetadataReferenceProvider(metadataProvider).
                     WithAssemblyIdentityComparer(assemblyIdentityComparer).
                     WithStrongNameProvider(strongNameProvider).
                     WithXmlReferenceResolver(xmlFileResolver).
                     WithSourceReferenceResolver(sourceFileResolver))

            Return result
        End Function

        Private Sub PrintReferences(resolvedReferences As List(Of MetadataReference), consoleOutput As TextWriter)
            For Each reference In resolvedReferences
                If reference.Properties.Kind = MetadataImageKind.Module Then
                    consoleOutput.WriteLine(ErrorFactory.ResourceManager.GetString("IDS_MSG_ADDMODULE"), reference.Display)
                ElseIf reference.Properties.EmbedInteropTypes Then
                    consoleOutput.WriteLine(ErrorFactory.ResourceManager.GetString("IDS_MSG_ADDLINKREFERENCE"), reference.Display)
                Else
                    consoleOutput.WriteLine(ErrorFactory.ResourceManager.GetString("IDS_MSG_ADDREFERENCE"), reference.Display)
                End If
            Next

            consoleOutput.WriteLine()
        End Sub

        Friend Overrides Sub PrintError(Diagnostic As DiagnosticInfo, consoleOutput As TextWriter)
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
        Protected Overrides Sub PrintLogo(consoleOutput As TextWriter)
            Dim thisAssembly As Assembly = Me.GetType().Assembly
            consoleOutput.WriteLine(VBResources.LogoLine1, FileVersionInfo.GetVersionInfo(thisAssembly.Location).FileVersion)
            consoleOutput.WriteLine(VBResources.LogoLine2)
            consoleOutput.WriteLine()
        End Sub

        ''' <summary>
        ''' Print Commandline help message (up to 80 English characters per line)
        ''' </summary>
        ''' <param name="consoleOutput"></param>
        Protected Overrides Sub PrintHelp(consoleOutput As TextWriter)
            consoleOutput.WriteLine(VBResources.VBCHelp)
        End Sub

        Protected Overrides Function TryGetCompilerDiagnosticCode(diagnosticId As String, ByRef code As UInteger) As Boolean
            Return CommonCompiler.TryGetCompilerDiagnosticCode(diagnosticId, "BC", code)
        End Function

        Protected Overrides Function ResolveAnalyzersFromArguments(diagnostics As List(Of DiagnosticInfo), messageProvider As CommonMessageProvider, touchedFiles As TouchedFileLogger) As ImmutableArray(Of IDiagnosticAnalyzer)
            Return Arguments.ResolveAnalyzersFromArguments(LanguageNames.VisualBasic, diagnostics, messageProvider, touchedFiles)
        End Function
    End Class
End Namespace

