// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class CSharpCommandLineParser : CommandLineParser
    {
        public static CSharpCommandLineParser Default { get; } = new CSharpCommandLineParser();
        public static CSharpCommandLineParser Script { get; } = new CSharpCommandLineParser(isScriptCommandLineParser: true);

        private static readonly char[] s_quoteOrEquals = new[] { '"', '=' };
        private static readonly char[] s_warningSeparators = new char[] { ',', ';', ' ' };

        internal CSharpCommandLineParser(bool isScriptCommandLineParser = false)
            : base(CSharp.MessageProvider.Instance, isScriptCommandLineParser)
        {
        }

        protected override string RegularFileExtension { get { return ".cs"; } }
        protected override string ScriptFileExtension { get { return ".csx"; } }

        internal sealed override CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string? sdkDirectory, string? additionalReferenceDirectories)
        {
            return Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories);
        }

        /// <summary>
        /// Parses a command line.
        /// </summary>
        /// <param name="args">A collection of strings representing the command line arguments.</param>
        /// <param name="baseDirectory">The base directory used for qualifying file locations.</param>
        /// <param name="sdkDirectory">The directory to search for mscorlib, or null if not available.</param>
        /// <param name="additionalReferenceDirectories">A string representing additional reference paths.</param>
        /// <returns>a commandlinearguments object representing the parsed command line.</returns>
        public new CSharpCommandLineArguments Parse(IEnumerable<string> args, string? baseDirectory, string? sdkDirectory, string? additionalReferenceDirectories = null)
        {
            Debug.Assert(baseDirectory == null || PathUtilities.IsAbsolute(baseDirectory));

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            var flattenedArgs = ArrayBuilder<string>.GetInstance();
            List<string>? scriptArgs = IsScriptCommandLineParser ? new List<string>() : null;
            List<string>? responsePaths = IsScriptCommandLineParser ? new List<string>() : null;
            FlattenArgs(args, diagnostics, flattenedArgs, scriptArgs, baseDirectory, responsePaths);

            string? appConfigPath = null;
            bool displayLogo = true;
            bool displayHelp = false;
            bool displayVersion = false;
            bool displayLangVersions = false;
            bool optimize = false;
            bool checkOverflow = false;
            NullableContextOptions nullableContextOptions = NullableContextOptions.Disable;
            bool allowUnsafe = false;
            bool concurrentBuild = true;
            bool deterministic = false; // TODO(5431): Enable deterministic mode by default
            bool emitPdb = false;
            DebugInformationFormat debugInformationFormat = PathUtilities.IsUnixLikePlatform ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb;
            bool debugPlus = false;
            string? pdbPath = null;
            bool noStdLib = IsScriptCommandLineParser; // don't add mscorlib from sdk dir when running scripts
            bool noSdkPath = false;
            string? outputDirectory = baseDirectory;
            ImmutableArray<KeyValuePair<string, string>> pathMap = ImmutableArray<KeyValuePair<string, string>>.Empty;
            string? outputFileName = null;
            string? outputRefFilePath = null;
            bool refOnly = false;
            string? generatedFilesOutputDirectory = null;
            string? documentationPath = null;
            ErrorLogOptions? errorLogOptions = null;
            bool parseDocumentationComments = false; //Don't just null check documentationFileName because we want to do this even if the file name is invalid.
            bool utf8output = false;
            OutputKind outputKind = OutputKind.ConsoleApplication;
            SubsystemVersion subsystemVersion = SubsystemVersion.None;
            LanguageVersion languageVersion = LanguageVersion.Default;
            string? mainTypeName = null;
            string? win32ManifestFile = null;
            string? win32ResourceFile = null;
            string? win32IconFile = null;
            bool noWin32Manifest = false;
            Platform platform = Platform.AnyCpu;
            ulong baseAddress = 0;
            int fileAlignment = 0;
            bool? delaySignSetting = null;
            string? keyFileSetting = null;
            string? keyContainerSetting = null;
            List<CommandLineResource> managedResources = new List<CommandLineResource>();
            List<CommandLineSourceFile> sourceFiles = new List<CommandLineSourceFile>();
            List<CommandLineSourceFile> additionalFiles = new List<CommandLineSourceFile>();
            var analyzerConfigPaths = ArrayBuilder<string>.GetInstance();
            List<CommandLineSourceFile> embeddedFiles = new List<CommandLineSourceFile>();
            bool sourceFilesSpecified = false;
            bool embedAllSourceFiles = false;
            bool resourcesOrModulesSpecified = false;
            Encoding? codepage = null;
            var checksumAlgorithm = SourceHashAlgorithms.Default;
            var defines = ArrayBuilder<string>.GetInstance();
            List<CommandLineReference> metadataReferences = new List<CommandLineReference>();
            List<CommandLineAnalyzerReference> analyzers = new List<CommandLineAnalyzerReference>();
            List<string> libPaths = new List<string>();
            List<string> sourcePaths = new List<string>();
            List<string> keyFileSearchPaths = new List<string>();
            List<string> usings = new List<string>();
            var generalDiagnosticOption = ReportDiagnostic.Default;
            var diagnosticOptions = new Dictionary<string, ReportDiagnostic>();
            var noWarns = new Dictionary<string, ReportDiagnostic>();
            var warnAsErrors = new Dictionary<string, ReportDiagnostic>();
            int warningLevel = Diagnostic.DefaultWarningLevel;
            bool highEntropyVA = false;
            bool printFullPaths = false;
            string? moduleAssemblyName = null;
            string? moduleName = null;
            List<string> features = new List<string>();
            string? runtimeMetadataVersion = null;
            bool errorEndLocation = false;
            bool reportAnalyzer = false;
            bool skipAnalyzers = false;
            ArrayBuilder<InstrumentationKind> instrumentationKinds = ArrayBuilder<InstrumentationKind>.GetInstance();
            CultureInfo? preferredUILang = null;
            string? touchedFilesPath = null;
            bool optionsEnded = false;
            bool interactiveMode = false;
            bool publicSign = false;
            string? sourceLink = null;
            string? ruleSetPath = null;
            bool reportIVTs = false;

            // Process ruleset files first so that diagnostic severity settings specified on the command line via
            // /nowarn and /warnaserror can override diagnostic severity settings specified in the ruleset file.
            if (!IsScriptCommandLineParser)
            {
                foreach (string arg in flattenedArgs)
                {
                    if (IsOption("ruleset", arg, out ReadOnlyMemory<char> name, out ReadOnlyMemory<char>? value))
                    {
                        var unquoted = RemoveQuotesAndSlashes(value);

                        if (RoslynString.IsNullOrEmpty(unquoted))
                        {
                            AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name.ToString());
                        }
                        else
                        {
                            ruleSetPath = ParseGenericPathToFile(unquoted, diagnostics, baseDirectory);
                            generalDiagnosticOption = GetDiagnosticOptionsFromRulesetFile(ruleSetPath, out diagnosticOptions, diagnostics);
                        }
                    }
                }
            }

            foreach (string arg in flattenedArgs)
            {
                Debug.Assert(optionsEnded || !arg.StartsWith("@", StringComparison.Ordinal));

                ArrayBuilder<string> filePathBuilder;
                ReadOnlyMemory<char> nameMemory;
                ReadOnlyMemory<char>? valueMemory;
                if (optionsEnded || !TryParseOption(arg, out nameMemory, out valueMemory))
                {
                    filePathBuilder = ArrayBuilder<string>.GetInstance();
                    ParseFileArgument(arg.AsMemory(), baseDirectory, filePathBuilder, diagnostics);
                    foreach (var path in filePathBuilder)
                    {
                        sourceFiles.Add(ToCommandLineSourceFile(path));
                    }
                    filePathBuilder.Free();

                    if (sourceFiles.Count > 0)
                    {
                        sourceFilesSpecified = true;
                    }

                    continue;
                }

                string? value;
                string? valueMemoryString() => valueMemory is { } m ? m.Span.ToString() : null;

                // The main 'switch' for argument handling forces an allocation of the option name field. For the most 
                // common options we special case the handling below to avoid this allocation as it can contribute significantly 
                // to parsing allocations.
                //
                // When we allow for switching on Span<char> this can be undone as the name 'switch' will be allocation free
                // https://github.com/dotnet/roslyn/pull/44388
                if (IsOptionName("r", "reference", nameMemory))
                {
                    ParseAssemblyReferences(arg, valueMemory, diagnostics, embedInteropTypes: false, metadataReferences);
                    continue;
                }
                else if (IsOptionName("langversion", nameMemory))
                {
                    value = RemoveQuotesAndSlashes(valueMemory);
                    if (RoslynString.IsNullOrEmpty(value))
                    {
                        AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "/langversion:");
                    }
                    else if (value.StartsWith("0", StringComparison.Ordinal))
                    {
                        // This error was added in 7.1 to stop parsing versions as ints (behaviour in previous Roslyn compilers), and explicitly
                        // treat them as identifiers (behaviour in native compiler). This error helps users identify that breaking change.
                        AddDiagnostic(diagnostics, ErrorCode.ERR_LanguageVersionCannotHaveLeadingZeroes, value);
                    }
                    else if (value == "?")
                    {
                        displayLangVersions = true;
                    }
                    else if (!LanguageVersionFacts.TryParse(value, out languageVersion))
                    {
                        AddDiagnostic(diagnostics, ErrorCode.ERR_BadCompatMode, value);
                    }
                    continue;
                }
                else if (!IsScriptCommandLineParser && IsOptionName("a", "analyzer", nameMemory))
                {
                    ParseAnalyzers(arg, valueMemory, analyzers, diagnostics);
                    continue;
                }
                else if (!IsScriptCommandLineParser && IsOptionName("nowarn", nameMemory))
                {
                    if (valueMemory is null)
                    {
                        AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, nameMemory.ToString());
                        continue;
                    }

                    if (valueMemory.Value.Length == 0)
                    {
                        AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, nameMemory.ToString());
                    }
                    else
                    {
                        AddWarnings(noWarns, ReportDiagnostic.Suppress, valueMemory.Value);
                    }
                    continue;
                }

                string name = nameMemory.Span.ToString().ToLowerInvariant();
                switch (name)
                {
                    case "?":
                    case "help":
                        displayHelp = true;
                        continue;

                    case "version":
                        displayVersion = true;
                        continue;

                    case "features":
                        value = valueMemoryString();
                        if (value == null)
                        {
                            features.Clear();
                        }
                        else
                        {
                            // When a features value like "InterceptorsNamespaces=NS1;NS2" is provided,
                            // the build system will quote it so that splitting doesn't occur in the wrong layer.
                            // We need to unquote here so that subsequent layers can properly identify the feature name and value.
                            features.Add(value.Unquote());
                        }
                        continue;

                    case "lib":
                    case "libpath":
                    case "libpaths":
                        ParseAndResolveReferencePaths(name, valueMemory, baseDirectory, libPaths, MessageID.IDS_LIB_OPTION, diagnostics);
                        continue;

#if DEBUG
                    case "attachdebugger":
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            // Debugger.Launch() only works on Windows.
                            _ = Debugger.Launch();
                        }
                        else
                        {
                            var timeout = TimeSpan.FromMinutes(2);
                            Console.WriteLine($"Compiler started with process ID {Environment.ProcessId}");
                            Console.WriteLine($"Waiting {timeout:g} for a debugger to attach");
                            using var timeoutSource = new CancellationTokenSource(timeout);
                            while (!Debugger.IsAttached && !timeoutSource.Token.IsCancellationRequested)
                            {
                                Thread.Sleep(100);
                            }
                        }
                        continue;
