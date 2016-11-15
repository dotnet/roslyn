' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' The VisualBasicCommandLineParser class contains members used to perform various Visual Basic command line parsing operations.
    ''' </summary>
    Public Class VisualBasicCommandLineParser
        Inherits CommandLineParser
        ''' <summary>
        ''' Gets the current command line parser.
        ''' </summary>
        Public Shared ReadOnly Property [Default] As New VisualBasicCommandLineParser()

        ''' <summary>
        ''' Gets the current interactive command line parser.
        ''' </summary>
        Friend Shared ReadOnly Property ScriptRunner As New VisualBasicCommandLineParser(isScriptRunner:=True)

        ''' <summary>
        ''' Creates a new command line parser.
        ''' </summary>
        ''' <param name="isScriptRunner">An optional parameter indicating whether to create a interactive command line parser.</param>
        Friend Sub New(Optional isScriptRunner As Boolean = False)
            MyBase.New(VisualBasic.MessageProvider.Instance, isScriptRunner)
        End Sub

        Private Const s_win32Manifest As String = "win32manifest"
        Private Const s_win32Icon As String = "win32icon"
        Private Const s_win32Res As String = "win32resource"

        ''' <summary>
        ''' Gets the standard Visual Basic source file extension
        ''' </summary>
        ''' <returns>A string representing the standard Visual Basic source file extension.</returns>
        Protected Overrides ReadOnly Property RegularFileExtension As String = ".vb"


        ''' <summary>
        ''' Gets the standard Visual Basic script file extension.
        ''' </summary>
        ''' <returns>A string representing the standard Visual Basic script file extension.</returns>
        Protected Overrides ReadOnly Property ScriptFileExtension As String = ".vbx"

        Friend NotOverridable Overrides Function CommonParse(args As IEnumerable(Of String), baseDirectory As String, sdkDirectoryOpt As String, additionalReferenceDirectories As String) As CommandLineArguments
            Return Parse(args, baseDirectory, sdkDirectoryOpt, additionalReferenceDirectories)
        End Function

        Protected Friend Function Parse_Help(value As String, ByRef display As (Logo As Boolean, Help As Boolean, Version As Boolean)) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                display.Help = True
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Protected Friend Function Parser_Version(value As String, ByRef display As (Logo As Boolean, Help As Boolean, Version As Boolean)) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                display.Version = True
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Protected Friend Enum FlagParse
            Valid_ContinueFor
            Invalid_ExitSelect
        End Enum

        ''' <summary>
        ''' Parses a command line.
        ''' </summary>
        ''' <param name="args">A collection of strings representing the command line arguments.</param>
        ''' <param name="baseDirectory">The base directory used for qualifying file locations.</param>
        ''' <param name="sdkDirectory">The directory to search for mscorlib, or Nothing if not available.</param>
        ''' <param name="additionalReferenceDirectories">A string representing additional reference paths.</param>
        ''' <returns>A CommandLineArguments object representing the parsed command line.</returns>
        Public Shadows Function Parse(args As IEnumerable(Of String), baseDirectory As String, sdkDirectory As String, Optional additionalReferenceDirectories As String = Nothing) As VisualBasicCommandLineArguments
            Const GenerateFileNameForDocComment As String = "USE-OUTPUT-NAME"

            Dim diagnostics As List(Of Diagnostic) = New List(Of Diagnostic)()
            Dim flattenedArgs As List(Of String) = New List(Of String)()
            Dim scriptArgs As List(Of String) = If(IsScriptRunner, New List(Of String)(), Nothing)

            Dim Paths = (SDK:=New List(Of String),
                       [LIB]:=New List(Of String),
                      Source:=New List(Of String),
               KeyFileSearch:=New List(Of String),
                    Response:=New List(Of String))
            ' normalized paths to directories containing response files:

            FlattenArgs(args, diagnostics, flattenedArgs, scriptArgs, baseDirectory, Paths.Response)

            Dim display As (Logo As Boolean, Help As Boolean, Version As Boolean) = (Logo:=True, Help:=False, Version:=False)

            Dim outputLevel As OutputLevel = OutputLevel.Normal
            Dim optimize As Boolean = False
            Dim checkOverflow As Boolean = True
            Dim concurrentBuild As Boolean = True
            Dim deterministic As Boolean = False
            Dim emitPdb As Boolean
            Dim debugInformationFormat As DebugInformationFormat = DebugInformationFormat.Pdb
            Dim noStdLib As Boolean = False

            Dim Output As (UTF8 As Boolean, FileName As String, Directory As String, Kind As OutputKind) = (UTF8:=False, FileName:=Nothing, Directory:=baseDirectory, Kind:=CodeAnalysis.OutputKind.ConsoleApplication)

            Dim errorLogPath As String = Nothing
            'Dim parseDocumentationComments As Boolean = False ' Don't just null check documentationFileName because we want to do this even if the file name is invalid.
            'Dim documentationPath As String = Nothing
            Dim _Documentation As (_Path As String, ParseComments As Boolean) = (_Path:=Nothing,
                                                                         ParseComments:=False '  Don't just null check documentationFileName because we want to do this even if the file name is invalid.
                                                                         )
            Dim ssVersion As SubsystemVersion = SubsystemVersion.None
            Dim languageVersion As LanguageVersion = LanguageVersion.Default
            Dim mainTypeName As String = Nothing

            Dim win32ManifestFile As String = Nothing
            Dim win32ResourceFile As String = Nothing
            Dim win32IconFile As String = Nothing
            Dim noWin32Manifest As Boolean = False

            Dim managedResources = New List(Of ResourceDescription)()
            Dim sourceFiles = New List(Of CommandLineSourceFile)()
            Dim hasSourceFiles = False
            Dim additionalFiles = New List(Of CommandLineSourceFile)()
            Dim embeddedFiles = New List(Of CommandLineSourceFile)()
            Dim embedAllSourceFiles = False
            Dim codepage As Encoding = Nothing
            Dim checksumAlgorithm = SourceHashAlgorithm.Sha1
            Dim defines As IReadOnlyDictionary(Of String, Object) = Nothing
            Dim metadataReferences = New List(Of CommandLineReference)()
            Dim analyzers = New List(Of CommandLineAnalyzerReference)()

            Dim globalImports = New List(Of GlobalImport)
            Dim rootNamespace As String = ""

            Dim _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean) =
                          (Strict:=OptionStrict.Off,
                            Infer:=False,  ' MSDN says: ...The compiler default for this option is /optioninfer-.
                         Explicit:=True,
                      CompareText:=False)

            Dim platform As Platform = Platform.AnyCpu
            Dim preferredUILang As CultureInfo = Nothing
            Dim fileAlignment As Integer = 0
            Dim baseAddress As ULong = 0
            Dim highEntropyVA As Boolean = False

            Dim _VBRuntime As (_Path As String, IncludeReference As Boolean, EmbedCore As Boolean) = (Nothing, True, False)

            Dim generalDiagnosticOption As ReportDiagnostic = ReportDiagnostic.Default
            Dim pathMap As ImmutableArray(Of KeyValuePair(Of String, String)) = ImmutableArray(Of KeyValuePair(Of String, String)).Empty

            ' Diagnostic ids specified via /nowarn /warnaserror must be processed in case-insensitive fashion.
            Dim specificDiagnosticOptionsFromRuleSet = New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
            Dim specificDiagnosticOptionsFromGeneralArguments = New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
            Dim specificDiagnosticOptionsFromSpecificArguments = New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
            Dim specificDiagnosticOptionsFromNoWarnArguments = New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)

            Dim keyFileSetting As String = Nothing
            Dim keyContainerSetting As String = Nothing
            Dim delaySignSetting As Boolean? = Nothing
            Dim moduleAssemblyName As String = Nothing
            Dim moduleName As String = Nothing
            Dim touchedFilesPath As String = Nothing
            Dim features = New List(Of String)()
            Dim reportAnalyzer As Boolean = False
            Dim publicSign As Boolean = False
            Dim interactiveMode As Boolean = False
            Dim instrumentationKinds As ArrayBuilder(Of InstrumentationKind) = ArrayBuilder(Of InstrumentationKind).GetInstance()
            Dim sourceLink As String = Nothing

            ' Process ruleset files first so that diagnostic severity settings specified on the command line via
            ' /nowarn and /warnaserror can override diagnostic severity settings specified in the ruleset file.
            If Not IsScriptRunner Then
                For Each arg In flattenedArgs
                    Dim name As String = Nothing
                    Dim value As String = Nothing
                    If TryParseOption(arg, name, value) AndAlso (name = "ruleset") Then
                        Dim unquoted = RemoveQuotesAndSlashes(value)
                        If String.IsNullOrEmpty(unquoted) Then
                            AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file>")
                            Continue For
                        End If

                        generalDiagnosticOption = GetDiagnosticOptionsFromRulesetFile(specificDiagnosticOptionsFromRuleSet, diagnostics, unquoted, baseDirectory)
                    End If
                Next
            End If

            For Each arg In flattenedArgs
                Debug.Assert(Not arg.StartsWith("@", StringComparison.Ordinal))
                Dim result As FlagParse = FlagParse.Invalid_ExitSelect
                Dim name As String = Nothing
                Dim value As String = Nothing
                If Not TryParseOption(arg, name, value) Then
                    sourceFiles.AddRange(ParseFileArgument(arg, baseDirectory, diagnostics))
                    hasSourceFiles = True
                    Continue For
                End If

                Select Case name
                    Case "?", "help"
                        result = Parse_Help(value, display)

                    Case "version"
                        result = Parser_Version(value, display)

                    Case "r", "reference"
                        result = Parse_Reference(diagnostics, metadataReferences, name, value)

                    Case "a", "analyzer"
                        result = Parse_Analyzer(diagnostics, analyzers, name, value)

                    Case "d", "define"
                        result = Parse_Define(diagnostics, defines, name, value)

                    Case "imports", "import"
                        result = Parse_Imports(diagnostics, globalImports, name, value)

                    Case "optionstrict"
                        result = Parse_Option_Strict(diagnostics, _option, value)

                    Case "optionstrict+", "optionstrict-"
                        result = Parse_Option_Strict(diagnostics, _option, name, value)

                    Case "optioncompare"
                        result = Parse_Option_Compare(diagnostics, _option, value)

                    Case "optionexplicit", "optionexplicit+", "optionexplicit-"
                        result = Parse_Option_Explicit(diagnostics, _option, name, value)

                    Case "optioninfer", "optioninfer+", "optioninfer-"
                        result = Parse_Option_Infer(diagnostics, _option, name, value)

                    Case "codepage"
                        result = Parse_CodePage(diagnostics, codepage, name, value)

                    Case "checksumalgorithm"
                        result = Parse_ChecksumAlgorithm(diagnostics, checksumAlgorithm, value)

                    Case "removeintchecks", "removeintchecks+", "removeintchecks-"
                        result = Parse_RemoveIntChecks(diagnostics, checkOverflow, name, value)

                    Case "sqmsessionguid"
                        result = Parse_SQMSessionGuid(diagnostics, name, value)

                    Case "preferreduilang"
                        result = Parse_PreferredUILang(diagnostics, preferredUILang, name, value)

                    Case "lib", "libpath", "libpaths"
                        result = Parse_LibPath(diagnostics, Paths, name, value)

#If DEBUG Then
                    Case "attachdebugger"
                        Debugger.Launch()
                        result = FlagParse.Valid_ContinueFor
#End If
                End Select
                Select Case result
                    Case FlagParse.Valid_ContinueFor
                        Continue For
                    Case FlagParse.Invalid_ExitSelect
                    Case Else
                        Throw New Exception
                End Select

                If IsScriptRunner Then
                    Select Case name
                        Case "i", "i+", "i-"
                            result = Parse_interactive(diagnostics, interactiveMode, name, value)

                        Case "loadpath", "loadpaths"
                            result = Parse_LoadPaths(diagnostics, Paths, name, value)

                    End Select

                Else

                    Select Case name
                        Case "out"
                            result = Parse_Out(baseDirectory, diagnostics, Output, name, value)

                        Case "t", "target"
                            result = Parse_Target(diagnostics, Output, name, value)

                        Case "moduleassemblyname"
                            result = Parse_ModuleAssemblyName(diagnostics, moduleAssemblyName, arg, value)

                        Case "rootnamespace"
                            result = Parse_RootNamespace(diagnostics, rootNamespace, value)

                        Case "doc"
                            result = Parse_Doc(baseDirectory, GenerateFileNameForDocComment, diagnostics, _Documentation, value)

                        Case "doc+", "doc-"
                            result = Parse_Doc(GenerateFileNameForDocComment, diagnostics, _Documentation, name, value)

                        Case "errorlog"
                            result = Parse_ErrorLog(baseDirectory, diagnostics, errorLogPath, value)

                        Case "netcf"
                            ' Do nothing as we no longer have any use for implementing this switch and  want to avoid failing with any warnings/errors
                            result = FlagParse.Valid_ContinueFor

                        Case "sdkpath"
                            result = Parse_SDKPath(diagnostics, Paths, value)

                        Case "instrument"
                            result = Parse_Instrument(diagnostics, instrumentationKinds, value)

                        Case "recurse"
                            result = Parse_Recurse(baseDirectory, diagnostics, sourceFiles, hasSourceFiles, value)

                        Case "addmodule"
                            result = Parse_AddModule(diagnostics, metadataReferences, value)

                        Case "l", "link"
                            result = Parse_Link(diagnostics, metadataReferences, result, name, value)

                        Case "win32resource"
                            result = Parse_Win32_Resource(diagnostics, win32ResourceFile, value)

                        Case "win32icon"
                            result = Parse_Win32_Icon(diagnostics, win32IconFile, value)

                        Case "win32manifest"
                            result = Parse_Win32_Manifest(diagnostics, win32ManifestFile, value)

                        Case "nowin32manifest"
                            result = Parse_NoWin32_Manifest(noWin32Manifest, value)

                        Case "res", "resource"
                            result = Parse_Resource(baseDirectory, diagnostics, managedResources, name, value)

                        Case "linkres", "linkresource"
                            result = Parse_LinkResource(baseDirectory, diagnostics, managedResources, name, value)

                        Case "sourcelink"
                            result = Parse_SourceLink(baseDirectory, diagnostics, sourceLink, value)

                        Case "debug"
                            result = Parse_Debug(diagnostics, emitPdb, debugInformationFormat, value)

                        Case "debug+", "debug-"
                            result = Parse_Debug(diagnostics, emitPdb, name, value)

                        Case "optimize", "optimize+", "optimize-"
                            result = Parse_Optimize(diagnostics, optimize, name, value)

                        Case "parallel", "p", "parallel+", "p+", "parallel-", "p-"
                            result = Parse_Parallel(diagnostics, concurrentBuild, name, value)

                        Case "deterministic", "deterministic+", "deterministic-"
                            result = Parse_Deterministic(diagnostics, deterministic, name, value)

                        Case "warnaserror", "warnaserror+"
                            result = Parse_WarnAsError(generalDiagnosticOption, specificDiagnosticOptionsFromRuleSet, specificDiagnosticOptionsFromGeneralArguments, specificDiagnosticOptionsFromSpecificArguments, value)

                        Case "warnaserror-"
                            result = Parse_WarnAsError_Minus(generalDiagnosticOption, specificDiagnosticOptionsFromRuleSet, specificDiagnosticOptionsFromGeneralArguments, specificDiagnosticOptionsFromSpecificArguments, value)

                        Case "nowarn"
                            result = Parse_NoWarn(generalDiagnosticOption, specificDiagnosticOptionsFromRuleSet, specificDiagnosticOptionsFromGeneralArguments, specificDiagnosticOptionsFromNoWarnArguments, value)

                        Case "langversion"
                            result = Parse_LangVersion(diagnostics, languageVersion, value)

                        Case "delaysign", "delaysign+", "delaysign-"
                            result = Parse_DelaySign(diagnostics, delaySignSetting, name, value)

                        Case "publicsign", "publicsign+", "publicsign-"
                            result = Parse_PublicSign(diagnostics, publicSign, name, value)

                        Case "keycontainer"
                            result = Parse_KeyContainer(diagnostics, keyFileSetting, keyContainerSetting, name, value)

                        Case "keyfile"
                            result = Parse_KeyFile(diagnostics, keyFileSetting, keyContainerSetting, name, value)

                        Case "highentropyva", "highentropyva+", "highentropyva-"
                            result = Parse_HighEntropyVA(highEntropyVA, name, value)

                        Case "nologo", "nologo+", "nologo-"
                            result = Parse_NoLogo(display, name, value)

                        Case "quiet", "verbose"
                            result = Parse_OutputLevel(outputLevel, name, value)

                        Case "quiet+", "quiet-"
                            result = Parse_Quiet(diagnostics, outputLevel, name, value)

                        Case "verbose-", "verbose+"
                            result = Parse_Verbose(diagnostics, outputLevel, name, value)

                        Case "utf8output", "utf8output+", "utf8output-"
                            result = Parse_UTF8_Output(diagnostics, Output, name, value)

                        Case "noconfig"
                            ' It is already handled (see CommonCommandLineCompiler.cs).
                            result = FlagParse.Valid_ContinueFor

                        Case "bugreport"
                            ' Do nothing as we no longer have any use for implementing this switch and  want to avoid failing with any warnings/errors
                            ' We do no further checking as to a value provided or not and this will cause no diagnostics for invalid values.
                            result = FlagParse.Valid_ContinueFor

                        Case "errorreport"
                            ' Allows any value to be entered and will just silently do nothing
                            ' previously we would validate value for prompt, send Or Queue
                            ' This will cause no diagnostics for invalid values.

                            result = FlagParse.Valid_ContinueFor

                        Case "novbruntimeref"
                            ' The switch is no longer supported and for backwards compat ignored.
                            result = FlagParse.Valid_ContinueFor

                        Case "m", "main"
                            result = Parse_Main(diagnostics, mainTypeName, name, value)

                        Case "subsystemversion"
                            result = Parse_SubSystemVersion(diagnostics, ssVersion, name, value)

                        Case "touchedfiles"
                            result = Parse_TouchedFiles(diagnostics, touchedFilesPath, name, value)

                        Case "fullpaths", "errorendlocation"
                            UnimplementedSwitch(diagnostics, name)
                            result = FlagParse.Valid_ContinueFor

                        Case "pathmap"
                            result = Parse_PathMap(diagnostics, pathMap, value)

                        Case "reportanalyzer"
                            reportAnalyzer = True
                            result = FlagParse.Valid_ContinueFor

                        Case "nostdlib"
                            result = Parse_NoSTDLib(noStdLib, value)

                        Case "vbruntime"
                            result = Parse_VBRuntime(_VBRuntime, value)

                        Case "vbruntime+", "vbruntime-", "vbruntime*"
                            result = Parse_VBRuntime(_VBRuntime, name, value)

                        Case "platform"
                            result = Parse_Platform(diagnostics, platform, name, value)

                        Case "filealign"
                            fileAlignment = ParseFileAlignment(name, RemoveQuotesAndSlashes(value), diagnostics)
                            result = FlagParse.Valid_ContinueFor

                        Case "baseaddress"
                            baseAddress = ParseBaseAddress(name, RemoveQuotesAndSlashes(value), diagnostics)
                            result = FlagParse.Valid_ContinueFor

                        Case "ruleset"
                            '  The ruleset arg has already been processed in a separate pass above.
                            result = FlagParse.Valid_ContinueFor

                        Case "features"
                            result = Parse_Features(features, value)

                        Case "additionalfile"
                            result = Parse_AdditionalFile(baseDirectory, diagnostics, additionalFiles, name, value)

                        Case "embed"
                            result = Parse_Embed(baseDirectory, diagnostics, embeddedFiles, embedAllSourceFiles, value)

                    End Select
                End If

                Select Case result
                    Case FlagParse.Valid_ContinueFor
                    Case FlagParse.Invalid_ExitSelect
                        AddDiagnostic(diagnostics, ERRID.WRN_BadSwitch, arg)

                End Select
            Next

            Dim specificDiagnosticOptions = New Dictionary(Of String, ReportDiagnostic)(specificDiagnosticOptionsFromRuleSet, CaseInsensitiveComparison.Comparer)

            For Each item In specificDiagnosticOptionsFromGeneralArguments
                specificDiagnosticOptions(item.Key) = item.Value
            Next

            For Each item In specificDiagnosticOptionsFromSpecificArguments
                specificDiagnosticOptions(item.Key) = item.Value
            Next

            For Each item In specificDiagnosticOptionsFromNoWarnArguments
                specificDiagnosticOptions(item.Key) = item.Value
            Next

            If Not IsScriptRunner AndAlso Not hasSourceFiles AndAlso managedResources.IsEmpty() Then
                ' VB displays help when there is nothing specified on the command line
                If flattenedArgs.Any Then
                    AddDiagnostic(diagnostics, ERRID.ERR_NoSources)
                Else
                    display.Help = True
                End If
            End If

            ' Prepare SDK PATH
            If sdkDirectory IsNot Nothing AndAlso (Paths.SDK.Count = 0) Then
                Paths.SDK.Add(sdkDirectory)
            End If

            ' Locate default 'mscorlib.dll' or 'System.Runtime.dll', if any.
            Dim defaultCoreLibraryReference As CommandLineReference? = LoadCoreLibraryReference(Paths.SDK, baseDirectory)

            ' If /nostdlib is not specified, load System.dll
            ' Dev12 does it through combination of CompilerHost::InitStandardLibraryList and CompilerProject::AddStandardLibraries.
            If Not noStdLib Then
                Dim systemDllPath As String = FindFileInSdkPath(Paths.SDK, "System.dll", baseDirectory)
                If systemDllPath Is Nothing Then
                    AddDiagnostic(diagnostics, ERRID.WRN_CannotFindStandardLibrary1, "System.dll")
                Else
                    metadataReferences.Add(
                            New CommandLineReference(systemDllPath, New MetadataReferenceProperties(MetadataImageKind.Assembly)))
                End If
                ' Dev11 also adds System.Core.dll in VbHostedCompiler::CreateCompilerProject()
            End If

            ' Add reference to 'Microsoft.VisualBasic.dll' if needed
            If _VBRuntime.IncludeReference Then
                If _VBRuntime._Path Is Nothing Then
                    Dim msVbDllPath As String = FindFileInSdkPath(Paths.SDK, "Microsoft.VisualBasic.dll", baseDirectory)
                    If msVbDllPath Is Nothing Then
                        AddDiagnostic(diagnostics, ERRID.ERR_LibNotFound, "Microsoft.VisualBasic.dll")
                    Else
                        metadataReferences.Add(
                                New CommandLineReference(msVbDllPath, New MetadataReferenceProperties(MetadataImageKind.Assembly)))
                    End If
                Else
                    metadataReferences.Add(New CommandLineReference(_VBRuntime._Path, New MetadataReferenceProperties(MetadataImageKind.Assembly)))
                End If
            End If

            ' add additional reference paths if specified
            If Not String.IsNullOrWhiteSpace(additionalReferenceDirectories) Then
                Paths.LIB.AddRange(ParseSeparatedPaths(additionalReferenceDirectories))
            End If

            ' Build search path
            Dim searchPaths As ImmutableArray(Of String) = BuildSearchPaths(baseDirectory, Paths.SDK, Paths.Response, Paths.LIB)

            ' Public sign doesn't use legacy search path settings
            If publicSign AndAlso Not String.IsNullOrWhiteSpace(keyFileSetting) Then
                keyFileSetting = ParseGenericPathToFile(keyFileSetting, diagnostics, baseDirectory)
            End If

            ValidateWin32Settings(noWin32Manifest, win32ResourceFile, win32IconFile, win32ManifestFile, Output.Kind, diagnostics)

            If sourceLink IsNot Nothing Then
                If Not emitPdb OrElse debugInformationFormat <> DebugInformationFormat.PortablePdb AndAlso debugInformationFormat <> DebugInformationFormat.Embedded Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SourceLinkRequiresPortablePdb)
                End If
            End If

            If embedAllSourceFiles Then
                embeddedFiles.AddRange(sourceFiles)
            End If

            If embeddedFiles.Count > 0 Then
                ' Restricted to portable PDBs for now, but the IsPortable condition should be removed
                ' And the error message adjusted accordingly when native PDB support Is added.
                If Not emitPdb OrElse Not debugInformationFormat.IsPortable() Then
                    AddDiagnostic(diagnostics, ERRID.ERR_CannotEmbedWithoutPdb)
                End If
            End If

            ' Validate root namespace if specified
            Debug.Assert(rootNamespace IsNot Nothing)
            ' NOTE: empty namespace is a valid option
            If Not String.Empty.Equals(rootNamespace) Then
                rootNamespace = rootNamespace.Unquote()
                If String.IsNullOrWhiteSpace(rootNamespace) OrElse Not OptionsValidator.IsValidNamespaceName(rootNamespace) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_BadNamespaceName1, rootNamespace)
                    rootNamespace = "" ' To make it pass compilation options' check
                End If
            End If

            ' Dev10 searches for the keyfile in the current directory and assembly output directory.
            ' We always look to base directory and then examine the search paths.
            Paths.KeyFileSearch.Add(baseDirectory)
            If baseDirectory <> Output.Directory Then
                Paths.KeyFileSearch.Add(Output.Directory)
            End If

            Dim parsedFeatures = ParseFeatures(features)

            Dim compilationName As String = Nothing
            GetCompilationAndModuleNames(diagnostics, Output.Kind, sourceFiles, moduleAssemblyName, Output.FileName, moduleName, compilationName)

            If Not IsScriptRunner AndAlso
                Not hasSourceFiles AndAlso
                Not managedResources.IsEmpty() AndAlso
                Output.FileName = Nothing AndAlso
                Not flattenedArgs.IsEmpty() Then

                AddDiagnostic(diagnostics, ERRID.ERR_NoSourcesOut)
            End If

            Dim parseOptions = New VisualBasicParseOptions(
                languageVersion:=languageVersion,
                documentationMode:=If(_Documentation.ParseComments, DocumentationMode.Diagnose, DocumentationMode.None),
                kind:=SourceCodeKind.Regular,
                preprocessorSymbols:=AddPredefinedPreprocessorSymbols(Output.Kind, defines.AsImmutableOrEmpty()),
                features:=parsedFeatures)

            Dim scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script)

            ' We want to report diagnostics with source suppression in the error log file.
            ' However, these diagnostics won't be reported on the command line.
            Dim reportSuppressedDiagnostics = errorLogPath IsNot Nothing

            Dim options = New VisualBasicCompilationOptions(
                outputKind:=Output.Kind,
                moduleName:=moduleName,
                mainTypeName:=mainTypeName,
                scriptClassName:=WellKnownMemberNames.DefaultScriptClassName,
                globalImports:=globalImports,
                rootNamespace:=rootNamespace,
                 optionStrict:=_option.Strict,
                optionInfer:=_option.Infer,
                optionExplicit:=_option.Explicit,
                optionCompareText:=_option.CompareText,
                embedVbCoreRuntime:=_VBRuntime.EmbedCore,
                checkOverflow:=checkOverflow,
                concurrentBuild:=concurrentBuild,
                deterministic:=deterministic,
                cryptoKeyContainer:=keyContainerSetting,
                cryptoKeyFile:=keyFileSetting,
                delaySign:=delaySignSetting,
                publicSign:=publicSign,
                platform:=platform,
                generalDiagnosticOption:=generalDiagnosticOption,
                specificDiagnosticOptions:=specificDiagnosticOptions,
                optimizationLevel:=If(optimize, OptimizationLevel.Release, OptimizationLevel.Debug),
                parseOptions:=parseOptions,
                reportSuppressedDiagnostics:=reportSuppressedDiagnostics)

            Dim emitOptions = New EmitOptions(
                metadataOnly:=False,
                debugInformationFormat:=debugInformationFormat,
                pdbFilePath:=Nothing, ' to be determined later
                outputNameOverride:=Nothing,  ' to be determined later
                fileAlignment:=fileAlignment,
                baseAddress:=baseAddress,
                highEntropyVirtualAddressSpace:=highEntropyVA,
                subsystemVersion:=ssVersion,
                runtimeMetadataVersion:=Nothing,
                instrumentationKinds:=instrumentationKinds.ToImmutableAndFree())

            ' add option incompatibility errors if any
            diagnostics.AddRange(options.Errors)

            If _Documentation._Path Is GenerateFileNameForDocComment Then
                _Documentation._Path = PathUtilities.CombineAbsoluteAndRelativePaths(Output.Directory, PathUtilities.RemoveExtension(Output.FileName))
                _Documentation._Path = _Documentation._Path & ".xml"
            End If

            ' Enable interactive mode if either `\i` option is passed in or no arguments are specified (`vbi`, `vbi script.vbx \i`).
            ' If the script is passed without the `\i` option simply execute the script (`vbi script.vbx`).
            interactiveMode = interactiveMode Or (IsScriptRunner AndAlso sourceFiles.Count = 0)

            Return New VisualBasicCommandLineArguments With
            {
                .IsScriptRunner = IsScriptRunner,
                .InteractiveMode = interactiveMode,
                .BaseDirectory = baseDirectory,
                .Errors = diagnostics.AsImmutable(),
                .Utf8Output = Output.UTF8,
                .CompilationName = compilationName,
                .OutputFileName = Output.FileName,
                .OutputDirectory = Output.Directory,
                .DocumentationPath = _Documentation._Path,
                .ErrorLogPath = errorLogPath,
                .SourceFiles = sourceFiles.AsImmutable(),
                .PathMap = pathMap,
                .Encoding = codepage,
                .ChecksumAlgorithm = checksumAlgorithm,
                .MetadataReferences = metadataReferences.AsImmutable(),
                .AnalyzerReferences = analyzers.AsImmutable(),
                .AdditionalFiles = additionalFiles.AsImmutable(),
                .ReferencePaths = searchPaths,
                .SourcePaths = Paths.Source.AsImmutable(),
                .KeyFileSearchPaths = Paths.KeyFileSearch.AsImmutable(),
                .Win32ResourceFile = win32ResourceFile,
                .Win32Icon = win32IconFile,
                .Win32Manifest = win32ManifestFile,
                .NoWin32Manifest = noWin32Manifest,
                .DisplayLogo = display.Logo,
                .DisplayHelp = display.Help,
                .DisplayVersion = display.Version,
                .ManifestResources = managedResources.AsImmutable(),
                .CompilationOptions = options,
                .ParseOptions = If(IsScriptRunner, scriptParseOptions, parseOptions),
                .EmitOptions = emitOptions,
                .ScriptArguments = scriptArgs.AsImmutableOrEmpty(),
                .TouchedFilesPath = touchedFilesPath,
                .OutputLevel = outputLevel,
                .EmitPdb = emitPdb,
                .SourceLink = sourceLink,
                .DefaultCoreLibraryReference = defaultCoreLibraryReference,
                .PreferredUILang = preferredUILang,
                .ReportAnalyzer = reportAnalyzer,
                .EmbeddedFiles = embeddedFiles.AsImmutable()
            }
        End Function

        Private Shared Function Parse_NoWarn(ByRef generalDiagnosticOption As ReportDiagnostic, specificDiagnosticOptionsFromRuleSet As Dictionary(Of String, ReportDiagnostic), specificDiagnosticOptionsFromGeneralArguments As Dictionary(Of String, ReportDiagnostic), specificDiagnosticOptionsFromNoWarnArguments As Dictionary(Of String, ReportDiagnostic), value As String) As FlagParse
            If value Is Nothing Then
                generalDiagnosticOption = ReportDiagnostic.Suppress

                specificDiagnosticOptionsFromGeneralArguments.Clear()
                For Each pair In specificDiagnosticOptionsFromRuleSet
                    If pair.Value <> ReportDiagnostic.Error Then
                        specificDiagnosticOptionsFromGeneralArguments.Add(pair.Key, ReportDiagnostic.Suppress)
                    End If
                Next
            Else
                AddWarnings(specificDiagnosticOptionsFromNoWarnArguments, ReportDiagnostic.Suppress, ParseWarnings(value))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_WarnAsError_Minus(ByRef generalDiagnosticOption As ReportDiagnostic, specificDiagnosticOptionsFromRuleSet As Dictionary(Of String, ReportDiagnostic), specificDiagnosticOptionsFromGeneralArguments As Dictionary(Of String, ReportDiagnostic), ByRef specificDiagnosticOptionsFromSpecificArguments As Dictionary(Of String, ReportDiagnostic), value As String) As FlagParse
            If value Is Nothing Then
                If generalDiagnosticOption <> ReportDiagnostic.Suppress Then
                    generalDiagnosticOption = ReportDiagnostic.Default
                End If
                specificDiagnosticOptionsFromGeneralArguments.Clear()
            Else
                For Each id In ParseWarnings(value)
                    Dim ruleSetValue As ReportDiagnostic
                    If specificDiagnosticOptionsFromRuleSet.TryGetValue(id, ruleSetValue) Then
                        specificDiagnosticOptionsFromSpecificArguments(id) = ruleSetValue
                    Else
                        specificDiagnosticOptionsFromSpecificArguments(id) = ReportDiagnostic.Default
                    End If
                Next
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_WarnAsError(ByRef generalDiagnosticOption As ReportDiagnostic, specificDiagnosticOptionsFromRuleSet As Dictionary(Of String, ReportDiagnostic), specificDiagnosticOptionsFromGeneralArguments As Dictionary(Of String, ReportDiagnostic), specificDiagnosticOptionsFromSpecificArguments As Dictionary(Of String, ReportDiagnostic), value As String) As FlagParse
            If value Is Nothing Then
                generalDiagnosticOption = ReportDiagnostic.Error

                specificDiagnosticOptionsFromGeneralArguments.Clear()
                For Each pair In specificDiagnosticOptionsFromRuleSet
                    If pair.Value = ReportDiagnostic.Warn Then
                        specificDiagnosticOptionsFromGeneralArguments.Add(pair.Key, ReportDiagnostic.Error)
                    End If
                Next
            Else
                AddWarnings(specificDiagnosticOptionsFromSpecificArguments, ReportDiagnostic.Error, ParseWarnings(value))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_OutputLevel(ByRef outputLevel As OutputLevel, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                Dim ch = name(0)
                Select Case ch
                    Case "q"c : outputLevel = VisualBasic.OutputLevel.Quiet
                    Case "v"c : outputLevel = VisualBasic.OutputLevel.Verbose
                End Select
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Private Shared Function Parse_Quiet(diagnostics As List(Of Diagnostic), ByRef outputLevel As OutputLevel, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "quiet")
            Else
                Dim param = name.Last
                Select Case param
                    Case "+"c : outputLevel = VisualBasic.OutputLevel.Quiet
                    Case "-"c : outputLevel = VisualBasic.OutputLevel.Normal
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Verbose(diagnostics As List(Of Diagnostic), ByRef outputLevel As OutputLevel, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, name.Substring(0, name.Length - 1))
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "-"c : outputLevel = VisualBasic.OutputLevel.Normal
                    Case "+"c : outputLevel = VisualBasic.OutputLevel.Verbose
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_HighEntropyVA(ByRef highEntropyVA As Boolean, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "a"c,
                         "+"c : highEntropyVA = True
                    Case "-"c : highEntropyVA = False
                End Select
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Private Shared Function Parse_KeyFile(diagnostics As List(Of Diagnostic), ByRef keyFileSetting As String, ByRef keyContainerSetting As String, name As String, value As String) As FlagParse
            Return Parse_Key_(diagnostics, keyFileSetting, keyContainerSetting, name, value, ":<file>")
        End Function

        Private Shared Function Parse_KeyContainer(diagnostics As List(Of Diagnostic), ByRef keyFileSetting As String, ByRef keyContainerSetting As String, name As String, value As String) As FlagParse
            Return Parse_Key_(diagnostics, keyFileSetting, keyContainerSetting, name, value, ":<string>")
        End Function

        Private Shared Function Parse_Key_(diagnostics As List(Of Diagnostic),
                                           ByRef keyFileSetting As String,
                                           ByRef keyContainerSetting As String,
                                           name As String,
                                           value As String, arg As String) As FlagParse

            ' NOTE: despite what MSDN says, Dev11 resets '/keycontainer' in this case:
            '
            ' MSDN: In case both /keyfile and /keycontainer are specified (either by command-line 
            ' MSDN: option or by custom attribute) in the same compilation, the compiler first tries 
            ' MSDN: the key container. If that succeeds, then the assembly is signed with the 
            ' MSDN: information in the key container. If the compiler does not find the key container, 
            ' MSDN: it tries the file specified with /keyfile. If this succeeds, the assembly is 
            ' MSDN: signed with the information in the key file, and the key information is installed 
            ' MSDN: in the key container (similar to sn -i) so that on the next compilation, 
            ' MSDN: the key container will be valid.
            value = RemoveQuotesAndSlashes(value)
            keyContainerSetting = Nothing
            If String.IsNullOrWhiteSpace(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, arg)
            Else
                keyFileSetting = RemoveQuotesAndSlashes(value)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function


        Private Shared Function Parse_PublicSign(diagnostics As List(Of Diagnostic), ByRef publicSign As Boolean, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "publicsign")
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "n"c,
                         "+"c : publicSign = True
                    Case "-"c : publicSign = False
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Deterministic(diagnostics As List(Of Diagnostic), ByRef deterministic As Boolean, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, name)
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "c"c,
                         "+"c : deterministic = True
                    Case "-"c : deterministic = False
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_NoLogo(ByRef display As (Logo As Boolean, Help As Boolean, Version As Boolean), name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "o"c,
                         "+"c : display.Logo = False
                    Case "-"c : display.Logo = True
                End Select
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Private Shared Function Parse_UTF8_Output(diagnostics As List(Of Diagnostic), ByRef Output As (UTF8 As Boolean, FileName As String, Directory As String, Kind As OutputKind), name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "utf8output")
            End If
            Dim ch = name.Last
            Select Case ch
                Case "t"c,
                     "+"c : Output.UTF8 = True
                Case "-"c : Output.UTF8 = False
            End Select
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_DelaySign(diagnostics As List(Of Diagnostic), ByRef delaySignSetting As Boolean?, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "delaysign")
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "n"c,
                         "+"c : delaySignSetting = True
                    Case "-"c : delaySignSetting = False
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_LangVersion(diagnostics As List(Of Diagnostic), ByRef languageVersion As LanguageVersion, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If value Is Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "langversion", ":<number>")
            Else
                If String.IsNullOrEmpty(value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "langversion", ":<number>")
                Else
                    Select Case value.ToLowerInvariant()
                        Case "9", "9.0"
                            languageVersion = LanguageVersion.VisualBasic9
                        Case "10", "10.0"
                            languageVersion = LanguageVersion.VisualBasic10
                        Case "11", "11.0"
                            languageVersion = LanguageVersion.VisualBasic11
                        Case "12", "12.0"
                            languageVersion = LanguageVersion.VisualBasic12
                        Case "14", "14.0"
                            languageVersion = LanguageVersion.VisualBasic14
                        Case "15", "15.0"
                            languageVersion = LanguageVersion.VisualBasic15
                        Case "default"
                            languageVersion = LanguageVersion.Default
                        Case "latest"
                            languageVersion = LanguageVersion.Latest
                        Case Else
                            AddDiagnostic(diagnostics, ERRID.ERR_InvalidSwitchValue, "langversion", value)
                    End Select
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Parallel(diagnostics As List(Of Diagnostic), ByRef concurrentBuild As Boolean, name As String, value As String) As FlagParse
            Dim x = name.Length
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, If((x = 1 OrElse x = 8), name, name.Substring(0, name.Length - 1)))
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "l"c,
                         "p"c,
                         "+"c : concurrentBuild = True
                    Case "-"c : concurrentBuild = False
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Main(diagnostics As List(Of Diagnostic), ByRef mainTypeName As String, name As String, ByRef value As String) As FlagParse
            ' MSBuild can result in maintypename being passed in quoted when Cyrillic namespace was being used resulting
            ' in ERRID.ERR_StartupCodeNotFound1 diagnostic.   The additional quotes cause problems and quotes are not a 
            ' valid character in typename.
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<class>")
            Else
                mainTypeName = value
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Features(features As List(Of String), value As String) As FlagParse
            If value Is Nothing Then
                features.Clear()
            Else
                features.Add(RemoveQuotesAndSlashes(value))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_AdditionalFile(baseDirectory As String, diagnostics As List(Of Diagnostic), additionalFiles As List(Of CommandLineSourceFile), name As String, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file_list>")
            Else
                additionalFiles.AddRange(ParseSeparatedFileArgument(value, baseDirectory, diagnostics))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_Embed(baseDirectory As String, diagnostics As List(Of Diagnostic), embeddedFiles As List(Of CommandLineSourceFile), ByRef embedAllSourceFiles As Boolean, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                embedAllSourceFiles = True
            Else
                embeddedFiles.AddRange(ParseSeparatedFileArgument(value, baseDirectory, diagnostics))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_PathMap(diagnostics As List(Of Diagnostic), ByRef pathMap As ImmutableArray(Of KeyValuePair(Of String, String)), value As String) As FlagParse
            ' "/pathmap:K1=V1,K2=V2..."
            If value = Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                pathMap = pathMap.Concat(ParsePathMap(value, diagnostics))
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Private Shared Function Parse_SubSystemVersion(diagnostics As List(Of Diagnostic), ByRef ssVersion As SubsystemVersion, name As String, ByRef value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<version>")
            Else
                Dim version As SubsystemVersion = Nothing
                If SubsystemVersion.TryParse(value, version) Then
                    ssVersion = version
                Else
                    AddDiagnostic(diagnostics, ERRID.ERR_InvalidSubsystemVersion, value)
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_TouchedFiles(diagnostics As List(Of Diagnostic), ByRef touchedFilesPath As String, name As String, value As String) As FlagParse
            Dim unquoted = RemoveQuotesAndSlashes(value)
            If (String.IsNullOrEmpty(unquoted)) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<touchedfiles>")
            Else
                touchedFilesPath = unquoted
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_NoSTDLib(ByRef noStdLib As Boolean, value As String) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                noStdLib = True
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Private Shared Function Parse_Platform(diagnostics As List(Of Diagnostic), ByRef platform As Platform, name As String, ByRef value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If value IsNot Nothing Then
                platform = ParsePlatform(name, value, diagnostics)
            Else
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "platform", ":<string>")
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Optimize(diagnostics As List(Of Diagnostic), ByRef optimize As Boolean, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optimize")
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "e"c,
                         "+"c : optimize = True
                    Case "-"c : optimize = False
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Debug(diagnostics As List(Of Diagnostic), ByRef emitPdb As Boolean, ByRef debugInformationFormat As DebugInformationFormat, value As String) As FlagParse
            ' parse only for backwards compat
            value = RemoveQuotesAndSlashes(value)
            If value IsNot Nothing Then
                Select Case value.ToLower()
                    Case "full", "pdbonly"
                        debugInformationFormat = If(PathUtilities.IsUnixLikePlatform, DebugInformationFormat.PortablePdb, DebugInformationFormat.Pdb)
                    Case "portable"
                        debugInformationFormat = DebugInformationFormat.PortablePdb
                    Case "embedded"
                        debugInformationFormat = DebugInformationFormat.Embedded
                    Case Else
                        AddDiagnostic(diagnostics, ERRID.ERR_InvalidSwitchValue, "debug", value)
                End Select
            End If

            emitPdb = True
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Debug(diagnostics As List(Of Diagnostic), ByRef emitPdb As Boolean, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "debug")
            End If
            Dim ch = name.Last
            Select Case ch
                Case "+"c : emitPdb = True
                Case "-"c : emitPdb = False

            End Select
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_SourceLink(baseDirectory As String, diagnostics As List(Of Diagnostic), ByRef sourceLink As String, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "sourcelink", ":<file>")
            Else
                sourceLink = ParseGenericPathToFile(value, diagnostics, baseDirectory)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_LinkResource(baseDirectory As String, diagnostics As List(Of Diagnostic), managedResources As List(Of ResourceDescription), name As String, value As String) As FlagParse
            Dim linkedResource = ParseResourceDescription(name, value, baseDirectory, diagnostics, embedded:=False)
            If linkedResource IsNot Nothing Then
                managedResources.Add(linkedResource)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Resource(baseDirectory As String, diagnostics As List(Of Diagnostic), managedResources As List(Of ResourceDescription), name As String, value As String) As FlagParse
            Dim embeddedResource = ParseResourceDescription(name, value, baseDirectory, diagnostics, embedded:=True)
            If embeddedResource IsNot Nothing Then
                managedResources.Add(embeddedResource)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_NoWin32_Manifest(ByRef noWin32Manifest As Boolean, value As String) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                noWin32Manifest = True
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Private Shared Function Parse_Win32_Manifest(diagnostics As List(Of Diagnostic), ByRef win32ManifestFile As String, value As String) As FlagParse
            win32ManifestFile = GetWin32Setting(s_win32Manifest, RemoveQuotesAndSlashes(value), diagnostics)
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Win32_Icon(diagnostics As List(Of Diagnostic), ByRef win32IconFile As String, value As String) As FlagParse
            win32IconFile = GetWin32Setting(s_win32Icon, RemoveQuotesAndSlashes(value), diagnostics)
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Link(diagnostics As List(Of Diagnostic), metadataReferences As List(Of CommandLineReference), result As FlagParse, name As String, value As String) As FlagParse
            metadataReferences.AddRange(ParseAssemblyReferences(name, value, diagnostics, embedInteropTypes:=True))
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Win32_Resource(diagnostics As List(Of Diagnostic), ByRef win32ResourceFile As String, value As String) As FlagParse
            win32ResourceFile = GetWin32Setting(s_win32Res, RemoveQuotesAndSlashes(value), diagnostics)
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_AddModule(diagnostics As List(Of Diagnostic), metadataReferences As List(Of CommandLineReference), value As String) As FlagParse
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "addmodule", ":<file_list>")
            Else

                ' NOTE(tomat): Dev10 reports "Command line error BC2017 : could not find library."
                ' Since we now support /referencePaths option we would need to search them to see if the resolved path is a directory.
                ' An error will be reported by the assembly manager anyways.
                metadataReferences.AddRange(
                    ParseSeparatedPaths(value).Select(
                        Function(path) New CommandLineReference(path, New MetadataReferenceProperties(MetadataImageKind.Module))))

            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_Recurse(baseDirectory As String, diagnostics As List(Of Diagnostic), sourceFiles As List(Of CommandLineSourceFile), ByRef hasSourceFiles As Boolean, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "recurse", ":<wildcard>")
            Else
                Dim before As Integer = sourceFiles.Count
                sourceFiles.AddRange(ParseRecurseArgument(value, baseDirectory, diagnostics))
                If sourceFiles.Count > before Then
                    hasSourceFiles = True
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Instrument(diagnostics As List(Of Diagnostic), instrumentationKinds As ArrayBuilder(Of InstrumentationKind), value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "instrument", ":<string>")
            Else

                For Each instrumentationKind As InstrumentationKind In ParseInstrumentationKinds(value, diagnostics)
                    If Not instrumentationKinds.Contains(instrumentationKind) Then
                        instrumentationKinds.Add(instrumentationKind)
                    End If
                Next
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_SDKPath(diagnostics As List(Of Diagnostic), Paths As (SDK As List(Of String), [LIB] As List(Of String), Source As List(Of String), KeyFileSearch As List(Of String), Response As List(Of String)), value As String) As FlagParse
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "sdkpath", ":<path>")
            Else
                Paths.SDK.Clear()
                Paths.SDK.AddRange(ParseSeparatedPaths(value))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_ErrorLog(baseDirectory As String, diagnostics As List(Of Diagnostic), ByRef errorLogPath As String, value As String) As FlagParse
            Dim unquoted = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(unquoted) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "errorlog", ":<file>")
            Else
                errorLogPath = ParseGenericPathToFile(unquoted, diagnostics, baseDirectory)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_Doc(
                                    baseDirectory As String,
                                    GenerateFileNameForDocComment As String,
                                    diagnostics As List(Of Diagnostic),
                              ByRef _Documentation As (_Path As String, ParseComments As Boolean),
                                    value As String
                                  ) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            _Documentation.ParseComments = True
            If value Is Nothing Then
                ' Illegal in C#, but works in VB
                _Documentation._Path = GenerateFileNameForDocComment
            Else
                Dim unquoted = RemoveQuotesAndSlashes(value)
                If unquoted.Length = 0 Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "doc", ":<file>")
                Else
                    _Documentation._Path = ParseGenericPathToFile(unquoted, diagnostics, baseDirectory, generateDiagnostic:=False)
                    If String.IsNullOrWhiteSpace(_Documentation._Path) Then
                        AddDiagnostic(diagnostics, ERRID.WRN_XMLCannotWriteToXMLDocFile2, unquoted, New LocalizableErrorArgument(ERRID.IDS_TheSystemCannotFindThePathSpecified))
                        _Documentation._Path = Nothing
                    End If
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Doc(
                                           GenerateFileNameForDocComment As String,
                                           diagnostics As List(Of Diagnostic),
                                     ByRef _Documentation As (_Path As String, ParseComments As Boolean),
                                           name As String,
                                           value As String
                                         ) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "doc")
            End If
            Dim ch = name.Last
            Select Case ch
                Case "+"c
                    ' Seems redundant with default values, but we need to clobber any preceding /doc switches
                    _Documentation = (GenerateFileNameForDocComment, True)
                Case "-"c
                    ' Seems redundant with default values, but we need to clobber any preceding /doc switches
                    _Documentation = (Nothing, False)
            End Select
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_RootNamespace(diagnostics As List(Of Diagnostic), ByRef rootNamespace As String, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "rootnamespace", ":<string>")
            Else
                rootNamespace = value
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_VBRuntime(ByRef _VBRuntime As (_Path As String, IncludeReference As Boolean, EmbedCore As Boolean), value As String) As FlagParse
            If value Is Nothing Then
                _VBRuntime = (_Path:=Nothing, IncludeReference:=True, EmbedCore:=False)
            Else
                ' NOTE: that Dev11 does not report errors on empty or invalid file specified
                _VBRuntime = (_Path:=RemoveQuotesAndSlashes(value), IncludeReference:=True, EmbedCore:=False)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_VBRuntime(ByRef _VBRuntime As (_Path As String, IncludeReference As Boolean, EmbedCore As Boolean), name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                Return FlagParse.Invalid_ExitSelect
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "+"c : _VBRuntime = (_Path:=Nothing, IncludeReference:=True, EmbedCore:=False)
                    Case "-"c : _VBRuntime = (_Path:=Nothing, IncludeReference:=False, EmbedCore:=False)
                    Case "*"c : _VBRuntime = (_Path:=Nothing, IncludeReference:=False, EmbedCore:=True)
                End Select
                Return FlagParse.Valid_ContinueFor
            End If
        End Function

        Private Shared Function Parse_ModuleAssemblyName(diagnostics As List(Of Diagnostic), ByRef moduleAssemblyName As String, arg As String, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            Dim identity As AssemblyIdentity = Nothing

            ' Note that native compiler also extracts public key, but Roslyn doesn't use it.

            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "moduleassemblyname", ":<string>")
            ElseIf Not AssemblyIdentity.TryParseDisplayName(value, identity) OrElse
                       Not MetadataHelpers.IsValidAssemblyOrModuleName(identity.Name) Then
                AddDiagnostic(diagnostics, ERRID.ERR_InvalidAssemblyName, value, arg)
            Else
                moduleAssemblyName = identity.Name
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Target(diagnostics As List(Of Diagnostic), ByRef Output As (UTF8 As Boolean, FileName As String, Directory As String, Kind As OutputKind), name As String, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            Output.Kind = ParseTarget(name, value, diagnostics)
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function Parse_Out(baseDirectory As String, diagnostics As List(Of Diagnostic), ByRef Output As (UTF8 As Boolean, FileName As String, Directory As String, Kind As OutputKind), name As String, value As String) As FlagParse
            If String.IsNullOrWhiteSpace(value) Then
                ' When the value has " " (e.g., "/out: ")
                ' the Roslyn VB compiler reports "BC 2006 : option 'out' requires ':<file>',
                ' While the Dev11 VB compiler reports "BC2012 : can't open ' ' for writing,
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file>")
            Else
                ' Even when value is neither null or whitespace, the output file name still could be invalid. (e.g., "/out:sub\ ")
                ' While the Dev11 VB compiler reports "BC2012: can't open 'sub\ ' for writing,
                ' the Roslyn VB compiler reports "BC2032: File name 'sub\ ' is empty, contains invalid characters, ..."
                ' which is generated by the following ParseOutputFile.
                ParseOutputFile(value, diagnostics, baseDirectory, Output.FileName, Output.Directory)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_LoadPaths(diagnostics As List(Of Diagnostic),
                                                Paths As (SDK As List(Of String), [LIB] As List(Of String), Source As List(Of String), KeyFileSearch As List(Of String), Response As List(Of String)),
                                                name As String, value As String) As FlagParse
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<path_list>")
            Else
                Paths.Source.AddRange(ParseSeparatedPaths(value))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_interactive(diagnostics As List(Of Diagnostic), ByRef interactiveMode As Boolean, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "i")
            End If
            Dim ch = name.Last
            Select Case ch
                Case "i"c,
                     "+"c : interactiveMode = True
                Case "-"c : interactiveMode = False
            End Select
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_LibPath(diagnostics As List(Of Diagnostic), Paths As (SDK As List(Of String), [LIB] As List(Of String), Source As List(Of String), KeyFileSearch As List(Of String), Response As List(Of String)), name As String, value As String) As FlagParse
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<path_list>")
            Else
                Paths.LIB.AddRange(ParseSeparatedPaths(value))
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_PreferredUILang(diagnostics As List(Of Diagnostic), ByRef preferredUILang As CultureInfo, name As String, ByRef value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If (String.IsNullOrEmpty(value)) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<string>")
            Else
                Try
                    preferredUILang = New CultureInfo(value)
                    If (CorLightup.Desktop.IsUserCustomCulture(preferredUILang)) Then
                        ' Do not use user custom cultures.
                        preferredUILang = Nothing
                    End If
                Catch ex As CultureNotFoundException
                End Try

                If preferredUILang Is Nothing Then
                    AddDiagnostic(diagnostics, ERRID.WRN_BadUILang, value)
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_SQMSessionGuid(diagnostics As List(Of Diagnostic), name As String, ByRef value As String) As FlagParse
            ' The use of SQM is deprecated in the compiler but we still support the command line parsing for 
            ' back compat reasons.
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrWhiteSpace(value) = True Then
                AddDiagnostic(diagnostics, ERRID.ERR_MissingGuidForOption, value, name)
            Else
                Dim sqmsessionguid As Guid
                If Not Guid.TryParse(value, sqmsessionguid) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_InvalidFormatForGuidForOption, value, name)
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_RemoveIntChecks(diagnostics As List(Of Diagnostic), ByRef checkOverflow As Boolean, name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "removeintchecks")
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "s"c,
                         "+"c : checkOverflow = False
                    Case "-"c : checkOverflow = True
                    Case Else
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_ChecksumAlgorithm(diagnostics As List(Of Diagnostic), ByRef checksumAlgorithm As SourceHashAlgorithm, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "checksumalgorithm", ":<algorithm>")
            Else

                Dim newChecksumAlgorithm = TryParseHashAlgorithmName(value)
                If newChecksumAlgorithm = SourceHashAlgorithm.None Then
                    AddDiagnostic(diagnostics, ERRID.ERR_BadChecksumAlgorithm, value)
                Else
                    checksumAlgorithm = newChecksumAlgorithm
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_CodePage(diagnostics As List(Of Diagnostic), ByRef codepage As Encoding, name As String, value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "codepage", ":<number>")
            Else
                Dim encoding = TryParseEncodingName(value)
                If encoding Is Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_BadCodepage, value)
                Else
                    codepage = encoding
                End If
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Option_Infer(diagnostics As List(Of Diagnostic), ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean), name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optioninfer")
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "r"c,
                         "+"c : _option.Infer = True
                    Case "-"c : _option.Infer = False
                    Case Else
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Option_Compare(diagnostics As List(Of Diagnostic), ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean), value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If value Is Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "optioncompare", ":binary|text")
            ElseIf String.Equals(value, "text", StringComparison.OrdinalIgnoreCase) Then
                _option.CompareText = True
            ElseIf String.Equals(value, "binary", StringComparison.OrdinalIgnoreCase) Then
                _option.CompareText = False
            Else
                AddDiagnostic(diagnostics, ERRID.ERR_InvalidSwitchValue, "optioncompare", value)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Option_Strict(diagnostics As List(Of Diagnostic), ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean), name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optionstrict")
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "+"c : _option.Strict = VisualBasic.OptionStrict.On
                    Case "-"c : _option.Strict = VisualBasic.OptionStrict.Off
                    Case Else
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Option_Strict(diagnostics As List(Of Diagnostic), ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean), value As String) As FlagParse
            value = RemoveQuotesAndSlashes(value)
            If value Is Nothing Then
                _option.Strict = VisualBasic.OptionStrict.On
            ElseIf String.Equals(value, "custom", StringComparison.OrdinalIgnoreCase) Then
                _option.Strict = VisualBasic.OptionStrict.Custom
            Else
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "optionstrict", ":custom")
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Option_Explicit(diagnostics As List(Of Diagnostic), ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean), name As String, value As String) As FlagParse
            If value IsNot Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optionexplicit")
            Else
                Dim ch = name.Last
                Select Case ch
                    Case "t"c,
                         "+"c : _option.Explicit = True
                    Case "-"c : _option.Explicit = False
                    Case Else
                End Select
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Imports(diagnostics As List(Of Diagnostic), ByRef globalImports As List(Of GlobalImport), name As String, value As String) As FlagParse
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, If(name = "import", ":<str>", ":<import_list>"))
            Else
                ParseGlobalImports(value, globalImports, diagnostics)
            End If
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Define(diagnostics As List(Of Diagnostic), ByRef defines As IReadOnlyDictionary(Of String, Object), name As String, value As String) As FlagParse
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<symbol_list>")
            Else
                Dim conditionalCompilationDiagnostics As IEnumerable(Of Diagnostic) = Nothing
                defines = ParseConditionalCompilationSymbols(value, conditionalCompilationDiagnostics, defines)
                diagnostics.AddRange(conditionalCompilationDiagnostics)
            End If

            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Analyzer(diagnostics As List(Of Diagnostic), analyzers As List(Of CommandLineAnalyzerReference), name As String, value As String) As FlagParse
            analyzers.AddRange(ParseAnalyzers(name, value, diagnostics))
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Shared Function Parse_Reference(diagnostics As List(Of Diagnostic), metadataReferences As List(Of CommandLineReference), name As String, value As String) As FlagParse
            metadataReferences.AddRange(ParseAssemblyReferences(name, value, diagnostics, embedInteropTypes:=False))
            Return FlagParse.Valid_ContinueFor
        End Function

        Private Function LoadCoreLibraryReference(sdkPaths As List(Of String), baseDirectory As String) As CommandLineReference?
            ' Load Core library in Dev11:
            ' Traditionally VB compiler has hard-coded the name of mscorlib.dll. In the Immersive profile the
            ' library is called System.Runtime.dll. Ideally we should get rid of the dependency on the name and
            ' identify the core library as the assembly that contains System.Object. At this point in the compiler,
            ' it is too early though as we haven't loaded any types or assemblies. Changing this now is a deep 
            ' change. So the workaround here is to allow mscorlib or system.runtime and prefer system.runtime if present.
            ' There is an extra check to only pick an assembly with no other assembly refs. This is so that is an 
            ' user drops a user-defined binary called System.runtime.dll into the fx directory we still want to pick 
            ' mscorlib. 
            Dim msCorLibPath As String = FindFileInSdkPath(sdkPaths, "mscorlib.dll", baseDirectory)
            Dim systemRuntimePath As String = FindFileInSdkPath(sdkPaths, "System.Runtime.dll", baseDirectory)

            If systemRuntimePath IsNot Nothing Then
                If msCorLibPath Is Nothing Then
                    Return New CommandLineReference(systemRuntimePath, New MetadataReferenceProperties(MetadataImageKind.Assembly))
                End If

                ' Load System.Runtime.dll and see if it has any references
                Try
                    Using metadata = AssemblyMetadata.CreateFromFile(systemRuntimePath)
                        ' Prefer 'System.Runtime.dll' if it does not have any references
                        If metadata.GetModules()(0).Module.IsLinkedModule AndAlso
                           metadata.GetAssembly().AssemblyReferences.Length = 0 Then
                            Return New CommandLineReference(systemRuntimePath, New MetadataReferenceProperties(MetadataImageKind.Assembly))
                        End If
                    End Using
                Catch
                    ' If we caught anything, there is something wrong with System.Runtime.dll and we fall back to mscorlib.dll
                End Try

                ' Otherwise prefer 'mscorlib.dll'
                Return New CommandLineReference(msCorLibPath, New MetadataReferenceProperties(MetadataImageKind.Assembly))
            End If

            If msCorLibPath IsNot Nothing Then
                ' We return a reference to 'mscorlib.dll'
                Return New CommandLineReference(msCorLibPath, New MetadataReferenceProperties(MetadataImageKind.Assembly))
            End If

            Return Nothing
        End Function

        Private Shared Function FindFileInSdkPath(sdkPaths As List(Of String), fileName As String, baseDirectory As String) As String
            For Each path In sdkPaths
                Debug.Assert(path IsNot Nothing)

                Dim absolutePath = FileUtilities.ResolveRelativePath(path, baseDirectory)
                If absolutePath IsNot Nothing Then
                    Dim filePath = PathUtilities.CombineAbsoluteAndRelativePaths(absolutePath, fileName)
                    If File.Exists(filePath) Then
                        Return filePath
                    End If
                End If
            Next
            Return Nothing
        End Function

        Private Shared Function GetWin32Setting(arg As String, value As String, diagnostics As List(Of Diagnostic)) As String
            If value Is Nothing Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, arg, ":<file>")
            Else
                Dim noQuotes As String = RemoveQuotesAndSlashes(value)
                If String.IsNullOrWhiteSpace(noQuotes) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, arg, ":<file>")
                Else
                    Return noQuotes
                End If
            End If

            Return Nothing
        End Function

        Private Shared Function BuildSearchPaths(baseDirectory As String, sdkPaths As List(Of String), responsePaths As List(Of String), libPaths As List(Of String)) As ImmutableArray(Of String)
            Dim builder = ArrayBuilder(Of String).GetInstance()

            ' Match how Dev11 builds the list of search paths
            '   see void GetSearchPath(CComBSTR& strSearchPath)

            ' current folder -- base directory is searched by default by the FileResolver

            ' SDK path is specified or current runtime directory
            AddNormalizedPaths(builder, sdkPaths, baseDirectory)

            ' Response file path, see the following comment from Dev11:
            '   // .NET FX 3.5 will have response file in the FX 3.5 directory but SdkPath will still be in 2.0 directory.
            '   // Therefore we need to make sure the response file directories are also on the search path
            '   // so response file authors can continue to use relative paths in the response files.
            builder.AddRange(responsePaths)

            ' libpath
            AddNormalizedPaths(builder, libPaths, baseDirectory)

            Return builder.ToImmutableAndFree()
        End Function

        Private Shared Sub AddNormalizedPaths(builder As ArrayBuilder(Of String), paths As List(Of String), baseDirectory As String)
            For Each path In paths
                Dim normalizedPath = FileUtilities.NormalizeRelativePath(path, basePath:=Nothing, baseDirectory:=baseDirectory)
                If normalizedPath Is Nothing Then
                    ' just ignore invalid paths, native compiler doesn't report any errors
                    Continue For
                End If

                builder.Add(normalizedPath)
            Next
        End Sub

        Private Shared Sub ValidateWin32Settings(noWin32Manifest As Boolean, win32ResSetting As String, win32IconSetting As String, win32ManifestSetting As String, outputKind As OutputKind, diagnostics As List(Of Diagnostic))
            If noWin32Manifest AndAlso (win32ManifestSetting IsNot Nothing) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ConflictingManifestSwitches)
            End If

            If win32ResSetting IsNot Nothing Then
                If win32IconSetting IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_IconFileAndWin32ResFile)
                End If

                If win32ManifestSetting IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_CantHaveWin32ResAndManifest)
                End If
            End If

            If win32ManifestSetting IsNot Nothing AndAlso outputKind.IsNetModule() Then
                AddDiagnostic(diagnostics, ERRID.WRN_IgnoreModuleManifest)
            End If
        End Sub

        Private Shared Function ParseTarget(optionName As String, value As String, diagnostics As IList(Of Diagnostic)) As OutputKind
            Select Case If(value, "").ToLowerInvariant()
                Case "exe"
                    Return OutputKind.ConsoleApplication
                Case "winexe"
                    Return OutputKind.WindowsApplication
                Case "library"
                    Return OutputKind.DynamicallyLinkedLibrary
                Case "module"
                    Return OutputKind.NetModule
                Case "appcontainerexe"
                    Return OutputKind.WindowsRuntimeApplication
                Case "winmdobj"
                    Return OutputKind.WindowsRuntimeMetadata
                Case ""
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, optionName, ":exe|winexe|library|module|appcontainerexe|winmdobj")
                    Return OutputKind.ConsoleApplication
                Case Else
                    AddDiagnostic(diagnostics, ERRID.ERR_InvalidSwitchValue, optionName, value)
                    Return OutputKind.ConsoleApplication
            End Select
        End Function

        Friend Shared Function ParseAssemblyReferences(name As String, value As String, diagnostics As IList(Of Diagnostic), embedInteropTypes As Boolean) As IEnumerable(Of CommandLineReference)
            If String.IsNullOrEmpty(value) Then
                ' TODO: localize <file_list>?
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file_list>")
                Return SpecializedCollections.EmptyEnumerable(Of CommandLineReference)()
            End If

            Return ParseSeparatedPaths(value).
                   Select(Function(path) New CommandLineReference(path, New MetadataReferenceProperties(MetadataImageKind.Assembly, embedInteropTypes:=embedInteropTypes)))
        End Function

        Private Shared Function ParseAnalyzers(name As String, value As String, diagnostics As IList(Of Diagnostic)) As IEnumerable(Of CommandLineAnalyzerReference)
            If String.IsNullOrEmpty(value) Then
                ' TODO: localize <file_list>?
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file_list>")
                Return SpecializedCollections.EmptyEnumerable(Of CommandLineAnalyzerReference)()
            End If

            Return ParseSeparatedPaths(value).
                   Select(Function(path)
                              Return New CommandLineAnalyzerReference(path)
                          End Function)
        End Function

        ' See ParseCommandLine in vbc.cpp.
        Friend Overloads Shared Function ParseResourceDescription(name As String, resourceDescriptor As String, baseDirectory As String, diagnostics As IList(Of Diagnostic), embedded As Boolean) As ResourceDescription
            If String.IsNullOrEmpty(resourceDescriptor) Then
                AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, name, ":<resinfo>")
                Return Nothing
            End If

            ' NOTE: these are actually passed to out parameters of .ParseResourceDescription.
            Dim filePath As String = Nothing
            Dim fullPath As String = Nothing
            Dim fileName As String = Nothing
            Dim resourceName As String = Nothing
            Dim accessibility As String = Nothing

            ParseResourceDescription(
                resourceDescriptor,
                baseDirectory,
                True,
                filePath,
                fullPath,
                fileName,
                resourceName,
                accessibility)

            If String.IsNullOrWhiteSpace(filePath) Then
                AddInvalidSwitchValueDiagnostic(diagnostics, name, filePath)
                Return Nothing
            End If

            If fullPath Is Nothing OrElse fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 Then
                AddDiagnostic(diagnostics, ERRID.FTL_InputFileNameTooLong, filePath)
                Return Nothing
            End If

            Dim isPublic As Boolean
            If String.IsNullOrEmpty(accessibility) Then
                ' If no accessibility is given, we default to "public".
                ' NOTE: Dev10 treats empty the same as null (the difference being that empty indicates a comma after the resource name).
                ' NOTE: Dev10 distinguishes between empty and whitespace-only.
                isPublic = True
            ElseIf String.Equals(accessibility, "public", StringComparison.OrdinalIgnoreCase) Then
                isPublic = True
            ElseIf String.Equals(accessibility, "private", StringComparison.OrdinalIgnoreCase) Then
                isPublic = False
            Else
                AddInvalidSwitchValueDiagnostic(diagnostics, name, accessibility)
                Return Nothing
            End If

            Dim dataProvider As Func(Of Stream) = Function()
                                                      ' Use FileShare.ReadWrite because the file could be opened by the current process.
                                                      ' For example, it Is an XML doc file produced by the build.
                                                      Return New FileStream(fullPath,
                                                                            FileMode.Open,
                                                                            FileAccess.Read,
                                                                            FileShare.ReadWrite)
                                                  End Function
            Return New ResourceDescription(resourceName, fileName, dataProvider, isPublic, embedded, checkArgs:=False)
        End Function

        Private Shared Sub AddInvalidSwitchValueDiagnostic(diagnostics As IList(Of Diagnostic), ByVal name As String, ByVal nullStringText As String)
            If String.IsNullOrEmpty(name) Then
                ' NOTE: "(null)" to match Dev10.
                ' CONSIDER: should this be a resource string?
                name = "(null)"
            End If

            AddDiagnostic(diagnostics, ERRID.ERR_InvalidSwitchValue, name, nullStringText)
        End Sub

        Private Shared Sub ParseGlobalImports(value As String, globalImports As List(Of GlobalImport), errors As List(Of Diagnostic))
            Dim importsArray = ParseSeparatedPaths(value)

            For Each importNamespace In importsArray
                Dim importDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
                Dim import = GlobalImport.Parse(importNamespace, importDiagnostics)
                errors.AddRange(importDiagnostics)
                globalImports.Add(import)
            Next
        End Sub

        ''' <summary>
        ''' Converts a sequence of definitions provided by a caller (public API) into map 
        ''' of definitions used internally.
        ''' </summary>
        ''' <exception cref="ArgumentException">Invalid value provided.</exception>
        Private Shared Function PublicSymbolsToInternalDefines(symbols As IEnumerable(Of KeyValuePair(Of String, Object)),
                                                               parameterName As String) As ImmutableDictionary(Of String, InternalSyntax.CConst)

            Dim result = ImmutableDictionary.CreateBuilder(Of String, InternalSyntax.CConst)(CaseInsensitiveComparison.Comparer)

            If symbols IsNot Nothing Then
                For Each symbol In symbols
                    Dim constant = InternalSyntax.CConst.TryCreate(symbol.Value)

                    If constant Is Nothing Then
                        Throw New ArgumentException(String.Format(ErrorFactory.IdToString(ERRID.IDS_InvalidPreprocessorConstantType, Culture), symbol.Key, symbol.Value.GetType()), parameterName)
                    End If

                    result(symbol.Key) = constant
                Next
            End If

            Return result.ToImmutable()
        End Function

        ''' <summary>
        ''' Converts ImmutableDictionary of definitions used internally into IReadOnlyDictionary of definitions 
        ''' returned to a caller (of public API)
        ''' </summary>
        Private Shared Function InternalDefinesToPublicSymbols(defines As ImmutableDictionary(Of String, InternalSyntax.CConst)) As IReadOnlyDictionary(Of String, Object)
            Dim result = ImmutableDictionary.CreateBuilder(Of String, Object)(CaseInsensitiveComparison.Comparer)

            For Each kvp In defines
                result(kvp.Key) = kvp.Value.ValueAsObject
            Next

            Return result.ToImmutable()
        End Function

        ''' <summary>
        ''' Parses Conditional Compilations Symbols.   Given the string of conditional compilation symbols from the project system, parse them and merge them with an IReadOnlyDictionary
        ''' ready to be given to the compilation.
        ''' </summary>
        ''' <param name="symbolList">
        ''' The conditional compilation string. This takes the form of a comma delimited list
        ''' of NAME=Value pairs, where Value may be a quoted string or integer.
        ''' </param>
        ''' <param name="diagnostics">A collection of reported diagnostics during parsing of symbolList, can be empty IEnumerable.</param>
        ''' <param name="symbols">A collection representing existing symbols. Symbols parsed from <paramref name="symbolList"/> will be merged with this dictionary. </param>
        ''' <exception cref="ArgumentException">Invalid value provided.</exception>
        Public Shared Function ParseConditionalCompilationSymbols(
            symbolList As String,
            <Out> ByRef diagnostics As IEnumerable(Of Diagnostic),
            Optional symbols As IEnumerable(Of KeyValuePair(Of String, Object)) = Nothing
        ) As IReadOnlyDictionary(Of String, Object)

            Dim diagnosticBuilder = ArrayBuilder(Of Diagnostic).GetInstance()
            Dim parsedTokensAsString As New StringBuilder

            Dim defines As ImmutableDictionary(Of String, InternalSyntax.CConst) = PublicSymbolsToInternalDefines(symbols, "symbols")

            ' remove quotes around the whole /define argument (incl. nested)
            Dim unquotedString As String
            Do
                unquotedString = symbolList
                symbolList = symbolList.Unquote()
            Loop While Not String.Equals(symbolList, unquotedString, StringComparison.Ordinal)

            ' unescape quotes \" -> "
            symbolList = symbolList.Replace("\""", """")

            Dim trimmedSymbolList As String = symbolList.TrimEnd(Nothing)
            If trimmedSymbolList.Length > 0 AndAlso IsConnectorPunctuation(trimmedSymbolList(trimmedSymbolList.Length - 1)) Then
                ' In case the symbol list ends with '_' we add ',' to the end of the list which in some 
                ' cases will produce an error 30999 to match Dev11 behavior
                symbolList = symbolList + ","
            End If

            ' In order to determine our conditional compilation symbols, we must parse the string we get from the
            ' project system. We take a cue from the legacy language services and use the VB scanner, since this string
            ' apparently abides by the same tokenization rules

            Dim tokenList = SyntaxFactory.ParseTokens(symbolList)

            Using tokens = tokenList.GetEnumerator()
                If tokens.MoveNext() Then
                    Do
                        ' This is the beginning of declaration like 'A' or 'A=123' with optional extra 
                        ' separators (',' or ':') in the beginning, if this is NOT the first declaration,
                        ' the tokens.Current should be either separator or EOF
                        If tokens.Current.Position > 0 AndAlso Not IsSeparatorOrEndOfFile(tokens.Current) Then
                            parsedTokensAsString.Append(" ^^ ^^ ")

                            ' Complete parsedTokensAsString until the next comma or end of stream
                            While Not IsSeparatorOrEndOfFile(tokens.Current)
                                parsedTokensAsString.Append(tokens.Current.ToFullString())
                                tokens.MoveNext()
                            End While

                            diagnosticBuilder.Add(
                                New DiagnosticWithInfo(
                                    ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                        ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedEOS),
                                        parsedTokensAsString.ToString),
                                    Location.None))

                            Exit Do
                        End If

                        Dim lastSeparatorToken As SyntaxToken = Nothing

                        ' If we're on a comma, it means there was an empty item in the list (item1,,item2),
                        ' so just eat it and move on...
                        While tokens.Current.Kind = SyntaxKind.CommaToken OrElse tokens.Current.Kind = SyntaxKind.ColonToken

                            If lastSeparatorToken.Kind = SyntaxKind.None Then
                                ' accept multiple : or ,
                                lastSeparatorToken = tokens.Current

                            ElseIf lastSeparatorToken.Kind <> tokens.Current.Kind Then
                                ' but not mixing them, e.g. ::,,::
                                GetErrorStringForRemainderOfConditionalCompilation(tokens, parsedTokensAsString, stopTokenKind:=lastSeparatorToken.Kind, includeCurrentToken:=True)

                                diagnosticBuilder.Add(
                                    New DiagnosticWithInfo(
                                        ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                            ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedIdentifier),
                                            parsedTokensAsString.ToString),
                                        Location.None))
                            End If

                            parsedTokensAsString.Append(tokens.Current.ToString)

                            ' this can happen when the while loop above consumed all tokens for the diagnostic message
                            If tokens.Current.Kind <> SyntaxKind.EndOfFileToken Then
                                Dim moveNextResult = tokens.MoveNext
                                Debug.Assert(moveNextResult)
                            End If
                        End While

                        parsedTokensAsString.Clear()

                        ' If we're at the end of the list, we're done
                        If tokens.Current.Kind = SyntaxKind.EndOfFileToken Then

                            Dim eof = tokens.Current

                            If eof.FullWidth > 0 Then
                                If Not eof.LeadingTrivia.All(Function(t) t.Kind = SyntaxKind.WhitespaceTrivia) Then
                                    ' This is an invalid line like "'Blah'" 
                                    GetErrorStringForRemainderOfConditionalCompilation(tokens, parsedTokensAsString, True)

                                    diagnosticBuilder.Add(
                                        New DiagnosticWithInfo(
                                            ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                            ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedIdentifier),
                                            parsedTokensAsString.ToString),
                                        Location.None))
                                End If
                            End If

                            Exit Do
                        End If

                        parsedTokensAsString.Append(tokens.Current.ToFullString())

                        If Not tokens.Current.Kind = SyntaxKind.IdentifierToken Then
                            GetErrorStringForRemainderOfConditionalCompilation(tokens, parsedTokensAsString)

                            diagnosticBuilder.Add(
                                New DiagnosticWithInfo(
                                    ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                        ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedIdentifier),
                                        parsedTokensAsString.ToString),
                                    Location.None))
                            Exit Do
                        End If

                        Dim symbolName = tokens.Current.ValueText

                        ' there should at least be a end of file token
                        Dim moveResult As Boolean = tokens.MoveNext
                        Debug.Assert(moveResult)

                        If tokens.Current.Kind = SyntaxKind.EqualsToken Then
                            parsedTokensAsString.Append(tokens.Current.ToFullString())

                            ' there should at least be a end of file token
                            moveResult = tokens.MoveNext
                            Debug.Assert(moveResult)

                            ' Parse expression starting with the offset
                            Dim offset As Integer = tokens.Current.SpanStart
                            Dim expression As ExpressionSyntax = ParseConditionalCompilationExpression(symbolList, offset)
                            Dim parsedEnd As Integer = offset + expression.Span.End

                            Dim atTheEndOrSeparator As Boolean = IsSeparatorOrEndOfFile(tokens.Current)

                            ' Consume tokens that are supposed to belong to the expression; we loop 
                            ' until the token's end position is the end of the expression, but not consume 
                            ' the last token as it will be consumed in uppermost While
                            While tokens.Current.Kind <> SyntaxKind.EndOfFileToken AndAlso tokens.Current.Span.End <= parsedEnd
                                parsedTokensAsString.Append(tokens.Current.ToFullString())
                                moveResult = tokens.MoveNext
                                Debug.Assert(moveResult)
                                atTheEndOrSeparator = IsSeparatorOrEndOfFile(tokens.Current)
                            End While

                            If expression.ContainsDiagnostics Then
                                ' Dev11 reports syntax errors in not consistent way, sometimes errors are not reported by 
                                ' command line utility at all; this implementation tries to repro Dev11 when possible
                                parsedTokensAsString.Append(" ^^ ^^ ")

                                ' Compete parsedTokensAsString until the next comma or end of stream
                                While Not IsSeparatorOrEndOfFile(tokens.Current)
                                    parsedTokensAsString.Append(tokens.Current.ToFullString())
                                    tokens.MoveNext()
                                End While

                                ' NOTE: Dev11 reports ERR_ExpectedExpression and ERR_BadCCExpression in different 
                                '       cases compared to what ParseConditionalCompilationExpression(...) generates,
                                '       so we have to use different criteria here; if we don't want to match Dev11 
                                '       errors we may simplify the code below
                                Dim errorSkipped As Boolean = False
                                For Each diag In expression.VbGreen.GetSyntaxErrors
                                    If diag.Code <> ERRID.ERR_ExpectedExpression AndAlso diag.Code <> ERRID.ERR_BadCCExpression Then
                                        diagnosticBuilder.Add(New DiagnosticWithInfo(ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1, diag, parsedTokensAsString.ToString), Location.None))
                                    Else
                                        errorSkipped = True
                                    End If
                                Next

                                If errorSkipped Then
                                    diagnosticBuilder.Add(
                                        New DiagnosticWithInfo(
                                            ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                                ErrorFactory.ErrorInfo(If(atTheEndOrSeparator, ERRID.ERR_ExpectedExpression, ERRID.ERR_BadCCExpression)),
                                                parsedTokensAsString.ToString),
                                            Location.None))
                                End If

                                Exit Do
                            End If

                            ' Expression parsed successfully --> evaluate it

                            Dim value As InternalSyntax.CConst =
                                InternalSyntax.ExpressionEvaluator.EvaluateExpression(
                                    DirectCast(expression.Green, InternalSyntax.ExpressionSyntax), defines)

                            Dim err As ERRID = value.ErrorId
                            If err <> 0 Then
                                GetErrorStringForRemainderOfConditionalCompilation(tokens, parsedTokensAsString)

                                diagnosticBuilder.Add(
                                    New DiagnosticWithInfo(
                                        ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                            ErrorFactory.ErrorInfo(err, value.ErrorArgs),
                                            parsedTokensAsString.ToString),
                                        Location.None))
                                Exit Do
                            End If

                            ' Expression evaluated successfully --> add to 'defines'
                            If defines.ContainsKey(symbolName) Then
                                defines = defines.Remove(symbolName)
                            End If
                            defines = defines.Add(symbolName, value)

                        ElseIf tokens.Current.Kind = SyntaxKind.CommaToken OrElse
                            tokens.Current.Kind = SyntaxKind.ColonToken OrElse
                            tokens.Current.Kind = SyntaxKind.EndOfFileToken Then
                            ' We have no value being assigned, so we'll just assign it to true

                            If defines.ContainsKey(symbolName) Then
                                defines = defines.Remove(symbolName)
                            End If
                            defines = defines.Add(symbolName, InternalSyntax.CConst.Create(True))

                        ElseIf tokens.Current.Kind = SyntaxKind.BadToken Then
                            GetErrorStringForRemainderOfConditionalCompilation(tokens, parsedTokensAsString)

                            diagnosticBuilder.Add(
                                New DiagnosticWithInfo(
                                    ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                        ErrorFactory.ErrorInfo(ERRID.ERR_IllegalChar),
                                        parsedTokensAsString.ToString),
                                    Location.None))
                            Exit Do
                        Else
                            GetErrorStringForRemainderOfConditionalCompilation(tokens, parsedTokensAsString)

                            diagnosticBuilder.Add(
                                New DiagnosticWithInfo(
                                    ErrorFactory.ErrorInfo(ERRID.ERR_ProjectCCError1,
                                        ErrorFactory.ErrorInfo(ERRID.ERR_ExpectedEOS),
                                        parsedTokensAsString.ToString),
                                    Location.None))
                            Exit Do
                        End If
                    Loop
                End If
            End Using

            diagnostics = diagnosticBuilder.ToArrayAndFree()
            Return InternalDefinesToPublicSymbols(defines)
        End Function

        ''' <summary>
        ''' NOTE: implicit line continuation will not be handled here and an error will be generated, 
        ''' but explicit one (like ".... _\r\n ....") should work fine
        ''' </summary>
        Private Shared Function ParseConditionalCompilationExpression(symbolList As String, offset As Integer) As ExpressionSyntax
            Using p = New InternalSyntax.Parser(SyntaxFactory.MakeSourceText(symbolList, offset), VisualBasicParseOptions.Default)
                p.GetNextToken()
                Return DirectCast(p.ParseConditionalCompilationExpression().CreateRed(Nothing, 0), ExpressionSyntax)
            End Using
        End Function

        Private Shared Iterator Function ParseInstrumentationKinds(value As String, diagnostics As IList(Of Diagnostic)) As IEnumerable(Of InstrumentationKind)
            Dim instrumentationKindStrs = value.Split({","c}, StringSplitOptions.RemoveEmptyEntries)
            For Each instrumentationKindStr In instrumentationKindStrs
                Select Case instrumentationKindStr.ToLower()
                    Case "testcoverage"
                        Yield InstrumentationKind.TestCoverage
                    Case Else
                        AddDiagnostic(diagnostics, ERRID.ERR_InvalidInstrumentationKind, instrumentationKindStr)
                End Select
            Next
        End Function

        Private Shared Function IsSeparatorOrEndOfFile(token As SyntaxToken) As Boolean
            Return token.Kind = SyntaxKind.EndOfFileToken OrElse token.Kind = SyntaxKind.ColonToken OrElse token.Kind = SyntaxKind.CommaToken
        End Function

        Private Shared Sub GetErrorStringForRemainderOfConditionalCompilation(
            tokens As IEnumerator(Of SyntaxToken),
            remainderErrorLine As StringBuilder,
            Optional includeCurrentToken As Boolean = False,
            Optional stopTokenKind As SyntaxKind = SyntaxKind.CommaToken
        )
            If includeCurrentToken Then
                remainderErrorLine.Append(" ^^ ")

                If tokens.Current.Kind = SyntaxKind.ColonToken AndAlso tokens.Current.FullWidth = 0 Then
                    remainderErrorLine.Append(SyntaxFacts.GetText(SyntaxKind.ColonToken))
                Else
                    remainderErrorLine.Append(tokens.Current.ToFullString())
                End If

                remainderErrorLine.Append(" ^^ ")
            Else
                remainderErrorLine.Append(" ^^ ^^ ")
            End If

            While tokens.MoveNext AndAlso Not tokens.Current.Kind = stopTokenKind
                remainderErrorLine.Append(tokens.Current.ToFullString())
            End While
        End Sub

        ''' <summary>
        ''' Parses the given platform option. Legal strings are "anycpu", "x64", "x86", "itanium", "anycpu32bitpreferred", "arm".
        ''' In case an invalid value was passed, anycpu is returned.
        ''' </summary>
        ''' <param name="value">The value for platform.</param>
        ''' <param name="errors">The error bag.</param>
        Private Shared Function ParsePlatform(name As String, value As String, errors As List(Of Diagnostic)) As Platform
            If value.IsEmpty Then
                AddDiagnostic(errors, ERRID.ERR_ArgumentRequired, name, ":<string>")
            Else
                Select Case value.ToLowerInvariant()
                    Case "x86"
                        Return Platform.X86
                    Case "x64"
                        Return Platform.X64
                    Case "itanium"
                        Return Platform.Itanium
                    Case "anycpu"
                        Return Platform.AnyCpu
                    Case "anycpu32bitpreferred"
                        Return Platform.AnyCpu32BitPreferred
                    Case "arm"
                        Return Platform.Arm
                    Case Else
                        AddDiagnostic(errors, ERRID.ERR_InvalidSwitchValue, name, value)
                End Select
            End If

            Return Platform.AnyCpu
        End Function

        ''' <summary>
        ''' Parses the file alignment option.
        ''' In case an invalid value was passed, nothing is returned.
        ''' </summary>
        ''' <param name="name">The name of the option.</param>
        ''' <param name="value">The value for the option.</param>
        ''' <param name="errors">The error bag.</param><returns></returns>
        Private Shared Function ParseFileAlignment(name As String, value As String, errors As List(Of Diagnostic)) As Integer
            Dim alignment As UShort

            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(errors, ERRID.ERR_ArgumentRequired, name, ":<number>")
            ElseIf Not TryParseUInt16(value, alignment) Then
                AddDiagnostic(errors, ERRID.ERR_InvalidSwitchValue, name, value)
            ElseIf Not Microsoft.CodeAnalysis.CompilationOptions.IsValidFileAlignment(alignment) Then
                AddDiagnostic(errors, ERRID.ERR_InvalidSwitchValue, name, value)
            Else
                Return alignment
            End If

            Return 0
        End Function

        ''' <summary>
        ''' Parses the base address option.
        ''' In case an invalid value was passed, nothing is returned.
        ''' </summary>
        ''' <param name="name">The name of the option.</param>
        ''' <param name="value">The value for the option.</param>
        ''' <param name="errors">The error bag.</param><returns></returns>
        Private Shared Function ParseBaseAddress(name As String, value As String, errors As List(Of Diagnostic)) As ULong
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(errors, ERRID.ERR_ArgumentRequired, name, ":<number>")
            Else
                Dim baseAddress As ULong
                Dim parseValue As String = value

                If value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) Then
                    parseValue = value.Substring(2) ' UInt64.TryParse does not accept hex format strings
                End If

                ' always treat the base address string as being a hex number, regardless of the given format.
                ' This handling was hardcoded in the command line option parsing of Dev10 and Dev11.
                If Not ULong.TryParse(parseValue,
                                      NumberStyles.HexNumber,
                                      CultureInfo.InvariantCulture,
                                      baseAddress) Then

                    AddDiagnostic(errors, ERRID.ERR_InvalidSwitchValue, name, value.ToString())
                Else
                    Return baseAddress
                End If
            End If

            Return 0
        End Function

        ''' <summary>
        ''' Parses the warning option.
        ''' </summary>
        ''' <param name="value">The value for the option.</param>
        Private Shared Function ParseWarnings(value As String) As IEnumerable(Of String)
            Dim values = ParseSeparatedPaths(value)
            Dim results = New List(Of String)()

            For Each id In values
                Dim number As UShort
                If UShort.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, number) AndAlso
                   (VisualBasic.MessageProvider.Instance.GetSeverity(number) = DiagnosticSeverity.Warning) AndAlso
                   (VisualBasic.MessageProvider.Instance.GetWarningLevel(number) = 1) Then
                    ' The id refers to a compiler warning.
                    ' Only accept real warnings from the compiler not including the command line warnings.
                    ' Also only accept the numbers that are actually declared in the enum.
                    results.Add(VisualBasic.MessageProvider.Instance.GetIdForErrorCode(CInt(number)))
                Else
                    ' Previous versions of the compiler used to report warnings (BC2026, BC2014)
                    ' whenever unrecognized warning codes were supplied in /nowarn or 
                    ' /warnaserror. We no longer generate a warning in such cases.
                    ' Instead we assume that the unrecognized id refers to a custom diagnostic.
                    results.Add(id)
                End If
            Next

            Return results
        End Function

        Private Shared Sub AddWarnings(d As IDictionary(Of String, ReportDiagnostic), kind As ReportDiagnostic, items As IEnumerable(Of String))
            For Each id In items
                Dim existing As ReportDiagnostic
                If d.TryGetValue(id, existing) Then
                    ' Rewrite the existing value with the latest one unless it is for /nowarn.
                    If existing <> ReportDiagnostic.Suppress Then
                        d(id) = kind
                    End If
                Else
                    d.Add(id, kind)
                End If
            Next
        End Sub

        Private Shared Sub UnimplementedSwitch(diagnostics As IList(Of Diagnostic), switchName As String)
            AddDiagnostic(diagnostics, ERRID.WRN_UnimplementedCommandLineSwitch, "/" + switchName)
        End Sub

        Friend Overrides Sub GenerateErrorForNoFilesFoundInRecurse(path As String, errors As IList(Of Diagnostic))
            AddDiagnostic(errors, ERRID.ERR_InvalidSwitchValue, "recurse", path)
        End Sub

        Private Shared Sub AddDiagnostic(diagnostics As IList(Of Diagnostic), errorCode As ERRID, ParamArray arguments As Object())
            diagnostics.Add(Diagnostic.Create(VisualBasic.MessageProvider.Instance, CInt(errorCode), arguments))
        End Sub

        ''' <summary>
        ''' In VB, if the output file name isn't specified explicitly, then it is derived from the name of the
        ''' first input file.
        ''' </summary>
        ''' <remarks>
        ''' http://msdn.microsoft.com/en-us/library/std9609e(v=vs.110)
        ''' Specify the full name and extension of the file to create. If you do not, the .exe file takes 
        ''' its name from the source-code file containing the Sub Main procedure, and the .dll file takes
        ''' its name from the first source-code file.
        ''' 
        ''' However, vbc.cpp has: 
        ''' <![CDATA[
        '''   // Calculate the output name and directory
        '''   dwCharCount = GetFullPathName(pszOut ? pszOut : g_strFirstFile, &wszFileName);
        ''' ]]>
        ''' </remarks>
        Private Sub GetCompilationAndModuleNames(diagnostics As List(Of Diagnostic),
                                                 kind As OutputKind,
                                                 sourceFiles As List(Of CommandLineSourceFile),
                                                 moduleAssemblyName As String,
                                                 ByRef outputFileName As String,
                                                 ByRef moduleName As String,
                                                 <Out> ByRef compilationName As String)
            Dim simpleName As String = Nothing

            If outputFileName Is Nothing Then
                Dim first = sourceFiles.FirstOrDefault()
                If first.Path IsNot Nothing Then
                    simpleName = PathUtilities.RemoveExtension(PathUtilities.GetFileName(first.Path))
                    outputFileName = simpleName & kind.GetDefaultExtension()

                    If simpleName.Length = 0 AndAlso Not kind.IsNetModule() Then
                        AddDiagnostic(diagnostics, ERRID.FTL_InputFileNameTooLong, outputFileName)
                        simpleName = Nothing
                        outputFileName = Nothing
                    End If
                End If
            Else
                Dim ext As String = PathUtilities.GetExtension(outputFileName)

                If kind.IsNetModule() Then
                    If ext.Length = 0 Then
                        outputFileName = outputFileName & ".netmodule"
                    End If
                Else
                    If Not ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) And
                        Not ext.Equals(".dll", StringComparison.OrdinalIgnoreCase) And
                        Not ext.Equals(".netmodule", StringComparison.OrdinalIgnoreCase) And
                        Not ext.Equals(".winmdobj", StringComparison.OrdinalIgnoreCase) Then
                        simpleName = outputFileName
                        outputFileName = outputFileName & kind.GetDefaultExtension()
                    End If

                    If simpleName Is Nothing Then
                        simpleName = PathUtilities.RemoveExtension(outputFileName)

                        ' /out:".exe"
                        ' Dev11 emits assembly with an empty name, we don't
                        If simpleName.Length = 0 Then
                            AddDiagnostic(diagnostics, ERRID.FTL_InputFileNameTooLong, outputFileName)
                            simpleName = Nothing
                            outputFileName = Nothing
                        End If
                    End If
                End If
            End If

            If kind.IsNetModule() Then
                Debug.Assert(Not IsScriptRunner)

                compilationName = moduleAssemblyName
            Else
                If moduleAssemblyName IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_NeedModule)
                End If

                compilationName = simpleName
            End If

            If moduleName Is Nothing Then
                moduleName = outputFileName
            End If
        End Sub
    End Class
End Namespace

