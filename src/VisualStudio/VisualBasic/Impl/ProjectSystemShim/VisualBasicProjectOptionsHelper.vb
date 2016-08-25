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
    ''' Helper to convert a legacy VBCompilerOptions into the new Roslyn CompilerOptions and ParseOptions.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class VisualBasicProjectOptionsHelper

        ''' <summary>
        ''' Maps a string to the parsed conditional compilation symbols.
        ''' It is expected that most projects in a solution will have similar (if not identical)
        ''' sets of conditional compilation symbols. From a performance perspective, it makes sense
        ''' to cache these rather than reparse them every time we create a new <see cref="VisualBasicProjectOptionsHelper"/>
        ''' instance. We also expect the total set of these to be small, which is why we never evict anything from this cache.
        ''' </summary>
        Private Shared s_conditionalCompilationSymbolsCache As Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object))) =
            New Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object)))

        Private Shared ReadOnly s_EmptyCommandLineArguments As VisualBasicCommandLineArguments = VisualBasicCommandLineParser.Default.Parse(SpecializedCollections.EmptyEnumerable(Of String)(), baseDirectory:="", sdkDirectory:=Nothing)

        Public Shared Function CreateCompilationOptions(baseCompilationOptionsOpt As VisualBasicCompilationOptions,
                                                     newParseOptions As VisualBasicParseOptions,
                                                     compilerOptions As VBCompilerOptions,
                                                     compilerHost As IVbCompilerHost,
                                                     globalImports As IEnumerable(Of GlobalImport),
                                                     projectDirectoryOpt As String,
                                                     ruleSetOpt As IRuleSetFile) As VisualBasicCompilationOptions
            Dim platform As Platform
            If Not System.Enum.TryParse(compilerOptions.wszPlatformType, ignoreCase:=True, result:=platform) Then
                platform = Platform.AnyCpu
            End If

            Dim ruleSetFileGeneralDiagnosticOption As ReportDiagnostic? = Nothing
            Dim ruleSetFileSpecificDiagnosticOptions As IDictionary(Of String, ReportDiagnostic) = Nothing

            If ruleSetOpt IsNot Nothing Then
                ruleSetFileGeneralDiagnosticOption = ruleSetOpt.GetGeneralDiagnosticOption()
                ruleSetFileSpecificDiagnosticOptions = ruleSetOpt.GetSpecificDiagnosticOptions()
            End If

            Dim generalDiagnosticOption As ReportDiagnostic = DetermineGeneralDiagnosticOption(compilerOptions.WarningLevel, ruleSetFileGeneralDiagnosticOption)
            Dim specificDiagnosticOptions As IReadOnlyDictionary(Of String, ReportDiagnostic) = DetermineSpecificDiagnosticOptions(compilerOptions, ruleSetFileSpecificDiagnosticOptions)
            Dim outputKind = GetOutputKind(compilerOptions)

            If baseCompilationOptionsOpt Is Nothing Then
                baseCompilationOptionsOpt = New VisualBasicCompilationOptions(outputKind) _
                    .WithConcurrentBuild(False) _
                    .WithXmlReferenceResolver(New XmlFileResolver(projectDirectoryOpt)) _
                    .WithSourceReferenceResolver(New SourceFileResolver(Array.Empty(Of String), projectDirectoryOpt)) _
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default) _
                    .WithStrongNameProvider(New DesktopStrongNameProvider(ImmutableArray(Of String).Empty))
            End If

            Return baseCompilationOptionsOpt.WithOverflowChecks(Not compilerOptions.bRemoveIntChecks) _
                .WithCryptoKeyContainer(compilerOptions.wszStrongNameContainer) _
                .WithCryptoKeyFile(compilerOptions.wszStrongNameKeyFile) _
                .WithDelaySign(If(compilerOptions.bDelaySign, CType(True, Boolean?), Nothing)) _
                .WithEmbedVbCoreRuntime(compilerOptions.vbRuntimeKind = VBRuntimeKind.EmbeddedRuntime) _
                .WithGeneralDiagnosticOption(generalDiagnosticOption) _
                .WithGlobalImports(globalImports) _
                .WithMainTypeName(If(compilerOptions.wszStartup <> String.Empty, compilerOptions.wszStartup, Nothing)) _
                .WithOptionExplicit(Not compilerOptions.bOptionExplicitOff) _
                .WithOptionInfer(Not compilerOptions.bOptionInferOff) _
                .WithOptionStrict(If(compilerOptions.bOptionStrictOff, OptionStrict.Custom, OptionStrict.On)) _
                .WithOptionCompareText(compilerOptions.bOptionCompareText) _
                .WithOptimizationLevel(If(compilerOptions.bOptimize, OptimizationLevel.Release, OptimizationLevel.Debug)) _
                .WithOutputKind(outputKind) _
                .WithPlatform(platform) _
                .WithRootNamespace(If(compilerOptions.wszDefaultNamespace, String.Empty)) _
                .WithSpecificDiagnosticOptions(specificDiagnosticOptions) _
                .WithParseOptions(newParseOptions)
        End Function

        Private Shared Function GetOutputKind(options As VBCompilerOptions) As OutputKind
            Select Case options.OutputType
                Case VBCompilerOutputTypes.OUTPUT_ConsoleEXE
                    Return OutputKind.ConsoleApplication
                Case VBCompilerOutputTypes.OUTPUT_Library, VBCompilerOutputTypes.OUTPUT_None
                    Return OutputKind.DynamicallyLinkedLibrary
                Case VBCompilerOutputTypes.OUTPUT_Module
                    Return OutputKind.NetModule
                Case VBCompilerOutputTypes.OUTPUT_WindowsEXE
                    Return OutputKind.WindowsApplication
                Case VBCompilerOutputTypes.OUTPUT_AppContainerEXE
                    Return OutputKind.WindowsRuntimeApplication
                Case VBCompilerOutputTypes.OUTPUT_WinMDObj
                    Return OutputKind.WindowsRuntimeMetadata
                Case Else
                    Return Nothing
            End Select
        End Function

        Public Shared Function GetRuntimeLibraries(compilerHost As IVbCompilerHost, options As VBCompilerOptions) As List(Of String)
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

            Return runtimes
        End Function

        Public Shared Function GetOutputPath(compilerOptions As VBCompilerOptions) As String
            If compilerOptions.wszOutputPath IsNot Nothing AndAlso compilerOptions.wszExeName IsNot Nothing Then
                Return PathUtilities.CombinePathsUnchecked(compilerOptions.wszOutputPath, compilerOptions.wszExeName)
            End If

            Return String.Empty
        End Function
        Public Shared Function CreateParseOptions(baseParseOptionsOpt As VisualBasicParseOptions, compilerOptions As VBCompilerOptions) As VisualBasicParseOptions
            Dim outputKind = GetOutputKind(compilerOptions)
            Dim conditionalCompilationSymbols = GetConditionalCompilationSymbols(outputKind, If(compilerOptions.wszCondComp, ""))

            ' The project system may pass us zero to mean "default". Old project system binaries (prior to mid-September 2014)
            ' would also use other constants that we just got rid of. This check can be replaced with an explicit check for just
            ' zero in October 2014 or later.
            If compilerOptions.langVersion < LanguageVersion.VisualBasic9 Then
                compilerOptions.langVersion = VisualBasicParseOptions.Default.LanguageVersion
            End If

            baseParseOptionsOpt = If(baseParseOptionsOpt, New VisualBasicParseOptions())
            Return baseParseOptionsOpt.WithLanguageVersion(compilerOptions.langVersion) _
                .WithPreprocessorSymbols(conditionalCompilationSymbols) _
                .WithDocumentationMode(If(Not String.IsNullOrEmpty(compilerOptions.wszXMLDocName), DocumentationMode.Diagnose, DocumentationMode.Parse))
        End Function

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
