' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class Mutable_VBCommandLineArguments

        Protected Friend Sub New(args As IEnumerable(Of String), c As VisualBasicCommandLineParser, baseDirectory As String, IsScriptRunner As Boolean)
            Me.IsScriptRunner = IsScriptRunner
            scriptArgs = If(IsScriptRunner, New List(Of String)(), Nothing)
            c.FlattenArgs(args, diagnostics, flattenedArgs, scriptArgs, baseDirectory, responsePaths)
            Me.BaseDirectory = baseDirectory
            outputDirectory = Me.BaseDirectory
        End Sub

        Friend BaseDirectory As String
        Friend IsScriptRunner As Boolean
        Friend diagnostics As List(Of Diagnostic) = New List(Of Diagnostic)()
        Friend flattenedArgs As List(Of String) = New List(Of String)()
        Friend scriptArgs As List(Of String)
        ' normalized paths to directories containing response files:
        Friend responsePaths As New List(Of String)

        Friend displayLogo As Boolean = True
        Friend displayHelp As Boolean = False
        Friend outputLevel As OutputLevel = OutputLevel.Normal
        Friend optimize As Boolean = False
        Friend checkOverflow As Boolean = True
        Friend concurrentBuild As Boolean = True
        Friend deterministic As Boolean = False
        Friend emitPdb As Boolean
        Friend debugInformationFormat As DebugInformationFormat = DebugInformationFormat.Pdb
        Friend noStdLib As Boolean = False
        Friend utf8output As Boolean = False
        Friend outputFileName As String = Nothing
        Friend outputDirectory As String = baseDirectory
        Friend documentationPath As String = Nothing
        Friend errorLogPath As String = Nothing
        Friend parseDocumentationComments As Boolean = False ' Don't just null check documentationFileName because we want to do this even if the file name is invalid.
        Friend outputKind As OutputKind = OutputKind.ConsoleApplication
        Friend ssVersion As SubsystemVersion = SubsystemVersion.None
        Friend languageVersion As LanguageVersion = LanguageVersion.VisualBasic14
        Friend mainTypeName As String = Nothing
        Friend win32ManifestFile As String = Nothing
        Friend win32ResourceFile As String = Nothing
        Friend win32IconFile As String = Nothing
        Friend noWin32Manifest As Boolean = False
        Friend managedResources As New List(Of ResourceDescription)()
        Friend sourceFiles As New List(Of CommandLineSourceFile)()
        Friend hasSourceFiles As Boolean = False
        Friend additionalFiles As New List(Of CommandLineSourceFile)()
        Friend codepage As Encoding = Nothing
        Friend checksumAlgorithm As SourceHashAlgorithm = SourceHashAlgorithm.Sha1
        Friend defines As IReadOnlyDictionary(Of String, Object) = Nothing
        Friend metadataReferences As New List(Of CommandLineReference)()
        Friend analyzers As New List(Of CommandLineAnalyzerReference)()
        Friend sdkPaths As New List(Of String)()
        Friend libPaths As New List(Of String)()
        Friend sourcePaths As New List(Of String)()
        Friend keyFileSearchPaths As New List(Of String)()
        Friend globalImports As New List(Of GlobalImport)
        Friend rootNamespace As String = ""
        Friend optionStrict As OptionStrict = OptionStrict.Off
        Friend optionInfer As Boolean = False ' MSDN says: ...The compiler default for this option is /optioninfer-.
        Friend optionExplicit As Boolean = True
        Friend optionCompareText As Boolean = False
        Friend embedVbCoreRuntime As Boolean = False
        Friend platform As Platform = Platform.AnyCpu
        Friend preferredUILang As CultureInfo = Nothing
        Friend fileAlignment As Integer = 0
        Friend baseAddress As ULong = 0
        Friend highEntropyVA As Boolean = False
        Friend vbRuntimePath As String = Nothing
        Friend includeVbRuntimeReference As Boolean = True
        Friend generalDiagnosticOption As ReportDiagnostic = ReportDiagnostic.Default
        Friend pathMap As ImmutableArray(Of KeyValuePair(Of String, String)) = ImmutableArray(Of KeyValuePair(Of String, String)).Empty

        ' Diagnostic ids specified via /nowarn /warnaserror must be processed in case-insensitive fashion.
        Friend specificDiagnosticOptionsFromRuleSet As New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
        Friend specificDiagnosticOptionsFromGeneralArguments As New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
        Friend specificDiagnosticOptionsFromSpecificArguments As New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
        Friend specificDiagnosticOptionsFromNoWarnArguments As New Dictionary(Of String, ReportDiagnostic)(CaseInsensitiveComparison.Comparer)
        Friend keyFileSetting As String = Nothing
        Friend keyContainerSetting As String = Nothing
        Friend delaySignSetting As Boolean? = Nothing
        Friend moduleAssemblyName As String = Nothing
        Friend moduleName As String = Nothing
        Friend sqmsessionguid As Guid = Nothing
        Friend touchedFilesPath As String = Nothing
        Friend features As New List(Of String)()
        Friend reportAnalyzer As Boolean = False
        Friend publicSign As Boolean = False

        Friend Function ToImmutable(parsedFeatures As ImmutableDictionary(Of String, String),
                                    specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)),
                                    compilationName As String,
                                    GenerateFileNameForDocComment As String,
                                    defaultCoreLibraryReference As CommandLineReference?,
                                    searchPaths As ImmutableArray(Of String)
                                    ) As VisualBasicCommandLineArguments
            Dim parseOptions = New VisualBasicParseOptions(
languageVersion:=languageVersion,
documentationMode:=If(parseDocumentationComments, DocumentationMode.Diagnose, DocumentationMode.None),
kind:=SourceCodeKind.Regular,
preprocessorSymbols:=AddPredefinedPreprocessorSymbols(outputKind, defines.AsImmutableOrEmpty()),
features:=parsedFeatures)

            Dim scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script)

            ' We want to report diagnostics with source suppression in the error log file.
            ' However, these diagnostics won't be reported on the command line.
            Dim reportSuppressedDiagnostics = errorLogPath IsNot Nothing

            Dim options = New VisualBasicCompilationOptions(
                outputKind:=outputKind,
                moduleName:=moduleName,
                mainTypeName:=mainTypeName,
                scriptClassName:=WellKnownMemberNames.DefaultScriptClassName,
                globalImports:=globalImports,
                rootNamespace:=rootNamespace,
                optionStrict:=optionStrict,
                optionInfer:=optionInfer,
                optionExplicit:=optionExplicit,
                optionCompareText:=optionCompareText,
                embedVbCoreRuntime:=embedVbCoreRuntime,
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
                runtimeMetadataVersion:=Nothing)

            ' add option incompatibility errors if any
            diagnostics.AddRange(options.Errors)

            If documentationPath Is GenerateFileNameForDocComment Then
                documentationPath = PathUtilities.CombineAbsoluteAndRelativePaths(outputDirectory, PathUtilities.RemoveExtension(outputFileName))
                documentationPath = documentationPath + ".xml"
            End If

            Return New VisualBasicCommandLineArguments With
            {
                .IsScriptRunner = IsScriptRunner,
                .BaseDirectory = BaseDirectory,
                .Errors = diagnostics.AsImmutable(),
                .Utf8Output = utf8output,
                .CompilationName = compilationName,
                .OutputFileName = outputFileName,
                .OutputDirectory = outputDirectory,
                .DocumentationPath = documentationPath,
                .ErrorLogPath = errorLogPath,
                .SourceFiles = sourceFiles.AsImmutable(),
                .PathMap = pathMap,
                .Encoding = codepage,
                .ChecksumAlgorithm = checksumAlgorithm,
                .MetadataReferences = metadataReferences.AsImmutable(),
                .AnalyzerReferences = analyzers.AsImmutable(),
                .AdditionalFiles = additionalFiles.AsImmutable(),
                .ReferencePaths = searchPaths,
                .SourcePaths = sourcePaths.AsImmutable(),
                .KeyFileSearchPaths = keyFileSearchPaths.AsImmutable(),
                .Win32ResourceFile = win32ResourceFile,
                .Win32Icon = win32IconFile,
                .Win32Manifest = win32ManifestFile,
                .NoWin32Manifest = noWin32Manifest,
                .DisplayLogo = displayLogo,
                .DisplayHelp = displayHelp,
                .ManifestResources = managedResources.AsImmutable(),
                .CompilationOptions = options,
                .ParseOptions = If(IsScriptRunner, scriptParseOptions, parseOptions),
                .EmitOptions = emitOptions,
                .ScriptArguments = scriptArgs.AsImmutableOrEmpty(),
                .TouchedFilesPath = touchedFilesPath,
                .OutputLevel = outputLevel,
                .EmitPdb = emitPdb,
                .DefaultCoreLibraryReference = defaultCoreLibraryReference,
                .PreferredUILang = preferredUILang,
                .SqmSessionGuid = sqmsessionguid,
                .ReportAnalyzer = reportAnalyzer
            }
        End Function

    End Class

End Namespace