#endif
                }

                if (IsScriptCommandLineParser)
                {
                    value = valueMemoryString();
                    switch (name)
                    {
                        case "-": // csi -- script.csx
                            if (value != null) break;
                            if (arg == "-")
                            {
                                if (Console.IsInputRedirected)
                                {
                                    sourceFiles.Add(new CommandLineSourceFile("-", isScript: true, isInputRedirected: true));
                                    sourceFilesSpecified = true;
                                }
                                else
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_StdInOptionProvidedButConsoleInputIsNotRedirected);
                                }
                                continue;
                            }

                            // Indicates that the remaining arguments should not be treated as options.
                            optionsEnded = true;
                            continue;

                        case "i":
                        case "i+":
                            if (value != null) break;
                            interactiveMode = true;
                            continue;

                        case "i-":
                            if (value != null) break;
                            interactiveMode = false;
                            continue;

                        case "loadpath":
                        case "loadpaths":
                            ParseAndResolveReferencePaths(name, valueMemory, baseDirectory, sourcePaths, MessageID.IDS_REFERENCEPATH_OPTION, diagnostics);
                            continue;

                        case "u":
                        case "using":
                        case "usings":
                        case "import":
                        case "imports":
                            usings.AddRange(ParseUsings(arg, value, diagnostics));
                            continue;
                    }
                }
                else
                {
                    switch (name)
                    {
                        case "d":
                        case "define":
                            if (valueMemory is not { Length: > 0 })
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", arg);
                                continue;
                            }

                            IEnumerable<Diagnostic> defineDiagnostics;
                            ParseConditionalCompilationSymbols(RemoveQuotesAndSlashesEx(valueMemory.Value), defines, out defineDiagnostics);
                            diagnostics.AddRange(defineDiagnostics);
                            continue;

                        case "codepage":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (value == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                continue;
                            }

                            var encoding = TryParseEncodingName(value);
                            if (encoding == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.FTL_BadCodepage, value);
                                continue;
                            }

                            codepage = encoding;
                            continue;

                        case "checksumalgorithm":
                            value = valueMemoryString();
                            if (string.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                continue;
                            }

                            if (!SourceHashAlgorithms.TryParseAlgorithmName(value!, out var newChecksumAlgorithm))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.FTL_BadChecksumAlgorithm, value);
                                continue;
                            }

                            checksumAlgorithm = newChecksumAlgorithm;
                            continue;

                        case "checked":
                        case "checked+":
                            if (valueMemory is not null)
                            {
                                break;
                            }

                            checkOverflow = true;
                            continue;

                        case "checked-":
                            if (valueMemory is not null)
                                break;

                            checkOverflow = false;
                            continue;

                        case "nullable":

                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (value != null)
                            {
                                if (value.IsEmpty())
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), name);
                                    continue;
                                }

                                string loweredValue = value.ToLower();
                                switch (loweredValue)
                                {
                                    case "disable":
                                        Debug.Assert(loweredValue == nameof(NullableContextOptions.Disable).ToLower());
                                        nullableContextOptions = NullableContextOptions.Disable;
                                        break;
                                    case "enable":
                                        Debug.Assert(loweredValue == nameof(NullableContextOptions.Enable).ToLower());
                                        nullableContextOptions = NullableContextOptions.Enable;
                                        break;
                                    case "warnings":
                                        Debug.Assert(loweredValue == nameof(NullableContextOptions.Warnings).ToLower());
                                        nullableContextOptions = NullableContextOptions.Warnings;
                                        break;
                                    case "annotations":
                                        Debug.Assert(loweredValue == nameof(NullableContextOptions.Annotations).ToLower());
                                        nullableContextOptions = NullableContextOptions.Annotations;
                                        break;
                                    default:
                                        AddDiagnostic(diagnostics, ErrorCode.ERR_BadNullableContextOption, value);
                                        break;
                                }
                            }
                            else
                            {
                                nullableContextOptions = NullableContextOptions.Enable;
                            }
                            continue;

                        case "nullable+":
                            if (valueMemory is not null)
                            {
                                break;
                            }

                            nullableContextOptions = NullableContextOptions.Enable;
                            continue;

                        case "nullable-":
                            if (valueMemory is not null)
                                break;

                            nullableContextOptions = NullableContextOptions.Disable;
                            continue;

                        case "instrument":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                            }
                            else
                            {
                                foreach (InstrumentationKind instrumentationKind in ParseInstrumentationKinds(value, diagnostics))
                                {
                                    if (!instrumentationKinds.Contains(instrumentationKind))
                                    {
                                        instrumentationKinds.Add(instrumentationKind);
                                    }
                                }
                            }

                            continue;

                        case "noconfig":
                            // It is already handled (see CommonCommandLineCompiler.cs).
                            continue;

                        case "sqmsessionguid":
                            // The use of SQM is deprecated in the compiler but we still support the parsing of the option for
                            // back compat reason
                            value = valueMemoryString();
                            if (value == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_MissingGuidForOption, "<text>", name);
                            }
                            else
                            {
                                Guid sqmSessionGuid;
                                if (!Guid.TryParse(value, out sqmSessionGuid))
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_InvalidFormatForGuidForOption, value, name);
                                }
                            }
                            continue;

                        case "preferreduilang":
                            value = RemoveQuotesAndSlashes(valueMemory);

                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", arg);
                                continue;
                            }

                            try
                            {
                                preferredUILang = new CultureInfo(value);
                                if ((preferredUILang.CultureTypes & CultureTypes.UserCustomCulture) != 0)
                                {
                                    // Do not use user custom cultures.
                                    preferredUILang = null;
                                }
                            }
                            catch (CultureNotFoundException)
                            {
                            }

                            if (preferredUILang == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.WRN_BadUILang, value);
                            }

                            continue;

                        case "nosdkpath":
                            noSdkPath = true;

                            continue;

                        case "sdkpath":
                            noSdkPath = false;

                            if (valueMemory is not { Length: > 0 })
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<path>", name);
                                continue;
                            }

                            sdkDirectory = RemoveQuotesAndSlashes(valueMemory);

                            continue;

                        case "out":
                            value = valueMemoryString();
                            if (RoslynString.IsNullOrWhiteSpace(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                            }
                            else
                            {
                                ParseOutputFile(value, diagnostics, baseDirectory, out outputFileName, out outputDirectory);
                            }

                            continue;

                        case "refout":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                            }
                            else
                            {
                                outputRefFilePath = ParseGenericPathToFile(value, diagnostics, baseDirectory);
                            }

                            continue;

                        case "refonly":
                            if (valueMemory is not null)
                                break;

                            refOnly = true;
                            continue;

                        case "t":
                        case "target":
                            value = valueMemoryString();
                            if (value == null)
                            {
                                break; // force 'unrecognized option'
                            }

                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidTarget);
                            }
                            else
                            {
                                outputKind = ParseTarget(value, diagnostics);
                            }

                            continue;

                        case "moduleassemblyname":
                            value = valueMemoryString();
                            value = value != null ? value.Unquote() : null;

                            if (RoslynString.IsNullOrEmpty(value))
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

                        case "modulename":
                            var unquotedModuleName = RemoveQuotesAndSlashes(valueMemory);
                            if (string.IsNullOrEmpty(unquotedModuleName))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "modulename");
                                continue;
                            }
                            else
                            {
                                moduleName = unquotedModuleName;
                            }

                            continue;

                        case "platform":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<string>", arg);
                            }
                            else
                            {
                                platform = ParsePlatform(value, diagnostics);
                            }
                            continue;

                        case "recurse":
                            value = RemoveQuotesAndSlashes(valueMemory);

                            if (value == null)
                            {
                                break; // force 'unrecognized option'
                            }
                            else if (RoslynString.IsNullOrEmpty(value))
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

                        case "generatedfilesout":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
                            }
                            else
                            {
                                generatedFilesOutputDirectory = ParseGenericPathToFile(value, diagnostics, baseDirectory);
                            }
                            continue;

                        case "doc":
                            parseDocumentationComments = true;
                            value = valueMemoryString();
                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
                                continue;
                            }
                            string? unquoted = RemoveQuotesAndSlashes(valueMemory);
                            if (RoslynString.IsNullOrEmpty(unquoted))
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
                            value = valueMemoryString();
                            if (value == null)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), "/addmodule:");
                            }
                            else if (value.Length == 0)
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
                            ParseAssemblyReferences(arg, valueMemory, diagnostics, embedInteropTypes: true, metadataReferences);
                            continue;

                        case "win32res":
                            win32ResourceFile = GetWin32Setting(arg, valueMemoryString(), diagnostics);
                            continue;

                        case "win32icon":
                            win32IconFile = GetWin32Setting(arg, valueMemoryString(), diagnostics);
                            continue;

                        case "win32manifest":
                            win32ManifestFile = GetWin32Setting(arg, valueMemoryString(), diagnostics);
                            noWin32Manifest = false;
                            continue;

                        case "nowin32manifest":
                            noWin32Manifest = true;
                            win32ManifestFile = null;
                            continue;

                        case "res":
                        case "resource":
                            if (valueMemory is null)
                            {
                                break; // Dev11 reports unrecognized option
                            }

                            if (TryParseResourceDescription(arg, valueMemory.Value, baseDirectory, diagnostics, isEmbedded: true, out var embeddedResource))
                            {
                                managedResources.Add(embeddedResource);
                                resourcesOrModulesSpecified = true;
                            }

                            continue;

                        case "linkres":
                        case "linkresource":
                            if (valueMemory is null)
                            {
                                break; // Dev11 reports unrecognized option
                            }

                            if (TryParseResourceDescription(arg, valueMemory.Value, baseDirectory, diagnostics, isEmbedded: false, out var linkedResource))
                            {
                                managedResources.Add(linkedResource);
                                resourcesOrModulesSpecified = true;
                            }

                            continue;

                        case "sourcelink":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                            }
                            else
                            {
                                sourceLink = ParseGenericPathToFile(value, diagnostics, baseDirectory);
                            }
                            continue;

                        case "debug":
                            emitPdb = true;

                            // unused, parsed for backward compat only
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (value != null)
                            {
                                if (value.IsEmpty())
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), name);
                                    continue;
                                }
                                switch (value.ToLower())
                                {
                                    case "full":
                                    case "pdbonly":
                                        debugInformationFormat = PathUtilities.IsUnixLikePlatform ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb;
                                        break;
                                    case "portable":
                                        debugInformationFormat = DebugInformationFormat.PortablePdb;
                                        break;
                                    case "embedded":
                                        debugInformationFormat = DebugInformationFormat.Embedded;
                                        break;
                                    default:
                                        AddDiagnostic(diagnostics, ErrorCode.ERR_BadDebugType, value);
                                        break;
                                }
                            }
                            continue;

                        case "debug+":
                            //guard against "debug+:xx"
                            if (valueMemory is not null)
                                break;

                            emitPdb = true;
                            debugPlus = true;
                            continue;

                        case "debug-":
                            if (valueMemory is not null)
                                break;

                            emitPdb = false;
                            debugPlus = false;
                            continue;

                        case "o":
                        case "optimize":
                        case "o+":
                        case "optimize+":
                            if (valueMemory is not null)
                                break;

                            optimize = true;
                            continue;

                        case "o-":
                        case "optimize-":
                            if (valueMemory is not null)
                                break;

                            optimize = false;
                            continue;

                        case "deterministic":
                        case "deterministic+":
                            if (valueMemory is not null)
                                break;

                            deterministic = true;
                            continue;

                        case "deterministic-":
                            if (valueMemory is not null)
                                break;
                            deterministic = false;
                            continue;

                        case "p":
                        case "parallel":
                        case "p+":
                        case "parallel+":
                            if (valueMemory is not null)
                                break;

                            concurrentBuild = true;
                            continue;

                        case "p-":
                        case "parallel-":
                            if (valueMemory is not null)
                                break;

                            concurrentBuild = false;
                            continue;

                        case "warnaserror":
                        case "warnaserror+":
                            if (valueMemory is null)
                            {
                                generalDiagnosticOption = ReportDiagnostic.Error;

                                // Reset specific warnaserror options (since last /warnaserror flag on the command line always wins),
                                // and bump warnings to errors.
                                warnAsErrors.Clear();
                                foreach (var key in diagnosticOptions.Keys)
                                {
                                    if (diagnosticOptions[key] == ReportDiagnostic.Warn)
                                    {
                                        warnAsErrors[key] = ReportDiagnostic.Error;
                                    }
                                }

                                continue;
                            }

                            if (valueMemory.Value.Length == 0)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                            }
                            else
                            {
                                AddWarnings(warnAsErrors, ReportDiagnostic.Error, valueMemory.Value);
                            }
                            continue;

                        case "warnaserror-":
                            if (valueMemory is null)
                            {
                                generalDiagnosticOption = ReportDiagnostic.Default;

                                // Clear specific warnaserror options (since last /warnaserror flag on the command line always wins).
                                warnAsErrors.Clear();

                                continue;
                            }

                            if (valueMemory is not { Length: > 0 })
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                            }
                            else
                            {
                                var builder = ArrayBuilder<string>.GetInstance();
                                ParseWarnings(valueMemory.Value, builder);
                                foreach (var id in builder)
                                {
                                    ReportDiagnostic ruleSetValue;
                                    if (diagnosticOptions.TryGetValue(id, out ruleSetValue))
                                    {
                                        warnAsErrors[id] = ruleSetValue;
                                    }
                                    else
                                    {
                                        warnAsErrors[id] = ReportDiagnostic.Default;
                                    }
                                }
                                builder.Free();
                            }
                            continue;

                        case "w":
                        case "warn":
                            value = RemoveQuotesAndSlashes(valueMemory);
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
                            else if (newWarningLevel < 0)
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_BadWarningLevel);
                            }
                            else
                            {
                                warningLevel = newWarningLevel;
                            }
                            continue;

                        case "unsafe":
                        case "unsafe+":
                            if (valueMemory is not null)
                                break;

                            allowUnsafe = true;
                            continue;

                        case "unsafe-":
                            if (valueMemory is not null)
                                break;

                            allowUnsafe = false;
                            continue;

                        case "delaysign":
                        case "delaysign+":
                            if (valueMemory is not null)
                            {
                                break;
                            }

                            delaySignSetting = true;
                            continue;

                        case "delaysign-":
                            if (valueMemory is not null)
                            {
                                break;
                            }

                            delaySignSetting = false;
                            continue;

                        case "publicsign":
                        case "publicsign+":
                            if (valueMemory is not null)
                            {
                                break;
                            }

                            publicSign = true;
                            continue;

                        case "publicsign-":
                            if (valueMemory is not null)
                            {
                                break;
                            }

                            publicSign = false;
                            continue;

                        case "keyfile":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (string.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, "keyfile");
                            }
                            else
                            {
                                keyFileSetting = value;
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
                            value = valueMemoryString();
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
                            if (valueMemory is not null)
                                break;

                            highEntropyVA = true;
                            continue;

                        case "highentropyva-":
                            if (valueMemory is not null)
                                break;

                            highEntropyVA = false;
                            continue;

                        case "nologo":
                            displayLogo = false;
                            continue;

                        case "baseaddress":
                            value = RemoveQuotesAndSlashes(valueMemory);

                            ulong newBaseAddress;
                            if (string.IsNullOrEmpty(value) || !TryParseUInt64(value, out newBaseAddress))
                            {
                                if (RoslynString.IsNullOrEmpty(value))
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
                            value = valueMemoryString();
                            if (RoslynString.IsNullOrEmpty(value))
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
                                AddDiagnostic(diagnostics, ErrorCode.ERR_InvalidSubsystemVersion, value);
                            }

                            continue;

                        case "touchedfiles":
                            unquoted = RemoveQuotesAndSlashes(valueMemory);
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
                            if (valueMemory is not null)
                                break;

                            utf8output = true;
                            continue;

                        case "m":
                        case "main":
                            // Remove any quotes for consistent behavior as MSBuild can return quoted or 
                            // unquoted main.    
                            unquoted = RemoveQuotesAndSlashes(valueMemory);
                            if (string.IsNullOrEmpty(unquoted))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                continue;
                            }

                            mainTypeName = unquoted;
                            continue;

                        case "fullpaths":
                            if (valueMemory is not null)
                                break;

                            printFullPaths = true;
                            continue;

                        case "pathmap":
                            // "/pathmap:K1=V1,K2=V2..."
                            {
                                unquoted = RemoveQuotesAndSlashes(valueMemory);

                                if (unquoted == null)
                                {
                                    break;
                                }

                                pathMap = pathMap.Concat(ParsePathMap(unquoted, diagnostics));
                            }
                            continue;

                        case "filealign":
                            value = RemoveQuotesAndSlashes(valueMemory);

                            ushort newAlignment;
                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsNumber, name);
                            }
                            else if (!TryParseUInt16(value, out newAlignment))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_InvalidFileAlignment, value);
                            }
                            else if (!CompilationOptions.IsValidFileAlignment(newAlignment))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_InvalidFileAlignment, value);
                            }
                            else
                            {
                                fileAlignment = newAlignment;
                            }
                            continue;

                        case "pdb":
                            value = RemoveQuotesAndSlashes(valueMemory);
                            if (RoslynString.IsNullOrEmpty(value))
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

                        case "reportanalyzer":
                            reportAnalyzer = true;
                            continue;

                        case "skipanalyzers":
                        case "skipanalyzers+":
                            if (valueMemory is not null)
                                break;

                            skipAnalyzers = true;
                            continue;

                        case "skipanalyzers-":
                            if (valueMemory is not null)
                                break;

                            skipAnalyzers = false;
                            continue;

                        case "nostdlib":
                        case "nostdlib+":
                            if (valueMemory is not null)
                                break;

                            noStdLib = true;
                            continue;

                        case "nostdlib-":
                            if (valueMemory is not null)
                                break;

                            noStdLib = false;
                            continue;

                        case "errorreport":
                            continue;

                        case "errorlog":
                            valueMemory = RemoveQuotesAndSlashesEx(valueMemory);
                            if (valueMemory is not { Length: > 0 })
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, ErrorLogOptionFormat, RemoveQuotesAndSlashes(arg));
                            }
                            else
                            {
                                errorLogOptions = ParseErrorLogOptions(valueMemory.Value, diagnostics, baseDirectory, out bool diagnosticAlreadyReported);
                                if (errorLogOptions == null && !diagnosticAlreadyReported)
                                {
                                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadSwitchValue, valueMemory.Value.ToString(), "/errorlog:", ErrorLogOptionFormat);
                                }
                            }
                            continue;

                        case "appconfig":
                            unquoted = RemoveQuotesAndSlashes(valueMemory);
                            if (RoslynString.IsNullOrEmpty(unquoted))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, ":<text>", RemoveQuotesAndSlashes(arg));
                            }
                            else
                            {
                                appConfigPath = ParseGenericPathToFile(unquoted, diagnostics, baseDirectory);
                            }
                            continue;

                        case "runtimemetadataversion":
                            unquoted = RemoveQuotesAndSlashes(valueMemory);
                            if (string.IsNullOrEmpty(unquoted))
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<text>", name);
                                continue;
                            }

                            runtimeMetadataVersion = unquoted;
                            continue;

                        case "ruleset":
                            // The ruleset arg has already been processed in a separate pass above.
                            continue;

                        case "additionalfile":
                            if (valueMemory is not { Length: > 0 })
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<file list>", name);
                                continue;
                            }

                            filePathBuilder = ArrayBuilder<string>.GetInstance();
                            ParseSeparatedFileArgument(valueMemory.Value, baseDirectory, filePathBuilder, diagnostics);
                            foreach (var path in filePathBuilder)
                            {
                                additionalFiles.Add(ToCommandLineSourceFile(path));
                            }
                            filePathBuilder.Free();
                            continue;
                        case "analyzerconfig":
                            if (valueMemory is not { Length: > 0 })
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, "<file list>", name);
                                continue;
                            }

                            filePathBuilder = ArrayBuilder<string>.GetInstance();
                            ParseSeparatedFileArgument(valueMemory.Value, baseDirectory, filePathBuilder, diagnostics);
                            analyzerConfigPaths.AddRange(filePathBuilder);
                            filePathBuilder.Free();
                            continue;

                        case "embed":
                            value = valueMemoryString();
                            if (RoslynString.IsNullOrEmpty(value))
                            {
                                embedAllSourceFiles = true;
                                continue;
                            }

                            filePathBuilder = ArrayBuilder<string>.GetInstance();
                            ParseSeparatedFileArgument(value.AsMemory(), baseDirectory, filePathBuilder, diagnostics);
                            foreach (var path in filePathBuilder)
                            {
                                embeddedFiles.Add(ToCommandLineSourceFile(path));
                            }
                            filePathBuilder.Free();
                            continue;

                        case "-":
                            if (Console.IsInputRedirected)
                            {
                                sourceFiles.Add(new CommandLineSourceFile("-", isScript: false, isInputRedirected: true));
                                sourceFilesSpecified = true;
                            }
                            else
                            {
                                AddDiagnostic(diagnostics, ErrorCode.ERR_StdInOptionProvidedButConsoleInputIsNotRedirected);
                            }
                            continue;

                        case "reportivts":
                        case "reportivts+":
                            if (valueMemory is not null) break;
                            reportIVTs = true;
                            continue;

                        case "reportivts-":
                            if (valueMemory is not null) break;
                            reportIVTs = false;
                            continue;
                    }
                }

                AddDiagnostic(diagnostics, ErrorCode.ERR_BadSwitch, arg);
            }

            foreach (var o in warnAsErrors)
            {
                diagnosticOptions[o.Key] = o.Value;
            }

            // Specific nowarn options always override specific warnaserror options.
            foreach (var o in noWarns)
            {
                diagnosticOptions[o.Key] = o.Value;
            }

            if (refOnly && outputRefFilePath != null)
            {
                AddDiagnostic(diagnostics, diagnosticOptions, ErrorCode.ERR_NoRefOutWhenRefOnly);
            }

            if (outputKind == OutputKind.NetModule && (refOnly || outputRefFilePath != null))
            {
                AddDiagnostic(diagnostics, diagnosticOptions, ErrorCode.ERR_NoNetModuleOutputWhenRefOutOrRefOnly);
            }

            if (!IsScriptCommandLineParser && !sourceFilesSpecified && (outputKind.IsNetModule() || !resourcesOrModulesSpecified))
            {
                AddDiagnostic(diagnostics, diagnosticOptions, ErrorCode.WRN_NoSources);
            }

            if (noSdkPath)
            {
                sdkDirectory = null;
            }

            if (!noStdLib && sdkDirectory != null)
            {
                metadataReferences.Insert(0, new CommandLineReference(Path.Combine(sdkDirectory, "mscorlib.dll"), MetadataReferenceProperties.Assembly));
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
            if (!string.IsNullOrEmpty(additionalReferenceDirectories))
            {
                ParseAndResolveReferencePaths(null, additionalReferenceDirectories.AsMemory(), baseDirectory, libPaths, MessageID.IDS_LIB_ENV, diagnostics);
            }

            ImmutableArray<string> referencePaths = BuildSearchPaths(sdkDirectory, libPaths, responsePaths);

            ValidateWin32Settings(win32ResourceFile, win32IconFile, win32ManifestFile, outputKind, diagnostics);

            // Dev11 searches for the key file in the current directory and assembly output directory.
            // We always look to base directory and then examine the search paths.
            if (!RoslynString.IsNullOrEmpty(baseDirectory))
            {
                keyFileSearchPaths.Add(baseDirectory);
            }

            if (RoslynString.IsNullOrEmpty(outputDirectory))
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoOutputDirectory);
            }
            else if (baseDirectory != outputDirectory)
            {
                keyFileSearchPaths.Add(outputDirectory);
            }

            // Public sign doesn't use the legacy search path settings
            if (publicSign && !RoslynString.IsNullOrEmpty(keyFileSetting))
            {
                keyFileSetting = ParseGenericPathToFile(keyFileSetting, diagnostics, baseDirectory);
            }

            if (sourceLink != null && !emitPdb)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_SourceLinkRequiresPdb);
            }

            if (embedAllSourceFiles)
            {
                embeddedFiles.AddRange(sourceFiles);
            }

            if (embeddedFiles.Count > 0 && !emitPdb)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_CannotEmbedWithoutPdb);
            }

            var parsedFeatures = ParseFeatures(features);

            string? compilationName;
            GetCompilationAndModuleNames(diagnostics, outputKind, sourceFiles, sourceFilesSpecified, moduleAssemblyName, ref outputFileName, ref moduleName, out compilationName);

            flattenedArgs.Free();

            var parseOptions = new CSharpParseOptions
            (
                languageVersion: languageVersion,
                preprocessorSymbols: defines.ToImmutableAndFree(),
                documentationMode: parseDocumentationComments ? DocumentationMode.Diagnose : DocumentationMode.None,
                kind: IsScriptCommandLineParser ? SourceCodeKind.Script : SourceCodeKind.Regular,
                features: parsedFeatures
            );

            // We want to report diagnostics with source suppression in the error log file.
            // However, these diagnostics won't be reported on the command line.
            var reportSuppressedDiagnostics = errorLogOptions is object;

            var options = new CSharpCompilationOptions
            (
                outputKind: outputKind,
                moduleName: moduleName,
                mainTypeName: mainTypeName,
                scriptClassName: WellKnownMemberNames.DefaultScriptClassName,
                usings: usings,
                optimizationLevel: optimize ? OptimizationLevel.Release : OptimizationLevel.Debug,
                checkOverflow: checkOverflow,
                nullableContextOptions: nullableContextOptions,
                allowUnsafe: allowUnsafe,
                deterministic: deterministic,
                concurrentBuild: concurrentBuild,
                cryptoKeyContainer: keyContainerSetting,
                cryptoKeyFile: keyFileSetting,
                delaySign: delaySignSetting,
                platform: platform,
                generalDiagnosticOption: generalDiagnosticOption,
                warningLevel: warningLevel,
                specificDiagnosticOptions: diagnosticOptions,
                reportSuppressedDiagnostics: reportSuppressedDiagnostics,
                publicSign: publicSign
            );

            if (debugPlus)
            {
                options = options.WithDebugPlusMode(debugPlus);
            }

            var emitOptions = new EmitOptions
            (
                metadataOnly: refOnly,
                includePrivateMembers: !refOnly && outputRefFilePath == null,
                debugInformationFormat: debugInformationFormat,
                pdbFilePath: null, // to be determined later
                outputNameOverride: null, // to be determined later
                baseAddress: baseAddress,
                highEntropyVirtualAddressSpace: highEntropyVA,
                fileAlignment: fileAlignment,
                subsystemVersion: subsystemVersion,
                runtimeMetadataVersion: runtimeMetadataVersion,
                instrumentationKinds: instrumentationKinds.ToImmutableAndFree(),
                // TODO: set from /checksumalgorithm (see https://github.com/dotnet/roslyn/issues/24735)
                pdbChecksumAlgorithm: HashAlgorithmName.SHA256,
                defaultSourceFileEncoding: codepage
            );

            // add option incompatibility errors if any
            diagnostics.AddRange(options.Errors);
            diagnostics.AddRange(parseOptions.Errors);

            if (nullableContextOptions != NullableContextOptions.Disable && parseOptions.LanguageVersion < MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())
            {
                diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_NullableOptionNotAvailable,
                                                 "nullable", nullableContextOptions, parseOptions.LanguageVersion.ToDisplayString(),
                                                 new CSharpRequiredLanguageVersion(MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion())), Location.None));
            }

            pathMap = SortPathMap(pathMap);

            return new CSharpCommandLineArguments
            {
                IsScriptRunner = IsScriptCommandLineParser,
                InteractiveMode = interactiveMode || IsScriptCommandLineParser && sourceFiles.Count == 0,
                BaseDirectory = baseDirectory,
                PathMap = pathMap,
                Errors = diagnostics.AsImmutable(),
                Utf8Output = utf8output,
                CompilationName = compilationName,
                OutputFileName = outputFileName,
                OutputRefFilePath = outputRefFilePath,
                PdbPath = pdbPath,
                EmitPdb = emitPdb && !refOnly, // silently ignore emitPdb when refOnly is set
                SourceLink = sourceLink,
                RuleSetPath = ruleSetPath,
                OutputDirectory = outputDirectory!, // error produced when null
                DocumentationPath = documentationPath,
                GeneratedFilesOutputDirectory = generatedFilesOutputDirectory,
                ErrorLogOptions = errorLogOptions,
                AppConfigPath = appConfigPath,
                SourceFiles = sourceFiles.AsImmutable(),
                Encoding = codepage,
                ChecksumAlgorithm = checksumAlgorithm,
                MetadataReferences = metadataReferences.AsImmutable(),
                AnalyzerReferences = analyzers.AsImmutable(),
                AnalyzerConfigPaths = analyzerConfigPaths.ToImmutableAndFree(),
                AdditionalFiles = additionalFiles.AsImmutable(),
                ReferencePaths = referencePaths,
                SourcePaths = sourcePaths.AsImmutable(),
                KeyFileSearchPaths = keyFileSearchPaths.AsImmutable(),
                Win32ResourceFile = win32ResourceFile,
                Win32Icon = win32IconFile,
                Win32Manifest = win32ManifestFile,
                NoWin32Manifest = noWin32Manifest,
                DisplayLogo = displayLogo,
                DisplayHelp = displayHelp,
                DisplayVersion = displayVersion,
                DisplayLangVersions = displayLangVersions,
                ManifestResourceArguments = managedResources.AsImmutable(),
                CompilationOptions = options,
                ParseOptions = parseOptions,
                EmitOptions = emitOptions,
                ScriptArguments = scriptArgs.AsImmutableOrEmpty(),
                TouchedFilesPath = touchedFilesPath,
                PrintFullPaths = printFullPaths,
                ShouldIncludeErrorEndLocation = errorEndLocation,
                PreferredUILang = preferredUILang,
                ReportAnalyzer = reportAnalyzer,
                SkipAnalyzers = skipAnalyzers,
                EmbeddedFiles = embeddedFiles.AsImmutable(),
                ReportInternalsVisibleToAttributes = reportIVTs,
            };
        }

        private static void ParseAndResolveReferencePaths(string? switchName, ReadOnlyMemory<char>? switchValue, string? baseDirectory, List<string> builder, MessageID origin, List<Diagnostic> diagnostics)
        {
            if (switchValue is not { Length: > 0 })
            {
                RoslynDebug.Assert(!RoslynString.IsNullOrEmpty(switchName));
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_PathList.Localize(), switchName);
                return;
            }

            foreach (string path in ParseSeparatedPaths(switchValue.Value.ToString()))
            {
                string? resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
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

        private static string? GetWin32Setting(string arg, string? value, List<Diagnostic> diagnostics)
        {
            if (value == null)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
            }
            else
            {
                string noQuotes = RemoveQuotesAndSlashes(value);
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
            string? moduleAssemblyName,
            ref string? outputFileName,
            ref string? moduleName,
            out string? compilationName)
        {
            string? simpleName;
            if (outputFileName == null)
            {
                // In C#, if the output file name isn't specified explicitly, then executables take their
                // names from the files containing their entrypoints and libraries derive their names from 
                // their first input files.

                if (!IsScriptCommandLineParser && !sourceFilesSpecified)
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

                    if (simpleName.Length == 0 && !outputKind.IsNetModule())
                    {
                        AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidInputFileName, outputFileName);
                        outputFileName = simpleName = null;
                    }
                }
            }
            else
            {
                simpleName = PathUtilities.RemoveExtension(outputFileName);

                if (simpleName.Length == 0)
                {
                    AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidInputFileName, outputFileName);
                    outputFileName = simpleName = null;
                }
            }

            if (outputKind.IsNetModule())
            {
                Debug.Assert(!IsScriptCommandLineParser);

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

        private ImmutableArray<string> BuildSearchPaths(string? sdkDirectoryOpt, List<string> libPaths, List<string>? responsePathsOpt)
        {
            var builder = ArrayBuilder<string>.GetInstance();

            // Match how Dev11 builds the list of search paths
            //    see PCWSTR LangCompiler::GetSearchPath()

            // current folder first -- base directory is searched by default

            // Add SDK directory if it is available
            if (sdkDirectoryOpt != null)
            {
                builder.Add(sdkDirectoryOpt);
            }

            // libpath
            builder.AddRange(libPaths);

            // csi adds paths of the response file(s) to the search paths, so that we can initialize the script environment
            // with references relative to csi.exe (e.g. System.ValueTuple.dll).
            if (responsePathsOpt != null)
            {
                Debug.Assert(IsScriptCommandLineParser);
                builder.AddRange(responsePathsOpt);
            }

            return builder.ToImmutableAndFree();
        }

        public static IEnumerable<string> ParseConditionalCompilationSymbols(string value, out IEnumerable<Diagnostic> diagnostics)
        {
            var builder = ArrayBuilder<string>.GetInstance();
            ParseConditionalCompilationSymbols(value.AsMemory(), builder, out diagnostics);
            return builder.ToArrayAndFree();
        }

        internal static void ParseConditionalCompilationSymbols(ReadOnlyMemory<char> valueMemory, ArrayBuilder<string> defines, out IEnumerable<Diagnostic> diagnostics)
        {
            DiagnosticBag outputDiagnostics = DiagnosticBag.GetInstance();

            if (valueMemory.IsWhiteSpace())
            {
                outputDiagnostics.Add(Diagnostic.Create(CSharp.MessageProvider.Instance, (int)ErrorCode.WRN_DefineIdentifierRequired, valueMemory.ToString()));
                diagnostics = outputDiagnostics.ToReadOnlyAndFree();
                return;
            }

            var valueSpan = valueMemory.Span;
            var nextIndex = 0;
            var index = 0;
            while (index < valueSpan.Length)
            {
                if (valueSpan[index] is ';' or ',')
                {
                    add();
                    nextIndex = index + 1;
                }
                index++;
            }

            if (nextIndex < valueSpan.Length)
            {
                add();
            }

            void add()
            {
                var id = valueMemory.Slice(nextIndex, index - nextIndex).Trim().ToString();
                if (SyntaxFacts.IsValidIdentifier(id))
                {
                    defines.Add(id);
                }
                else
                {
                    outputDiagnostics.Add(Diagnostic.Create(CSharp.MessageProvider.Instance, (int)ErrorCode.WRN_DefineIdentifierRequired, id));
                }
            }

            diagnostics = outputDiagnostics.ToReadOnlyAndFree();
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
                case "arm64":
                    return Platform.Arm64;
                default:
                    // This switch supports architectures that .NET Framework runs on Windows only.
                    // Non-Windows architectures that .NET runs on are intentionally not supported here.
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

        private static IEnumerable<string> ParseUsings(string arg, string? value, IList<Diagnostic> diagnostics)
        {
            if (RoslynString.IsNullOrEmpty(value))
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Namespace1.Localize(), arg);
                yield break;
            }

            foreach (var u in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return u;
            }
        }

        private static void ParseAnalyzers(string arg, ReadOnlyMemory<char>? valueMemory, List<CommandLineAnalyzerReference> analyzerReferences, List<Diagnostic> diagnostics)
        {
            if (valueMemory is not { } value)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
                return;
            }
            else if (value.Length == 0)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                return;
            }

            var builder = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            ParseSeparatedPathsEx(value, builder);
            foreach (var path in builder)
            {
                if (path.Length == 0)
                {
                    continue;
                }

                analyzerReferences.Add(new CommandLineAnalyzerReference(path.ToString()));
            }
            builder.Free();
        }

        private static void ParseAssemblyReferences(string arg, ReadOnlyMemory<char>? valueMemory, IList<Diagnostic> diagnostics, bool embedInteropTypes, List<CommandLineReference> commandLineReferences)
        {
            if (valueMemory is null)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_SwitchNeedsString, MessageID.IDS_Text.Localize(), arg);
                return;
            }

            var value = valueMemory.Value;
            if (value.Length == 0)
            {
                AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, arg);
                return;
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

            var valueSpan = value.Span;
            int eqlOrQuote = valueSpan.IndexOfAny(s_quoteOrEquals);

            string? alias;
            if (eqlOrQuote >= 0 && valueSpan[eqlOrQuote] == '=')
            {
                alias = value.Slice(0, eqlOrQuote).ToString();
                value = value.Slice(eqlOrQuote + 1);

                if (!SyntaxFacts.IsValidIdentifier(alias))
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadExternIdentifier, alias);
                    return;
                }
            }
            else
            {
                alias = null;
            }

            var builder = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            ParseSeparatedPathsEx(value, builder);
            var pathCount = 0;
            foreach (var path in builder)
            {
                if (path.IsWhiteSpace())
                {
                    continue;
                }

                pathCount++;

                // NOTE(tomat): Dev10 used to report CS1541: ERR_CantIncludeDirectory if the path was a directory.
                // Since we now support /referencePaths option we would need to search them to see if the resolved path is a directory.

                var aliases = (alias != null) ? ImmutableArray.Create(alias) : ImmutableArray<string>.Empty;

                var properties = new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, embedInteropTypes);
                commandLineReferences.Add(new CommandLineReference(path.ToString(), properties));
            }
            builder.Free();

            if (alias != null)
            {
                if (pathCount > 1)
                {
                    commandLineReferences.RemoveRange(commandLineReferences.Count - pathCount, pathCount);
                    AddDiagnostic(diagnostics, ErrorCode.ERR_OneAliasPerReference);
                    return;
                }

                if (pathCount == 0)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_AliasMissingFile, alias);
                    return;
                }
            }
        }

        private static void ValidateWin32Settings(string? win32ResourceFile, string? win32IconResourceFile, string? win32ManifestFile, OutputKind outputKind, IList<Diagnostic> diagnostics)
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

        private static IEnumerable<InstrumentationKind> ParseInstrumentationKinds(string value, IList<Diagnostic> diagnostics)
        {
            string[] kinds = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var kind in kinds)
            {
                switch (kind.ToLower())
                {
                    case "testcoverage":
                        yield return InstrumentationKind.TestCoverage;
                        break;

                    default:
                        AddDiagnostic(diagnostics, ErrorCode.ERR_InvalidInstrumentationKind, kind);
                        break;
                }
            }
        }

        internal static bool TryParseResourceDescription(
            string argName,
            ReadOnlyMemory<char> resourceDescriptor,
            string? baseDirectory,
            IList<Diagnostic> diagnostics,
            bool isEmbedded,
            out CommandLineResource resource)
        {
            if (!TryParseResourceDescription(
                resourceDescriptor,
                baseDirectory,
                skipLeadingSeparators: false,
                allowEmptyAccessibility: false,
                out var filePath,
                out var fullPath,
                out var fileName,
                out var resourceName,
                out var isPublic,
                out var rawAccessibility))
            {
                if (isPublic == null)
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_BadResourceVis, rawAccessibility ?? "");
                }
                else if (RoslynString.IsNullOrWhiteSpace(filePath))
                {
                    AddDiagnostic(diagnostics, ErrorCode.ERR_NoFileSpec, argName);
                }
                else
                {
                    Debug.Assert(!PathUtilities.IsValidFilePath(fullPath));
                    AddDiagnostic(diagnostics, ErrorCode.FTL_InvalidInputFileName, filePath);
                }

                resource = default;
                return false;
            }

            resource = new CommandLineResource(
                resourceName: resourceName,
                fullPath: fullPath,
                linkedResourceFileName: isEmbedded ? null : fileName,
                isPublic.Value);

            return true;
        }

        private static void ParseWarnings(ReadOnlyMemory<char> value, ArrayBuilder<string> ids)
        {
            value = value.Unquote();
            var parts = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();

            var nullableSpan = "nullable".AsSpan();
            ParseSeparatedStrings(value, s_warningSeparators, removeEmptyEntries: true, parts);
            foreach (ReadOnlyMemory<char> part in parts)
            {
                if (part.Span.Equals(nullableSpan, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var errorCode in ErrorFacts.NullableWarnings)
                    {
                        ids.Add(errorCode);
                    }

                    ids.Add(CSharp.MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotation));
                    ids.Add(CSharp.MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode));
                    continue;
                }

                var id = part.ToString();
                if (ushort.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort number) &&
                       ErrorFacts.IsWarning((ErrorCode)number))
                {
                    // The id refers to a compiler warning.
                    ids.Add(CSharp.MessageProvider.Instance.GetIdForErrorCode(number));
                }
                else
                {
                    // Previous versions of the compiler used to report a warning (CS1691)
                    // whenever an unrecognized warning code was supplied in /nowarn or 
                    // /warnaserror. We no longer generate a warning in such cases.
                    // Instead we assume that the unrecognized id refers to a custom diagnostic.
                    ids.Add(id);
                }
            }

            parts.Free();
        }

        private static void AddWarnings(Dictionary<string, ReportDiagnostic> d, ReportDiagnostic kind, ReadOnlyMemory<char> warningArgument)
        {
            var idsBuilder = ArrayBuilder<string>.GetInstance();
            ParseWarnings(warningArgument, idsBuilder);
            foreach (var id in idsBuilder)
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

            idsBuilder.Free();
        }

        private static void UnimplementedSwitch(IList<Diagnostic> diagnostics, string switchName)
        {
            AddDiagnostic(diagnostics, ErrorCode.WRN_UnimplementedCommandLineSwitch, "/" + switchName);
        }

        internal override void GenerateErrorForNoFilesFoundInRecurse(string path, IList<Diagnostic> diagnostics)
        {
            //  no error in csc.exe
        }

        private static void AddDiagnostic(IList<Diagnostic> diagnostics, ErrorCode errorCode)
        {
            diagnostics.Add(Diagnostic.Create(CSharp.MessageProvider.Instance, (int)errorCode));
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
