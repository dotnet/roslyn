// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Instrumentation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class CSharpCommandLineParser : CommandLineParser
    {
        public static readonly CSharpCommandLineParser Default = new CSharpCommandLineParser();
        public static readonly CSharpCommandLineParser Interactive = new CSharpCommandLineParser(isInteractive: true);

        internal CSharpCommandLineParser(bool isInteractive = false)
            : base(CSharp.MessageProvider.Instance, isInteractive)
        {
        }

        protected override string RegularFileExtension { get { return ".cs"; } }
        protected override string ScriptFileExtension { get { return ".csx"; } }

        internal sealed override CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string additionalReferencePaths)
        {
            return Parse(args, baseDirectory, additionalReferencePaths);
        }

        public new CSharpCommandLineArguments Parse(IEnumerable<string> args, string baseDirectory, string additionalReferencePaths = null)
        {
            using (Logger.LogBlock(FunctionId.CSharp_CommandLineParser_Parse))
            {
                List<Diagnostic> diagnostics = new List<Diagnostic>();
                List<string> flattenedArgs = new List<string>();
                List<string> scriptArgs = IsInteractive ? new List<string>() : null;
                FlattenArgs(args, diagnostics, flattenedArgs, scriptArgs, baseDirectory);

                string appConfigPath = null;
                bool displayLogo = true;
                bool displayHelp = false;
                bool optimize = false;
                bool checkOverflow = false;
                bool allowUnsafe = false;
                bool concurrentBuild = true;
                bool emitDebugInformation = false;
                var debugInformationKind = DebugInformationKind.Full;
                string pdbPath = null;
                bool noStdLib = false;
                string outputDirectory = baseDirectory;
                string outputFileName = null;
                string documentationPath = null;
                bool parseDocumentationComments = false; //Don't just null check documentationFileName because we want to do this even if the file name is invalid.
                bool utf8output = false;
                OutputKind outputKind = OutputKind.ConsoleApplication;
                SubsystemVersion subsystemVersion = SubsystemVersion.None;
                LanguageVersion languageVersion = CSharpParseOptions.Default.LanguageVersion;
                string mainTypeName = null;
                string win32ManifestFile = null;
                string win32ResourceFile = null;
                string win32IconFile = null;
                bool noWin32Manifest = false;
                Platform platform = Platform.AnyCpu;
                ulong baseAddress = 0;
                int fileAlignment = 0;
                bool? delaySignSetting = null;
                string keyFileSetting = null;
                string keyContainerSetting = null;
                List<ResourceDescription> managedResources = new List<ResourceDescription>();
                List<CommandLineSourceFile> sourceFiles = new List<CommandLineSourceFile>();
                bool sourceFilesSpecified = false;
                bool resourcesOrModulesSpecified = false;
                Encoding codepage = null;
                var defines = ArrayBuilder<string>.GetInstance();
                List<CommandLineReference> metadataReferences = new List<CommandLineReference>();
                List<CommandLineAnalyzerReference> analyzers = new List<CommandLineAnalyzerReference>();
                List<string> libPaths = new List<string>();
                List<string> keyFileSearchPaths = new List<string>();
                List<string> usings = new List<string>();
                var generalDiagnosticOption = ReportDiagnostic.Default;
                var diagnosticOptions = new Dictionary<string, ReportDiagnostic>();
                int warningLevel = 4;
                bool highEntropyVA = false;
                bool printFullPaths = false;
                string moduleAssemblyName = null;
                string moduleName = null;
                List<string> features = new List<string>();
                string runtimeMetadataVersion = null;
                bool errorEndLocation = false;
                CultureInfo preferredUILang = null;
                string touchedFilesPath = null;
                var sqmSessionGuid = Guid.Empty;

                foreach (string arg in flattenedArgs)
                {
                    Debug.Assert(!arg.StartsWith("@"));

                    string name, value;
                    if (!TryParseOption(arg, out name, out value))
                    {
                        sourceFiles.AddRange(ParseFileArgument(arg, baseDirectory, diagnostics));
                        sourceFilesSpecified = true;
                        continue;
                    }

                    switch (name)
                    {
                        case "?":
                        case "help":
                            displayHelp = true;
                            continue;

                        case "r":
                        case "reference":
                            metadataReferences.AddRange(ParseAssemblyReferences(arg, value, diagnostics, embedInteropTypes: false));
                            continue;

                        case "a":
                        case "analyzer":
                            analyzers.AddRange(ParseAnalyzers(arg, value, diagnostics));
                            continue;

                        case "d":
                        case "define":
                            if (string.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", arg);
                                continue;
                            }

                            IEnumerable<Diagnostic> defineDiagnostics;
                            defines.AddRange(ParseConditionalCompilationSymbols(value, out defineDiagnostics));
                            diagnostics.AddRange(defineDiagnostics);
                            continue;

                        case "codepage":
                            if (value == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                continue;
                            }

                            var encoding = ParseCodepage(value);
                            if (encoding == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.FTL_BadCodepage, value);
                                continue;
                            }

                            codepage = encoding;
                            continue;

                        case "checked":
                        case "checked+":
                            if (value != null)
                            {
                                break;
                            }

                            checkOverflow = true;
                            continue;

                        case "checked-":
                            if (value != null)
                                break;

                            checkOverflow = false;
                            continue;

                        case "features":
                            if (value == null)
                            {
                                features.Clear();
                            }
                            else
                            {
                                features.Add(value);
                            }
                            continue;

                        case "noconfig":
                            // It is already handled (see CommonCommandLineCompiler.cs).
                            continue;

                        case "sqmsessionguid":
                            if (value == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_MissingGuidForOption, "<text>", name);
                            }
                            else
                            {
                                if (!Guid.TryParse(value, out sqmSessionGuid))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_InvalidFormatForGuidForOption, value, name);
                                }
                            }
                            continue;
