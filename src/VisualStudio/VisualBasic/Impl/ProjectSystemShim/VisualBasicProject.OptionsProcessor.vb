' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    ''' <summary>
    ''' Helper to convert a legacy VBCompilerOptions into the new Roslyn CompilerOptions and ParseOptions.
    ''' </summary>
    ''' <remarks></remarks>
    Partial Friend NotInheritable Class VisualBasicProject
        Friend NotInheritable Class OptionsProcessor
            Inherits VisualStudioProjectOptionsProcessor

            Private _rawOptions As VBCompilerOptions
            Private ReadOnly _imports As New List(Of GlobalImport)

            ''' <summary>
            ''' Maps a string to the parsed conditional compilation symbols.
            ''' It is expected that most projects in a solution will have similar (if not identical)
            ''' sets of conditional compilation symbols. We expect the total set of these to be small, which is why we never evict anything from this cache.
            ''' </summary>
            Private Shared ReadOnly s_conditionalCompilationSymbolsCache As Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object))) =
                New Dictionary(Of KeyValuePair(Of String, OutputKind), ImmutableArray(Of KeyValuePair(Of String, Object)))

            ''' <summary>
            ''' Maps a string to the related <see cref="GlobalImport"/>. Since many projects in a solution
            ''' will have similar (if not identical) sets of imports, there are performance benefits to
            ''' caching these rather than parsing them anew for each project. It is expected that the total
            ''' number of imports will be rather small, which is why we never evict anything from this cache.
            ''' </summary>
            Private Shared ReadOnly s_importsCache As Dictionary(Of String, GlobalImport) = New Dictionary(Of String, GlobalImport)

            Public Sub New(project As VisualStudioProject, workspaceServices As HostWorkspaceServices)
                MyBase.New(project, workspaceServices)
            End Sub

            Public Sub SetNewRawOptions(ByRef rawOptions As VBCompilerOptions)
                _rawOptions = rawOptions
                UpdateProjectForNewHostValues()
            End Sub

            Protected Overrides Function ComputeCompilationOptionsWithHostValues(compilationOptions As CompilationOptions, ruleSetFileOpt As IRuleSetFile) As CompilationOptions
                Return ApplyCompilationOptionsFromVBCompilerOptions(compilationOptions, _rawOptions, ruleSetFileOpt) _
                    .WithGlobalImports(_imports)
            End Function

            Public Shared Function ApplyCompilationOptionsFromVBCompilerOptions(compilationOptions As CompilationOptions, compilerOptions As VBCompilerOptions, Optional ruleSetFileOpt As IRuleSetFile = Nothing) As VisualBasicCompilationOptions
                Dim platform As Platform
                If Not System.Enum.TryParse(compilerOptions.wszPlatformType, ignoreCase:=True, result:=platform) Then
                    platform = Platform.AnyCpu
                End If

                Dim ruleSetFileGeneralDiagnosticOption As ReportDiagnostic? = Nothing
                Dim ruleSetFileSpecificDiagnosticOptions As IDictionary(Of String, ReportDiagnostic) = Nothing

                If ruleSetFileOpt IsNot Nothing Then
                    ruleSetFileGeneralDiagnosticOption = ruleSetFileOpt.GetGeneralDiagnosticOption()
                    ruleSetFileSpecificDiagnosticOptions = ruleSetFileOpt.GetSpecificDiagnosticOptions()
                End If

                Dim generalDiagnosticOption As ReportDiagnostic = DetermineGeneralDiagnosticOption(compilerOptions.WarningLevel, ruleSetFileGeneralDiagnosticOption)
                Dim specificDiagnosticOptions As IReadOnlyDictionary(Of String, ReportDiagnostic) = DetermineSpecificDiagnosticOptions(compilerOptions, ruleSetFileSpecificDiagnosticOptions)

                Dim visualBasicCompilationOptions = DirectCast(compilationOptions, VisualBasicCompilationOptions)
                Dim visualBasicParseOptions = ApplyVisualBasicParseOptionsFromCompilerOptions(visualBasicCompilationOptions.ParseOptions, compilerOptions)

                Return visualBasicCompilationOptions _
                    .WithOverflowChecks(Not compilerOptions.bRemoveIntChecks) _
                    .WithCryptoKeyContainer(compilerOptions.wszStrongNameContainer) _
                    .WithCryptoKeyFile(compilerOptions.wszStrongNameKeyFile) _
                    .WithDelaySign(If(compilerOptions.bDelaySign, CType(True, Boolean?), Nothing)) _
                    .WithEmbedVbCoreRuntime(compilerOptions.vbRuntimeKind = VBRuntimeKind.EmbeddedRuntime) _
                    .WithGeneralDiagnosticOption(generalDiagnosticOption) _
                    .WithMainTypeName(If(compilerOptions.wszStartup <> String.Empty, compilerOptions.wszStartup, Nothing)) _
                    .WithOptionExplicit(Not compilerOptions.bOptionExplicitOff) _
                    .WithOptionInfer(Not compilerOptions.bOptionInferOff) _
                    .WithOptionStrict(If(compilerOptions.bOptionStrictOff, OptionStrict.Custom, OptionStrict.On)) _
                    .WithOptionCompareText(compilerOptions.bOptionCompareText) _
                    .WithOptimizationLevel(If(compilerOptions.bOptimize, OptimizationLevel.Release, OptimizationLevel.Debug)) _
                    .WithOutputKind(GetOutputKind(compilerOptions)) _
                    .WithPlatform(platform) _
                    .WithRootNamespace(If(compilerOptions.wszDefaultNamespace, String.Empty)) _
                    .WithParseOptions(DirectCast(visualBasicParseOptions, VisualBasicParseOptions)) _
                    .WithSpecificDiagnosticOptions(specificDiagnosticOptions)
            End Function

            Public Sub AddImport(wszImport As String)
                ' Add the import to the list. The legacy language services didn't do any sort of
                ' checking to see if the import is already added. Instead, they'd just have two entries
                ' in the list. This is OK because the UI in Project Property Pages disallows users from
                ' adding multiple entries. Hence the potential first-chance exception here is not a
                ' problem, it should in theory never happen.

                Try
                    Dim import As GlobalImport = Nothing
                    If Not s_importsCache.TryGetValue(wszImport, import) Then
                        import = GlobalImport.Parse(wszImport)
                        s_importsCache(wszImport) = import
                    End If

                    _imports.Add(import)
                Catch ex As ArgumentException
                    'TODO: report error
                End Try

                UpdateProjectForNewHostValues()
            End Sub

            Private Shared Function GetOutputKind(ByRef compilerOptions As VBCompilerOptions) As OutputKind
                Select Case compilerOptions.OutputType
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

            Public Function GetRuntimeLibraries(compilerHost As IVbCompilerHost) As ImmutableArray(Of String)
                Return GetRuntimeLibraries(compilerHost, _rawOptions)
            End Function

            Public Shared Function GetRuntimeLibraries(compilerHost As IVbCompilerHost, ByRef compilerOptions As VBCompilerOptions) As ImmutableArray(Of String)
                ' GetSDKPath can return E_NOTIMPL if there is no SDK path at all
                Dim sdkPath As String = Nothing
                Dim sdkPathHResult = compilerHost.GetSdkPath(sdkPath)

                If sdkPathHResult = VSConstants.E_NOTIMPL Then
                    sdkPath = Nothing
                Else
                    Marshal.ThrowExceptionForHR(sdkPathHResult, New IntPtr(-1))
                End If

                Dim runtimes = ImmutableArray.CreateBuilder(Of String)
                Select Case compilerOptions.vbRuntimeKind
                    Case VBRuntimeKind.DefaultRuntime
                        If sdkPath IsNot Nothing Then
                            runtimes.Add(PathUtilities.CombinePathsUnchecked(sdkPath, "Microsoft.VisualBasic.dll"))
                        End If

                    Case VBRuntimeKind.SpecifiedRuntime
                        If compilerOptions.wszSpecifiedVBRuntime IsNot Nothing Then
                            ' If they specified a fully qualified file, use it
                            If File.Exists(compilerOptions.wszSpecifiedVBRuntime) Then
                                runtimes.Add(compilerOptions.wszSpecifiedVBRuntime)
                            ElseIf sdkPath IsNot Nothing Then
                                ' If it's just a filename, try to find it in the SDK path.
                                If compilerOptions.wszSpecifiedVBRuntime = PathUtilities.GetFileName(compilerOptions.wszSpecifiedVBRuntime) Then
                                    Dim runtimePath = PathUtilities.CombinePathsUnchecked(sdkPath, compilerOptions.wszSpecifiedVBRuntime)
                                    If File.Exists(runtimePath) Then
                                        runtimes.Add(runtimePath)
                                    End If
                                End If
                            End If
                        End If
                End Select

                If sdkPath IsNot Nothing Then
                    If Not compilerOptions.bNoStandardLibs Then
                        runtimes.Add(PathUtilities.CombinePathsUnchecked(sdkPath, "System.dll"))
                    End If

                    runtimes.Add(PathUtilities.CombinePathsUnchecked(sdkPath, "mscorlib.dll"))
                End If

                Return runtimes.ToImmutable()
            End Function

            Friend Sub DeleteImport(wszImport As String)
                Dim index = _imports.FindIndex(Function(import) import.Clause.ToFullString() = wszImport)
                If index >= 0 Then
                    _imports.RemoveAt(index)
                    UpdateProjectForNewHostValues()
                End If
            End Sub

            Friend Sub DeleteAllImports()
                _imports.Clear()
                UpdateProjectForNewHostValues()
            End Sub

            Protected Overrides Function ComputeParseOptionsWithHostValues(parseOptions As ParseOptions) As ParseOptions
                Dim visualBasicParseOptions = DirectCast(parseOptions, VisualBasicParseOptions)
                Return ApplyVisualBasicParseOptionsFromCompilerOptions(visualBasicParseOptions, _rawOptions)
            End Function

            Friend Shared Function ApplyVisualBasicParseOptionsFromCompilerOptions(parseOptions As VisualBasicParseOptions, ByRef compilerOptions As VBCompilerOptions) As VisualBasicParseOptions
                parseOptions = parseOptions.WithPreprocessorSymbols(
                    GetConditionalCompilationSymbols(GetOutputKind(compilerOptions), If(compilerOptions.wszCondComp, "")))

                ' For language versions after VB 15, we expect the version to be passed from MSBuild to the IDE
                ' via command-line arguments (`ICompilerOptionsHostObject.SetCompilerOptions`)
                ' instead of using `IVbcHostObject3.SetLanguageVersion`. Thus, if we already got a value, then we're good
                If parseOptions.LanguageVersion <= LanguageVersion.VisualBasic15 Then
                    parseOptions = parseOptions.WithLanguageVersion(compilerOptions.langVersion)
                End If

                Return parseOptions _
                    .WithDocumentationMode(If(Not String.IsNullOrEmpty(compilerOptions.wszXMLDocName), DocumentationMode.Diagnose, DocumentationMode.Parse))
            End Function

            Private Shared Function GetConditionalCompilationSymbols(kind As OutputKind, str As String) As ImmutableArray(Of KeyValuePair(Of String, Object))
                Debug.Assert(str IsNot Nothing)
                Dim key = KeyValuePairUtil.Create(str, kind)

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
                        Throw ExceptionUtilities.UnexpectedValue(level)
                End Select
            End Function
        End Class
    End Class
End Namespace
