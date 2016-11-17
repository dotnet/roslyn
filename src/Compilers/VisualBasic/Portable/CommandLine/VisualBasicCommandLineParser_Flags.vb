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

    Partial Public Class VisualBasicCommandLineParser
        Inherits CommandLineParser

        Friend Class Flags

            Protected Friend Enum Validation
                Success
                Failure
            End Enum

            Protected Friend Class Win32

                Private Const s_win32Manifest As String = "win32manifest"
                Private Const s_win32Icon As String = "win32icon"
                Private Const s_win32Res As String = "win32resource"

                Protected Friend Shared Function Manifest(diagnostics As List(Of Diagnostic), ByRef win32ManifestFile As String, value As String) As Validation
                    win32ManifestFile = GetWin32Setting(s_win32Manifest, RemoveQuotesAndSlashes(value), diagnostics)
                    Return Validation.Success
                End Function

                Protected Friend Shared Function Icon(diagnostics As List(Of Diagnostic), ByRef win32IconFile As String, value As String) As Validation
                    win32IconFile = GetWin32Setting(s_win32Icon, RemoveQuotesAndSlashes(value), diagnostics)
                    Return Validation.Success
                End Function

                Protected Friend Shared Function Resource(Diagnostics As List(Of Diagnostic), ByRef win32ResourceFile As String, value As String) As Validation
                    win32ResourceFile = GetWin32Setting(s_win32Res, RemoveQuotesAndSlashes(value), Diagnostics)
                    Return Validation.Success
                End Function

                Protected Friend Shared Function NoManifest(ByRef noWin32Manifest As Boolean, value As String) As Validation
                    If value IsNot Nothing Then
                        Return Validation.Failure
                    Else
                        noWin32Manifest = True
                        Return Validation.Success
                    End If
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

                Friend Shared Sub ValidateWin32Settings(noWin32Manifest As Boolean, win32ResSetting As String, win32IconSetting As String, win32ManifestSetting As String, outputKind As OutputKind, diagnostics As List(Of Diagnostic))
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

            End Class

            Protected Friend Shared Function Help(
                                                   value As String,
                                             ByRef display As (Logo As Boolean, Help As Boolean, Version As Boolean)
                                                 ) As Validation
                If value IsNot Nothing Then
                    Return Validation.Failure
                Else
                    display.Help = True
                    Return Validation.Success
                End If
            End Function

            Protected Friend Shared Function Version(
                                                      value As String,
                                                ByRef display As (Logo As Boolean, Help As Boolean, Version As Boolean)
                                                    ) As Validation
                If value IsNot Nothing Then
                    Return Validation.Failure
                Else
                    display.Version = True
                    Return Validation.Success
                End If
            End Function

            Protected Friend Shared Function Analyzer(
                                                       diagnostics As List(Of Diagnostic),
                                                       analyzers As List(Of CommandLineAnalyzerReference),
                                                       _arg As (name As String, value As String)
                                                     ) As Validation
                analyzers.AddRange(ParseAnalyzers(_arg, diagnostics))
                Return Validation.Success
            End Function

            Protected Friend Shared Function Reference(
                                                        diagnostics As List(Of Diagnostic),
                                                        metadataReferences As List(Of CommandLineReference),
                                                        _arg As (name As String, value As String)
                                                      ) As Validation
                metadataReferences.AddRange(ParseAssemblyReferences(_arg.name, _arg.value, diagnostics, embedInteropTypes:=False))
                Return Validation.Success
            End Function

            Protected Friend Shared Function Define(
                                                     diagnostics As List(Of Diagnostic),
                                               ByRef defines As IReadOnlyDictionary(Of String, Object),
                                                     _arg As (name As String, value As String)
                                                   ) As Validation
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, ":<symbol_list>")
                Else
                    Dim conditionalCompilationDiagnostics As IEnumerable(Of Diagnostic) = Nothing
                    defines = ParseConditionalCompilationSymbols(_arg.value, conditionalCompilationDiagnostics, defines)
                    diagnostics.AddRange(conditionalCompilationDiagnostics)
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function [Imports](
                                                        diagnostics As List(Of Diagnostic),
                                                  ByRef globalImports As List(Of GlobalImport),
                                                        _arg As (name As String, value As String)
                                                      ) As Validation
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, If(_arg.name = "import", ":<str>", ":<import_list>"))
                Else
                    ParseGlobalImports(_arg.value, globalImports, diagnostics)
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Option_Strict(
                                                            diagnostics As List(Of Diagnostic),
                                                      ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean),
                                                            _arg As (name As String, value As String)
                                                          ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optionstrict")
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "+"c : _option.Strict = VisualBasic.OptionStrict.On
                        Case "-"c : _option.Strict = VisualBasic.OptionStrict.Off
                        Case Else
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Option_Strict(
                                                            diagnostics As List(Of Diagnostic),
                                                      ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean),
                                                            value As String
                                                          ) As Validation
                value = RemoveQuotesAndSlashes(value)
                If value Is Nothing Then
                    _option.Strict = VisualBasic.OptionStrict.On
                ElseIf String.Equals(value, "custom", StringComparison.OrdinalIgnoreCase) Then
                    _option.Strict = VisualBasic.OptionStrict.Custom
                Else
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "optionstrict", ":custom")
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Option_Compare(
                                                             diagnostics As List(Of Diagnostic),
                                                       ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean),
                                                             value As String
                                                           ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function Option_Explicit(
                                                              diagnostics As List(Of Diagnostic),
                                                        ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean),
                                                              _arg As (name As String, value As String)
                                                            ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optionexplicit")
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "t"c,
                         "+"c : _option.Explicit = True
                        Case "-"c : _option.Explicit = False
                        Case Else
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Option_Infer(
                                                           diagnostics As List(Of Diagnostic),
                                                     ByRef _option As (Strict As OptionStrict, Infer As Boolean, Explicit As Boolean, CompareText As Boolean),
                                                           _arg As (name As String, value As String)
                                                         ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optioninfer")
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "r"c,
                         "+"c : _option.Infer = True
                        Case "-"c : _option.Infer = False
                        Case Else
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function CodePage(
                                                       diagnostics As List(Of Diagnostic),
                                                 ByRef _codepage As Encoding,
                                                       _arg As (name As String, value As String)
                                                     ) As Validation
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "codepage", ":<number>")
                Else
                    Dim encoding = TryParseEncodingName(_arg.value)
                    If encoding Is Nothing Then
                        AddDiagnostic(diagnostics, ERRID.ERR_BadCodepage, _arg.value)
                    Else
                        _codepage = encoding
                    End If
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function ChecksumAlgorithm(
                                                                diagnostics As List(Of Diagnostic),
                                                          ByRef _checksumAlgorithm As SourceHashAlgorithm,
                                                                value As String
                                                              ) As Validation
                value = RemoveQuotesAndSlashes(value)
                If String.IsNullOrEmpty(value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "checksumalgorithm", ":<algorithm>")
                Else

                    Dim newChecksumAlgorithm = TryParseHashAlgorithmName(value)
                    If newChecksumAlgorithm = SourceHashAlgorithm.None Then
                        AddDiagnostic(diagnostics, ERRID.ERR_BadChecksumAlgorithm, value)
                    Else
                        _checksumAlgorithm = newChecksumAlgorithm
                    End If
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function RemoveIntChecks(
                                                              diagnostics As List(Of Diagnostic),
                                                        ByRef checkOverflow As Boolean,
                                                              _arg As (name As String, value As String)
                                                            ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "removeintchecks")
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "s"c,
                         "+"c : checkOverflow = False
                        Case "-"c : checkOverflow = True
                        Case Else
                    End Select
                End If
                Return Validation.Success
            End Function

            ''' <summary>
            ''' The use of SQM is deprecated in the compiler but we still support the command line parsing for  back compat reasons.
            ''' </summary>
            Protected Friend Shared Function SQMSessionGuid(
                                                             diagnostics As List(Of Diagnostic),
                                                       ByRef _arg As (name As String, value As String)
                                                           ) As Validation
                ' The use of SQM is deprecated in the compiler but we still support the command line parsing for 
                ' back compat reasons.
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                If String.IsNullOrWhiteSpace(_arg.value) = True Then
                    AddDiagnostic(diagnostics, ERRID.ERR_MissingGuidForOption, _arg.value, _arg.name)
                Else
                    Dim _sqmsessionguid As Guid
                    If Not Guid.TryParse(_arg.value, _sqmsessionguid) Then
                        AddDiagnostic(diagnostics, ERRID.ERR_InvalidFormatForGuidForOption, _arg.value, _arg.name)
                    End If
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function PreferredUILang(
                                                              diagnostics As List(Of Diagnostic),
                                                        ByRef _preferredUILang As CultureInfo,
                                                              _arg As (name As String, value As String)
                                                            ) As Validation
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                If (String.IsNullOrEmpty(_arg.value)) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, ":<string>")
                Else
                    Try
                        _preferredUILang = New CultureInfo(_arg.value)
                        If (CorLightup.Desktop.IsUserCustomCulture(_preferredUILang)) Then
                            ' Do not use user custom cultures.
                            _preferredUILang = Nothing
                        End If
                    Catch ex As CultureNotFoundException
                    End Try

                    If _preferredUILang Is Nothing Then
                        AddDiagnostic(diagnostics, ERRID.WRN_BadUILang, _arg.value)
                    End If
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function LibPath(
                                                      diagnostics As List(Of Diagnostic),
                                                      Paths As (SDK As List(Of String),
                                                                [LIB] As List(Of String),
                                                                Source As List(Of String),
                                                                KeyFileSearch As List(Of String),
                                                                Response As List(Of String)),
                                                      _arg As (name As String, value As String)
                                                    ) As Validation
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, ":<path_list>")
                Else
                    Paths.LIB.AddRange(ParseSeparatedPaths(_arg.value))
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Interactive(
                                                          diagnostics As List(Of Diagnostic),
                                                    ByRef interactiveMode As Boolean,
                                                          _arg As (name As String, value As String)
                                                        ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "i")
                End If
                Dim ch = _arg.name.Last
                Select Case ch
                    Case "i"c,
                     "+"c : interactiveMode = True
                    Case "-"c : interactiveMode = False
                End Select
                Return Validation.Success
            End Function

            Protected Friend Shared Function LoadPaths(
                                                        diagnostics As List(Of Diagnostic),
                                                  ByRef Paths As (SDK As List(Of String),
                                                                  [LIB] As List(Of String),
                                                                  Source As List(Of String),
                                                                  KeyFileSearch As List(Of String),
                                                                  Response As List(Of String)),
                                                        _arg As (name As String, value As String)
                                                      ) As Validation
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, ":<path_list>")
                Else
                    Paths.Source.AddRange(ParseSeparatedPaths(_arg.value))
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Target(
                                                     diagnostics As List(Of Diagnostic),
                                               ByRef Output As (UTF8 As Boolean,
                                                                FileName As String,
                                                                Directory As String,
                                                                Kind As OutputKind),
                                                     _arg As (name As String, value As String)
                                                   ) As Validation
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                Output.Kind = ParseTarget(_arg.name, _arg.value, diagnostics)
                Return Validation.Success
            End Function

            Protected Friend Shared Function ModuleAssemblyName(
                                                                 diagnostics As List(Of Diagnostic),
                                                           ByRef _moduleAssemblyName As String,
                                                                 _arg As (name As String, value As String)
                                                               ) As Validation
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                Dim identity As AssemblyIdentity = Nothing
                ' Note that native compiler also extracts public key, but Roslyn doesn't use it.
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "moduleassemblyname", ":<string>")
                ElseIf Not AssemblyIdentity.TryParseDisplayName(_arg.value, identity) OrElse
                       Not MetadataHelpers.IsValidAssemblyOrModuleName(identity.Name) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_InvalidAssemblyName, _arg.value, _arg.value)
                Else
                    _moduleAssemblyName = identity.Name
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function RootNamespace(
                                                            diagnostics As List(Of Diagnostic),
                                                      ByRef _rootNamespace As String,
                                                            value As String
                                                          ) As Validation
                value = RemoveQuotesAndSlashes(value)
                If String.IsNullOrEmpty(value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "rootnamespace", ":<string>")
                Else
                    _rootNamespace = value
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function SDKPath(
                                                      diagnostics As List(Of Diagnostic),
                                                      Paths As (SDK As List(Of String),
                                                                [LIB] As List(Of String),
                                                                Source As List(Of String),
                                                                KeyFileSearch As List(Of String),
                                                                Response As List(Of String)),
                                                      value As String) As Validation
                If String.IsNullOrEmpty(value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "sdkpath", ":<path>")
                Else
                    Paths.SDK.Clear()
                    Paths.SDK.AddRange(ParseSeparatedPaths(value))
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Instrument(
                                                         diagnostics As List(Of Diagnostic),
                                                         instrumentationKinds As ArrayBuilder(Of InstrumentationKind),
                                                         value As String
                                                       ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function AddModule(
                                                        diagnostics As List(Of Diagnostic),
                                                        metadataReferences As List(Of CommandLineReference),
                                                        value As String
                                                      ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function Link(
                                                   diagnostics As List(Of Diagnostic),
                                                   metadataReferences As List(Of CommandLineReference),
                                                   _arg As (name As String, value As String)
                                                 ) As Validation
                metadataReferences.AddRange(ParseAssemblyReferences(_arg.name, _arg.value, diagnostics, embedInteropTypes:=True))
                Return Validation.Success
            End Function

            Protected Friend Shared Function Resource(
                                                       baseDirectory As String,
                                                       diagnostics As List(Of Diagnostic),
                                                       managedResources As List(Of ResourceDescription),
                                                       _arg As (name As String, value As String)
                                                     ) As Validation
                Dim embeddedResource = ParseResourceDescription(_arg.name, _arg.value, baseDirectory, diagnostics, embedded:=True)
                If embeddedResource IsNot Nothing Then
                    managedResources.Add(embeddedResource)
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function LinkResource(
                                                           baseDirectory As String,
                                                           diagnostics As List(Of Diagnostic),
                                                           managedResources As List(Of ResourceDescription),
                                                           _arg As (name As String, value As String)
                                                         ) As Validation
                Dim linkedResource = ParseResourceDescription(_arg.name, _arg.value, baseDirectory, diagnostics, embedded:=False)
                If linkedResource IsNot Nothing Then
                    managedResources.Add(linkedResource)
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Debug(
                                                    diagnostics As List(Of Diagnostic),
                                              ByRef emitPdb As Boolean,
                                              ByRef debugInformationFormat As DebugInformationFormat,
                                                    value As String
                                                  ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function Debug(
                                                    diagnostics As List(Of Diagnostic),
                                              ByRef emitPdb As Boolean,
                                                    _arg As (name As String, value As String)
                                                  ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "debug")
                End If
                Dim ch = _arg.name.Last
                Select Case ch
                    Case "+"c : emitPdb = True
                    Case "-"c : emitPdb = False
                End Select
                Return Validation.Success
            End Function

            Protected Friend Shared Function Optimize(
                                                       diagnostics As List(Of Diagnostic),
                                                 ByRef _optimize As Boolean,
                                                       _arg As (name As String, value As String)
                                                     ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "optimize")
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "e"c,
                             "+"c : _optimize = True
                        Case "-"c : _optimize = False
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Parallel(
                                                       diagnostics As List(Of Diagnostic),
                                                 ByRef concurrentBuild As Boolean,
                                                       _arg As (name As String, value As String)
                                                     ) As Validation
                Dim x = _arg.name.Length
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, If((x = 1 OrElse x = 8), _arg.name, _arg.name.Substring(0, _arg.name.Length - 1)))
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "l"c,
                             "p"c,
                             "+"c : concurrentBuild = True
                        Case "-"c : concurrentBuild = False
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Deterministic(
                                                            diagnostics As List(Of Diagnostic),
                                                      ByRef _deterministic As Boolean,
                                                            _arg As (name As String, value As String)
                                                          ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, _arg.name)
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "c"c,
                             "+"c : _deterministic = True
                        Case "-"c : _deterministic = False
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function WarnAsError(
                                                    ByRef generalDiagnosticOption As ReportDiagnostic,
                                                          specificDiagnosticOptionsFromRuleSet As Dictionary(Of String, ReportDiagnostic),
                                                          specificDiagnosticOptionsFromGeneralArguments As Dictionary(Of String, ReportDiagnostic),
                                                          specificDiagnosticOptionsFromSpecificArguments As Dictionary(Of String, ReportDiagnostic),
                                                          value As String
                                                        ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function WarnAsError_Minus(
                                                          ByRef generalDiagnosticOption As ReportDiagnostic,
                                                                specificDiagnosticOptionsFromRuleSet As Dictionary(Of String, ReportDiagnostic),
                                                                specificDiagnosticOptionsFromGeneralArguments As Dictionary(Of String, ReportDiagnostic),
                                                          ByRef specificDiagnosticOptionsFromSpecificArguments As Dictionary(Of String, ReportDiagnostic),
                                                                value As String
                                                              ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function NoWarn(
                                               ByRef generalDiagnosticOption As ReportDiagnostic,
                                                     specificDiagnosticOptionsFromRuleSet As Dictionary(Of String, ReportDiagnostic),
                                                     specificDiagnosticOptionsFromGeneralArguments As Dictionary(Of String, ReportDiagnostic),
                                                     specificDiagnosticOptionsFromNoWarnArguments As Dictionary(Of String, ReportDiagnostic),
                                                     value As String
                                                   ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function LangVersion(
                                                          diagnostics As List(Of Diagnostic),
                                                    ByRef languageVersion As LanguageVersion,
                                                          value As String
                                                        ) As Validation
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
                Return Validation.Success
            End Function

            Protected Friend Shared Function DelaySign(
                                                        diagnostics As List(Of Diagnostic),
                                                  ByRef delaySignSetting As Boolean?,
                                                        _arg As (name As String, value As String)
                                                      ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "delaysign")
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "n"c,
                         "+"c : delaySignSetting = True
                        Case "-"c : delaySignSetting = False
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function PublicSign(
                                                         diagnostics As List(Of Diagnostic),
                                                   ByRef _publicSign As Boolean,
                                                         _arg As (name As String, value As String)
                                                       ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "publicsign")
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "n"c,
                             "+"c : _publicSign = True
                        Case "-"c : _publicSign = False
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function KeyFile(
                                                      diagnostics As List(Of Diagnostic),
                                                ByRef keyFileSetting As String,
                                                ByRef keyContainerSetting As String,
                                                      _arg As (name As String, value As String)
                                                    ) As Validation
                Return Parse_Key_(diagnostics, keyFileSetting, keyContainerSetting, _arg, ":<file>")
            End Function

            Protected Friend Shared Function KeyContainer(
                                                           diagnostics As List(Of Diagnostic),
                                                     ByRef keyFileSetting As String,
                                                     ByRef keyContainerSetting As String,
                                                           _arg As (name As String, value As String)
                                                         ) As Validation
                Return Parse_Key_(diagnostics, keyContainerSetting, keyFileSetting, _arg, ":<string>")
            End Function

            Protected Friend Shared Function Parse_Key_(
                                                         diagnostics As List(Of Diagnostic),
                                                   ByRef SettingA As String,
                                                   ByRef SettingB As String,
                                                         _arg As (name As String, value As String),
                                                         param As String
                                                       ) As Validation

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
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                SettingB = Nothing
                If String.IsNullOrWhiteSpace(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, param)
                Else
                    SettingA = RemoveQuotesAndSlashes(_arg.value)
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function HighEntropyVA(
                                                      ByRef _highEntropyVA As Boolean,
                                                            _arg As (name As String, value As String)
                                                          ) As Validation
                If _arg.value IsNot Nothing Then
                    Return Validation.Failure
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "a"c,
                             "+"c : _highEntropyVA = True
                        Case "-"c : _highEntropyVA = False
                    End Select
                    Return Validation.Success
                End If
            End Function

            Protected Friend Shared Function NoLogo(
                                               ByRef display As (Logo As Boolean, Help As Boolean, Version As Boolean),
                                                     _arg As (name As String, value As String)
                                                   ) As Validation
                If _arg.value IsNot Nothing Then
                    Return Validation.Failure
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "o"c,
                         "+"c : display.Logo = False
                        Case "-"c : display.Logo = True
                    End Select
                    Return Validation.Success
                End If
            End Function

            Protected Friend Shared Function OutputLevel(
                                                    ByRef _outputLevel As OutputLevel,
                                                          _arg As (name As String, value As String)
                                                        ) As Validation
                If _arg.value IsNot Nothing Then
                    Return Validation.Failure
                Else
                    Dim ch = _arg.name(0)
                    Select Case ch
                        Case "q"c : _outputLevel = VisualBasic.OutputLevel.Quiet
                        Case "v"c : _outputLevel = VisualBasic.OutputLevel.Verbose
                    End Select
                    Return Validation.Success
                End If
            End Function

            Protected Friend Shared Function Quiet(
                                                    diagnostics As List(Of Diagnostic),
                                              ByRef outputLevel As OutputLevel,
                                                    _arg As (name As String, value As String)
                                                  ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "quiet")
                Else
                    Dim param = _arg.name.Last
                    Select Case param
                        Case "+"c : outputLevel = VisualBasic.OutputLevel.Quiet
                        Case "-"c : outputLevel = VisualBasic.OutputLevel.Normal
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Verbose(
                                                      diagnostics As List(Of Diagnostic),
                                                ByRef outputLevel As OutputLevel,
                                                      _arg As (name As String, value As String)
                                                    ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, _arg.name.Substring(0, _arg.name.Length - 1))
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "-"c : outputLevel = VisualBasic.OutputLevel.Normal
                        Case "+"c : outputLevel = VisualBasic.OutputLevel.Verbose
                    End Select
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function UTF8_Output(
                                                          diagnostics As List(Of Diagnostic),
                                                    ByRef Output As (UTF8 As Boolean, FileName As String, Directory As String, Kind As OutputKind),
                                                          _arg As (name As String, value As String)
                                                        ) As Validation
                If _arg.value IsNot Nothing Then
                    AddDiagnostic(diagnostics, ERRID.ERR_SwitchNeedsBool, "utf8output")
                End If
                Dim ch = _arg.name.Last
                Select Case ch
                    Case "t"c,
                     "+"c : Output.UTF8 = True
                    Case "-"c : Output.UTF8 = False
                End Select
                Return Validation.Success
            End Function

            Protected Friend Shared Function Main(
                                                   diagnostics As List(Of Diagnostic),
                                             ByRef mainTypeName As String,
                                                   _arg As (name As String, value As String)
                                                 ) As Validation
                ' MSBuild can result in maintypename being passed in quoted when Cyrillic namespace was being used resulting
                ' in ERRID.ERR_StartupCodeNotFound1 diagnostic.   The additional quotes cause problems and quotes are not a 
                ' valid character in typename.
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, ":<class>")
                Else
                    mainTypeName = _arg.value
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function SubSystemVersion(
                                                               diagnostics As List(Of Diagnostic),
                                                         ByRef ssVersion As SubsystemVersion,
                                                               _arg As (name As String, value As String)
                                                             ) As Validation
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                If String.IsNullOrEmpty(_arg.value) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, ":<version>")
                Else
                    Dim version As SubsystemVersion = Nothing
                    If CodeAnalysis.SubsystemVersion.TryParse(_arg.value, version) Then
                        ssVersion = version
                    Else
                        AddDiagnostic(diagnostics, ERRID.ERR_InvalidSubsystemVersion, _arg.value)
                    End If
                End If
                Return Flags.Validation.Success
            End Function

            Protected Friend Shared Function TouchedFiles(
                                                           diagnostics As List(Of Diagnostic),
                                                     ByRef touchedFilesPath As String,
                                                           _arg As (name As String, value As String)
                                                         ) As Validation
                Dim unquoted = RemoveQuotesAndSlashes(_arg.value)
                If (String.IsNullOrEmpty(unquoted)) Then
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, _arg.name, ":<touchedfiles>")
                Else
                    touchedFilesPath = unquoted
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function NoSTDLib(
                                                 ByRef _noStdLib As Boolean,
                                                       value As String
                                                     ) As Validation
                If value IsNot Nothing Then
                    Return Validation.Failure
                Else
                    _noStdLib = True
                    Return Validation.Success
                End If
            End Function

            Protected Friend Shared Function VBRuntime(
                                                  ByRef _VBRuntime As (_Path As String, IncludeReference As Boolean, EmbedCore As Boolean),
                                                        value As String
                                                      ) As Validation
                If value Is Nothing Then
                    _VBRuntime = (_Path:=Nothing, IncludeReference:=True, EmbedCore:=False)
                Else
                    ' NOTE: that Dev11 does not report errors on empty or invalid file specified
                    _VBRuntime = (_Path:=RemoveQuotesAndSlashes(value), IncludeReference:=True, EmbedCore:=False)
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function VBRuntime(
                                                  ByRef _VBRuntime As (_Path As String, IncludeReference As Boolean, EmbedCore As Boolean),
                                                        _arg As (name As String, value As String)
                                                      ) As Validation
                If _arg.value IsNot Nothing Then
                    Return Validation.Failure
                Else
                    Dim ch = _arg.name.Last
                    Select Case ch
                        Case "+"c : _VBRuntime = (_Path:=Nothing, IncludeReference:=True, EmbedCore:=False)
                        Case "-"c : _VBRuntime = (_Path:=Nothing, IncludeReference:=False, EmbedCore:=False)
                        Case "*"c : _VBRuntime = (_Path:=Nothing, IncludeReference:=False, EmbedCore:=True)
                    End Select
                    Return Validation.Success
                End If
            End Function

            Protected Friend Shared Function Platform(
                                                       diagnostics As List(Of Diagnostic),
                                                 ByRef _platform As Platform,
                                                       _arg As (name As String, value As String)
                                                     ) As Validation
                _arg.value = RemoveQuotesAndSlashes(_arg.value)
                If _arg.value IsNot Nothing Then
                    _platform = ParsePlatform(_arg.name, _arg.value, diagnostics)
                Else
                    AddDiagnostic(diagnostics, ERRID.ERR_ArgumentRequired, "platform", ":<string>")
                End If
                Return Validation.Success
            End Function

            Protected Friend Shared Function Features(
                                                       _features As List(Of String),
                                                       value As String
                                                     ) As Validation
                If value Is Nothing Then
                    _features.Clear()
                Else
                    _features.Add(RemoveQuotesAndSlashes(value))
                End If
                Return Validation.Success
            End Function

        End Class

    End Class
End Namespace

