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
    ''' <summary>
    ''' The VisualBasicCommandLineParser class contains members used to perform various Visual Basic command line parsing operations.
    ''' </summary>
    Public Class VisualBasicCommandLineParser
        Inherits CommandLineParser
        ''' <summary>
        ''' Gets the current command line parser.
        ''' </summary>
        Public Shared ReadOnly Property [Default] As VisualBasicCommandLineParser = New VisualBasicCommandLineParser()

        ''' <summary>
        ''' Gets the current interactive command line parser.
        ''' </summary>
        Friend Shared ReadOnly Property ScriptRunner As VisualBasicCommandLineParser = New VisualBasicCommandLineParser(isScriptRunner:=True)

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
        Protected Overrides ReadOnly Property RegularFileExtension As String
            Get
                Return ".vb"
            End Get
        End Property

        ''' <summary>
        ''' Gets the standard Visual Basic script file extension.
        ''' </summary>
        ''' <returns>A string representing the standard Visual Basic script file extension.</returns>
        Protected Overrides ReadOnly Property ScriptFileExtension As String
            Get
                Return ".vbx"
            End Get
        End Property

        Friend NotOverridable Overrides Function CommonParse(args As IEnumerable(Of String), baseDirectory As String, sdkDirectoryOpt As String, additionalReferenceDirectories As String) As CommandLineArguments
            Return Parse(args, baseDirectory, sdkDirectoryOpt, additionalReferenceDirectories)
        End Function

        Private Shared Function ARG_Help(q As Mutable_VBCommandLineArguments, name As String, value As String) As Boolean
            If value IsNot Nothing Then
                Return False
            Else
                q.displayHelp = True
                Return True
            End If
        End Function

        Private Function ARG_OptionStrict(q As Mutable_VBCommandLineArguments, name As String, value As String) As Boolean
            value = RemoveQuotesAndSlashes(value)
            If value Is Nothing Then
                q.optionStrict = VisualBasic.OptionStrict.On
            ElseIf String.Equals(value, "custom", StringComparison.OrdinalIgnoreCase) Then
                q.optionStrict = VisualBasic.OptionStrict.Custom
            Else
                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "optionstrict", ":custom")
            End If
            Return True
        End Function

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
            Dim q As New Mutable_VBCommandLineArguments(args, Me, baseDirectory, IsScriptRunner)
            ' Process ruleset files first so that diagnostic severity settings specified on the command line via
            ' /nowarn and /warnaserror can override diagnostic severity settings specified in the ruleset file.
            If Not IsScriptRunner Then
                For Each arg In q.flattenedArgs
                    Dim name As String = Nothing
                    Dim value As String = Nothing
                    If TryParseOption(arg, name, value) AndAlso (name = "ruleset") Then
                        Dim unquoted = RemoveQuotesAndSlashes(value)
                        If String.IsNullOrEmpty(unquoted) Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file>")
                            Continue For
                        End If

                        q.generalDiagnosticOption = GetDiagnosticOptionsFromRulesetFile(q.specificDiagnosticOptionsFromRuleSet, q.diagnostics, unquoted, baseDirectory)
                    End If
                Next
            End If

            For Each arg In q.flattenedArgs
                Dim ok = False
                Debug.Assert(Not arg.StartsWith("@", StringComparison.Ordinal))

                Dim name As String = Nothing
                Dim value As String = Nothing
                If Not TryParseOption(arg, name, value) Then
                    q.sourceFiles.AddRange(ParseFileArgument(arg, baseDirectory, q.diagnostics))
                    q.hasSourceFiles = True
                    Continue For
                End If

                Select Case name
                    Case "?", "help"
                        ok = ARG_Help(q, name, value)
                    Case "r", "reference"
                        ok = ARG_Reference(q, name, value)
                    Case "a", "analyzer"
                        ok = ARG_Analyzer(q, name, value)
                    Case "d", "define"
                        ok = ARG_Define(q, name, value)
                    Case "imports", "import"
                        ok = ARG_Imports(q, name, value)
                    Case "optionstrict"
                        ok = ARG_OptionStrict(q, name, value)

                    Case "optionstrict+"
                        ok = ARG_OptionStrict_Plus(q, value)

                    Case "optionstrict-"
                        If value IsNot Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optionstrict")
                        Else
                            q.optionStrict = VisualBasic.OptionStrict.Off
                        End If
                        Continue For

                    Case "optioncompare"
                        value = RemoveQuotesAndSlashes(value)
                        If value Is Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "optioncompare", ":binary|text")
                        ElseIf String.Equals(value, "text", StringComparison.OrdinalIgnoreCase) Then
                            q.optionCompareText = True
                        ElseIf String.Equals(value, "binary", StringComparison.OrdinalIgnoreCase) Then
                            q.optionCompareText = False
                        Else
                            AddDiagnostic(q.diagnostics, ERRID.ERR_InvalidSwitchValue, "optioncompare", value)
                        End If

                        Continue For

                    Case "optionexplicit", "optionexplicit+"
                        If value IsNot Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optionexplicit")
                        Else
                            q.optionExplicit = True
                        End If
                        Continue For

                    Case "optionexplicit-"
                        If value IsNot Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optionexplicit")
                        Else
                            q.optionExplicit = False
                        End If
                        Continue For

                    Case "optioninfer", "optioninfer+"
                        If value IsNot Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optioninfer")
                        Else
                            q.optionInfer = True
                        End If
                        Continue For

                    Case "optioninfer-"
                        If value IsNot Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optioninfer")
                            Continue For
                        End If

                        q.optionInfer = False
                        Continue For

                    Case "codepage"
                        value = RemoveQuotesAndSlashes(value)
                        If String.IsNullOrEmpty(value) Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "codepage", ":<number>")
                        Else
                            Dim encoding = TryParseEncodingName(value)
                            If encoding Is Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_BadCodepage, value)
                            Else
                                q.codepage = encoding
                            End If
                        End If
                        Continue For

                    Case "checksumalgorithm"
                        value = RemoveQuotesAndSlashes(value)
                        If String.IsNullOrEmpty(value) Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "checksumalgorithm", ":<algorithm>")
                        Else
                            Dim newChecksumAlgorithm = TryParseHashAlgorithmName(value)
                            If newChecksumAlgorithm = SourceHashAlgorithm.None Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_BadChecksumAlgorithm, value)
                            Else
                                q.checksumAlgorithm = newChecksumAlgorithm
                            End If
                        End If
                        Continue For

                    Case "removeintchecks", "removeintchecks+"
                        If value IsNot Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "removeintchecks")
                        Else
                            q.checkOverflow = False
                        End If
                        Continue For

                    Case "removeintchecks-"
                        If value IsNot Nothing Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "removeintchecks")
                        Else
                            q.checkOverflow = True
                        End If
                        Continue For

                    Case "sqmsessionguid"
                        value = RemoveQuotesAndSlashes(value)
                        If String.IsNullOrWhiteSpace(value) = True Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_MissingGuidForOption, value, name)
                        Else
                            If Not Guid.TryParse(value, q.sqmsessionguid) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_InvalidFormatForGuidForOption, value, name)
                            End If
                        End If
                        Continue For

                    Case "preferreduilang"
                        value = RemoveQuotesAndSlashes(value)
                        If (String.IsNullOrEmpty(value)) Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<string>")
                        Else

                            Try
                                q.preferredUILang = New CultureInfo(value)
                                If (CorLightup.Desktop.IsUserCustomCulture(q.preferredUILang)) Then
                                    ' Do not use user custom cultures.
                                    q.preferredUILang = Nothing
                                End If
                            Catch ex As CultureNotFoundException
                            End Try

                            If q.preferredUILang Is Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.WRN_BadUILang, value)
                            End If
                        End If
                        Continue For

                    Case "lib", "libpath", "libpaths"
                        If String.IsNullOrEmpty(value) Then
                            AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<path_list>")
                        Else
                            q.libPaths.AddRange(ParseSeparatedPaths(value))
                        End If
                        Continue For

#If DEBUG Then
                    Case "attachdebugger"
                        Debugger.Launch()
                        Continue For
#End If
                    Case Else

                End Select
                If ok Then
                    Continue For
                End If
                If IsScriptRunner Then
                    Select Case name
                        Case "loadpath", "loadpaths"
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<path_list>")
                                Continue For
                            End If

                            q.sourcePaths.AddRange(ParseSeparatedPaths(value))
                            Continue For
                    End Select
                Else
                    Select Case name
                        Case "out"
                            If String.IsNullOrWhiteSpace(value) Then
                                ' When the value has " " (e.g., "/out: ")
                                ' the Roslyn VB compiler reports "BC 2006 : option 'out' requires ':<file>',
                                ' While the Dev11 VB compiler reports "BC2012 : can't open ' ' for writing,
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file>")
                            Else
                                ' Even when value is neither null or whitespace, the output file name still could be invalid. (e.g., "/out:sub\ ")
                                ' While the Dev11 VB compiler reports "BC2012: can't open 'sub\ ' for writing,
                                ' the Roslyn VB compiler reports "BC2032: File name 'sub\ ' is empty, contains invalid characters, ..."
                                ' which is generated by the following ParseOutputFile.
                                ParseOutputFile(value, q.diagnostics, baseDirectory, q.outputFileName, q.outputDirectory)
                            End If
                            Continue For

                        Case "t", "target"
                            value = RemoveQuotesAndSlashes(value)
                            q.outputKind = ParseTarget(name, value, q.diagnostics)
                            Continue For

                        Case "moduleassemblyname"
                            value = RemoveQuotesAndSlashes(value)
                            Dim identity As AssemblyIdentity = Nothing

                            ' Note that native compiler also extracts public key, but Roslyn doesn't use it.

                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "moduleassemblyname", ":<string>")
                            ElseIf Not AssemblyIdentity.TryParseDisplayName(value, identity) OrElse
                                       Not MetadataHelpers.IsValidAssemblyOrModuleName(identity.Name) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_InvalidAssemblyName, value, arg)
                            Else
                                q.moduleAssemblyName = identity.Name
                            End If

                            Continue For

                        Case "rootnamespace"
                            value = RemoveQuotesAndSlashes(value)
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "rootnamespace", ":<string>")
                                Continue For
                            End If

                            q.rootNamespace = value
                            Continue For

                        Case "doc"
                            value = RemoveQuotesAndSlashes(value)
                            q.parseDocumentationComments = True
                            If value Is Nothing Then
                                ' Illegal in C#, but works in VB
                                q.documentationPath = GenerateFileNameForDocComment
                                Continue For
                            End If
                            Dim unquoted = RemoveQuotesAndSlashes(value)
                            If unquoted.Length = 0 Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "doc", ":<file>")
                            Else
                                q.documentationPath = ParseGenericPathToFile(unquoted, q.diagnostics, baseDirectory, generateDiagnostic:=False)
                                If String.IsNullOrWhiteSpace(q.documentationPath) Then
                                    AddDiagnostic(q.diagnostics, ERRID.WRN_XMLCannotWriteToXMLDocFile2, unquoted, New LocalizableErrorArgument(ERRID.IDS_TheSystemCannotFindThePathSpecified))
                                    q.documentationPath = Nothing
                                End If
                            End If

                            Continue For

                        Case "doc+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "doc")
                            End If

                            ' Seems redundant with default values, but we need to clobber any preceding /doc switches
                            q.documentationPath = GenerateFileNameForDocComment
                            q.parseDocumentationComments = True
                            Continue For

                        Case "doc-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "doc")
                            End If

                            ' Seems redundant with default values, but we need to clobber any preceding /doc switches
                            q.documentationPath = Nothing
                            q.parseDocumentationComments = False
                            Continue For

                        Case "errorlog"
                            Dim unquoted = RemoveQuotesAndSlashes(value)
                            If String.IsNullOrEmpty(unquoted) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "errorlog", ":<file>")
                            Else
                                q.errorLogPath = ParseGenericPathToFile(unquoted, q.diagnostics, baseDirectory)
                            End If

                            Continue For

                        Case "netcf"
                            ' Do nothing as we no longer have any use for implementing this switch and 
                            ' want to avoid failing with any warnings/errors
                            Continue For

                        Case "sdkpath"
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "sdkpath", ":<path>")
                                Continue For
                            End If

                            q.sdkPaths.Clear()
                            q.sdkPaths.AddRange(ParseSeparatedPaths(value))
                            Continue For

                        Case "recurse"
                            value = RemoveQuotesAndSlashes(value)
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "recurse", ":<wildcard>")
                                Continue For
                            End If

                            Dim before As Integer = q.sourceFiles.Count
                            q.sourceFiles.AddRange(ParseRecurseArgument(value, baseDirectory, q.diagnostics))
                            If q.sourceFiles.Count > before Then
                                q.hasSourceFiles = True
                            End If
                            Continue For

                        Case "addmodule"
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "addmodule", ":<file_list>")
                                Continue For
                            End If

                            ' NOTE(tomat): Dev10 reports "Command line error BC2017 : could not find library."
                            ' Since we now support /referencePaths option we would need to search them to see if the resolved path is a directory.
                            ' An error will be reported by the assembly manager anyways.
                            q.metadataReferences.AddRange(
                                    ParseSeparatedPaths(value).Select(
                                        Function(path) New CommandLineReference(path, New MetadataReferenceProperties(MetadataImageKind.Module))))
                            Continue For

                        Case "l", "link"
                            q.metadataReferences.AddRange(ParseAssemblyReferences(name, value, q.diagnostics, embedInteropTypes:=True))
                            Continue For

                        Case "win32resource"
                            q.win32ResourceFile = GetWin32Setting(s_win32Res, RemoveQuotesAndSlashes(value), q.diagnostics)
                            Continue For

                        Case "win32icon"
                            q.win32IconFile = GetWin32Setting(s_win32Icon, RemoveQuotesAndSlashes(value), q.diagnostics)
                            Continue For

                        Case "win32manifest"
                            q.win32ManifestFile = GetWin32Setting(s_win32Manifest, RemoveQuotesAndSlashes(value), q.diagnostics)
                            Continue For

                        Case "nowin32manifest"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.noWin32Manifest = True
                            Continue For

                        Case "res", "resource"
                            Dim embeddedResource = ParseResourceDescription(name, value, baseDirectory, q.diagnostics, embedded:=True)
                            If embeddedResource IsNot Nothing Then
                                q.managedResources.Add(embeddedResource)
                            End If
                            Continue For

                        Case "linkres", "linkresource"
                            Dim linkedResource = ParseResourceDescription(name, value, baseDirectory, q.diagnostics, embedded:=False)
                            If linkedResource IsNot Nothing Then
                                q.managedResources.Add(linkedResource)
                            End If
                            Continue For

                        Case "debug"
                            ' parse only for backwards compat
                            value = RemoveQuotesAndSlashes(value)
                            If value IsNot Nothing Then
                                Select Case value.ToLower()
                                    Case "full", "pdbonly"
                                        q.debugInformationFormat = DebugInformationFormat.Pdb
                                    Case "portable"
                                        q.debugInformationFormat = DebugInformationFormat.PortablePdb
                                    Case "embedded"
                                        q.debugInformationFormat = DebugInformationFormat.Embedded
                                    Case Else
                                        AddDiagnostic(q.diagnostics, ERRID.ERR_InvalidSwitchValue, "debug", value)
                                End Select
                            End If

                            q.emitPdb = True
                            Continue For

                        Case "debug+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "debug")
                            End If

                            q.emitPdb = True
                            Continue For

                        Case "debug-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "debug")
                            End If

                            q.emitPdb = False
                            Continue For

                        Case "optimize", "optimize+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optimize")
                                Continue For
                            End If

                            q.optimize = True
                            Continue For

                        Case "optimize-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optimize")
                                Continue For
                            End If

                            q.optimize = False
                            Continue For

                        Case "parallel", "p"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, name)
                                Continue For
                            End If

                            q.concurrentBuild = True
                            Continue For

                        Case "deterministic", "deterministic+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, name)
                                Continue For
                            End If

                            q.deterministic = True
                            Continue For

                        Case "deterministic-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, name)
                                Continue For
                            End If

                            q.deterministic = False
                            Continue For

                        Case "parallel+", "p+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, name.Substring(0, name.Length - 1))
                                Continue For
                            End If

                            q.concurrentBuild = True
                            Continue For

                        Case "parallel-", "p-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, name.Substring(0, name.Length - 1))
                                Continue For
                            End If

                            q.concurrentBuild = False
                            Continue For

                        Case "warnaserror", "warnaserror+"
                            If value Is Nothing Then
                                q.generalDiagnosticOption = ReportDiagnostic.Error

                                q.specificDiagnosticOptionsFromGeneralArguments.Clear()
                                For Each pair In q.specificDiagnosticOptionsFromRuleSet
                                    If pair.Value = ReportDiagnostic.Warn Then
                                        q.specificDiagnosticOptionsFromGeneralArguments.Add(pair.Key, ReportDiagnostic.Error)
                                    End If
                                Next

                                Continue For
                            End If

                            AddWarnings(q.specificDiagnosticOptionsFromSpecificArguments, ReportDiagnostic.Error, ParseWarnings(value))
                            Continue For

                        Case "warnaserror-"
                            If value Is Nothing Then
                                If q.generalDiagnosticOption <> ReportDiagnostic.Suppress Then
                                    q.generalDiagnosticOption = ReportDiagnostic.Default
                                End If

                                q.specificDiagnosticOptionsFromGeneralArguments.Clear()

                                Continue For
                            End If

                            For Each id In ParseWarnings(value)
                                Dim ruleSetValue As ReportDiagnostic
                                If q.specificDiagnosticOptionsFromRuleSet.TryGetValue(id, ruleSetValue) Then
                                    q.specificDiagnosticOptionsFromSpecificArguments(id) = ruleSetValue
                                Else
                                    q.specificDiagnosticOptionsFromSpecificArguments(id) = ReportDiagnostic.Default
                                End If
                            Next

                            Continue For

                        Case "nowarn"
                            If value Is Nothing Then
                                q.generalDiagnosticOption = ReportDiagnostic.Suppress

                                q.specificDiagnosticOptionsFromGeneralArguments.Clear()
                                For Each pair In q.specificDiagnosticOptionsFromRuleSet
                                    If pair.Value <> ReportDiagnostic.Error Then
                                        q.specificDiagnosticOptionsFromGeneralArguments.Add(pair.Key, ReportDiagnostic.Suppress)
                                    End If
                                Next

                                Continue For
                            End If

                            AddWarnings(q.specificDiagnosticOptionsFromNoWarnArguments, ReportDiagnostic.Suppress, ParseWarnings(value))
                            Continue For

                        Case "langversion"
                            value = RemoveQuotesAndSlashes(value)
                            If value Is Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "langversion", ":<number>")
                                Continue For
                            End If

                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "langversion", ":<number>")
                            Else
                                Select Case value.ToLowerInvariant()
                                    Case "9", "9.0"
                                        q.languageVersion = LanguageVersion.VisualBasic9
                                    Case "10", "10.0"
                                        q.languageVersion = LanguageVersion.VisualBasic10
                                    Case "11", "11.0"
                                        q.languageVersion = LanguageVersion.VisualBasic11
                                    Case "12", "12.0"
                                        q.languageVersion = LanguageVersion.VisualBasic12
                                    Case "14", "14.0"
                                        q.languageVersion = LanguageVersion.VisualBasic14
                                    Case Else
                                        AddDiagnostic(q.diagnostics, ERRID.ERR_InvalidSwitchValue, "langversion", value)
                                End Select
                            End If

                            Continue For

                        Case "delaysign", "delaysign+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "delaysign")
                                Continue For
                            End If

                            q.delaySignSetting = True
                            Continue For

                        Case "delaysign-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "delaysign")
                                Continue For
                            End If

                            q.delaySignSetting = False
                            Continue For

                        Case "keycontainer"
                            ' NOTE: despite what MSDN says, Dev11 resets '/keyfile' in this case:
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
                            q.keyFileSetting = Nothing
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "keycontainer", ":<string>")
                            Else
                                q.keyContainerSetting = value
                            End If
                            Continue For

                        Case "keyfile"
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
                            q.keyContainerSetting = Nothing
                            If String.IsNullOrWhiteSpace(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "keyfile", ":<file>")
                            Else
                                q.keyFileSetting = RemoveQuotesAndSlashes(value)
                            End If
                            Continue For

                        Case "highentropyva", "highentropyva+"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.highEntropyVA = True
                            Continue For

                        Case "highentropyva-"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.highEntropyVA = False
                            Continue For

                        Case "nologo", "nologo+"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.displayLogo = False
                            Continue For

                        Case "nologo-"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.displayLogo = True
                            Continue For

                        Case "quiet+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "quiet")
                                Continue For
                            End If

                            q.outputLevel = VisualBasic.OutputLevel.Quiet
                            Continue For

                        Case "quiet"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.outputLevel = VisualBasic.OutputLevel.Quiet
                            Continue For

                        Case "verbose"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.outputLevel = VisualBasic.OutputLevel.Verbose
                            Continue For

                        Case "verbose+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "verbose")
                                Continue For
                            End If

                            q.outputLevel = VisualBasic.OutputLevel.Verbose
                            Continue For

                        Case "quiet-", "verbose-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, name.Substring(0, name.Length - 1))
                                Continue For
                            End If

                            q.outputLevel = VisualBasic.OutputLevel.Normal
                            Continue For

                        Case "utf8output", "utf8output+"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "utf8output")
                            End If

                            q.utf8output = True
                            Continue For

                        Case "utf8output-"
                            If value IsNot Nothing Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "utf8output")
                            End If

                            q.utf8output = False
                            Continue For

                        Case "noconfig"
                            ' It is already handled (see CommonCommandLineCompiler.cs).
                            Continue For

                        Case "bugreport"
                            ' Do nothing as we no longer have any use for implementing this switch and 
                            ' want to avoid failing with any warnings/errors
                            ' We do no further checking as to a value provided or not  and                             '
                            ' this will cause no diagnostics for invalid values.

                            Continue For
                        Case "errorreport"
                            ' Allows any value to be entered and will just silently do nothing
                            ' previously we would validate value for prompt, send Or Queue
                            ' This will cause no diagnostics for invalid values.

                            Continue For

                        Case "novbruntimeref"
                            ' The switch is no longer supported and for backwards compat ignored.
                            Continue For

                        Case "m", "main"
                            ' MSBuild can result in maintypename being passed in quoted when Cyrillic namespace was being used resulting
                            ' in ERRID.ERR_StartupCodeNotFound1 diagnostic.   The additional quotes cause problems and quotes are not a 
                            ' valid character in typename.
                            value = RemoveQuotesAndSlashes(value)
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<class>")
                                Continue For
                            End If

                            q.mainTypeName = value
                            Continue For

                        Case "subsystemversion"
                            value = RemoveQuotesAndSlashes(value)
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<version>")
                                Continue For
                            End If

                            Dim version As SubsystemVersion = Nothing
                            If SubsystemVersion.TryParse(value, version) Then
                                q.ssVersion = version
                            Else
                                AddDiagnostic(q.diagnostics, ERRID.ERR_InvalidSubsystemVersion, value)
                            End If
                            Continue For

                        Case "touchedfiles"
                            Dim unquoted = RemoveQuotesAndSlashes(value)
                            If (String.IsNullOrEmpty(unquoted)) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<touchedfiles>")
                                Continue For
                            Else
                                q.touchedFilesPath = unquoted
                            End If
                            Continue For

                        Case "fullpaths", "errorendlocation"
                            UnimplementedSwitch(q.diagnostics, name)
                            Continue For

                        Case "pathmap"
                            ' "/pathmap:K1=V1,K2=V2..."
                            If value = Nothing Then
                                Exit Select
                            End If

                            q.pathMap = q.pathMap.Concat(ParsePathMap(value, q.diagnostics))
                            Continue For

                        Case "reportanalyzer"
                            q.reportAnalyzer = True
                            Continue For

                        Case "nostdlib"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.noStdLib = True
                            Continue For

                        Case "vbruntime"
                            If value Is Nothing Then
                                GoTo lVbRuntimePlus
                            End If

                            ' NOTE: that Dev11 does not report errors on empty or invalid file specified
                            q.vbRuntimePath = RemoveQuotesAndSlashes(value)
                            q.includeVbRuntimeReference = True
                            q.embedVbCoreRuntime = False
                            Continue For

                        Case "vbruntime+"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

lVbRuntimePlus:
                            q.vbRuntimePath = Nothing
                            q.includeVbRuntimeReference = True
                            q.embedVbCoreRuntime = False
                            Continue For

                        Case "vbruntime-"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.vbRuntimePath = Nothing
                            q.includeVbRuntimeReference = False
                            q.embedVbCoreRuntime = False
                            Continue For

                        Case "vbruntime*"
                            If value IsNot Nothing Then
                                Exit Select
                            End If

                            q.vbRuntimePath = Nothing
                            q.includeVbRuntimeReference = False
                            q.embedVbCoreRuntime = True
                            Continue For

                        Case "platform"
                            value = RemoveQuotesAndSlashes(value)
                            If value IsNot Nothing Then
                                q.platform = ParsePlatform(name, value, q.diagnostics)
                            Else
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, "platform", ":<string>")
                            End If

                            Continue For

                        Case "filealign"
                            q.fileAlignment = ParseFileAlignment(name, RemoveQuotesAndSlashes(value), q.diagnostics)
                            Continue For

                        Case "baseaddress"
                            q.baseAddress = ParseBaseAddress(name, RemoveQuotesAndSlashes(value), q.diagnostics)
                            Continue For

                        Case "ruleset"
                            '  The ruleset arg has already been processed in a separate pass above.
                            Continue For

                        Case "features"
                            If value Is Nothing Then
                                q.features.Clear()
                            Else
                                q.features.Add(RemoveQuotesAndSlashes(value))
                            End If
                            Continue For

                        Case "additionalfile"
                            value = RemoveQuotesAndSlashes(value)
                            If String.IsNullOrEmpty(value) Then
                                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<file_list>")
                                Continue For
                            End If

                            q.additionalFiles.AddRange(ParseAdditionalFileArgument(value, baseDirectory, q.diagnostics))
                            Continue For
                    End Select
                End If

                AddDiagnostic(q.diagnostics, ERRID.WRN_BadSwitch, arg)
            Next

            Dim specificDiagnosticOptions = New Dictionary(Of String, ReportDiagnostic)(q.specificDiagnosticOptionsFromRuleSet, CaseInsensitiveComparison.Comparer)

            For Each item In q.specificDiagnosticOptionsFromGeneralArguments
                specificDiagnosticOptions(item.Key) = item.Value
            Next

            For Each item In q.specificDiagnosticOptionsFromSpecificArguments
                specificDiagnosticOptions(item.Key) = item.Value
            Next

            For Each item In q.specificDiagnosticOptionsFromNoWarnArguments
                specificDiagnosticOptions(item.Key) = item.Value
            Next

            If Not IsScriptRunner AndAlso Not q.hasSourceFiles AndAlso q.managedResources.IsEmpty() AndAlso q.outputKind.IsApplication Then
                ' VB displays help when there is nothing specified on the command line
                If q.flattenedArgs.Any Then
                    AddDiagnostic(q.diagnostics, ERRID.ERR_NoSources)
                Else
                    q.displayHelp = True
                End If
            End If

            ' Prepare SDK PATH
            If sdkDirectory IsNot Nothing AndAlso q.sdkPaths.Count = 0 Then
                q.sdkPaths.Add(sdkDirectory)
            End If

            ' Locate default 'mscorlib.dll' or 'System.Runtime.dll', if any.
            Dim defaultCoreLibraryReference As CommandLineReference? = LoadCoreLibraryReference(q.sdkPaths, baseDirectory)

            ' If /nostdlib is not specified, load System.dll
            ' Dev12 does it through combination of CompilerHost::InitStandardLibraryList and CompilerProject::AddStandardLibraries.
            If Not q.noStdLib Then
                Dim systemDllPath As String = FindFileInSdkPath(q.sdkPaths, "System.dll", baseDirectory)
                If systemDllPath Is Nothing Then
                    AddDiagnostic(q.diagnostics, ERRID.WRN_CannotFindStandardLibrary1, "System.dll")
                Else
                    q.metadataReferences.Add(
                            New CommandLineReference(systemDllPath, New MetadataReferenceProperties(MetadataImageKind.Assembly)))
                End If
                ' Dev11 also adds System.Core.dll in VbHostedCompiler::CreateCompilerProject()
            End If

            ' Add reference to 'Microsoft.VisualBasic.dll' if needed
            If q.includeVbRuntimeReference Then
                If q.vbRuntimePath Is Nothing Then
                    Dim msVbDllPath As String = FindFileInSdkPath(q.sdkPaths, "Microsoft.VisualBasic.dll", baseDirectory)
                    If msVbDllPath Is Nothing Then
                        AddDiagnostic(q.diagnostics, ERRID.ERR_LibNotFound, "Microsoft.VisualBasic.dll")
                    Else
                        q.metadataReferences.Add(
                                New CommandLineReference(msVbDllPath, New MetadataReferenceProperties(MetadataImageKind.Assembly)))
                    End If
                Else
                    q.metadataReferences.Add(New CommandLineReference(q.vbRuntimePath, New MetadataReferenceProperties(MetadataImageKind.Assembly)))
                End If
            End If

            ' add additional reference paths if specified
            If Not String.IsNullOrWhiteSpace(additionalReferenceDirectories) Then
                q.libPaths.AddRange(ParseSeparatedPaths(additionalReferenceDirectories))
            End If

            ' Build search path
            Dim searchPaths As ImmutableArray(Of String) = BuildSearchPaths(baseDirectory, q.sdkPaths, q.responsePaths, q.libPaths)

            ValidateWin32Settings(q.noWin32Manifest, q.win32ResourceFile, q.win32IconFile, q.win32ManifestFile, q.outputKind, q.diagnostics)

            ' Validate root namespace if specified
            Debug.Assert(q.rootNamespace IsNot Nothing)
            ' NOTE: empty namespace is a valid option
            If Not String.Empty.Equals(q.rootNamespace) Then
                q.rootNamespace = q.rootNamespace.Unquote()
                If String.IsNullOrWhiteSpace(q.rootNamespace) OrElse Not OptionsValidator.IsValidNamespaceName(q.rootNamespace) Then
                    AddDiagnostic(q.diagnostics, ERRID.ERR_BadNamespaceName1, q.rootNamespace)
                    q.rootNamespace = "" ' To make it pass compilation options' check
                End If
            End If

            ' Dev10 searches for the keyfile in the current directory and assembly output directory.
            ' We always look to base directory and then examine the search paths.
            q.keyFileSearchPaths.Add(baseDirectory)
            If baseDirectory <> q.outputDirectory Then
                q.keyFileSearchPaths.Add(q.outputDirectory)
            End If

            Dim parsedFeatures = CompilerOptionParseUtilities.ParseFeatures(q.features)

            Dim compilationName As String = Nothing
            GetCompilationAndModuleNames(q.diagnostics, q.outputKind, q.sourceFiles, q.moduleAssemblyName, q.outputFileName, q.moduleName, compilationName)

            If Not IsScriptRunner AndAlso
                Not q.hasSourceFiles AndAlso
                Not q.managedResources.IsEmpty() AndAlso
                q.outputFileName = Nothing AndAlso
                Not q.flattenedArgs.IsEmpty() Then

                AddDiagnostic(q.diagnostics, ERRID.ERR_NoSourcesOut)
            End If

            Return q.ToImmutable(parsedFeatures,
                                 specificDiagnosticOptions,
                                 compilationName,
                                 GenerateFileNameForDocComment,
                                 defaultCoreLibraryReference,
                                 searchPaths)

        End Function
#Region "ARGS"
        Private Shared Function ARG_OptionStrict_Plus(q As Mutable_VBCommandLineArguments, value As String) As Boolean
            If value IsNot Nothing Then
                AddDiagnostic(q.diagnostics, ERRID.ERR_SwitchNeedsBool, "optionstrict")
            Else
                q.optionStrict = VisualBasic.OptionStrict.On
            End If
            Return True
        End Function

        Private Shared Function ARG_Imports(q As Mutable_VBCommandLineArguments, name As String, value As String) As Boolean
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, If(name = "import", ":<str>", ":<import_list>"))
            Else
                ParseGlobalImports(value, q.globalImports, q.diagnostics)
            End If
            Return True
        End Function

        Private Shared Function ARG_Define(q As Mutable_VBCommandLineArguments, name As String, value As String) As Boolean
            If String.IsNullOrEmpty(value) Then
                AddDiagnostic(q.diagnostics, ERRID.ERR_ArgumentRequired, name, ":<symbol_list>")
            Else
                Dim conditionalCompilationDiagnostics As IEnumerable(Of Diagnostic) = Nothing
                q.defines = ParseConditionalCompilationSymbols(value, conditionalCompilationDiagnostics, q.defines)
                q.diagnostics.AddRange(conditionalCompilationDiagnostics)
            End If
            Return True
        End Function

        Private Function ARG_Analyzer(q As Mutable_VBCommandLineArguments, name As String, value As String) As Boolean
            q.analyzers.AddRange(ParseAnalyzers(name, value, q.diagnostics))
            Return True
        End Function

        Private Shared Function ARG_Reference(q As Mutable_VBCommandLineArguments, name As String, value As String) As Boolean
            q.metadataReferences.AddRange(ParseAssemblyReferences(name, value, q.diagnostics, embedInteropTypes:=False))
            Return True
        End Function
#End Region

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
                    If PortableShim.File.Exists(filePath) Then
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

        Private Function ParseAnalyzers(name As String, value As String, diagnostics As IList(Of Diagnostic)) As IEnumerable(Of CommandLineAnalyzerReference)
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
                                                      Return PortableShim.FileStream.Create(fullPath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read, PortableShim.FileShare.ReadWrite)
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
                    Dim defaultExtension As String = kind.GetDefaultExtension()
                    If Not String.Equals(ext, defaultExtension, StringComparison.OrdinalIgnoreCase) Then
                        simpleName = outputFileName
                        outputFileName = outputFileName & defaultExtension
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

