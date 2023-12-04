// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct BuildPaths
    {
        /// <summary>
        /// The path which contains the compiler binaries and response files.
        /// </summary>
        internal string ClientDirectory { get; }

        /// <summary>
        /// The path in which the compilation takes place. This is also referred to as "baseDirectory" in
        /// the code base.
        /// </summary>
        internal string WorkingDirectory { get; }

        /// <summary>
        /// The path which contains mscorlib.  This can be null when specified by the user or running in a
        /// CoreClr environment.
        /// </summary>
        internal string? SdkDirectory { get; }

        /// <summary>
        /// The temporary directory a compilation should use instead of Path.GetTempPath.  The latter
        /// relies on global state individual compilations should ignore.
        /// </summary>
        internal string? TempDirectory { get; }

        internal BuildPaths(string clientDir, string workingDir, string? sdkDir, string? tempDir)
        {
            ClientDirectory = clientDir;
            WorkingDirectory = workingDir;
            SdkDirectory = sdkDir;
            TempDirectory = tempDir;
        }
    }

    /// <summary>
    /// Base class for csc.exe, csi.exe, vbc.exe and vbi.exe implementations.
    /// </summary>
    internal abstract partial class CommonCompiler
    {
        internal const int Failed = 1;
        internal const int Succeeded = 0;

        /// <summary>
        /// Fallback encoding that is lazily retrieved if needed. If <see cref="EncodedStringText.CreateFallbackEncoding"/> is
        /// evaluated and stored, the value is used if a PDB is created for this compilation.
        /// </summary>
        private readonly Lazy<Encoding> _fallbackEncoding = new Lazy<Encoding>(EncodedStringText.CreateFallbackEncoding);

        public CommonMessageProvider MessageProvider { get; }
        public CommandLineArguments Arguments { get; }
        public IAnalyzerAssemblyLoader AssemblyLoader { get; private set; }
        public GeneratorDriverCache? GeneratorDriverCache { get; }
        public abstract DiagnosticFormatter DiagnosticFormatter { get; }

        /// <summary>
        /// The set of source file paths that are in the set of embedded paths.
        /// This is used to prevent reading source files that are embedded twice.
        /// </summary>
        public IReadOnlySet<string> EmbeddedSourcePaths { get; }

        /// <summary>
        /// The <see cref="ICommonCompilerFileSystem"/> used to access the file system inside this instance.
        /// </summary>
        internal ICommonCompilerFileSystem FileSystem { get; set; }

        private readonly HashSet<Diagnostic> _reportedDiagnostics = new HashSet<Diagnostic>();

        public abstract Compilation? CreateCompilation(
            TextWriter consoleOutput,
            TouchedFileLogger? touchedFilesLogger,
            ErrorLogger? errorLoggerOpt,
            ImmutableArray<AnalyzerConfigOptionsResult> analyzerConfigOptions,
            AnalyzerConfigOptionsResult globalConfigOptions);

        public abstract void PrintLogo(TextWriter consoleOutput);
        public abstract void PrintHelp(TextWriter consoleOutput);
        public abstract void PrintLangVersions(TextWriter consoleOutput);

        /// <summary>
        /// Print compiler version
        /// </summary>
        /// <param name="consoleOutput"></param>
        public virtual void PrintVersion(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(GetCompilerVersion());
        }

        protected abstract bool TryGetCompilerDiagnosticCode(string diagnosticId, out uint code);

        protected abstract void ResolveAnalyzersFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider,
            CompilationOptions compilationOptions,
            bool skipAnalyzers,
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators);

        public CommonCompiler(CommandLineParser parser, string? responseFile, string[] args, BuildPaths buildPaths, string? additionalReferenceDirectories, IAnalyzerAssemblyLoader assemblyLoader, GeneratorDriverCache? driverCache, ICommonCompilerFileSystem? fileSystem)
        {
            IEnumerable<string> allArgs = args;

            Debug.Assert(null == responseFile || PathUtilities.IsAbsolute(responseFile));
            if (!SuppressDefaultResponseFile(args) && File.Exists(responseFile))
            {
                allArgs = new[] { "@" + responseFile }.Concat(allArgs);
            }

            this.Arguments = parser.Parse(allArgs, buildPaths.WorkingDirectory, buildPaths.SdkDirectory, additionalReferenceDirectories);
            this.MessageProvider = parser.MessageProvider;
            this.AssemblyLoader = assemblyLoader;
            this.GeneratorDriverCache = driverCache;
            this.EmbeddedSourcePaths = GetEmbeddedSourcePaths(Arguments);
            this.FileSystem = fileSystem ?? StandardFileSystem.Instance;
        }

        internal abstract bool SuppressDefaultResponseFile(IEnumerable<string> args);

        /// <summary>
        /// The type of the compiler class for version information in /help and /version.
        /// We don't simply use this.GetType() because that would break mock subclasses.
        /// </summary>
        internal abstract Type Type { get; }

        /// <summary>
        /// The version of this compiler with commit hash, used in logo and /version output.
        /// </summary>
        internal string GetCompilerVersion()
        {
            return GetProductVersion(Type);
        }

        internal static string GetProductVersion(Type type)
        {
            string? assemblyVersion = GetInformationalVersionWithoutHash(type);
            string? hash = GetShortCommitHash(type);
            return $"{assemblyVersion} ({hash})";
        }

        [return: NotNullIfNotNull(nameof(hash))]
        internal static string? ExtractShortCommitHash(string? hash)
        {
            // leave "<developer build>" alone, but truncate SHA to 8 characters
            if (hash != null && hash.Length >= 8 && hash[0] != '<')
            {
                return hash.Substring(0, 8);
            }

            return hash;
        }

        private static string? GetInformationalVersionWithoutHash(Type type)
        {
            // The attribute stores a SemVer2-formatted string: `A.B.C(-...)?(+...)?`
            // We remove the section after the + (if any is present)
            return type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0];
        }

        private static string? GetShortCommitHash(Type type)
        {
            var hash = type.Assembly.GetCustomAttribute<CommitHashAttribute>()?.Hash;
            return ExtractShortCommitHash(hash);
        }

        /// <summary>
        /// Tool name used, along with assembly version, for error logging.
        /// </summary>
        internal abstract string GetToolName();

        /// <summary>
        /// Tool version identifier used for error logging.
        /// </summary>
        internal Version? GetAssemblyVersion()
        {
            return Type.GetTypeInfo().Assembly.GetName().Version;
        }

        internal string GetCultureName()
        {
            return Culture.Name;
        }

        internal virtual Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
        {
            return (path, properties) =>
            {
                var peStream = FileSystem.OpenFileWithNormalizedException(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return MetadataReference.CreateFromFile(peStream, path, properties);
            };
        }

        internal virtual MetadataReferenceResolver GetCommandLineMetadataReferenceResolver(TouchedFileLogger? loggerOpt)
        {
            var pathResolver = new CompilerRelativePathResolver(FileSystem, Arguments.ReferencePaths, Arguments.BaseDirectory!);
            return new LoggingMetadataFileReferenceResolver(pathResolver, GetMetadataProvider(), loggerOpt);
        }

        /// <summary>
        /// Resolves metadata references stored in command line arguments and reports errors for those that can't be resolved.
        /// </summary>
        internal List<MetadataReference> ResolveMetadataReferences(
            List<DiagnosticInfo> diagnostics,
            TouchedFileLogger? touchedFiles,
            out MetadataReferenceResolver referenceDirectiveResolver)
        {
            var commandLineReferenceResolver = GetCommandLineMetadataReferenceResolver(touchedFiles);

            List<MetadataReference> resolved = new List<MetadataReference>();
            Arguments.ResolveMetadataReferences(commandLineReferenceResolver, diagnostics, this.MessageProvider, resolved);

            if (Arguments.IsScriptRunner)
            {
                referenceDirectiveResolver = commandLineReferenceResolver;
            }
            else
            {
                // when compiling into an assembly (csc/vbc) we only allow #r that match references given on command line:
                referenceDirectiveResolver = new ExistingReferencesResolver(commandLineReferenceResolver, resolved.ToImmutableArray());
            }

            return resolved;
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText? TryReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics)
        {
            return TryReadFileContent(file, diagnostics, out _);
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <param name="normalizedFilePath">If given <paramref name="file"/> opens successfully, set to normalized absolute path of the file, null otherwise.</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText? TryReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics, out string? normalizedFilePath)
        {
            var filePath = file.Path;
            try
            {
                if (file.IsInputRedirected)
                {
                    using var data = Console.OpenStandardInput();
                    normalizedFilePath = filePath;
                    return EncodedStringText.Create(data, _fallbackEncoding, Arguments.Encoding, Arguments.ChecksumAlgorithm, canBeEmbedded: EmbeddedSourcePaths.Contains(file.Path));
                }
                else
                {
                    using var data = OpenFileForReadWithSmallBufferOptimization(filePath, out normalizedFilePath);
                    return EncodedStringText.Create(data, _fallbackEncoding, Arguments.Encoding, Arguments.ChecksumAlgorithm, canBeEmbedded: EmbeddedSourcePaths.Contains(file.Path));
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(ToFileReadDiagnostics(this.MessageProvider, e, filePath));
                normalizedFilePath = null;
                return null;
            }
        }

        /// <summary>
        /// Read all analyzer config files from the given paths.
        /// </summary>
        internal bool TryGetAnalyzerConfigSet(
            ImmutableArray<string> analyzerConfigPaths,
            DiagnosticBag diagnostics,
            [NotNullWhen(true)] out AnalyzerConfigSet? analyzerConfigSet)
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance(analyzerConfigPaths.Length);

            var processedDirs = PooledHashSet<string>.GetInstance();

            foreach (var configPath in analyzerConfigPaths)
            {
                // The editorconfig spec requires all paths use '/' as the directory separator.
                // Since no known system allows directory separators as part of the file name,
                // we can replace every instance of the directory separator with a '/'
                string? fileContent = TryReadFileContent(configPath, diagnostics, out string? normalizedPath);
                if (fileContent is null)
                {
                    // Error reading a file. Bail out and report error.
                    break;
                }

                Debug.Assert(normalizedPath is object);
                var directory = Path.GetDirectoryName(normalizedPath) ?? normalizedPath;
                var editorConfig = AnalyzerConfig.Parse(fileContent, normalizedPath);

                if (!editorConfig.IsGlobal)
                {
                    if (processedDirs.Contains(directory))
                    {
                        diagnostics.Add(Diagnostic.Create(
                            MessageProvider,
                            MessageProvider.ERR_MultipleAnalyzerConfigsInSameDir,
                            directory));
                        break;
                    }
                    processedDirs.Add(directory);
                }
                configs.Add(editorConfig);
            }

            processedDirs.Free();

            if (diagnostics.HasAnyErrors())
            {
                configs.Free();
                analyzerConfigSet = null;
                return false;
            }

            analyzerConfigSet = AnalyzerConfigSet.Create(configs, out var setDiagnostics);
            diagnostics.AddRange(setDiagnostics);
            return true;
        }

        /// <summary>
        /// Returns the fallback encoding for parsing source files, if used, or null
        /// if not used
        /// </summary>
        internal Encoding? GetFallbackEncoding()
        {
            if (_fallbackEncoding.IsValueCreated)
            {
                return _fallbackEncoding.Value;
            }

            return null;
        }

        /// <summary>
        /// Read a UTF-8 encoded file and return the text as a string.
        /// </summary>
        private string? TryReadFileContent(string filePath, DiagnosticBag diagnostics, out string? normalizedPath)
        {
            try
            {
                var data = OpenFileForReadWithSmallBufferOptimization(filePath, out normalizedPath);
                using (var reader = new StreamReader(data, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(Diagnostic.Create(ToFileReadDiagnostics(MessageProvider, e, filePath)));
                normalizedPath = null;
                return null;
            }
        }

        private Stream OpenFileForReadWithSmallBufferOptimization(string filePath, out string normalizedFilePath)
            // PERF: Using a very small buffer size for the FileStream opens up an optimization within EncodedStringText/EmbeddedText where
            // we read the entire FileStream into a byte array in one shot. For files that are actually smaller than the buffer
            // size, FileStream.Read still allocates the internal buffer.
            => FileSystem.OpenFileEx(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 1,
                options: FileOptions.None,
                out normalizedFilePath);

        internal EmbeddedText? TryReadEmbeddedFileContent(string filePath, DiagnosticBag diagnostics)
        {
            try
            {
                using (var stream = OpenFileForReadWithSmallBufferOptimization(filePath, out _))
                {
                    const int LargeObjectHeapLimit = 80 * 1024;
                    if (stream.Length < LargeObjectHeapLimit)
                    {
                        ArraySegment<byte> bytes;
                        if (EncodedStringText.TryGetBytesFromStream(stream, out bytes))
                        {
                            return EmbeddedText.FromBytes(filePath, bytes, Arguments.ChecksumAlgorithm);
                        }
                    }

                    return EmbeddedText.FromStream(filePath, stream, Arguments.ChecksumAlgorithm);
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(MessageProvider.CreateDiagnostic(ToFileReadDiagnostics(this.MessageProvider, e, filePath)));
                return null;
            }
        }

        private ImmutableArray<EmbeddedText?> AcquireEmbeddedTexts(Compilation compilation, DiagnosticBag diagnostics)
        {
            Debug.Assert(compilation.Options.SourceReferenceResolver is object);
            if (Arguments.EmbeddedFiles.IsEmpty)
            {
                return ImmutableArray<EmbeddedText?>.Empty;
            }

            var embeddedTreeMap = new Dictionary<string, SyntaxTree>(Arguments.EmbeddedFiles.Length);
            var embeddedFileOrderedSet = new OrderedSet<string>(Arguments.EmbeddedFiles.Select(e => e.Path));

            foreach (var tree in compilation.SyntaxTrees)
            {
                // Skip trees that will not have their text embedded.
                if (!EmbeddedSourcePaths.Contains(tree.FilePath))
                {
                    continue;
                }

                // Skip trees with duplicated paths. (VB allows this and "first tree wins" is same as PDB emit policy.)
                if (embeddedTreeMap.ContainsKey(tree.FilePath))
                {
                    continue;
                }

                // map embedded file path to corresponding source tree
                embeddedTreeMap.Add(tree.FilePath, tree);

                // also embed the text of any #line directive targets of embedded tree
                ResolveEmbeddedFilesFromExternalSourceDirectives(tree, compilation.Options.SourceReferenceResolver, embeddedFileOrderedSet, diagnostics);
            }

            var embeddedTextBuilder = ImmutableArray.CreateBuilder<EmbeddedText?>(embeddedFileOrderedSet.Count);
            foreach (var path in embeddedFileOrderedSet)
            {
                SyntaxTree? tree;
                EmbeddedText? text;

                if (embeddedTreeMap.TryGetValue(path, out tree))
                {
                    text = EmbeddedText.FromSource(path, tree.GetText());
                    Debug.Assert(text != null);
                }
                else
                {
                    text = TryReadEmbeddedFileContent(path, diagnostics);
                    Debug.Assert(text != null || diagnostics.HasAnyErrors());
                }

                // We can safely add nulls because result will be ignored if any error is produced.
                // This allows the MoveToImmutable to work below in all cases.
                embeddedTextBuilder.Add(text);
            }

            return embeddedTextBuilder.MoveToImmutable();
        }

        protected abstract void ResolveEmbeddedFilesFromExternalSourceDirectives(
            SyntaxTree tree,
            SourceReferenceResolver resolver,
            OrderedSet<string> embeddedFiles,
            DiagnosticBag diagnostics);

        private static IReadOnlySet<string> GetEmbeddedSourcePaths(CommandLineArguments arguments)
        {
            if (arguments.EmbeddedFiles.IsEmpty)
            {
                return SpecializedCollections.EmptyReadOnlySet<string>();
            }

            // Note that we require an exact match between source and embedded file paths (case-sensitive
            // and without normalization). If two files are the same but spelled differently, they will
            // be handled as separate files, meaning the embedding pass will read the content a second
            // time. This can also lead to more than one document entry in the PDB for the same document
            // if the PDB document de-duping policy in emit (normalize + case-sensitive in C#,
            // normalize + case-insensitive in VB) is not enough to converge them.
            var set = new HashSet<string>(arguments.EmbeddedFiles.Select(f => f.Path));
            set.IntersectWith(arguments.SourceFiles.Select(f => f.Path));
            return SpecializedCollections.StronglyTypedReadOnlySet(set);
        }

        internal static DiagnosticInfo ToFileReadDiagnostics(CommonMessageProvider messageProvider, Exception e, string filePath)
        {
            DiagnosticInfo diagnosticInfo;

            if (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                diagnosticInfo = new DiagnosticInfo(messageProvider, messageProvider.ERR_FileNotFound, filePath);
            }
            else if (e is InvalidDataException)
            {
                diagnosticInfo = new DiagnosticInfo(messageProvider, messageProvider.ERR_BinaryFile, filePath);
            }
            else
            {
                diagnosticInfo = new DiagnosticInfo(messageProvider, messageProvider.ERR_NoSourceFile, filePath, e.Message);
            }

            return diagnosticInfo;
        }

        /// <summary>Returns true if there were any errors, false otherwise.</summary>
        internal bool ReportDiagnostics(IEnumerable<Diagnostic> diagnostics, TextWriter consoleOutput, ErrorLogger? errorLoggerOpt, Compilation? compilation)
        {
            bool hasErrors = false;
            foreach (var diag in diagnostics)
            {
                reportDiagnostic(diag, compilation == null ? null : diag.GetSuppressionInfo(compilation));
            }

            return hasErrors;

            // Local functions
            void reportDiagnostic(Diagnostic diag, SuppressionInfo? suppressionInfo)
            {
                if (_reportedDiagnostics.Contains(diag))
                {
                    // TODO: This invariant fails (at least) in the case where we see a member declaration "x = 1;".
                    // First we attempt to parse a member declaration starting at "x".  When we see the "=", we
                    // create an IncompleteMemberSyntax with return type "x" and an error at the location of the "x".
                    // Then we parse a member declaration starting at "=".  This is an invalid member declaration start
                    // so we attach an error to the "=" and attach it (plus following tokens) to the IncompleteMemberSyntax
                    // we previously created.
                    //this assert isn't valid if we change the design to not bail out after each phase.
                    //System.Diagnostics.Debug.Assert(diag.Severity != DiagnosticSeverity.Error);
                    return;
                }
                else if (diag.Severity == DiagnosticSeverity.Hidden)
                {
                    // Not reported from the command-line compiler.
                    return;
                }

                // We want to report diagnostics with source suppression in the error log file.
                // However, these diagnostics should not be reported on the console output.
                errorLoggerOpt?.LogDiagnostic(diag, suppressionInfo);

                // If the diagnostic was suppressed by one or more DiagnosticSuppressor(s), then we report info diagnostics for each suppression
                // so that the suppression information is available in the binary logs and verbose build logs.
                if (diag.ProgrammaticSuppressionInfo != null)
                {
                    foreach (var (id, justification) in diag.ProgrammaticSuppressionInfo.Suppressions)
                    {
                        var suppressionDiag = new SuppressionDiagnostic(diag, id, justification);
                        if (_reportedDiagnostics.Add(suppressionDiag))
                        {
                            PrintError(suppressionDiag, consoleOutput);
                        }
                    }

                    _reportedDiagnostics.Add(diag);
                    return;
                }

                if (diag.IsSuppressed)
                {
                    return;
                }

                // Diagnostics that aren't suppressed will be reported to the console output and, if they are errors,
                // they should fail the run
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                }

                PrintError(diag, consoleOutput);

                _reportedDiagnostics.Add(diag);
            }
        }

        /// <summary>Returns true if there were any errors, false otherwise.</summary>
        private bool ReportDiagnostics(DiagnosticBag diagnostics, TextWriter consoleOutput, ErrorLogger? errorLoggerOpt, Compilation? compilation)
            => ReportDiagnostics(diagnostics.ToReadOnly(), consoleOutput, errorLoggerOpt, compilation);

        /// <summary>Returns true if there were any errors, false otherwise.</summary>
        internal bool ReportDiagnostics(IEnumerable<DiagnosticInfo> diagnostics, TextWriter consoleOutput, ErrorLogger? errorLoggerOpt, Compilation? compilation)
            => ReportDiagnostics(diagnostics.Select(info => Diagnostic.Create(info)), consoleOutput, errorLoggerOpt, compilation);

        /// <summary>
        /// Reports all IVT information for the given compilation and references, to aid in troubleshooting otherwise inexplicable IVT failures.
        /// </summary>
        private void ReportIVTInfos(TextWriter consoleOutput, ErrorLogger? errorLogger, Compilation compilation, ImmutableArray<Diagnostic> diagnostics)
        {
            // Annotate any bad accesses with what assemblies they came from, if they are from a foreign assembly
            DiagnoseBadAccesses(consoleOutput, errorLogger, compilation, diagnostics);

            consoleOutput.WriteLine();

            // Printing 'InternalsVisibleToAttribute' information for the current compilation and all referenced assemblies.
            consoleOutput.WriteLine(CodeAnalysisResources.InternalsVisibleToHeaderSummary);

            var currentAssembly = compilation.Assembly;
            var currentAssemblyInternal = compilation.GetSymbolInternal<IAssemblySymbolInternal>(currentAssembly);

            // Current assembly: '{0}'
            consoleOutput.WriteLine(string.Format(CodeAnalysisResources.InternalsVisibleToCurrentAssembly, currentAssembly.Identity.GetDisplayName(fullKey: true)));

            consoleOutput.WriteLine();

            // Now, go through each of the referenced assemblies and print their IVT information.
            foreach (var assembly in currentAssembly.Modules.First().ReferencedAssemblySymbols.OrderBy(a => a.Name))
            {
                // Assembly reference: '{0}'
                //   Grants IVT to current assembly: {1}
                //   Grants IVTs to:

                var assemblyInternal = compilation.GetSymbolInternal<IAssemblySymbolInternal>(assembly);
                bool grantsIvt = currentAssemblyInternal.AreInternalsVisibleToThisAssembly(assemblyInternal);

                consoleOutput.WriteLine(string.Format(CodeAnalysisResources.InternalsVisibleToReferencedAssembly, assembly.Identity.GetDisplayName(fullKey: true), grantsIvt));

                var enumerable = assemblyInternal.GetInternalsVisibleToAssemblyNames();

                if (enumerable.Any())
                {
                    foreach (var simpleName in enumerable.OrderBy<string, string>(n => n))
                    {
                        //     Assembly name: '{0}'
                        //     Public Keys:
                        consoleOutput.WriteLine(string.Format(CodeAnalysisResources.InternalsVisibleToReferencedAssemblyDetails, simpleName));
                        foreach (var key in assemblyInternal.GetInternalsVisibleToPublicKeys(simpleName).Select(k => AssemblyIdentity.PublicKeyToString(k)).OrderBy(k => k))
                        {
                            consoleOutput.Write("      ");
                            consoleOutput.WriteLine(key);
                        }
                    }
                }
                else
                {
                    // Nothing
                    consoleOutput.Write("    ");
                    consoleOutput.WriteLine(CodeAnalysisResources.Nothing);
                }

                consoleOutput.WriteLine();
            }
        }

        private protected abstract void DiagnoseBadAccesses(TextWriter consoleOutput, ErrorLogger? errorLogger, Compilation compilation, ImmutableArray<Diagnostic> diagnostics);

        /// <summary>
        /// Returns true if there are any error diagnostics in the bag which cannot be suppressed and
        /// are guaranteed to break the build.
        /// Only diagnostics which have default severity error and are tagged as NotConfigurable fall in this bucket.
        /// This includes all compiler error diagnostics and specific analyzer error diagnostics that are marked as not configurable by the analyzer author.
        /// </summary>
        internal static bool HasUnsuppressableErrors(DiagnosticBag diagnostics)
        {
            foreach (var diag in diagnostics.AsEnumerable())
            {
                if (diag.IsUnsuppressableError())
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool HasSuppressableWarningsOrErrors(DiagnosticBag diagnostics)
        {
            foreach (var diag in diagnostics.AsEnumerable())
            {
                if (!diag.IsUnsuppressableError())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the bag has any diagnostics with effective Severity=Error. Also returns true for warnings or informationals
        /// or warnings promoted to error via /warnaserror which are not suppressed.
        /// </summary>
        internal static bool HasUnsuppressedErrors(DiagnosticBag diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics.AsEnumerable())
            {
                if (diagnostic.IsUnsuppressedError)
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual void PrintError(Diagnostic diagnostic, TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(DiagnosticFormatter.Format(diagnostic, Culture));
        }

        public SarifErrorLogger? GetErrorLogger(TextWriter consoleOutput)
        {
            Debug.Assert(Arguments.ErrorLogOptions?.Path != null);

            var diagnostics = DiagnosticBag.GetInstance();
            var errorLog = OpenFile(Arguments.ErrorLogOptions.Path,
                                    diagnostics,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.ReadWrite | FileShare.Delete);

            SarifErrorLogger? logger;
            if (errorLog == null)
            {
                Debug.Assert(diagnostics.HasAnyErrors());
                logger = null;
            }
            else
            {
                string toolName = GetToolName();
                string compilerVersion = GetCompilerVersion();
                Version assemblyVersion = GetAssemblyVersion() ?? new Version();

                if (Arguments.ErrorLogOptions.SarifVersion == SarifVersion.Sarif1)
                {
                    logger = new SarifV1ErrorLogger(errorLog, toolName, compilerVersion, assemblyVersion, Culture);
                }
                else
                {
                    logger = new SarifV2ErrorLogger(errorLog, toolName, compilerVersion, assemblyVersion, Culture);
                }
            }

            ReportDiagnostics(diagnostics.ToReadOnlyAndFree(), consoleOutput, errorLoggerOpt: logger, compilation: null);
            return logger;
        }

        /// <summary>
        /// csc.exe and vbc.exe entry point.
        /// </summary>
        public virtual int Run(TextWriter consoleOutput, CancellationToken cancellationToken = default)
        {
            var saveUICulture = CultureInfo.CurrentUICulture;
            SarifErrorLogger? errorLogger = null;

            try
            {
                // Messages from exceptions can be used as arguments for errors and they are often localized.
                // Ensure they are localized to the right language.
                var culture = this.Culture;
                if (culture != null)
                {
                    CultureInfo.CurrentUICulture = culture;
                }

                if (Arguments.ErrorLogOptions?.Path != null)
                {
                    errorLogger = GetErrorLogger(consoleOutput);
                    if (errorLogger == null)
                    {
                        return Failed;
                    }
                }

                return RunCore(consoleOutput, errorLogger, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                var errorCode = MessageProvider.ERR_CompileCancelled;
                if (errorCode > 0)
                {
                    var diag = new DiagnosticInfo(MessageProvider, errorCode);
                    ReportDiagnostics(new[] { diag }, consoleOutput, errorLogger, compilation: null);
                }

                return Failed;
            }
            finally
            {
                CultureInfo.CurrentUICulture = saveUICulture;
                errorLogger?.Dispose();
            }
        }

        /// <summary>
        /// Perform source generation, if the compiler supports it.
        /// </summary>
        /// <param name="input">The compilation before any source generation has occurred.</param>
        /// <param name="parseOptions">The <see cref="ParseOptions"/> to use when parsing any generated sources.</param>
        /// <param name="generators">The generators to run</param>
        /// <param name="analyzerConfigOptionsProvider">A provider that returns analyzer config options.</param>
        /// <param name="additionalTexts">Any additional texts that should be passed to the generators when run.</param>
        /// <param name="generatorDiagnostics">Any diagnostics that were produced during generation.</param>
        /// <returns>A compilation that represents the original compilation with any additional, generated texts added to it.</returns>
        private protected (Compilation Compilation, GeneratorDriverTimingInfo DriverTimingInfo) RunGenerators(
            Compilation input,
            ParseOptions parseOptions,
            ImmutableArray<ISourceGenerator> generators,
            AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
            ImmutableArray<AdditionalText> additionalTexts,
            DiagnosticBag generatorDiagnostics)
        {
            GeneratorDriver? driver = null;
            string cacheKey = string.Empty;
            bool disableCache =
                !Arguments.ParseOptions.Features.ContainsKey("enable-generator-cache") ||
                string.IsNullOrWhiteSpace(Arguments.OutputFileName);
            if (this.GeneratorDriverCache is object && !disableCache)
            {
                cacheKey = deriveCacheKey();
                driver = this.GeneratorDriverCache.TryGetDriver(cacheKey)?
                                                  .WithUpdatedParseOptions(parseOptions)
                                                  .WithUpdatedAnalyzerConfigOptions(analyzerConfigOptionsProvider)
                                                  .ReplaceAdditionalTexts(additionalTexts);
            }

            driver ??= CreateGeneratorDriver(parseOptions, generators, analyzerConfigOptionsProvider, additionalTexts);
            driver = driver.RunGeneratorsAndUpdateCompilation(input, out var compilationOut, out var diagnostics);
            generatorDiagnostics.AddRange(diagnostics);

            if (!disableCache)
            {
                this.GeneratorDriverCache?.CacheGenerator(cacheKey, driver);
            }

            return (compilationOut, driver.GetTimingInfo());

            string deriveCacheKey()
            {
                Debug.Assert(!string.IsNullOrWhiteSpace(Arguments.OutputFileName));

                // CONSIDER: The only piece of the cache key that is required for correctness is the generators that were used.
                //           We set up the graph statically based on the generators, so as long as the generator inputs haven't
                //           changed we can technically run any project against another's cache and still get the correct results.
                //           Obviously that would remove the point of the cache, so we also key off of the output file name
                //           and output path so that collisions are unlikely and we'll usually get the correct cache for any
                //           given compilation.

                PooledStringBuilder sb = PooledStringBuilder.GetInstance();
                sb.Builder.Append(Arguments.GetOutputFilePath(Arguments.OutputFileName));
                foreach (var generator in generators)
                {
                    // append the generator FQN and the MVID of the assembly it came from, so any changes will invalidate the cache
                    var type = generator.GetGeneratorType();
                    sb.Builder.Append(type.AssemblyQualifiedName);
                    sb.Builder.Append(type.Assembly.ManifestModule.ModuleVersionId.ToString());
                }
                return sb.ToStringAndFree();
            }
        }

        private protected abstract GeneratorDriver CreateGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, ImmutableArray<AdditionalText> additionalTexts);

        private int RunCore(TextWriter consoleOutput, ErrorLogger? errorLogger, CancellationToken cancellationToken)
        {
            Debug.Assert(!Arguments.IsScriptRunner);

            cancellationToken.ThrowIfCancellationRequested();

            if (Arguments.DisplayVersion)
            {
                PrintVersion(consoleOutput);
                return Succeeded;
            }

            if (Arguments.DisplayLangVersions)
            {
                PrintLangVersions(consoleOutput);
                return Succeeded;
            }

            if (Arguments.DisplayLogo)
            {
                PrintLogo(consoleOutput);
            }

            if (Arguments.DisplayHelp)
            {
                PrintHelp(consoleOutput);
                return Succeeded;
            }

            if (ReportDiagnostics(Arguments.Errors, consoleOutput, errorLogger, compilation: null))
            {
                return Failed;
            }

            var touchedFilesLogger = (Arguments.TouchedFilesPath != null) ? new TouchedFileLogger() : null;

            var diagnostics = DiagnosticBag.GetInstance();

            AnalyzerConfigSet? analyzerConfigSet = null;
            ImmutableArray<AnalyzerConfigOptionsResult> sourceFileAnalyzerConfigOptions = default;
            AnalyzerConfigOptionsResult globalConfigOptions = default;

            if (Arguments.AnalyzerConfigPaths.Length > 0)
            {
                if (!TryGetAnalyzerConfigSet(Arguments.AnalyzerConfigPaths, diagnostics, out analyzerConfigSet))
                {
                    var hadErrors = ReportDiagnostics(diagnostics, consoleOutput, errorLogger, compilation: null);
                    Debug.Assert(hadErrors);
                    return Failed;
                }

                globalConfigOptions = analyzerConfigSet.GlobalConfigOptions;
                sourceFileAnalyzerConfigOptions = Arguments.SourceFiles.SelectAsArray(f => analyzerConfigSet.GetOptionsForSourcePath(f.Path));

                foreach (var sourceFileAnalyzerConfigOption in sourceFileAnalyzerConfigOptions)
                {
                    diagnostics.AddRange(sourceFileAnalyzerConfigOption.Diagnostics);
                }
            }

            Compilation? compilation = CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger, sourceFileAnalyzerConfigOptions, globalConfigOptions);
            if (compilation == null)
            {
                return Failed;
            }

            var diagnosticInfos = new List<DiagnosticInfo>();
            ResolveAnalyzersFromArguments(diagnosticInfos, MessageProvider, compilation.Options, Arguments.SkipAnalyzers, out var analyzers, out var generators);
            var additionalTextFiles = ResolveAdditionalFilesFromArguments(diagnosticInfos, MessageProvider, touchedFilesLogger);
            if (ReportDiagnostics(diagnosticInfos, consoleOutput, errorLogger, compilation))
            {
                return Failed;
            }

            ImmutableArray<EmbeddedText?> embeddedTexts = AcquireEmbeddedTexts(compilation, diagnostics);
            if (ReportDiagnostics(diagnostics, consoleOutput, errorLogger, compilation))
            {
                return Failed;
            }

            var additionalTexts = ImmutableArray<AdditionalText>.CastUp(additionalTextFiles);

            CompileAndEmit(
                touchedFilesLogger,
                ref compilation,
                analyzers,
                generators,
                additionalTexts,
                analyzerConfigSet,
                sourceFileAnalyzerConfigOptions,
                embeddedTexts,
                diagnostics,
                errorLogger,
                cancellationToken,
                out CancellationTokenSource? analyzerCts,
                out var analyzerDriver,
                out var driverTimingInfo);

            // At this point analyzers are already complete in which case this is a no-op.  Or they are
            // still running because the compilation failed before all of the compilation events were
            // raised.  In the latter case the driver, and all its associated state, will be waiting around
            // for events that are never coming.  Cancel now and let the clean up process begin.
            if (analyzerCts != null)
            {
                analyzerCts.Cancel();
            }

            var exitCode = ReportDiagnostics(diagnostics, consoleOutput, errorLogger, compilation)
                ? Failed
                : Succeeded;

            // The act of reporting errors can cause more errors to appear in
            // additional files due to forcing all additional files to fetch text
            foreach (var additionalFile in additionalTextFiles)
            {
                if (ReportDiagnostics(additionalFile.Diagnostics, consoleOutput, errorLogger, compilation))
                {
                    exitCode = Failed;
                }
            }

            if (Arguments.ReportAnalyzer)
            {
                ReportAnalyzerUtil.Report(consoleOutput, analyzerDriver, driverTimingInfo, Culture, compilation.Options.ConcurrentBuild);
            }

            if (Arguments.ReportInternalsVisibleToAttributes)
            {
                ReportIVTInfos(consoleOutput, errorLogger, compilation, diagnostics.ToReadOnly());
            }

            diagnostics.Free();

            return exitCode;
        }

        private static CompilerAnalyzerConfigOptionsProvider UpdateAnalyzerConfigOptionsProvider(
            CompilerAnalyzerConfigOptionsProvider existing,
            IEnumerable<SyntaxTree> syntaxTrees,
            ImmutableArray<AnalyzerConfigOptionsResult> sourceFileAnalyzerConfigOptions,
            ImmutableArray<AdditionalText> additionalFiles = default,
            ImmutableArray<AnalyzerConfigOptionsResult> additionalFileOptions = default)
        {
            var builder = ImmutableDictionary.CreateBuilder<object, AnalyzerConfigOptions>();
            int i = 0;
            foreach (var syntaxTree in syntaxTrees)
            {

                var options = sourceFileAnalyzerConfigOptions[i].AnalyzerOptions;

                // Optimization: don't create a bunch of entries pointing to a no-op
                if (options.Count > 0)
                {
                    Debug.Assert(existing.GetOptions(syntaxTree) == DictionaryAnalyzerConfigOptions.Empty);
                    builder.Add(syntaxTree, new DictionaryAnalyzerConfigOptions(options));
                }
                i++;
            }

            if (!additionalFiles.IsDefault)
            {
                for (i = 0; i < additionalFiles.Length; i++)
                {
                    var options = additionalFileOptions[i].AnalyzerOptions;

                    // Optimization: don't create a bunch of entries pointing to a no-op
                    if (options.Count > 0)
                    {
                        Debug.Assert(existing.GetOptions(additionalFiles[i]) == DictionaryAnalyzerConfigOptions.Empty);
                        builder.Add(additionalFiles[i], new DictionaryAnalyzerConfigOptions(options));
                    }
                }
            }

            return existing.WithAdditionalTreeOptions(builder.ToImmutable());
        }

        private CompilerAnalyzerConfigOptionsProvider GetCompilerAnalyzerConfigOptionsProvider(
            AnalyzerConfigSet? analyzerConfigSet,
            ImmutableArray<AdditionalText> additionalTextFiles,
            DiagnosticBag diagnostics,
            Compilation compilation,
            ImmutableArray<AnalyzerConfigOptionsResult> sourceFileAnalyzerConfigOptions)
        {
            var analyzerConfigProvider = CompilerAnalyzerConfigOptionsProvider.Empty;
            if (Arguments.AnalyzerConfigPaths.Length > 0)
            {
                Debug.Assert(analyzerConfigSet is object);
                analyzerConfigProvider = analyzerConfigProvider.WithGlobalOptions(new DictionaryAnalyzerConfigOptions(analyzerConfigSet.GetOptionsForSourcePath(string.Empty).AnalyzerOptions));

                // https://github.com/dotnet/roslyn/issues/31916: The compiler currently doesn't support
                // configuring diagnostic reporting on additional text files individually.
                ImmutableArray<AnalyzerConfigOptionsResult> additionalFileAnalyzerOptions =
                    additionalTextFiles.SelectAsArray(f => analyzerConfigSet.GetOptionsForSourcePath(f.Path));

                foreach (var result in additionalFileAnalyzerOptions)
                {
                    diagnostics.AddRange(result.Diagnostics);
                }

                analyzerConfigProvider = UpdateAnalyzerConfigOptionsProvider(
                    analyzerConfigProvider,
                    compilation.SyntaxTrees,
                    sourceFileAnalyzerConfigOptions,
                    additionalTextFiles,
                    additionalFileAnalyzerOptions);
            }

            return analyzerConfigProvider;
        }
        /// <summary>
        /// Perform all the work associated with actual compilation
        /// (parsing, binding, compile, emit), resulting in diagnostics
        /// and analyzer output.
        /// </summary>
        private void CompileAndEmit(
            TouchedFileLogger? touchedFilesLogger,
            ref Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            ImmutableArray<AdditionalText> additionalTextFiles,
            AnalyzerConfigSet? analyzerConfigSet,
            ImmutableArray<AnalyzerConfigOptionsResult> sourceFileAnalyzerConfigOptions,
            ImmutableArray<EmbeddedText?> embeddedTexts,
            DiagnosticBag diagnostics,
            ErrorLogger? errorLogger,
            CancellationToken cancellationToken,
            out CancellationTokenSource? analyzerCts,
            out AnalyzerDriver? analyzerDriver,
            out GeneratorDriverTimingInfo? generatorTimingInfo)
        {
            analyzerCts = null;
            analyzerDriver = null;
            generatorTimingInfo = null;

            // Print the diagnostics produced during the parsing stage and exit if there are any unsuppressible errors.
            compilation.GetDiagnostics(CompilationStage.Parse, includeEarlierStages: false, diagnostics, cancellationToken);

            DiagnosticBag? analyzerExceptionDiagnostics = null;

            // If there are parsing errors, we want to return immediately.
            // But first, we need to check two things: 
            // 1. Whether there are any suppressible warnings,
            // 2. Whether there are any diagnostic suppressors that could potentially suppress them.
            // If both conditions are true, run diagnostic suppressors before exiting from this method.
            if (HasUnsuppressableErrors(diagnostics))
            {
                if (HasSuppressableWarningsOrErrors(diagnostics) && analyzers.Any(a => a is DiagnosticSuppressor))
                {
                    var analyzerConfigProvider = GetCompilerAnalyzerConfigOptionsProvider(analyzerConfigSet, additionalTextFiles, diagnostics, compilation, sourceFileAnalyzerConfigOptions);

                    AnalyzerOptions analyzerOptions = CreateAnalyzerOptions(additionalTextFiles, analyzerConfigProvider);

                    (analyzerCts, analyzerExceptionDiagnostics, analyzerDriver) = initializeAnalyzerDriver(analyzerOptions, ref compilation);

                    analyzerDriver.ApplyProgrammaticSuppressions(diagnostics, compilation, analyzerCts.Token);
                }
                return;
            }

            if (!analyzers.IsEmpty || !generators.IsEmpty)
            {
                var analyzerConfigProvider =
                    GetCompilerAnalyzerConfigOptionsProvider(analyzerConfigSet, additionalTextFiles, diagnostics, compilation, sourceFileAnalyzerConfigOptions);

                if (!generators.IsEmpty)
                {
                    // At this point we have a compilation with nothing yet computed.
                    // We pass it to the generators, which will realize any symbols they require.
                    (compilation, generatorTimingInfo) = RunGenerators(compilation, Arguments.ParseOptions, generators, analyzerConfigProvider, additionalTextFiles, diagnostics);

                    bool hasAnalyzerConfigs = !Arguments.AnalyzerConfigPaths.IsEmpty;
                    bool hasGeneratedOutputPath = !string.IsNullOrWhiteSpace(Arguments.GeneratedFilesOutputDirectory);
                    var generatedSyntaxTrees = compilation.SyntaxTrees.Skip(Arguments.SourceFiles.Length).ToList();
                    var analyzerOptionsBuilder = hasAnalyzerConfigs ? ArrayBuilder<AnalyzerConfigOptionsResult>.GetInstance(generatedSyntaxTrees.Count) : null;
                    var embeddedTextBuilder = ArrayBuilder<EmbeddedText>.GetInstance(generatedSyntaxTrees.Count);
                    try
                    {
                        foreach (var tree in generatedSyntaxTrees)
                        {
                            Debug.Assert(!string.IsNullOrWhiteSpace(tree.FilePath));
                            cancellationToken.ThrowIfCancellationRequested();

                            var sourceText = tree.GetText(cancellationToken);

                            // embed the generated text and get analyzer options for it if needed
                            embeddedTextBuilder.Add(EmbeddedText.FromSource(tree.FilePath, sourceText));
                            if (analyzerOptionsBuilder is object)
                            {
                                analyzerOptionsBuilder.Add(analyzerConfigSet!.GetOptionsForSourcePath(tree.FilePath));
                            }

                            // write out the file if we have an output path
                            if (hasGeneratedOutputPath)
                            {
                                var path = Path.Combine(Arguments.GeneratedFilesOutputDirectory!, tree.FilePath);
                                if (Directory.Exists(Arguments.GeneratedFilesOutputDirectory))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                                }

                                var fileStream = OpenFile(path, diagnostics, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                                if (fileStream is object)
                                {
                                    Debug.Assert(tree.Encoding is object);

                                    using var disposer = new NoThrowStreamDisposer(fileStream, path, diagnostics, MessageProvider);
                                    using var writer = new StreamWriter(fileStream, tree.Encoding);

                                    sourceText.Write(writer, cancellationToken);
                                    touchedFilesLogger?.AddWritten(path);
                                }
                            }
                        }

                        embeddedTexts = embeddedTexts.AddRange(embeddedTextBuilder);
                        if (analyzerOptionsBuilder is object)
                        {
                            analyzerConfigProvider = UpdateAnalyzerConfigOptionsProvider(
                               analyzerConfigProvider,
                               generatedSyntaxTrees,
                               analyzerOptionsBuilder.ToImmutable());
                        }
                    }
                    finally
                    {
                        analyzerOptionsBuilder?.Free();
                        embeddedTextBuilder.Free();
                    }
                }

                AnalyzerOptions analyzerOptions = CreateAnalyzerOptions(
                      additionalTextFiles, analyzerConfigProvider);

                if (!analyzers.IsEmpty)
                {
                    (analyzerCts, analyzerExceptionDiagnostics, analyzerDriver) = initializeAnalyzerDriver(analyzerOptions, ref compilation);
                }
            }

            compilation.GetDiagnostics(CompilationStage.Declare, includeEarlierStages: false, diagnostics, cancellationToken);

            // If there are unsuppressable declaration errors, we want to exit early from this method.
            // But before we do so, we need to run diagnostic suppressors (if any) on all suppressable warnings/errors (if any).
            if (HasUnsuppressableErrors(diagnostics))
            {
                if (analyzerDriver == null || !analyzerDriver.HasDiagnosticSuppressors || !HasSuppressableWarningsOrErrors(diagnostics))
                {
                    return;
                }

                analyzerDriver.ApplyProgrammaticSuppressions(diagnostics, compilation, cancellationToken);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Given a compilation and a destination directory, determine three names:
            //   1) The name with which the assembly should be output.
            //   2) The path of the assembly/module file (default = destination directory + compilation output name).
            //   3) The path of the pdb file (default = assembly/module path with ".pdb" extension).
            string outputName = GetOutputFileName(compilation, cancellationToken)!;
            var finalPeFilePath = Arguments.GetOutputFilePath(outputName);
            var finalPdbFilePath = Arguments.GetPdbFilePath(outputName);
            var finalXmlFilePath = Arguments.DocumentationPath;

            NoThrowStreamDisposer? sourceLinkStreamDisposerOpt = null;

            try
            {
                // NOTE: Unlike the PDB path, the XML doc path is not embedded in the assembly, so we don't need to pass it to emit.
                var emitOptions = Arguments.EmitOptions.
                    WithOutputNameOverride(outputName).
                    WithPdbFilePath(PathUtilities.NormalizePathPrefix(finalPdbFilePath, Arguments.PathMap));

                // TODO(https://github.com/dotnet/roslyn/issues/19592):
                // This feature flag is being maintained until our next major release to avoid unnecessary
                // compat breaks with customers.
                if (Arguments.ParseOptions.Features.ContainsKey("pdb-path-determinism") && !string.IsNullOrEmpty(emitOptions.PdbFilePath))
                {
                    emitOptions = emitOptions.WithPdbFilePath(Path.GetFileName(emitOptions.PdbFilePath));
                }

                if (Arguments.ParseOptions.Features.ContainsKey("debug-determinism"))
                {
                    EmitDeterminismKey(compilation, FileSystem, additionalTextFiles, analyzers, generators, Arguments.PathMap, emitOptions);
                }

                if (Arguments.SourceLink != null)
                {
                    var sourceLinkStreamOpt = OpenFile(
                        Arguments.SourceLink,
                        diagnostics,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read);

                    if (sourceLinkStreamOpt != null)
                    {
                        sourceLinkStreamDisposerOpt = new NoThrowStreamDisposer(
                            sourceLinkStreamOpt,
                            Arguments.SourceLink,
                            diagnostics,
                            MessageProvider);
                    }
                }

                // Need to ensure the PDB file path validation is done on the original path as that is the
                // file we will write out to disk, there is no guarantee that the file paths emitted into
                // the PE / PDB are valid file paths because pathmap can be used to create deliberately
                // illegal names
                if (!PathUtilities.IsValidFilePath(finalPdbFilePath))
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(MessageProvider.FTL_InvalidInputFileName, Location.None, finalPdbFilePath));
                }

                var moduleBeingBuilt = compilation.CheckOptionsAndCreateModuleBuilder(
                    diagnostics,
                    Arguments.ManifestResources,
                    emitOptions,
                    debugEntryPoint: null,
                    sourceLinkStream: sourceLinkStreamDisposerOpt?.Stream,
                    embeddedTexts: embeddedTexts,
                    testData: null,
                    cancellationToken: cancellationToken);

                if (moduleBeingBuilt != null)
                {
                    bool success;

                    try
                    {
                        success = compilation.CompileMethods(
                            moduleBeingBuilt,
                            Arguments.EmitPdb,
                            diagnostics,
                            filterOpt: null,
                            cancellationToken: cancellationToken);

                        // Prior to generating the xml documentation file,
                        // we apply programmatic suppressions for compiler warnings from diagnostic suppressors.
                        // If there are still any unsuppressed errors or warnings escalated to errors
                        // then we bail out from generating the documentation file.
                        // This maintains the compiler invariant that xml documentation file should not be
                        // generated in presence of diagnostics that break the build.
                        if (analyzerDriver != null && !diagnostics.IsEmptyWithoutResolution)
                        {
                            analyzerDriver.ApplyProgrammaticSuppressions(diagnostics, compilation, cancellationToken);
                        }

                        if (HasUnsuppressedErrors(diagnostics))
                        {
                            success = false;
                        }

                        if (success)
                        {
                            // NOTE: as native compiler does, we generate the documentation file
                            // NOTE: 'in place', replacing the contents of the file if it exists
                            NoThrowStreamDisposer? xmlStreamDisposerOpt = null;

                            if (finalXmlFilePath != null)
                            {
                                var xmlStreamOpt = OpenFile(finalXmlFilePath,
                                                            diagnostics,
                                                            FileMode.OpenOrCreate,
                                                            FileAccess.Write,
                                                            FileShare.ReadWrite | FileShare.Delete);

                                if (xmlStreamOpt == null)
                                {
                                    return;
                                }

                                try
                                {
                                    xmlStreamOpt.SetLength(0);
                                }
                                catch (Exception e)
                                {
                                    MessageProvider.ReportStreamWriteException(e, finalXmlFilePath, diagnostics);
                                    return;
                                }
                                xmlStreamDisposerOpt = new NoThrowStreamDisposer(
                                    xmlStreamOpt,
                                    finalXmlFilePath,
                                    diagnostics,
                                    MessageProvider);
                            }

                            using (xmlStreamDisposerOpt)
                            {
                                using (var win32ResourceStreamOpt = GetWin32Resources(FileSystem, MessageProvider, Arguments, compilation, diagnostics))
                                {
                                    if (HasUnsuppressableErrors(diagnostics))
                                    {
                                        return;
                                    }

                                    success =
                                        compilation.GenerateResources(moduleBeingBuilt, win32ResourceStreamOpt, useRawWin32Resources: false, diagnostics, cancellationToken) &&
                                        compilation.GenerateDocumentationComments(xmlStreamDisposerOpt?.Stream, emitOptions.OutputNameOverride, diagnostics, cancellationToken);
                                }
                            }

                            if (xmlStreamDisposerOpt?.HasFailedToDispose == true)
                            {
                                return;
                            }

                            // only report unused usings if we have success.
                            if (success)
                            {
                                compilation.ReportUnusedImports(diagnostics, cancellationToken);
                            }
                        }

                        compilation.CompleteTrees(null);

                        if (analyzerDriver != null)
                        {
                            // GetDiagnosticsAsync is called after ReportUnusedImports
                            // since that method calls EventQueue.TryComplete. Without
                            // TryComplete, we may miss diagnostics.
                            var hostDiagnostics = analyzerDriver.GetDiagnosticsAsync(compilation, cancellationToken).Result;
                            diagnostics.AddRange(hostDiagnostics);

                            if (!diagnostics.IsEmptyWithoutResolution)
                            {
                                // Apply diagnostic suppressions for analyzer and/or compiler diagnostics from diagnostic suppressors.
                                analyzerDriver.ApplyProgrammaticSuppressions(diagnostics, compilation, cancellationToken);
                            }

                            if (errorLogger != null)
                            {
                                var descriptorsWithInfo = analyzerDriver.GetAllDiagnosticDescriptorsWithInfo(cancellationToken, out var totalAnalyzerExecutionTime);
                                AddAnalyzerDescriptorsAndExecutionTime(errorLogger, descriptorsWithInfo, totalAnalyzerExecutionTime);
                            }
                        }
                    }
                    finally
                    {
                        moduleBeingBuilt.CompilationFinished();
                    }

                    if (HasUnsuppressedErrors(diagnostics))
                    {
                        success = false;
                    }

                    if (success)
                    {
                        var peStreamProvider = new CompilerEmitStreamProvider(this, finalPeFilePath);
                        var pdbStreamProviderOpt = Arguments.EmitPdbFile ? new CompilerEmitStreamProvider(this, finalPdbFilePath) : null;

                        string? finalRefPeFilePath = Arguments.OutputRefFilePath;
                        var refPeStreamProviderOpt = finalRefPeFilePath != null ? new CompilerEmitStreamProvider(this, finalRefPeFilePath) : null;

                        RSAParameters? privateKeyOpt = null;
                        if (compilation.Options.StrongNameProvider != null && compilation.SignUsingBuilder && !compilation.Options.PublicSign)
                        {
                            privateKeyOpt = compilation.StrongNameKeys.PrivateKey;
                        }

                        // If we serialize to a PE stream we need to record the fallback encoding if it was used
                        // so the compilation can be recreated.
                        emitOptions = emitOptions.WithFallbackSourceFileEncoding(GetFallbackEncoding());

                        success = compilation.SerializeToPeStream(
                            moduleBeingBuilt,
                            peStreamProvider,
                            refPeStreamProviderOpt,
                            pdbStreamProviderOpt,
                            rebuildData: null,
                            testSymWriterFactory: null,
                            diagnostics: diagnostics,
                            emitOptions: emitOptions,
                            privateKeyOpt: privateKeyOpt,
                            cancellationToken: cancellationToken);

                        peStreamProvider.Close(diagnostics);
                        refPeStreamProviderOpt?.Close(diagnostics);
                        pdbStreamProviderOpt?.Close(diagnostics);

                        if (success && touchedFilesLogger != null)
                        {
                            if (pdbStreamProviderOpt != null)
                            {
                                touchedFilesLogger.AddWritten(finalPdbFilePath);
                            }
                            if (refPeStreamProviderOpt != null)
                            {
                                touchedFilesLogger.AddWritten(finalRefPeFilePath!);
                            }
                            touchedFilesLogger.AddWritten(finalPeFilePath);
                        }
                    }
                }

                if (HasUnsuppressableErrors(diagnostics))
                {
                    return;
                }
            }
            finally
            {
                sourceLinkStreamDisposerOpt?.Dispose();
            }

            if (sourceLinkStreamDisposerOpt?.HasFailedToDispose == true)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (analyzerExceptionDiagnostics != null)
            {
                diagnostics.AddRange(analyzerExceptionDiagnostics);
                if (HasUnsuppressableErrors(analyzerExceptionDiagnostics))
                {
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!WriteTouchedFiles(diagnostics, touchedFilesLogger, finalXmlFilePath))
            {
                return;
            }

            (CancellationTokenSource, DiagnosticBag, AnalyzerDriver) initializeAnalyzerDriver(AnalyzerOptions analyzerOptions, ref Compilation compilation)
            {
                var analyzerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var analyzerExceptionDiagnostics = new DiagnosticBag();

                // PERF: Avoid executing analyzers that report only Hidden and/or Info diagnostics, which don't appear in the build output.
                //  1. Always filter out 'Hidden' analyzer diagnostics in build.
                //  2. Filter out 'Info' analyzer diagnostics if they are not required to be logged in errorlog.
                var severityFilter = SeverityFilter.Hidden;
                if (Arguments.ErrorLogPath == null)
                    severityFilter |= SeverityFilter.Info;

                var analyzerDriver = AnalyzerDriver.CreateAndAttachToCompilation(
                    compilation,
                    analyzers,
                    analyzerOptions,
                    new AnalyzerManager(analyzers),
                    analyzerExceptionDiagnostics.Add,
                    reportAnalyzer: Arguments.ReportAnalyzer || errorLogger != null,
                    severityFilter,
                    trackSuppressedDiagnosticIds: errorLogger != null,
                    out compilation,
                    analyzerCts.Token);

                return (analyzerCts, analyzerExceptionDiagnostics, analyzerDriver);
            }
        }

        // virtual for testing
        protected virtual Diagnostics.AnalyzerOptions CreateAnalyzerOptions(
            ImmutableArray<AdditionalText> additionalTextFiles,
            AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
            => new Diagnostics.AnalyzerOptions(additionalTextFiles, analyzerConfigOptionsProvider);
        protected virtual void AddAnalyzerDescriptorsAndExecutionTime(ErrorLogger errorLogger, ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> descriptorsWithInfo, double totalAnalyzerExecutionTime)
            => errorLogger.AddAnalyzerDescriptorsAndExecutionTime(descriptorsWithInfo, totalAnalyzerExecutionTime);

        private bool WriteTouchedFiles(DiagnosticBag diagnostics, TouchedFileLogger? touchedFilesLogger, string? finalXmlFilePath)
        {
            if (Arguments.TouchedFilesPath != null)
            {
                Debug.Assert(touchedFilesLogger != null);

                if (finalXmlFilePath != null)
                {
                    touchedFilesLogger.AddWritten(finalXmlFilePath);
                }

                string readFilesPath = Arguments.TouchedFilesPath + ".read";
                string writtenFilesPath = Arguments.TouchedFilesPath + ".write";

                var readStream = OpenFile(readFilesPath, diagnostics, mode: FileMode.OpenOrCreate);
                var writtenStream = OpenFile(writtenFilesPath, diagnostics, mode: FileMode.OpenOrCreate);

                if (readStream == null || writtenStream == null)
                {
                    return false;
                }

                string? filePath = null;
                try
                {
                    filePath = readFilesPath;
                    using (var writer = new StreamWriter(readStream))
                    {
                        touchedFilesLogger.WriteReadPaths(writer);
                    }

                    filePath = writtenFilesPath;
                    using (var writer = new StreamWriter(writtenStream))
                    {
                        touchedFilesLogger.WriteWrittenPaths(writer);
                    }
                }
                catch (Exception e)
                {
                    Debug.Assert(filePath != null);
                    MessageProvider.ReportStreamWriteException(e, filePath, diagnostics);
                    return false;
                }
            }

            return true;
        }

        protected virtual ImmutableArray<AdditionalTextFile> ResolveAdditionalFilesFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger? touchedFilesLogger)
        {
            var builder = ArrayBuilder<AdditionalTextFile>.GetInstance();
            var filePaths = new HashSet<string>(PathUtilities.Comparer);

            foreach (var file in Arguments.AdditionalFiles)
            {
                Debug.Assert(PathUtilities.IsAbsolute(file.Path));
                if (filePaths.Add(PathUtilities.ExpandAbsolutePathWithRelativeParts(file.Path)))
                {
                    builder.Add(new AdditionalTextFile(file, this));
                }
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns the name with which the assembly should be output
        /// </summary>
        protected abstract string GetOutputFileName(Compilation compilation, CancellationToken cancellationToken);

        private Stream? OpenFile(
            string filePath,
            DiagnosticBag diagnostics,
            FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.ReadWrite,
            FileShare share = FileShare.None)
        {
            try
            {
                return FileSystem.OpenFile(filePath, mode, access, share);
            }
            catch (Exception e)
            {
                MessageProvider.ReportStreamWriteException(e, filePath, diagnostics);
                return null;
            }
        }

        // internal for testing
        internal static Stream? GetWin32ResourcesInternal(
            ICommonCompilerFileSystem fileSystem,
            CommonMessageProvider messageProvider,
            CommandLineArguments arguments,
            Compilation compilation,
            out IEnumerable<DiagnosticInfo> errors)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var stream = GetWin32Resources(fileSystem, messageProvider, arguments, compilation, diagnostics);
            errors = diagnostics.ToReadOnlyAndFree().SelectAsArray(diag => new DiagnosticInfo(messageProvider, diag.IsWarningAsError, diag.Code, (object[])diag.Arguments));
            return stream;
        }

        private static Stream? GetWin32Resources(
            ICommonCompilerFileSystem fileSystem,
            CommonMessageProvider messageProvider,
            CommandLineArguments arguments,
            Compilation compilation,
            DiagnosticBag diagnostics)
        {
            if (arguments.Win32ResourceFile != null)
            {
                return OpenStream(fileSystem, messageProvider, arguments.Win32ResourceFile, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Resource, diagnostics);
            }

            using (Stream? manifestStream = OpenManifestStream(fileSystem, messageProvider, compilation.Options.OutputKind, arguments, diagnostics))
            {
                using (Stream? iconStream = OpenStream(fileSystem, messageProvider, arguments.Win32Icon, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Icon, diagnostics))
                {
                    try
                    {
                        return compilation.CreateDefaultWin32Resources(true, arguments.NoWin32Manifest, manifestStream, iconStream);
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_ErrorBuildingWin32Resource, Location.None, ex.Message));
                    }
                }
            }

            return null;
        }

        private static Stream? OpenManifestStream(ICommonCompilerFileSystem fileSystem, CommonMessageProvider messageProvider, OutputKind outputKind, CommandLineArguments arguments, DiagnosticBag diagnostics)
        {
            return outputKind.IsNetModule()
                ? null
                : OpenStream(fileSystem, messageProvider, arguments.Win32Manifest, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Manifest, diagnostics);
        }

        private static Stream? OpenStream(ICommonCompilerFileSystem fileSystem, CommonMessageProvider messageProvider, string? path, string? baseDirectory, int errorCode, DiagnosticBag diagnostics)
        {
            if (path == null)
            {
                return null;
            }

            string? fullPath = ResolveRelativePath(messageProvider, path, baseDirectory, diagnostics);
            if (fullPath == null)
            {
                return null;
            }

            try
            {
                return fileSystem.OpenFile(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(errorCode, Location.None, fullPath, ex.Message));
            }

            return null;
        }

        private static string? ResolveRelativePath(CommonMessageProvider messageProvider, string path, string? baseDirectory, DiagnosticBag diagnostics)
        {
            string? fullPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
            if (fullPath == null)
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.FTL_InvalidInputFileName, Location.None, path ?? ""));
            }

            return fullPath;
        }

        internal static bool TryGetCompilerDiagnosticCode(string diagnosticId, string expectedPrefix, out uint code)
        {
            code = 0;
            return diagnosticId.StartsWith(expectedPrefix, StringComparison.Ordinal) && uint.TryParse(diagnosticId.Substring(expectedPrefix.Length), out code);
        }

        /// <summary>
        ///   When overridden by a derived class, this property can override the current thread's
        ///   CurrentUICulture property for diagnostic message resource lookups.
        /// </summary>
        protected virtual CultureInfo Culture
        {
            get
            {
                return Arguments.PreferredUILang ?? CultureInfo.CurrentUICulture;
            }
        }

        private void EmitDeterminismKey(
            Compilation compilation,
            ICommonCompilerFileSystem fileSystem,
            ImmutableArray<AdditionalText> additionalTexts,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            ImmutableArray<KeyValuePair<string, string>> pathMap,
            EmitOptions? emitOptions)
        {
            var key = compilation.GetDeterministicKey(additionalTexts, analyzers, generators, pathMap, emitOptions);
            var filePath = Path.Combine(Arguments.OutputDirectory, Arguments.OutputFileName + ".key");
            using var stream = fileSystem.OpenFile(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            var bytes = Encoding.UTF8.GetBytes(key);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
