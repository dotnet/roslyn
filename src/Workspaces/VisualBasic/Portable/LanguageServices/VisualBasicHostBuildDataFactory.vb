' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    <ExportLanguageService(GetType(ICommandLineArgumentsFactoryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCommandLineArgumentsFactoryService
        Implements ICommandLineArgumentsFactoryService

        Public Function CreateCommandLineArguments(arguments As IEnumerable(Of String), baseDirectory As String, isInteractive As Boolean, sdkDirectory As String) As CommandLineArguments Implements ICommandLineArgumentsFactoryService.CreateCommandLineArguments
#If SCRIPTING Then
            Dim parser = If(isInteractive, VisualBasicCommandLineParser.Interactive, VisualBasicCommandLineParser.Default)
#Else
            Dim parser = VisualBasicCommandLineParser.Default
#End If
            Return parser.Parse(arguments, baseDirectory, sdkDirectory)
        End Function
    End Class


    <ExportLanguageService(GetType(IHostBuildDataFactory), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicHostBuildDataFactory
        Implements IHostBuildDataFactory

        Public Function Create(options As IHostBuildOptions) As HostBuildData Implements IHostBuildDataFactory.Create

            Dim parseOptions = VisualBasicParseOptions.Default.WithDocumentationMode(DocumentationMode.Parse)
            Dim compilationOptions = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication,
                        xmlReferenceResolver:=New XmlFileResolver(options.ProjectDirectory),
                        sourceReferenceResolver:=New SourceFileResolver(ImmutableArray(Of String).Empty, options.ProjectDirectory),
                        metadataReferenceResolver:=New AssemblyReferenceResolver(
                            New MetadataFileReferenceResolver(ImmutableArray(Of String).Empty, options.ProjectDirectory),
                            MetadataFileReferenceProvider.Default),
                        strongNameProvider:=New DesktopStrongNameProvider(ImmutableArray.Create(Of String)(options.ProjectDirectory, options.OutputDirectory)),
                        assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default)

            If Not String.IsNullOrEmpty(options.PlatformWith32BitPreference) Then
                Dim plat As Platform
                If [Enum].TryParse(options.PlatformWith32BitPreference, True, plat) Then
                    Dim outputKind = compilationOptions.OutputKind
                    If plat = Platform.AnyCpu AndAlso outputKind <> OutputKind.DynamicallyLinkedLibrary AndAlso outputKind <> OutputKind.NetModule AndAlso outputKind <> OutputKind.WindowsRuntimeMetadata Then
                        plat = Platform.AnyCpu32BitPreferred
                    End If
                    compilationOptions = compilationOptions.WithPlatform(plat)
                End If
            End If

            Dim warnings = New Dictionary(Of String, ReportDiagnostic)()

            If options.OutputKind.HasValue Then
                Dim _outputKind = options.OutputKind.Value
                compilationOptions = compilationOptions.WithOutputKind(_outputKind)
                If compilationOptions.Platform = Platform.AnyCpu32BitPreferred AndAlso
                                    (_outputKind = OutputKind.DynamicallyLinkedLibrary Or _outputKind = OutputKind.NetModule Or _outputKind = OutputKind.WindowsRuntimeMetadata) Then
                    compilationOptions = compilationOptions.WithPlatform(Platform.AnyCpu)
                End If
            End If

            If Not String.IsNullOrEmpty(options.DefineConstants) Then
                Dim errors As IEnumerable(Of Diagnostic) = Nothing
                parseOptions = parseOptions.WithPreprocessorSymbols(VisualBasicCommandLineParser.ParseConditionalCompilationSymbols(options.DefineConstants, errors))
            End If

            If options.DelaySign IsNot Nothing Then
                compilationOptions = compilationOptions.WithDelaySign(options.DelaySign.Item1)
            End If

            If Not String.IsNullOrEmpty(options.DocumentationFile) Then
                parseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Diagnose)
            Else
                parseOptions = parseOptions.WithDocumentationMode(DocumentationMode.Parse)
            End If

            If options.GlobalImports.Count > 0 Then
                Dim e = options.GlobalImports.Select(Function(item) GlobalImport.Parse(item)).AsImmutable()
                compilationOptions = compilationOptions.WithGlobalImports(e)
            End If

            If Not String.IsNullOrEmpty(options.KeyContainer) Then
                compilationOptions = compilationOptions.WithCryptoKeyContainer(options.KeyContainer)
            End If

            If Not String.IsNullOrEmpty(options.KeyFile) Then
                compilationOptions = compilationOptions.WithCryptoKeyFile(options.KeyFile)
            End If

            If Not String.IsNullOrEmpty(options.MainEntryPoint) AndAlso options.MainEntryPoint <> "Sub Main" Then
                compilationOptions = compilationOptions.WithMainTypeName(options.MainEntryPoint)
            End If

            If options.NoWarnings.HasValue Then
                compilationOptions = compilationOptions.WithGeneralDiagnosticOption(If(options.NoWarnings.Value, ReportDiagnostic.Suppress, ReportDiagnostic.Warn))
            End If

            If options.Optimize.HasValue Then
                compilationOptions = compilationOptions.WithOptimizationLevel(If(options.Optimize, OptimizationLevel.Release, OptimizationLevel.Debug))
            End If

            If Not String.IsNullOrEmpty(options.OptionCompare) Then
                compilationOptions = compilationOptions.WithOptionCompareText(options.OptionCompare = "Text")
            End If

            If options.OptionExplicit.HasValue Then
                compilationOptions = compilationOptions.WithOptionExplicit(options.OptionExplicit.Value)
            End If

            If Not String.IsNullOrEmpty(options.OptionStrict) Then
                Dim _optionStrict As OptionStrict = OptionStrict.Custom
                If TryGetOptionStrict(options.OptionStrict, _optionStrict) Then
                    compilationOptions = compilationOptions.WithOptionStrict(_optionStrict)
                End If
            End If

            If Not String.IsNullOrEmpty(options.Platform) Then
                Dim plat As Platform
                If [Enum].TryParse(options.Platform, plat) Then
                    compilationOptions = compilationOptions.WithPlatform(plat)
                End If
            End If

            If options.CheckForOverflowUnderflow.HasValue Then
                compilationOptions = compilationOptions.WithOverflowChecks(options.CheckForOverflowUnderflow.Value)
            End If

            If Not String.IsNullOrEmpty(options.RootNamespace) Then
                compilationOptions = compilationOptions.WithRootNamespace(options.RootNamespace)
            End If

            If Not String.IsNullOrEmpty(options.RuleSetFile) Then
                Dim fullPath = FileUtilities.ResolveRelativePath(options.RuleSetFile, options.ProjectDirectory)

                Dim specificDiagnosticOptions As Dictionary(Of String, ReportDiagnostic) = Nothing
                Dim generalDiagnosticOption = RuleSet.GetDiagnosticOptionsFromRulesetFile(fullPath, specificDiagnosticOptions)
                compilationOptions = compilationOptions.WithGeneralDiagnosticOption(generalDiagnosticOption)
                warnings.AddRange(specificDiagnosticOptions)
            End If

            If options.WarningsAsErrors.HasValue Then
                compilationOptions = compilationOptions.WithGeneralDiagnosticOption(If(options.WarningsAsErrors.Value, ReportDiagnostic.Error, ReportDiagnostic.Warn))
            End If

            If options.OptionInfer.HasValue Then
                compilationOptions = compilationOptions.WithOptionInfer(options.OptionInfer.Value)
            End If

            If options.VBRuntime = "Embed" Then
                compilationOptions = compilationOptions.WithEmbedVbCoreRuntime(True)
            End If

            If Not String.IsNullOrEmpty(options.LanguageVersion) Then
                Dim version As Integer
                If Int32.TryParse(options.LanguageVersion, version) Then
                    Dim lv As LanguageVersion = CType(version, LanguageVersion)
                    If [Enum].IsDefined(GetType(LanguageVersion), lv) Then
                        parseOptions = parseOptions.WithLanguageVersion(lv)
                    End If
                End If
            End If

            parseOptions = parseOptions.WithPreprocessorSymbols(AddPredefinedPreprocessorSymbols(compilationOptions.OutputKind, parseOptions.PreprocessorSymbols))
            compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(warnings)
            Return New HostBuildData(parseOptions, compilationOptions)
        End Function

        Private Shared Function TryGetOptionStrict(text As String, ByRef optionStrictType As OptionStrict) As Boolean
            If text = "On" Then
                optionStrictType = OptionStrict.On
                Return True
            ElseIf text = "Off" OrElse text = "Custom" Then
                optionStrictType = OptionStrict.Custom
                Return True
            Else
                Return False
            End If
        End Function


    End Class

End Namespace

