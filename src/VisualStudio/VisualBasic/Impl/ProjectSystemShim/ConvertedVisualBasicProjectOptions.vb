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
        Private Shared s_conditionalCompilationSymbolsCache As Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object))) =
            New Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object)))

        Private Sub New()
            CompilationOptions = Nothing
            OutputPath = Nothing
            ParseOptions = Nothing
            RuntimeLibraries = SpecializedCollections.EmptyEnumerable(Of String)
        End Sub

        Private Shared ReadOnly s_EmptyCommandLineArguments As VisualBasicCommandLineArguments = VisualBasicCommandLineParser.Default.Parse(SpecializedCollections.EmptyEnumerable(Of String)(), baseDirectory:="", sdkDirectory:=Nothing)

        Public Sub New(options As VBCompilerOptions, compilerHost As IVbCompilerHost, globalImports As IEnumerable(Of GlobalImport), strongNameKeyPaths As ImmutableArray(Of String), projectDirectoryOpt As String, ruleSetOpt As IRuleSetFile, Optional parsedCommandLineArguments As CommandLineArguments = Nothing)
            parsedCommandLineArguments = If(parsedCommandLineArguments, s_EmptyCommandLineArguments)

            If options.wszOutputPath IsNot Nothing AndAlso options.wszExeName IsNot Nothing Then
                OutputPath = PathUtilities.CombinePathsUnchecked(options.wszOutputPath, options.wszExeName)
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

            ' GetSDKPath can return E_NOTIMPL if there is no SDK path at all
            Dim sdkPath As String = Nothing
            Dim sdkPathHResult = compilerHost.GetSdkPath(sdkPath)

            If sdkPathHResult = VSConstants.E_NOTIMPL Then
                sdkPath = Nothing
            Else
                Marshal.ThrowExceptionForHR(sdkPathHResult, New IntPtr(-1))
            End If

            Dim runtimes = New List(Of String)
            Select Case options.vbRuntimeKind
                Case VBRuntimeKind.DefaultRuntime
                    If sdkPath IsNot Nothing Then
                        runtimes.Add(PathUtilities.CombinePathsUnchecked(sdkPath, "Microsoft.VisualBasic.dll"))
                    End If

                Case VBRuntimeKind.SpecifiedRuntime
                    If options.wszSpecifiedVBRuntime IsNot Nothing Then
                        ' If they specified a fully qualified file, use it
                        If File.Exists(options.wszSpecifiedVBRuntime) Then
                            runtimes.Add(options.wszSpecifiedVBRuntime)
                        ElseIf sdkPath IsNot Nothing Then
                            ' If it's just a filename, try to find it in the SDK path.
                            If options.wszSpecifiedVBRuntime = PathUtilities.GetFileName(options.wszSpecifiedVBRuntime) Then
                                Dim runtimePath = PathUtilities.CombinePathsUnchecked(sdkPath, options.wszSpecifiedVBRuntime)
                                If File.Exists(runtimePath) Then
                                    runtimes.Add(runtimePath)
                                End If
                            End If
                        End If
                    End If
            End Select

            If sdkPath IsNot Nothing Then
                If Not options.bNoStandardLibs Then
                    runtimes.Add(PathUtilities.CombinePathsUnchecked(sdkPath, "System.dll"))
                End If

                runtimes.Add(PathUtilities.CombinePathsUnchecked(sdkPath, "mscorlib.dll"))
            End If

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
                documentationMode:=If(Not String.IsNullOrEmpty(options.wszXMLDocName), DocumentationMode.Diagnose, DocumentationMode.Parse)) _
                .WithFeatures(parsedCommandLineArguments.ParseOptions.Features)

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
                                    deterministic:=parsedCommandLineArguments.CompilationOptions.Deterministic,
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
                                    publicSign:=parsedCommandLineArguments.CompilationOptions.PublicSign,
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
            If s_conditionalCompilationSymbolsCache.TryGetValue(key, result) Then
                Return result
            End If

            Dim errors As IEnumerable(Of Diagnostic) = Nothing
            Dim defines = VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(str, errors)
            ' ignore errors

            Return AddPredefinedPreprocessorSymbols(kind, defines.AsImmutableOrEmpty())
        End Function

        Private Shared Iterator Function ParseWarningCodes(warnings As String) As IEnumerable(Of String)
            If warnings IsNot Nothing Then
                For Each warning In warnings.Split(New String() {" ", ",", ";"}, StringSplitOptions.RemoveEmptyEntries)
                    Dim warningId As Integer
                    If Integer.TryParse(warning, warningId) Then
                        Yield "BC" + warning
                    Else
                        Yield warning
                    End If
                Next
            End If
        End Function

        Private Shared Function DetermineGeneralDiagnosticOption(level As WarningLevel, ruleSetGeneralDiagnosticOption As ReportDiagnostic?) As ReportDiagnostic
            'If no option was supplied in the project file, but there is one in the ruleset file, use that one.
            If level = WarningLevel.WARN_Regular AndAlso
                ruleSetGeneralDiagnosticOption.HasValue Then

                Return ruleSetGeneralDiagnosticOption.Value
            End If

            Return ConvertWarningLevel(level)
        End Function

        Private Shared Function DetermineSpecificDiagnosticOptions(options As VBCompilerOptions, ruleSetSpecificDiagnosticOptions As IDictionary(Of String, ReportDiagnostic)) As IReadOnlyDictionary(Of String, ReportDiagnostic)
            If ruleSetSpecificDiagnosticOptions Is Nothing Then
                ruleSetSpecificDiagnosticOptions = New Dictionary(Of String, ReportDiagnostic)
            End If

            ' Start with the rule set options
            Dim diagnosticOptions = New Dictionary(Of String, ReportDiagnostic)(ruleSetSpecificDiagnosticOptions)

            ' Update the specific options based on the general settings
            If options.WarningLevel = WarningLevel.WARN_AsError Then
                For Each pair In ruleSetSpecificDiagnosticOptions
                    If pair.Value = ReportDiagnostic.Warn Then
                        diagnosticOptions(pair.Key) = ReportDiagnostic.Error
                    End If
                Next
            ElseIf options.WarningLevel = WarningLevel.WARN_None Then

                For Each pair In ruleSetSpecificDiagnosticOptions
                    If pair.Value <> ReportDiagnostic.Error Then
                        diagnosticOptions(pair.Key) = ReportDiagnostic.Suppress
                    End If
                Next
            End If

            ' Update the specific options based on the specific settings
            For Each diagnosticID In ParseWarningCodes(options.wszWarningsAsErrors)
                diagnosticOptions(diagnosticID) = ReportDiagnostic.Error
            Next

            For Each diagnosticID In ParseWarningCodes(options.wszWarningsNotAsErrors)
                Dim ruleSetOption As ReportDiagnostic
                If ruleSetSpecificDiagnosticOptions.TryGetValue(diagnosticID, ruleSetOption) Then
                    diagnosticOptions(diagnosticID) = ruleSetOption
                Else
                    diagnosticOptions(diagnosticID) = ReportDiagnostic.Default
                End If
            Next

            For Each diagnosticID In ParseWarningCodes(options.wszDisabledWarnings)
                diagnosticOptions(diagnosticID) = ReportDiagnostic.Suppress
            Next

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
