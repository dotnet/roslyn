' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Threading.Tasks
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
            ' TODO: Bug 872626 (angocke)
            ' WORKAROUND: REMOVE BEFORE SHIPPING
            ' First look for vbc.rsp, then look for rvbc.rsp.
            Get
                If String.IsNullOrEmpty(p_responseFileName) Then
                    p_responseFileName = Path.Combine(ResponseFileDirectory, "vbc.rsp")
                    If Not File.Exists(p_responseFileName) Then
                        p_responseFileName = Path.Combine(ResponseFileDirectory, "rvbc.rsp")
                    End If
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

            Dim tree = VisualBasicSyntaxTree.ParseText(content, file.Path, If(file.IsScript, scriptParseOptions, parseOptions))

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

            If PrintErrors(result.GetParseDiagnostics(), consoleOutput) Then
                Return Nothing
            End If

            If PrintErrors(result.GetDeclarationDiagnostics(), consoleOutput) Then
                Return Nothing
            End If

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

        Protected Overrides Function ResolveMetadataReferencesFromArguments(
            externalReferenceResolver As MetadataReferenceResolver,
            metadataProvider As MetadataReferenceProvider,
            diagnostics As List(Of DiagnosticInfo),
            resolved As List(Of MetadataReference)
        ) As Boolean
            If MyBase.ResolveMetadataReferencesFromArguments(externalReferenceResolver, metadataProvider, diagnostics, resolved) Then

                ' If there were no references, don't try to add default Cor library reference.
                If Arguments.DefaultCoreLibraryReference IsNot Nothing AndAlso resolved.Count > 0 Then

                    ' All references from arguments were resolved successfully. Let's see if we have a reference that can be used as a Cor library.
                    For Each reference In resolved
                        Dim refProps = reference.Properties

                        ' The logic about deciding what assembly is a candidate for being a Cor library here and in
                        ' CommonReferenceManager<TCompilation, TAssemblySymbol>.IndexOfCorLibrary
                        ' should be equivalent.
                        If Not refProps.EmbedInteropTypes AndAlso refProps.Kind = MetadataImageKind.Assembly Then
                            Dim metadata As Metadata

                            Try
                                metadata = DirectCast(reference, PortableExecutableReference).GetMetadata()
                            Catch
                                ' Failed to get metadata, there will be some errors reported later.
                                Return True
                            End Try

                            If metadata Is Nothing Then
                                ' Failed to get metadata, there will be some errors reported later.
                                Return True
                            End If

                            Dim assemblyMetadata = DirectCast(metadata, AssemblyMetadata)

                            If Not assemblyMetadata.IsValidAssembly Then
                                ' There will be some errors reported later.
                                Return True
                            End If

                            Dim assembly As PEAssembly = assemblyMetadata.Assembly

                            If assembly.AssemblyReferences.Length = 0 AndAlso Not assembly.ContainsNoPiaLocalTypes AndAlso assembly.DeclaresTheObjectClass Then
                                ' This reference looks like a valid Cor library candidate, bail out.
                                Return True
                            End If
                        End If
                    Next

                    ' None of the supplied references could be used as a Cor library. Let's add a default one.
                    Dim defaultCorLibrary As MetadataReference = Arguments.ResolveMetadataReference(Arguments.DefaultCoreLibraryReference.Value, externalReferenceResolver, metadataProvider, diagnostics, MessageProvider)

                    If defaultCorLibrary.IsUnresolved Then
                        Debug.Assert(diagnostics.Any())
                        Return False
                    Else
                        resolved.Insert(0, defaultCorLibrary)
                        Return True
                    End If
                End If

                Return True
            End If

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

            consoleOutput.WriteLine(
            <![CDATA[                  Visual Basic Compiler Options

                                  - OUTPUT FILE -
/out:<file>                       Specifies the output file name.
/target:exe                       Create a console application (default). 
                                  (Short form: /t)
/target:winexe                    Create a Windows application.
/target:library                   Create a library assembly.
/target:module                    Create a module that can be added to an 
                                  assembly.
/target:appcontainerexe           Create a Windows application that runs in 
                                  AppContainer.
/target:winmdobj                  Create a Windows Metadata intermediate file
/doc[+|-]                         Generates XML documentation file.
/doc:<file>                       Generates XML documentation file to <file>.

                                  - INPUT FILES -
/addmodule:<file_list>            Reference metadata from the specified modules
/link:<file_list>                 Embed metadata from the specified interop 
                                  assembly. (Short form: /l)
/recurse:<wildcard>               Include all files in the current directory 
                                  and subdirectories according to the
                                  wildcard specifications.
/reference:<file_list>            Reference metadata from the specified 
                                  assembly. (Short form: /r)
/analyzer:<file_list>             Run the analyzers from this assembly
                                  (Short form: /a)

                                  - RESOURCES -
/linkresource:<resinfo>           Links the specified file as an external 
                                  assembly resource.
                                  resinfo:<file>[,<name>[,public|private]] 
                                  (Short form: /linkres)
/nowin32manifest                  The default manifest should not be embedded 
                                  in the manifest section of the output PE.
/resource:<resinfo>               Adds the specified file as an embedded 
                                  assembly resource.
                                  resinfo:<file>[,<name>[,public|private]] 
                                  (Short form: /res)
/win32icon:<file>                 Specifies a Win32 icon file (.ico) for the 
                                  default Win32 resources.
/win32manifest:<file>             The provided file is embedded in the manifest
                                  section of the output PE.
/win32resource:<file>             Specifies a Win32 resource file (.res).

                                  - CODE GENERATION -
/optimize[+|-]                    Enable optimizations.
/removeintchecks[+|-]             Remove integer checks. Default off.
/debug[+|-]                       Emit debugging information.
/debug:full                       Emit full debugging information (default).
/debug:pdbonly                    Emit PDB file only.

                                  - ERRORS AND WARNINGS -
/nowarn                           Disable all warnings.
/nowarn:<number_list>             Disable a list of individual warnings.
/warnaserror[+|-]                 Treat all warnings as errors.
/warnaserror[+|-]:<number_list>   Treat a list of warnings as errors.
/ruleset:<file>                   Specify a ruleset file that disables specific
                                  diagnostics.

                                  - LANGUAGE -
/define:<symbol_list>             Declare global conditional compilation 
                                  symbol(s). symbol_list:name=value,... 
                                  (Short form: /d)
/imports:<import_list>            Declare global Imports for namespaces in 
                                  referenced metadata files. 
                                  import_list:namespace,...
/langversion:<number>             Specify language version: 
                                  9|9.0|10|10.0|11|11.0.
/optionexplicit[+|-]              Require explicit declaration of variables.
/optioninfer[+|-]                 Allow type inference of variables.
/rootnamespace:<string>           Specifies the root Namespace for all type 
                                  declarations.
/optionstrict[+|-]                Enforce strict language semantics.
/optionstrict:custom              Warn when strict language semantics are not 
                                  respected.
/optioncompare:binary             Specifies binary-style string comparisons. 
                                  This is the default.
/optioncompare:text               Specifies text-style string comparisons.

                                  - MISCELLANEOUS -
/help                             Display this usage message. (Short form: /?)
/noconfig                         Do not auto-include VBC.RSP file.
/nologo                           Do not display compiler copyright banner.
/quiet                            Quiet output mode.
/verbose                          Display verbose messages.
/parallel[+|-]                    Concurrent build. 

                                  - ADVANCED -
/baseaddress:<number>             The base address for a library or module 
                                  (hex).
/bugreport:<file>                 Create bug report file.
/codepage:<number>                Specifies the codepage to use when opening 
                                  source files.
/delaysign[+|-]                   Delay-sign the assembly using only the public
                                  portion of the strong name key.
/errorreport:<string>             Specifies how to handle internal compiler
                                  errors; must be prompt, send, none, or queue
                                  (default).
/filealign:<number>               Specify the alignment used for output file 
                                  sections.
/highentropyva[+|-]               Enable high-entropy ASLR.
/keycontainer:<string>            Specifies a strong name key container.
/keyfile:<file>                   Specifies a strong name key file.
/libpath:<path_list>              List of directories to search for metadata 
                                  references. (Semi-colon delimited.)
/main:<class>                     Specifies the Class or Module that contains 
                                  Sub Main. It can also be a Class that 
                                  inherits from System.Windows.Forms.Form. 
                                  (Short form: /m)
/moduleassemblyname:<string>      Name of the assembly which this module will 
                                  be a part of.
/netcf                            Target the .NET Compact Framework.
/nostdlib                         Do not reference standard libraries 
                                  (system.dll and VBC.RSP file).
/platform:<string>                Limit which platforms this code can run on; 
                                  must be x86, x64, Itanium, arm,
                                  AnyCPU32BitPreferred or anycpu (default).
/preferreduilang                  Specify the preferred output language name.
/sdkpath:<path>                   Location of the .NET Framework SDK directory
                                  (mscorlib.dll).
/subsystemversion:<version>       Specify subsystem version of the output PE. 
                                  version:<number>[.<number>]
/utf8output[+|-]                  Emit compiler output in UTF8 character 
                                  encoding.
@<file>                           Insert command-line settings from a text file
/vbruntime[+|-|*]                 Compile with/without the default Visual Basic
                                  runtime.
/vbruntime:<file>                 Compile with the alternate Visual Basic 
                                  runtime in <file>.
]]>.Value)

        End Sub
    End Class
End Namespace