#if DEBUG
                        case "attachdebugger":
                            Debugger.Launch();
                            continue;
#endif
                    }

                    if (IsInteractive)
                    {
                        switch (name)
                        {
                            // interactive:
                            case "rp":
                            case "referencepath":
                                // TODO: should it really go to libPaths?
                                ParseAndResolveReferencePaths(name, value, baseDirectory, libPaths, MessageID.IDS_REFERENCEPATH_OPTION, diagnostics);
                                continue;

                            case "u":
                            case "using":
                                usings.AddRange(ParseUsings(arg, value, diagnostics));
                                continue;
                        }
                    }
                    else
                    {
                        switch (name)
                        {
                            case "out":
                                if (string.IsNullOrWhiteSpace(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                                }
                                else
                                {
                                    ParseOutputFile(value, diagnostics, baseDirectory, out outputFileName, out outputDirectory);
                                }

                                continue;

                            case "t":
                            case "target":
                                if (value == null)
                                {
                                    break; // force 'unrecognized option'
                                }

                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidTarget);
                                }
                                else
                                {
                                    outputKind = ParseTarget(value, diagnostics);
                                }

                                continue;

                            case "moduleassemblyname":
                                value = value != null ? Unquote(value) : null;

                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", arg);
                                }
                                else if (!MetadataHelpers.IsValidAssemblyOrModuleName(value))
                                {
                                    // Dev11 C# doesn't check the name (VB does)
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_InvalidAssemblyName, "<text>", arg);
                                }
                                else
                                {
                                    moduleAssemblyName = value;
                                }

                                continue;

                            case "platform":
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<string>", arg);
                                }
                                else
                                {
                                    platform = ParsePlatform(value, diagnostics);
                                }
                                continue;

                            case "recurse":
                                if (value == null)
                                {
                                    break; // force 'unrecognized option'
                                }
                                else if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                                }
                                else
                                {
                                    int before = sourceFiles.Count;
                                    sourceFiles.AddRange(ParseRecurseArgument(value, baseDirectory, diagnostics));
                                    if (sourceFiles.Count > before)
                                    {
                                        sourceFilesSpecified = true;
                                    }
                                }
                                continue;

                            case "doc":
                                parseDocumentationComments = true;
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
                                    continue;
                                }
                                string unquoted = RemoveAllQuotes(value);
                                if (string.IsNullOrEmpty(unquoted))
                                {
                                    // CONSIDER: This diagnostic exactly matches dev11, but it would be simpler (and more consistent with /out)
                                    // if we just let the next case handle /doc:"".
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "/doc:"); // Different argument.
                                }
                                else
                                {
                                    documentationPath = ParseGenericPathToFile(unquoted, diagnostics, baseDirectory);
                                }
                                continue;

                            case "addmodule":
                                if (value == null)
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "/addmodule:");
                                }
                                else if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                                }
                                else
                                {
                                    // NOTE(tomat): Dev10 used to report CS1541: ERR_CantIncludeDirectory if the path was a directory.
                                    // Since we now support /referencePaths option we would need to search them to see if the resolved path is a directory.
                                    // An error will be reported by the assembly manager anyways.
                                    metadataReferences.AddRange(ParseSeparatedPaths(value).Select(path => new CommandLineReference(path, MetadataReferenceProperties.Module)));
                                    resourcesOrModulesSpecified = true;
                                }
                                continue;

                            case "l":
                            case "link":
                                metadataReferences.AddRange(ParseAssemblyReferences(arg, value, diagnostics, embedInteropTypes: true));
                                continue;

                            case "win32res":
                                win32ResourceFile = GetWin32Setting(arg, value, diagnostics);
                                continue;

                            case "win32icon":
                                win32IconFile = GetWin32Setting(arg, value, diagnostics);
                                continue;

                            case "win32manifest":
                                win32ManifestFile = GetWin32Setting(arg, value, diagnostics);
                                noWin32Manifest = false;
                                continue;

                            case "nowin32manifest":
                                noWin32Manifest = true;
                                win32ManifestFile = null;
                                continue;

                            case "res":
                            case "resource":
                                if (value == null)
                                {
                                    break; // Dev11 reports inrecognized option
                                }

                                var embeddedResource = ParseResourceDescription(arg, value, baseDirectory, diagnostics, embedded: true);
                                if (embeddedResource != null)
                                {
                                    managedResources.Add(embeddedResource);
                                    resourcesOrModulesSpecified = true;
                                }

                                continue;

                            case "linkres":
                            case "linkresource":
                                if (value == null)
                                {
                                    break; // Dev11 reports inrecognized option
                                }

                                var linkedResource = ParseResourceDescription(arg, value, baseDirectory, diagnostics, embedded: false);
                                if (linkedResource != null)
                                {
                                    managedResources.Add(linkedResource);
                                    resourcesOrModulesSpecified = true;
                                }

                                continue;

                            case "debug":
                                emitDebugInformation = true;
                                if (value != null)
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), name);
                                    }
                                    else if (string.Equals(value, "full", StringComparison.OrdinalIgnoreCase))
                                    {
                                        debugInformationKind = DebugInformationKind.Full;
                                    }
                                    else if (string.Equals(value, "pdbonly", StringComparison.OrdinalIgnoreCase))
                                    {
                                        debugInformationKind = DebugInformationKind.PDBOnly;
                                    }
                                    else
                                    {
                                        AddDiagnostic(diagnostics, ErrorCode.ERR_BadDebugType, value);
                                    }
                                }
                                continue;

                            case "debug+":
                                //guard against "debug+:xx"
                                if (value != null)
                                    break;

                                emitDebugInformation = true;
                                continue;

                            case "debug-":
                                if (value != null)
                                    break;

                                emitDebugInformation = false;
                                continue;

                            case "o":
                            case "optimize":
                            case "o+":
                            case "optimize+":
                                if (value != null)
                                    break;

                                optimize = true;
                                continue;

                            case "o-":
                            case "optimize-":
                                if (value != null)
                                    break;

                                optimize = false;
                                continue;

                            case "p":
                            case "parallel":
                            case "p+":
                            case "parallel+":
                                if (value != null)
                                    break;

                                concurrentBuild = true;
                                continue;

                            case "p-":
                            case "parallel-":
                                if (value != null)
                                    break;

                                concurrentBuild = false;
                                continue;

                            case "warnaserror":
                            case "warnaserror+":
                                if (value == null)
                                {
                                    generalDiagnosticOption = ReportDiagnostic.Error;
                                    continue;
                                }

                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                }
                                else
                                {
                                    AddWarnings(diagnosticOptions, ReportDiagnostic.Error, ParseWarnings(value, diagnostics));
                                }
                                continue;

                            case "warnaserror-":
                                if (value == null)
                                {
                                    generalDiagnosticOption = ReportDiagnostic.Default;
                                    continue;
                                }

                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                }
                                else
                                {
                                    AddWarnings(diagnosticOptions, ReportDiagnostic.Warn, ParseWarnings(value, diagnostics));
                                }
                                continue;

                            case "w":
                            case "warn":
                                if (value == null)
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                    continue;
                                }

                                int newWarningLevel;
                                if (string.IsNullOrEmpty(value) || 
                                    !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out newWarningLevel))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                }
                                else if (newWarningLevel < 0 || newWarningLevel > 4)
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadWarningLevel, name);
                                }
                                else
                                {
                                    warningLevel = newWarningLevel;
                                }
                                continue;

                            case "nowarn":
                                if (value == null)
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                    continue;
                                }

                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                }
                                else
                                {
                                    AddWarnings(diagnosticOptions, ReportDiagnostic.Suppress, ParseWarnings(value, diagnostics));
                                }
                                continue;

                            case "unsafe":
                            case "unsafe+":
                                if (value != null)
                                    break;

                                allowUnsafe = true;
                                continue;

                            case "unsafe-":
                                if (value != null)
                                    break;

                                allowUnsafe = false;
                                continue;

                            case "langversion":
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "/langversion:");
                                }
                                else if (!TryParseLanguageVersion(value, CSharpParseOptions.Default.LanguageVersion, out languageVersion))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadCompatMode, value);
                                }
                                continue;

                            case "delaysign":
                            case "delaysign+":
                                if (value != null)
                                {
                                    break;
                                }

                                delaySignSetting = true;
                                continue;

                            case "delaysign-":
                                if (value != null)
                                {
                                    break;
                                }

                                delaySignSetting = false;
                                continue;

                            case "keyfile":
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, "keyfile");
                                }
                                else
                                {
                                    keyFileSetting = RemoveAllQuotes(value);
                                }
                                // NOTE: Dev11/VB also clears "keycontainer", see also:
                                //
                                // MSDN: In case both /keyfile and /keycontainer are specified (either by command line option or by 
                                // MSDN: custom attribute) in the same compilation, the compiler will first try the key container. 
                                // MSDN: If that succeeds, then the assembly is signed with the information in the key container. 
                                // MSDN: If the compiler does not find the key container, it will try the file specified with /keyfile. 
                                // MSDN: If that succeeds, the assembly is signed with the information in the key file and the key 
                                // MSDN: information will be installed in the key container (similar to sn -i) so that on the next 
                                // MSDN: compilation, the key container will be valid.
                                continue;

                            case "keycontainer":
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "keycontainer");
                                }
                                else
                                {
                                    keyContainerSetting = value;
                                }
                                // NOTE: Dev11/VB also clears "keyfile", see also:
                                //
                                // MSDN: In case both /keyfile and /keycontainer are specified (either by command line option or by 
                                // MSDN: custom attribute) in the same compilation, the compiler will first try the key container. 
                                // MSDN: If that succeeds, then the assembly is signed with the information in the key container. 
                                // MSDN: If the compiler does not find the key container, it will try the file specified with /keyfile. 
                                // MSDN: If that succeeds, the assembly is signed with the information in the key file and the key 
                                // MSDN: information will be installed in the key container (similar to sn -i) so that on the next 
                                // MSDN: compilation, the key container will be valid.
                                continue;

                            case "highentropyva":
                            case "highentropyva+":
                                if (value != null)
                                    break;

                                highEntropyVA = true;
                                continue;

                            case "highentropyva-":
                                if (value != null)
                                    break;

                                highEntropyVA = false;
                                continue;

                            case "nologo":
                                displayLogo = false;
                                continue;

                            case "baseaddress":
                                ulong newBaseAddress;
                                if (string.IsNullOrEmpty(value) || !TryParseUInt64(value, out newBaseAddress))
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                    }
                                    else
                                    {
                                        AddDiagnostic(diagnostics, ErrorCode.ERR_BadBaseNumber, value);
                                    }
                                }
                                else
                                {
                                    baseAddress = newBaseAddress;
                                }

                                continue;

                            case "subsystemversion":
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "subsystemversion");
                                    continue;
                                }

                                // It seems VS 2012 just silently corrects invalid values and suppresses the error message
                                SubsystemVersion version = SubsystemVersion.None;
                                if (SubsystemVersion.TryParse(value, out version))
                                {
                                    subsystemVersion = version;
                                }
                                else
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadSubsystemVersion, value);
                                }

                                continue;

                            case "touchedfiles":
                                unquoted = RemoveAllQuotes(value);
                                if (string.IsNullOrEmpty(unquoted))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "touchedfiles");
                                    continue;
                                }
                                else
                                {
                                    touchedFilesPath = unquoted;
                                }

                                continue;

                            case "bugreport":
                                UnimplementedSwitch(diagnostics, name);
                                continue;

                            case "utf8output":
                                if (value != null)
                                    break;

                                utf8output = true;
                                continue;

                            case "m":
                            case "main":
                                // Remove any quotes for consistent behaviour as MSBuild can return quoted or 
                                // unquoted main.    
                                unquoted = RemoveAllQuotes(value);
                                if (string.IsNullOrEmpty(unquoted))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                    continue;
                                }

                                mainTypeName = unquoted;
                                continue;

                            case "fullpaths":
                                if (value != null)
                                    break;

                                printFullPaths = true;
                                continue;

                            case "filealign":
                                ushort newAlignment;
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                                }
                                else if (!TryParseUInt16(value, out newAlignment))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadFileAlignment, value);
                                }
                                else if (!CompilationOptions.IsValidFileAlignment(newAlignment))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadFileAlignment, value);
                                }
                                else
                                {
                                    fileAlignment = newAlignment;
                                }
                                continue;

                            case "pdb":
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                                }
                                else
                                {
                                    pdbPath = ParsePdbPath(value, diagnostics, baseDirectory);
                                }
                                continue;

                            case "errorendlocation":
                                errorEndLocation = true;
                                continue;

                            case "preferreduilang":
                                if (string.IsNullOrEmpty(value))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", arg);
                                    continue;
                                }

                                try
                                {
                                    preferredUILang = new CultureInfo(value);
                                }
                                catch (CultureNotFoundException)
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.WRN_BadUILang, value);
                                }
                                continue;

                            case "nostdlib":
                            case "nostdlib+":
                                if (value != null)
                                    break;

                                noStdLib = true;
                                continue;

                            case "lib":
                                ParseAndResolveReferencePaths(name, value, baseDirectory, libPaths, MessageID.IDS_LIB_OPTION, diagnostics);
                                continue;

                            case "nostdlib-":
                                if (value != null)
                                    break;

                                noStdLib = false;
                                continue;

                            case "errorreport":
                                continue;

                            case "appconfig":
                                unquoted = RemoveAllQuotes(value);
                                if (string.IsNullOrEmpty(unquoted))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, ":<text>", RemoveAllQuotes(arg));
                                }
                                else
                                {
                                    appConfigPath = ParseGenericPathToFile(
                                        unquoted, diagnostics, baseDirectory);
                                }
                                continue;

                            case "runtimemetadataversion":
                                unquoted = RemoveAllQuotes(value);
                                if (string.IsNullOrEmpty(unquoted))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                    continue;
                                }

                                runtimeMetadataVersion = unquoted;
                                continue;

                            case "ruleset":
                                unquoted = RemoveAllQuotes(value);

                                if (string.IsNullOrEmpty(unquoted))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                }
                                else
                                {
                                    generalDiagnosticOption = GetDiagnosticOptionsFromRulesetFile(diagnosticOptions, diagnostics, unquoted, baseDirectory);
                                }
                                continue;
                        }
                    }

                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadSwitch, arg);
                }

                if (!IsInteractive && !sourceFilesSpecified && (outputKind.IsNetModule() || !resourcesOrModulesSpecified))
                {
                    AddDiagnostic(diagnostics, diagnosticOptions, ErrorCode.WRN_NoSources);
                }

                if (!noStdLib)
                {
                    metadataReferences.Insert(0, new CommandLineReference(typeof(object).Assembly.Location, MetadataReferenceProperties.Assembly));
                }

                if (!platform.Requires64Bit())
                {
                    if (baseAddress > uint.MaxValue - 0x8000)
                    {
                        AddDiagnostic(diagnostics, ErrorCode.ERR_BadBaseNumber, string.Format("0x{0:X}", baseAddress));
                        baseAddress = 0;
                    }
                }

                // add additional reference paths if specified
                if (!string.IsNullOrWhiteSpace(additionalReferencePaths))
                {
                    ParseAndResolveReferencePaths(null, additionalReferencePaths, baseDirectory, libPaths, MessageID.IDS_LIB_ENV, diagnostics);
                }

                ImmutableArray<string> referencePaths = BuildSearchPaths(libPaths);

                ValidateWin32Settings(win32ResourceFile, win32IconFile, win32ManifestFile, outputKind, diagnostics);

                // Dev11 searches for the key file in the current directory and assembly output directory.
                // We always look to base directory and then examine the search paths.
                keyFileSearchPaths.Add(baseDirectory);
                if (baseDirectory != outputDirectory)
                {
                    keyFileSearchPaths.Add(outputDirectory);
                }

                if (!emitDebugInformation)
                {
                    if (pdbPath != null)
                    {
                        // Can't give a PDB file name and turn off debug information
                        AddDiagnostic(diagnostics, ErrorCode.ERR_MissingDebugSwitch);
                    }
                    debugInformationKind = DebugInformationKind.None;
                }

                string compilationName;
                GetCompilationAndModuleNames(diagnostics, outputKind, sourceFiles, sourceFilesSpecified, moduleAssemblyName, ref outputFileName, ref moduleName, out compilationName);

                var parseOptions = new CSharpParseOptions
                (
                    languageVersion: languageVersion,
                    preprocessorSymbols: defines.ToImmutableAndFree(),
                    documentationMode: parseDocumentationComments ? DocumentationMode.Diagnose : DocumentationMode.None,
                    kind: SourceCodeKind.Regular,
                    privateCtor: true
                );

                var scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script);

                var options = new CSharpCompilationOptions
                (
                    outputKind: outputKind,
                    moduleName: moduleName,
                    mainTypeName: mainTypeName,
                    scriptClassName: WellKnownMemberNames.DefaultScriptClassName,
                    usings: usings,
                    debugInformationKind: debugInformationKind,
                    optimize: optimize,
                    checkOverflow: checkOverflow,
                    allowUnsafe: allowUnsafe,
                    concurrentBuild: concurrentBuild,
                    cryptoKeyContainer: keyContainerSetting,
                    cryptoKeyFile: keyFileSetting,
                    delaySign: delaySignSetting,
                    fileAlignment: fileAlignment,
                    baseAddress: baseAddress,
                    platform: platform,
                    generalDiagnosticOption: generalDiagnosticOption,
                    warningLevel: warningLevel,
                    specificDiagnosticOptions: diagnosticOptions,
                    highEntropyVirtualAddressSpace: highEntropyVA,
                    subsystemVersion: subsystemVersion,
                    runtimeMetadataVersion: runtimeMetadataVersion
                ).WithFeatures(features.AsImmutable());

                // add option incompatibility errors if any
                diagnostics.AddRange(options.Errors);

                return new CSharpCommandLineArguments
                {
                    IsInteractive = IsInteractive,
                    BaseDirectory = baseDirectory,
                    Errors = diagnostics.AsImmutable(),
                    Utf8Output = utf8output,
                    CompilationName = compilationName,
                    OutputFileName = outputFileName,
                    PdbPath = pdbPath,
                    OutputDirectory = outputDirectory,
                    DocumentationPath = documentationPath,
                    AppConfigPath = appConfigPath,
                    SourceFiles = sourceFiles.AsImmutable(),
                    Encoding = codepage,
                    MetadataReferences = metadataReferences.AsImmutable(),
                    AnalyzerReferences = analyzers.AsImmutable(),
                    ReferencePaths = referencePaths,
                    KeyFileSearchPaths = keyFileSearchPaths.AsImmutable(),
                    Win32ResourceFile = win32ResourceFile,
                    Win32Icon = win32IconFile,
                    Win32Manifest = win32ManifestFile,
                    NoWin32Manifest = noWin32Manifest,
                    DisplayLogo = displayLogo,
                    DisplayHelp = displayHelp,
                    ManifestResources = managedResources.AsImmutable(),
                    CompilationOptions = options,
                    ParseOptions = IsInteractive ? scriptParseOptions : parseOptions,
                    ScriptArguments = scriptArgs.AsImmutableOrEmpty(),
                    TouchedFilesPath = touchedFilesPath,
                    PrintFullPaths = printFullPaths,
                    ShouldIncludeErrorEndLocation = errorEndLocation,
                    PreferredUILang = preferredUILang,
                    SqmSessionGuid = sqmSessionGuid
                };
            }
        }

        private static void ParseAndResolveReferencePaths(string switchName, string switchValue, string baseDirectory, List<string> builder, MessageID origin, List<Diagnostic> diagnostics)
        {
            if (string.IsNullOrEmpty(switchValue))
            {
                Debug.Assert(!string.IsNullOrEmpty(switchName));
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_PathList.Localize(), switchName);
                return;
            }

            foreach (string path in ParseSeparatedPaths(switchValue))
            {
                string resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                if (resolvedPath == null)
                {
                    AddDiagnostic(diagnostics, ErrorCode.WRN_InvalidSearchPathDir, path, origin.Localize(), MessageID.IDS_DirectoryHasInvalidPath.Localize());
                }
                else if (!Directory.Exists(resolvedPath))
                {
                    AddDiagnostic(diagnostics, ErrorCode.WRN_InvalidSearchPathDir, path, origin.Localize(), MessageID.IDS_DirectoryDoesNotExist.Localize());
                }
                else
                {
                    builder.Add(resolvedPath);
                }
            }
        }

        private static string GetWin32Setting(string arg, string value, List<Diagnostic> diagnostics)
        {
            if (value == null)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
            }
            else
            {
                string noQuotes = RemoveAllQuotes(value);
                if (string.IsNullOrWhiteSpace(noQuotes))
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                }
                else
                {
                    return noQuotes;
                }
            }

            return null;
        }

        private void GetCompilationAndModuleNames(
            List<Diagnostic> diagnostics,
            OutputKind outputKind,
            List<CommandLineSourceFile> sourceFiles,
            bool sourceFilesSpecified,
            string moduleAssemblyName,
            ref string outputFileName,
            ref string moduleName,
            out string compilationName)
        {
            string simpleName;
            if (outputFileName == null)
            {
                // In C#, if the output file name isn't specified explicitly, then executables take their
                // names from the files containing their entrypoints and libraries derive their names from 
                // their first input files.

                if (!IsInteractive && !sourceFilesSpecified)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_OutputNeedsName);
                    simpleName = null;
                }
                else if (outputKind.IsApplication())
                {
                    simpleName = null;
                }
                else
                {
                    simpleName = PathUtilities.RemoveExtension(PathUtilities.GetFileName(sourceFiles.FirstOrDefault().Path));
                    outputFileName = simpleName + outputKind.GetDefaultExtension();
                }
            }
            else
            {
                simpleName = PathUtilities.RemoveExtension(outputFileName);

                if (simpleName.Length == 0)
                {
                    AddDiagnostic(diagnostics, ErrorCode.FTL_InputFileNameTooLong, outputFileName);
                    outputFileName = simpleName = null;
                }
            }

            if (outputKind.IsNetModule())
            {
                Debug.Assert(!IsInteractive);

                compilationName = moduleAssemblyName;
            }
            else
            {
                if (moduleAssemblyName != null)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_AssemblyNameOnNonModule);
                }

                compilationName = simpleName;
            }

            if (moduleName == null)
            {
                moduleName = outputFileName;
            }
        }

        private static ImmutableArray<string> BuildSearchPaths(List<string> libPaths)
        {
            var builder = ArrayBuilder<string>.GetInstance();

            // Match how Dev11 builds the list of search paths
            //    see PCWSTR LangCompiler::GetSearchPath()

            // current folder first -- base directory is searched by default

            // SDK path is specified or current runtime directory
            builder.Add(RuntimeEnvironment.GetRuntimeDirectory());

            // libpath
            builder.AddRange(libPaths);

            return builder.ToImmutableAndFree();
        }

        public static IEnumerable<string> ParseConditionalCompilationSymbols(string value, out IEnumerable<Diagnostic> diagnostics)
        {
            Diagnostic myDiagnostic = null;

            value = value.TrimEnd(null);
            // Allow a trailing semicolon or comma in the options
            if (!value.IsEmpty() &&
                (value.Last() == ';' || value.Last() == ','))
            {
                value = value.Substring(0, value.Length - 1);
            }

            string[] values = value.Split(new char[] { ';', ',' } /*, StringSplitOptions.RemoveEmptyEntries*/);
            var defines = new ArrayBuilder<string>(values.Length);

            foreach (string id in values)
            {
                string trimmedId = id.Trim();
                if (SyntaxFacts.IsValidIdentifier(trimmedId))
                {
                    defines.Add(trimmedId);
                }
                else if (myDiagnostic == null)
                {
                    myDiagnostic = Diagnostic.Create(CSharp.MessageProvider.Instance, (int)ErrorCode.WRN_DefineIdentifierRequired, trimmedId);
                }
            }

            diagnostics = myDiagnostic == null ? SpecializedCollections.EmptyEnumerable<Diagnostic>()
                                                : SpecializedCollections.SingletonEnumerable(myDiagnostic);

            return defines.AsEnumerable();
        }

        private static Platform ParsePlatform(string value, IList<Diagnostic> diagnostics)
        {
            switch (value.ToLowerInvariant())
            {
                case "x86":
                    return Platform.X86;
                case "x64":
                    return Platform.X64;
                case "itanium":
                    return Platform.Itanium;
                case "anycpu":
                    return Platform.AnyCpu;
                case "anycpu32bitpreferred":
                    return Platform.AnyCpu32BitPreferred;
                case "arm":
                    return Platform.Arm;
                default:
                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadPlatformType, value);
                    return Platform.AnyCpu;
            }
        }

        private static OutputKind ParseTarget(string value, IList<Diagnostic> diagnostics)
        {
            switch (value.ToLowerInvariant())
            {
                case "exe":
                    return OutputKind.ConsoleApplication;

                case "winexe":
                    return OutputKind.WindowsApplication;

                case "library":
                    return OutputKind.DynamicallyLinkedLibrary;

                case "module":
                    return OutputKind.NetModule;

                case "appcontainerexe":
                    return OutputKind.WindowsRuntimeApplication;

                case "winmdobj":
                    return OutputKind.WindowsRuntimeMetadata;

                default:
                    AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidTarget);
                    return OutputKind.ConsoleApplication;
            }
        }

        private static IEnumerable<string> ParseUsings(string arg, string value, IList<Diagnostic> diagnostics)
        {
            if (value.Length == 0)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Namespace1.Localize(), arg);
                yield break;
            }

            foreach (var u in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return u;
            }
        }

        private IEnumerable<CommandLineAnalyzerReference> ParseAnalyzers(string arg, string value, List<Diagnostic> diagnostics)
        {
            if (value == null)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
                yield break;
            }
            else if (value.Length == 0)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                yield break;
            }

            List<string> paths = ParseSeparatedPaths(value).Where((path) => !string.IsNullOrWhiteSpace(path)).ToList();

            foreach (string path in paths)
            {
                yield return new CommandLineAnalyzerReference(path);
            }
        }

        private IEnumerable<CommandLineReference> ParseAssemblyReferences(string arg, string value, IList<Diagnostic> diagnostics, bool embedInteropTypes)
        {
            if (value == null)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
                yield break;
            }
            else if (value.Length == 0)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                yield break;
            }

            // /r:"reference"
            // /r:alias=reference
            // /r:alias="reference"
            // /r:reference;reference
            // /r:"path;containing;semicolons"
            // /r:"unterminated_quotes
            // /r:"quotes"in"the"middle
            // /r:alias=reference;reference      ... error 2034
            // /r:nonidf=reference               ... error 1679

            int eqlOrQuote = value.IndexOfAny(new[] { '"', '=' });

            string alias;
            if (eqlOrQuote >= 0 && value[eqlOrQuote] == '=')
            {
                alias = value.Substring(0, eqlOrQuote);
                value = value.Substring(eqlOrQuote + 1);

                if (!SyntaxFacts.IsValidIdentifier(alias))
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadExternIdentifier, alias);
                    yield break;
                }
            }
            else
            {
                alias = null;
            }

            List<string> paths = ParseSeparatedPaths(value).Where((path) => !string.IsNullOrWhiteSpace(path)).ToList();
            if (alias != null)
            {
                if (paths.Count > 1)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_OneAliasPerReference, value);
                    yield break;
                }

                if (paths.Count == 0)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_AliasMissingFile, alias);
                    yield break;
                }
            }

            foreach (string path in paths)
            {
                // NOTE(tomat): Dev10 used to report CS1541: ERR_CantIncludeDirectory if the path was a directory.
                // Since we now support /referencePaths option we would need to search them to see if the resolved path is a directory.

                var aliases = (alias != null) ? ImmutableArray.Create(alias) : ImmutableArray<string>.Empty;

                var properties = new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, embedInteropTypes);
                yield return new CommandLineReference(path, properties);
            }
        }

        private static void ValidateWin32Settings(string win32ResourceFile, string win32IconResourceFile, string win32ManifestFile, OutputKind outputKind, IList<Diagnostic> diagnostics)
        {
            if (win32ResourceFile != null)
            {
                if (win32IconResourceFile != null)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_CantHaveWin32ResAndIcon);
                }

                if (win32ManifestFile != null)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_CantHaveWin32ResAndManifest);
                }
            }

            if (outputKind.IsNetModule() && win32ManifestFile != null)
            {
                AddDiagnostic(diagnostics, ErrorCode.WRN_CantHaveManifestForModule);
            }
        }

        internal static ResourceDescription ParseResourceDescription(
            string arg,
            string resourceDescriptor,
            string baseDirectory,
            IList<Diagnostic> diagnostics,
            bool embedded)
        {
            string filePath;
            string fullPath;
            string fileName;
            string resourceName;
            string accessibility;

            ParseResourceDescription(
                resourceDescriptor,
                baseDirectory,
                false,
                out filePath,
                out fullPath,
                out fileName,
                out resourceName,
                out accessibility);

            bool isPublic;
            if (accessibility == null)
            {
                // If no accessibility is given, we default to "public".
                // NOTE: Dev10 distinguishes between null and empty/whitespace-only.
                isPublic = true;
            }
            else if (string.Equals(accessibility, "public", StringComparison.OrdinalIgnoreCase))
            {
                isPublic = true;
            }
            else if (string.Equals(accessibility, "private", StringComparison.OrdinalIgnoreCase))
            {
                isPublic = false;
            }
            else
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_BadResourceVis, accessibility);
                return null;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                return null;
            }

            if (fullPath == null || string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                AddDiagnostic(diagnostics, ErrorCode.FTL_InputFileNameTooLong, filePath);
                return null;
            }

            Func<Stream> dataProvider = () => new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return new ResourceDescription(resourceName, fileName, dataProvider, isPublic, embedded, checkArgs: false);
        }

        private static bool TryParseLanguageVersion(string str, LanguageVersion defaultVersion, out LanguageVersion version)
        {
            if (str == null)
            {
                version = defaultVersion;
                return true;
            }

            switch (str.ToLowerInvariant())
            {
                case "iso-1":
                    version = LanguageVersion.CSharp1;
                    return true;

                case "iso-2":
                    version = LanguageVersion.CSharp2;
                    return true;

                case "default":
                    version = defaultVersion;
                    return true;

                case "experimental":
                    version = LanguageVersion.Experimental;
                    return true;

                default:
                    int versionNumber;
                    if (int.TryParse(str, NumberStyles.None, CultureInfo.InvariantCulture, out versionNumber) && ((LanguageVersion)versionNumber).IsValid() && versionNumber != (int)LanguageVersion.Experimental)
                    {
                        version = (LanguageVersion)versionNumber;
                        return true;
                    }
                    version = defaultVersion;
                    return false;
            }
        }

        private static IEnumerable<string> ParseWarnings(string value, IList<Diagnostic> diagnostics)
        {
            value = Unquote(value);
            string[] values = value.Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string id in values)
            {
                ushort number;
                if (!ushort.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out number) || 
                    (!ErrorFacts.IsWarning((ErrorCode)number)))
                {
                    AddDiagnostic(diagnostics, ErrorCode.WRN_BadWarningNumber, id);
                }
                else
                {
                    yield return CSharp.MessageProvider.Instance.GetIdForErrorCode(number);
                }
            }
        }

        private static void AddWarnings(Dictionary<string, ReportDiagnostic> d, ReportDiagnostic kind, IEnumerable<string> items)
        {
            foreach (var id in items)
            {
                ReportDiagnostic existing;
                if (d.TryGetValue(id, out existing))
                {
                    // Rewrite the existing value with the latest one unless it is for /nowarn.
                    if (existing != ReportDiagnostic.Suppress)
                        d[id] = kind;
                }
                else
                {
                    d.Add(id, kind);
                }
            }
        }

        private static void UnimplementedSwitch(IList<Diagnostic> diagnostics, string switchName)
        {
            AddDiagnostic(diagnostics, ErrorCode.WRN_UnimplementedCommandLineSwitch, "/" + switchName);
        }

        private static void UnimplementedSwitchValue(IList<Diagnostic> diagnostics, string switchName, string value)
        {
            AddDiagnostic(diagnostics, ErrorCode.WRN_UnimplementedCommandLineSwitch, "/" + switchName + ":" + value);
        }

        internal override void GenerateErrorForNoFilesFoundInRecurse(string path, IList<Diagnostic> diagnostics)
        {
            //  no error in csc.exe
        }

        private static void AddDiagnostic(IList<Diagnostic> diagnostics, ErrorCode errorCode, params object[] arguments)
        {
            diagnostics.Add(Diagnostic.Create(CSharp.MessageProvider.Instance, (int)errorCode, arguments));
        }

        /// <summary>
        /// Diagnostic for the errorCode added if the warningOptions does not mention suppressed for the errorCode.
        /// </summary>
        private static void AddDiagnostic(IList<Diagnostic> diagnostics, Dictionary<string, ReportDiagnostic> warningOptions, ErrorCode errorCode, params object[] arguments)
        {
            int code = (int)errorCode;
            ReportDiagnostic value;
            warningOptions.TryGetValue(CSharp.MessageProvider.Instance.GetIdForErrorCode(code), out value);
            if (value != ReportDiagnostic.Suppress)
            {
                AddDiagnostic(diagnostics, errorCode, arguments);
            }
        }
    }
}
