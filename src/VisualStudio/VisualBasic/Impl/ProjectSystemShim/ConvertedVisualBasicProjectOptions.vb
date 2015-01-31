' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    ''' <summary>
    ''' Converts a legacy VBCompilerOptions into the new Roslyn CompilerOptions and ParseOptions.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ConvertedVisualBasicProjectOptions

        Public Shared ReadOnly EmptyOptions As ConvertedVisualBasicProjectOptions = New ConvertedVisualBasicProjectOptions()

        ''' <summary>
        ''' The resulting CompilationOptions.
        ''' </summary>
        Public ReadOnly CompilationOptions As VisualBasicCompilationOptions

        ''' <summary>
        ''' The full paths to any libraries (such as System.dll, or Microsoft.VisualBasic.dll) that
        ''' should be added.
        ''' </summary>
        Public ReadOnly RuntimeLibraries As IEnumerable(Of String)

        ''' <summary>
        ''' The full output path.
        ''' </summary>
        ''' <remarks></remarks>
        Public ReadOnly OutputPath As String
        Public ReadOnly ParseOptions As VisualBasicParseOptions

        ''' <summary>
        ''' Maps a string to the parsed conditional compilation symbols.
        ''' It is expected that most projects in a solution will have similar (if not identical)
        ''' sets of conditional compilation symbols. From a performance perspective, it makes sense
        ''' to cache these rather than reparse them every time we create a new <see cref="ConvertedVisualBasicProjectOptions"/>
        ''' instance. We also expect the total set of these to be small, which is why we never evict anything from this cache.
        ''' </summary>
        Private Shared conditionalCompilationSymbolsCache As Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object))) =
            New Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object)))

        Private Sub New()
            CompilationOptions = Nothing
            OutputPath = Nothing
            ParseOptions = Nothing
            RuntimeLibraries = SpecializedCollections.EmptyEnumerable(Of String)
        End Sub

        Public Sub New(options As VBCompilerOptions, compilerHost As IVbCompilerHost, globalImports As IEnumerable(Of GlobalImport), strongNameKeyPaths As ImmutableArray(Of String), projectDirectoryOpt As String, ruleSetOpt As IRuleSetFile)
            If options.wszOutputPath IsNot Nothing AndAlso options.wszExeName IsNot Nothing Then
                OutputPath = Path.Combine(options.wszOutputPath, options.wszExeName)
            Else
                OutputPath = String.Empty
            End If

            Dim kind As OutputKind

            Select Case options.OutputType
                Case VBCompilerOutputTypes.OUTPUT_ConsoleEXE
                    kind = OutputKind.ConsoleApplication
                Case VBCompilerOutputTypes.OUTPUT_Library, VBCompilerOutputTypes.OUTPUT_None
                    kind = OutputKind.DynamicallyLinkedLibrary
                Case VBCompilerOutputTypes.OUTPUT_Module
                    kind = OutputKind.NetModule
                Case VBCompilerOutputTypes.OUTPUT_WindowsEXE
                    kind = OutputKind.WindowsApplication
                Case VBCompilerOutputTypes.OUTPUT_AppContainerEXE
                    kind = OutputKind.WindowsRuntimeApplication
                Case VBCompilerOutputTypes.OUTPUT_WinMDObj
                    kind = OutputKind.WindowsRuntimeMetadata
            End Select

            Dim runtimes = New List(Of String)
            Select Case options.vbRuntimeKind
                Case VBRuntimeKind.DefaultRuntime
                    runtimes.Add(Path.Combine(compilerHost.GetSdkPath(), "Microsoft.VisualBasic.dll"))

                Case VBRuntimeKind.SpecifiedRuntime
                    If options.wszSpecifiedVBRuntime Is Nothing Then
                        Throw New ArgumentException()
                    End If

                    ' If they specified a fully qualified file, use it
                    If File.Exists(options.wszSpecifiedVBRuntime) Then
                        runtimes.Add(options.wszSpecifiedVBRuntime)
                    Else
                        ' If it's just a filename, try to find it in the SDK path.
                        If options.wszSpecifiedVBRuntime <> Path.GetFileName(options.wszSpecifiedVBRuntime) Then
                            Throw New ArgumentException()
                        End If

                        Dim runtimePath = Path.Combine(compilerHost.GetSdkPath(), options.wszSpecifiedVBRuntime)
                        If File.Exists(runtimePath) Then
                            runtimes.Add(runtimePath)
                        Else
                            Throw New ArgumentException()
                        End If
                    End If
            End Select

            If Not options.bNoStandardLibs Then
                runtimes.Add(Path.Combine(compilerHost.GetSdkPath(), "System.dll"))
            End If

            runtimes.Add(Path.Combine(compilerHost.GetSdkPath(), "mscorlib.dll"))

            RuntimeLibraries = runtimes

            Dim conditionalCompilationSymbols = GetConditionalCompilationSymbols(kind, If(options.wszCondComp, ""))

            ' The project system may pass us zero to mean "default". Old project system binaries (prior to mid-September 2014)
            ' would also use other constants that we just got rid of. This check can be replaced with an explicit check for just
            ' zero in October 2014 or later.
            If options.langVersion < LanguageVersion.VisualBasic9 Then
                options.langVersion = VisualBasicParseOptions.Default.LanguageVersion
            End If

            ParseOptions = New VisualBasicParseOptions(
                languageVersion:=options.langVersion,
                preprocessorSymbols:=conditionalCompilationSymbols,
                documentationMode:=If(Not String.IsNullOrEmpty(options.wszXMLDocName), DocumentationMode.Diagnose, DocumentationMode.Parse))

            Dim platform As Platform
            If Not System.Enum.TryParse(options.wszPlatformType, ignoreCase:=True, result:=platform) Then
                platform = Platform.AnyCpu
            End If

            ' TODO: support #load search paths
            Dim sourceSearchpaths = ImmutableArray(Of String).Empty

            Dim ruleSetFileGeneralDiagnosticOption As ReportDiagnostic? = Nothing
            Dim ruleSetFileSpecificDiagnosticOptions As IDictionary(Of String, ReportDiagnostic) = Nothing

            If ruleSetOpt IsNot Nothing Then
                ruleSetFileGeneralDiagnosticOption = ruleSetOpt.GetGeneralDiagnosticOption()
                ruleSetFileSpecificDiagnosticOptions = ruleSetOpt.GetSpecificDiagnosticOptions()
            End If

            Dim generalDiagnosticOption As ReportDiagnostic = DetermineGeneralDiagnosticOption(options.WarningLevel, ruleSetFileGeneralDiagnosticOption)
            Dim specificDiagnosticOptions As IReadOnlyDictionary(Of String, ReportDiagnostic) = DetermineSpecificDiagnosticOptions(options, ruleSetFileSpecificDiagnosticOptions)

            CompilationOptions = New VisualBasicCompilationOptions(
                                    checkOverflow:=Not options.bRemoveIntChecks,
                                    concurrentBuild:=False,
                                    cryptoKeyContainer:=options.wszStrongNameContainer,
                                    cryptoKeyFile:=options.wszStrongNameKeyFile,
                                    delaySign:=If(options.bDelaySign, CType(True, Boolean?), Nothing),
                                    embedVbCoreRuntime:=options.vbRuntimeKind = VBRuntimeKind.EmbeddedRuntime,
                                    generalDiagnosticOption:=generalDiagnosticOption,
                                    globalImports:=globalImports,
                                    mainTypeName:=If(options.wszStartup <> String.Empty, options.wszStartup, Nothing),
                                    optionExplicit:=Not options.bOptionExplicitOff,
                                    optionInfer:=Not options.bOptionInferOff,
                                    optionStrict:=If(options.bOptionStrictOff, OptionStrict.Custom, OptionStrict.On),
                                    optionCompareText:=options.bOptionCompareText,
                                    optimizationLevel:=If(options.bOptimize, OptimizationLevel.Release, OptimizationLevel.Debug),
                                    outputKind:=kind,
                                    parseOptions:=ParseOptions,
                                    platform:=platform,
                                    rootNamespace:=If(options.wszDefaultNamespace, ""),
                                    specificDiagnosticOptions:=specificDiagnosticOptions,
                                    sourceReferenceResolver:=New SourceFileResolver(sourceSearchpaths, projectDirectoryOpt),
                                    xmlReferenceResolver:=New XmlFileResolver(projectDirectoryOpt),
                                    assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default,
                                    strongNameProvider:=New DesktopStrongNameProvider(strongNameKeyPaths))
        End Sub

        Private Shared Function GetConditionalCompilationSymbols(kind As OutputKind, str As String) As ImmutableArray(Of KeyValuePair(Of String, Object))
            Debug.Assert(str IsNot Nothing)
            Dim key = KeyValuePair.Create(str, kind)

            Dim result As ImmutableArray(Of KeyValuePair(Of String, Object)) = Nothing
            If conditionalCompilationSymbolsCache.TryGetValue(key, result) Then
                Return result
            End If

            Dim errors As IEnumerable(Of Diagnostic) = Nothing
            Dim defines = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(str, errors)
            ' ignore errors

            Return AddPredefinedPreprocessorSymbols(kind, defines.AsImmutableOrEmpty())
        End Function

        Private Shared Function GetWarningOptions(options As VBCompilerOptions) As Dictionary(Of String, ReportDiagnostic)
            Dim dictionary As New Dictionary(Of String, ReportDiagnostic)
            UpdateDictionaryForWarning(dictionary, options.wszWarningsAsErrors, ReportDiagnostic.Error)
            UpdateDictionaryForWarning(dictionary, options.wszWarningsNotAsErrors, ReportDiagnostic.Default)
            UpdateDictionaryForWarning(dictionary, options.wszDisabledWarnings, ReportDiagnostic.Suppress)

            Return dictionary
        End Function

        Private Shared Sub UpdateDictionaryForWarning(dictionary As Dictionary(Of String, ReportDiagnostic), warnings As String, reportDiagnostic As ReportDiagnostic)
            If warnings IsNot Nothing Then
                For Each warning In warnings.Split(New String() {" ", ",", ";"}, StringSplitOptions.RemoveEmptyEntries)
                    Dim warningId As Integer
                    If Integer.TryParse(warning, warningId) Then
                        dictionary("BC" + warning) = reportDiagnostic
                    Else
                        dictionary(warning) = reportDiagnostic
                    End If
                Next
            End If
        End Sub

        Private Shared Function DetermineGeneralDiagnosticOption(level As WarningLevel, ruleSetGeneralDiagnosticOption As ReportDiagnostic?) As ReportDiagnostic
            'If no option was supplied in the project file, but there is one in the ruleset file, use that one.
            If level = WarningLevel.WARN_Regular AndAlso
                ruleSetGeneralDiagnosticOption.HasValue Then

                Return ruleSetGeneralDiagnosticOption.Value
            End If

            Return ConvertWarningLevel(level)
        End Function

        Private Shared Function DetermineSpecificDiagnosticOptions(options As VBCompilerOptions, ruleSetSpecificDiagnosticOptions As IDictionary(Of String, ReportDiagnostic)) As IReadOnlyDictionary(Of String, ReportDiagnostic)
            Dim diagnosticOptions As Dictionary(Of String, ReportDiagnostic)
            Dim diagnosticOptionsFromCompilerOptions = GetWarningOptions(options)

            If ruleSetSpecificDiagnosticOptions IsNot Nothing Then
                diagnosticOptions = New Dictionary(Of String, ReportDiagnostic)(ruleSetSpecificDiagnosticOptions)

                For Each kvp In diagnosticOptionsFromCompilerOptions
                    diagnosticOptions(kvp.Key) = kvp.Value
                Next
            Else
                diagnosticOptions = diagnosticOptionsFromCompilerOptions
            End If

            Return New ReadOnlyDictionary(Of String, ReportDiagnostic)(diagnosticOptions)
        End Function

        Private Shared Function ConvertWarningLevel(level As WarningLevel) As ReportDiagnostic
            Select Case level
                Case WarningLevel.WARN_None
                    Return ReportDiagnostic.Suppress

                Case WarningLevel.WARN_Regular
                    Return ReportDiagnostic.Default

                Case WarningLevel.WARN_AsError
                    Return ReportDiagnostic.Error

                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select
        End Function

        Private Shared Function GetArrayEntry(Of T As Structure)(array As IntPtr, index As Integer) As T
            Return DirectCast(Marshal.PtrToStructure(array + index * Marshal.SizeOf(GetType(T)), GetType(T)), T)
        End Function
    End Class
End Namespace
