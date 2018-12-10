// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Security.Cryptography;

namespace Microsoft.CodeAnalysis
{
    internal struct BuildPaths
    {
        /// <summary>
        /// The path which contains the compiler binaries and response files.
        /// </summary>
        internal string ClientDirectory { get; }

        /// <summary>
        /// The path in which the compilation takes place.
        /// </summary>
        internal string WorkingDirectory { get; }

        /// <summary>
        /// The path which contains mscorlib.  This can be null when specified by the user or running in a 
        /// CoreClr environment.
        /// </summary>
        internal string SdkDirectory { get; }

        /// <summary>
        /// The temporary directory a compilation should use instead of <see cref="Path.GetTempPath"/>.  The latter
        /// relies on global state individual compilations should ignore.
        /// </summary>
        internal string TempDirectory { get; }

        internal BuildPaths(string clientDir, string workingDir, string sdkDir, string tempDir)
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

        private readonly string _clientDirectory;

        public CommonMessageProvider MessageProvider { get; }
        public CommandLineArguments Arguments { get; }
        public IAnalyzerAssemblyLoader AssemblyLoader { get; private set; }
        public abstract DiagnosticFormatter DiagnosticFormatter { get; }

        /// <summary>
        /// The set of source file paths that are in the set of embedded paths.
        /// This is used to prevent reading source files that are embedded twice.
        /// </summary>
        public IReadOnlySet<string> EmbeddedSourcePaths { get; }

        private readonly HashSet<Diagnostic> _reportedDiagnostics = new HashSet<Diagnostic>();

        public abstract Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLoggerOpt);
        public abstract void PrintLogo(TextWriter consoleOutput);
        public abstract void PrintHelp(TextWriter consoleOutput);
        public abstract void PrintLangVersions(TextWriter consoleOutput);

        /// <summary>
        /// Print compiler version
        /// </summary>
        /// <param name="consoleOutput"></param>
        public virtual void PrintVersion(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(GetAssemblyFileVersion());
        }

        protected abstract bool TryGetCompilerDiagnosticCode(string diagnosticId, out uint code);
        protected abstract ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider);

        public CommonCompiler(CommandLineParser parser, string responseFile, string[] args, BuildPaths buildPaths, string additionalReferenceDirectories, IAnalyzerAssemblyLoader assemblyLoader)
        {
            IEnumerable<string> allArgs = args;
            _clientDirectory = buildPaths.ClientDirectory;

            Debug.Assert(null == responseFile || PathUtilities.IsAbsolute(responseFile));
            if (!SuppressDefaultResponseFile(args) && File.Exists(responseFile))
            {
                allArgs = new[] { "@" + responseFile }.Concat(allArgs);
            }

            this.Arguments = parser.Parse(allArgs, buildPaths.WorkingDirectory, buildPaths.SdkDirectory, additionalReferenceDirectories);
            this.MessageProvider = parser.MessageProvider;
            this.AssemblyLoader = assemblyLoader;
            this.EmbeddedSourcePaths = GetEmbeddedSourcePaths(Arguments);

            if (Arguments.ParseOptions.Features.ContainsKey("debug-determinism"))
            {
                EmitDeterminismKey(Arguments, args, buildPaths.WorkingDirectory, parser);
            }
        }

        internal abstract bool SuppressDefaultResponseFile(IEnumerable<string> args);

        /// <summary>
        /// The type of the compiler class for version information in /help and /version.
        /// We don't simply use this.GetType() because that would break mock subclasses.
        /// </summary>
        internal abstract Type Type { get; }

        /// <summary>
        /// The assembly file version of this compiler, used in logo and /version output.
        /// </summary>
        internal virtual string GetAssemblyFileVersion()
        {
            Assembly assembly = Type.GetTypeInfo().Assembly;
            return GetAssemblyFileVersion(assembly);
        }

        internal static string GetAssemblyFileVersion(Assembly assembly)
        {
            string assemblyVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            string hash = ExtractShortCommitHash(assembly.GetCustomAttribute<CommitHashAttribute>()?.Hash);
            return $"{assemblyVersion} ({hash})";
        }

        internal static string ExtractShortCommitHash(string hash)
        {
            // leave "<developer build>" alone, but truncate SHA to 8 characters
            if (hash != null && hash.Length >= 8 && hash[0] != '<')
            {
                return hash.Substring(0, 8);
            }

            return hash;
        }

        /// <summary>
        /// Tool name used, along with assembly version, for error logging.
        /// </summary>
        internal abstract string GetToolName();

        /// <summary>
        /// Tool version identifier used for error logging.
        /// </summary>
        internal Version GetAssemblyVersion()
        {
            return Type.GetTypeInfo().Assembly.GetName().Version;
        }

        internal string GetCultureName()
        {
            return Culture.Name;
        }

        internal virtual Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
        {
            return (path, properties) => MetadataReference.CreateFromFile(path, properties);
        }

        internal virtual MetadataReferenceResolver GetCommandLineMetadataReferenceResolver(TouchedFileLogger loggerOpt)
        {
            var pathResolver = new RelativePathResolver(Arguments.ReferencePaths, Arguments.BaseDirectory);
            return new LoggingMetadataFileReferenceResolver(pathResolver, GetMetadataProvider(), loggerOpt);
        }

        /// <summary>
        /// Resolves metadata references stored in command line arguments and reports errors for those that can't be resolved.
        /// </summary>
        internal List<MetadataReference> ResolveMetadataReferences(
            List<DiagnosticInfo> diagnostics,
            TouchedFileLogger touchedFiles,
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
        internal SourceText TryReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics)
        {
            string discarded;
            return TryReadFileContent(file, diagnostics, out discarded);
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <param name="normalizedFilePath">If given <paramref name="file"/> opens successfully, set to normalized absolute path of the file, null otherwise.</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText TryReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics, out string normalizedFilePath)
        {
            var filePath = file.Path;
            try
            {
                using (var data = OpenFileForReadWithSmallBufferOptimization(filePath))
                {
                    normalizedFilePath = data.Name;
                    return EncodedStringText.Create(data, Arguments.Encoding, Arguments.ChecksumAlgorithm, canBeEmbedded: EmbeddedSourcePaths.Contains(file.Path));
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(ToFileReadDiagnostics(this.MessageProvider, e, filePath));
                normalizedFilePath = null;
                return null;
            }
        }

        private static FileStream OpenFileForReadWithSmallBufferOptimization(string filePath)
        {
            // PERF: Using a very small buffer size for the FileStream opens up an optimization within EncodedStringText/EmbeddedText where
            // we read the entire FileStream into a byte array in one shot. For files that are actually smaller than the buffer
            // size, FileStream.Read still allocates the internal buffer.
            return new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 1,
                options: FileOptions.None);
        }

        internal EmbeddedText TryReadEmbeddedFileContent(string filePath, DiagnosticBag diagnostics)
        {
            try
            {
                using (var stream = OpenFileForReadWithSmallBufferOptimization(filePath))
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

        private ImmutableArray<EmbeddedText> AcquireEmbeddedTexts(Compilation compilation, DiagnosticBag diagnostics)
        {
            if (Arguments.EmbeddedFiles.IsEmpty)
            {
                return ImmutableArray<EmbeddedText>.Empty;
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

            var embeddedTextBuilder = ImmutableArray.CreateBuilder<EmbeddedText>(embeddedFileOrderedSet.Count);
            foreach (var path in embeddedFileOrderedSet)
            {
                SyntaxTree tree;
                EmbeddedText text;

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

            if (e is FileNotFoundException || e.GetType().Name == "DirectoryNotFoundException")
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

        internal bool ReportErrors(IEnumerable<Diagnostic> diagnostics, TextWriter consoleOutput, ErrorLogger errorLoggerOpt)
        {
            bool hasErrors = false;
            foreach (var diag in diagnostics)
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
                    continue;
                }
                else if (diag.Severity == DiagnosticSeverity.Hidden)
                {
                    // Not reported from the command-line compiler.
                    continue;
                }

                // We want to report diagnostics with source suppression in the error log file.
                // However, these diagnostics should not be reported on the console output.
                errorLoggerOpt?.LogDiagnostic(diag);
                if (diag.IsSuppressed)
                {
                    continue;
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

            return hasErrors;
        }

        private bool ReportErrors(DiagnosticBag diagnostics, TextWriter consoleOutput, ErrorLogger errorLoggerOpt)
            => ReportErrors(diagnostics.ToReadOnly(), consoleOutput, errorLoggerOpt);

        /// <summary>
        /// Returns true if the diagnostic is an error that should be reported.
        /// </summary>
        private static bool IsReportedError(Diagnostic diagnostic)
        {
            return (diagnostic.Severity == DiagnosticSeverity.Error) && !diagnostic.IsSuppressed;
        }

        public bool ReportErrors(IEnumerable<DiagnosticInfo> diagnostics, TextWriter consoleOutput, ErrorLogger errorLoggerOpt) =>
            ReportErrors(diagnostics.Select(info => Diagnostic.Create(info)), consoleOutput, errorLoggerOpt);

        protected virtual void PrintError(Diagnostic diagnostic, TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(DiagnosticFormatter.Format(diagnostic, Culture));
        }

        public StreamErrorLogger GetErrorLogger(TextWriter consoleOutput, CancellationToken cancellationToken)
        {
            Debug.Assert(Arguments.ErrorLogPath != null);

            var diagnostics = DiagnosticBag.GetInstance();
            var errorLog = OpenFile(Arguments.ErrorLogPath,
                                    diagnostics,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.ReadWrite | FileShare.Delete);

            StreamErrorLogger logger;
            if (errorLog == null)
            {
                Debug.Assert(diagnostics.HasAnyErrors());
                logger = null;
            }
            else
            {
                logger = new StreamErrorLogger(errorLog, GetToolName(), GetAssemblyFileVersion(), GetAssemblyVersion(), Culture);
            }

            ReportErrors(diagnostics.ToReadOnlyAndFree(), consoleOutput, errorLoggerOpt: logger);
            return logger;
        }

        /// <summary>
        /// csc.exe and vbc.exe entry point.
        /// </summary>
        public virtual int Run(TextWriter consoleOutput, CancellationToken cancellationToken = default(CancellationToken))
        {
            var saveUICulture = CultureInfo.CurrentUICulture;
            StreamErrorLogger errorLogger = null;

            try
            {
                // Messages from exceptions can be used as arguments for errors and they are often localized.
                // Ensure they are localized to the right language.
                var culture = this.Culture;
                if (culture != null)
                {
                    CultureInfo.CurrentUICulture = culture;
                }

                if (Arguments.ErrorLogPath != null)
                {
                    errorLogger = GetErrorLogger(consoleOutput, cancellationToken);
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
                    ReportErrors(new[] { diag }, consoleOutput, errorLogger);
                }

                return Failed;
            }
            finally
            {
                CultureInfo.CurrentUICulture = saveUICulture;
                errorLogger?.Dispose();
            }
        }

        private int RunCore(TextWriter consoleOutput, ErrorLogger errorLogger, CancellationToken cancellationToken)
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

            if (ReportErrors(Arguments.Errors, consoleOutput, errorLogger))
            {
                return Failed;
            }

            var touchedFilesLogger = (Arguments.TouchedFilesPath != null) ? new TouchedFileLogger() : null;

            Compilation compilation = CreateCompilation(consoleOutput, touchedFilesLogger, errorLogger);
            if (compilation == null)
            {
                return Failed;
            }

            var diagnosticInfos = new List<DiagnosticInfo>();
            ImmutableArray<DiagnosticAnalyzer> analyzers = ResolveAnalyzersFromArguments(diagnosticInfos, MessageProvider);
            var additionalTextFiles = ResolveAdditionalFilesFromArguments(diagnosticInfos, MessageProvider, touchedFilesLogger);
            if (ReportErrors(diagnosticInfos, consoleOutput, errorLogger))
            {
                return Failed;
            }

            var diagnostics = DiagnosticBag.GetInstance();

            ImmutableArray<EmbeddedText> embeddedTexts = AcquireEmbeddedTexts(compilation, diagnostics);
            if (ReportErrors(diagnostics, consoleOutput, errorLogger))
            {
                return Failed;
            }

            CompileAndEmit(
                touchedFilesLogger,
                ref compilation,
                analyzers,
                additionalTextFiles,
                embeddedTexts,
                diagnostics,
                cancellationToken,
                out CancellationTokenSource analyzerCts,
                out bool reportAnalyzer,
                out var analyzerDriver);

            // At this point analyzers are already complete in which case this is a no-op.  Or they are 
            // still running because the compilation failed before all of the compilation events were 
            // raised.  In the latter case the driver, and all its associated state, will be waiting around 
            // for events that are never coming.  Cancel now and let the clean up process begin.
            if (analyzerCts != null)
            {
                analyzerCts.Cancel();
            }

            var exitCode = ReportErrors(diagnostics, consoleOutput, errorLogger)
                ? Failed
                : Succeeded;

            // The act of reporting errors can cause more errors to appear in
            // additional files due to forcing all additional files to fetch text
            foreach (var additionalFile in additionalTextFiles)
            {
                if (ReportErrors(additionalFile.Diagnostics, consoleOutput, errorLogger))
                {
                    exitCode = Failed;
                }
            }

            diagnostics.Free();
            if (reportAnalyzer)
            {
                ReportAnalyzerExecutionTime(consoleOutput, analyzerDriver, Culture, compilation.Options.ConcurrentBuild);
            }

            return exitCode;
        }

        /// <summary>
        /// Perform all the work associated with actual compilation
        /// (parsing, binding, compile, emit), resulting in diagnostics
        /// and analyzer output.
        /// </summary>
        private void CompileAndEmit(
            TouchedFileLogger touchedFilesLogger,
            ref Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<AdditionalTextFile> additionalTextFiles,
            ImmutableArray<EmbeddedText> embeddedTexts,
            DiagnosticBag diagnostics,
            CancellationToken cancellationToken,
            out CancellationTokenSource analyzerCts,
            out bool reportAnalyzer,
            out AnalyzerDriver analyzerDriver)
        {
            analyzerCts = null;
            reportAnalyzer = false;
            analyzerDriver = null;

            // Print the diagnostics produced during the parsing stage and exit if there were any errors.
            compilation.GetDiagnostics(CompilationStage.Parse, includeEarlierStages: false, diagnostics, cancellationToken);
            if (diagnostics.HasAnyErrors())
            {
                return;
            }

            DiagnosticBag analyzerExceptionDiagnostics = null;

            if (!analyzers.IsEmpty)
            {
                analyzerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                analyzerExceptionDiagnostics = new DiagnosticBag();
                var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.CastUp(additionalTextFiles));

                analyzerDriver = AnalyzerDriver.CreateAndAttachToCompilation(
                    compilation,
                    analyzers,
                    analyzerOptions,
                    new AnalyzerManager(analyzers),
                    analyzerExceptionDiagnostics.Add,
                    Arguments.ReportAnalyzer,
                    out compilation,
                    analyzerCts.Token);
                reportAnalyzer = Arguments.ReportAnalyzer && !analyzers.IsEmpty;
            }

            compilation.GetDiagnostics(CompilationStage.Declare, includeEarlierStages: false, diagnostics, cancellationToken);
            if (diagnostics.HasAnyErrors())
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            string outputName = GetOutputFileName(compilation, cancellationToken);
            var finalPeFilePath = Path.Combine(Arguments.OutputDirectory, outputName);
            var finalPdbFilePath = Arguments.PdbPath ?? Path.ChangeExtension(finalPeFilePath, ".pdb");
            var finalXmlFilePath = Arguments.DocumentationPath;

            NoThrowStreamDisposer sourceLinkStreamDisposerOpt = null;

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
                            emitOptions.EmitMetadataOnly,
                            emitOptions.EmitTestCoverageData,
                            diagnostics,
                            filterOpt: null,
                            cancellationToken: cancellationToken);

                        if (success)
                        {
                            // NOTE: as native compiler does, we generate the documentation file
                            // NOTE: 'in place', replacing the contents of the file if it exists
                            NoThrowStreamDisposer xmlStreamDisposerOpt = null;

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
                                using (var win32ResourceStreamOpt = GetWin32Resources(MessageProvider, Arguments, compilation, diagnostics))
                                {
                                    if (diagnostics.HasAnyErrors())
                                    {
                                        return;
                                    }

                                    success = compilation.GenerateResourcesAndDocumentationComments(
                                        moduleBeingBuilt,
                                        xmlStreamDisposerOpt?.Stream,
                                        win32ResourceStreamOpt,
                                        emitOptions.OutputNameOverride,
                                        diagnostics,
                                        cancellationToken);
                                }
                            }

                            if (xmlStreamDisposerOpt?.HasFailedToDispose == true)
                            {
                                return;
                            }

                            // only report unused usings if we have success.
                            if (success)
                            {
                                compilation.ReportUnusedImports(null, diagnostics, cancellationToken);
                            }
                        }

                        compilation.CompleteTrees(null);

                        if (analyzerDriver != null)
                        {
                            // GetDiagnosticsAsync is called after ReportUnusedImports
                            // since that method calls EventQueue.TryComplete. Without
                            // TryComplete, we may miss diagnostics.
                            var hostDiagnostics = analyzerDriver.GetDiagnosticsAsync(compilation).Result;
                            diagnostics.AddRange(hostDiagnostics);
                            if (hostDiagnostics.Any(IsReportedError))
                            {
                                success = false;
                            }
                        }
                    }
                    finally
                    {
                        moduleBeingBuilt.CompilationFinished();
                    }

                    if (success)
                    {
                        bool emitPdbFile = Arguments.EmitPdb && emitOptions.DebugInformationFormat != Emit.DebugInformationFormat.Embedded;

                        var peStreamProvider = new CompilerEmitStreamProvider(this, finalPeFilePath);
                        var pdbStreamProviderOpt = emitPdbFile ? new CompilerEmitStreamProvider(this, finalPdbFilePath) : null;

                        string finalRefPeFilePath = Arguments.OutputRefFilePath;
                        var refPeStreamProviderOpt = finalRefPeFilePath != null ? new CompilerEmitStreamProvider(this, finalRefPeFilePath) : null;

                        RSAParameters? privateKeyOpt = null;
                        if (compilation.Options.StrongNameProvider?.Capability == SigningCapability.SignsPeBuilder && !compilation.Options.PublicSign)
                        {
                            privateKeyOpt = compilation.StrongNameKeys.PrivateKey;
                        }

                        success = compilation.SerializeToPeStream(
                            moduleBeingBuilt,
                            peStreamProvider,
                            refPeStreamProviderOpt,
                            pdbStreamProviderOpt,
                            testSymWriterFactory: null,
                            diagnostics: diagnostics,
                            metadataOnly: emitOptions.EmitMetadataOnly,
                            includePrivateMembers: emitOptions.IncludePrivateMembers,
                            emitTestCoverageData: emitOptions.EmitTestCoverageData,
                            pePdbFilePath: emitOptions.PdbFilePath,
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
                                touchedFilesLogger.AddWritten(finalRefPeFilePath);
                            }
                            touchedFilesLogger.AddWritten(finalPeFilePath);
                        }
                    }
                }

                if (diagnostics.HasAnyErrors())
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
                if (analyzerExceptionDiagnostics.HasAnyErrors())
                {
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!WriteTouchedFiles(diagnostics, touchedFilesLogger, finalXmlFilePath))
            {
                return;
            }
        }

        private bool WriteTouchedFiles(DiagnosticBag diagnostics, TouchedFileLogger touchedFilesLogger, string finalXmlFilePath)
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

                string filePath = null;
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

        protected virtual ImmutableArray<AdditionalTextFile> ResolveAdditionalFilesFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFilesLogger)
        {
            var builder = ImmutableArray.CreateBuilder<AdditionalTextFile>();

            foreach (var file in Arguments.AdditionalFiles)
            {
                builder.Add(new AdditionalTextFile(file, this));
            }

            return builder.ToImmutableArray();
        }

        private static void ReportAnalyzerExecutionTime(TextWriter consoleOutput, AnalyzerDriver analyzerDriver, CultureInfo culture, bool isConcurrentBuild)
        {
            Debug.Assert(analyzerDriver.AnalyzerExecutionTimes != null);
            if (analyzerDriver.AnalyzerExecutionTimes.IsEmpty)
            {
                return;
            }

            var totalAnalyzerExecutionTime = analyzerDriver.AnalyzerExecutionTimes.Sum(kvp => kvp.Value.TotalSeconds);
            Func<double, string> getFormattedTime = d => d.ToString("##0.000", culture);
            consoleOutput.WriteLine();
            consoleOutput.WriteLine(string.Format(CodeAnalysisResources.AnalyzerTotalExecutionTime, getFormattedTime(totalAnalyzerExecutionTime)));

            if (isConcurrentBuild)
            {
                consoleOutput.WriteLine(CodeAnalysisResources.MultithreadedAnalyzerExecutionNote);
            }

            var analyzersByAssembly = analyzerDriver.AnalyzerExecutionTimes
                .GroupBy(kvp => kvp.Key.GetType().GetTypeInfo().Assembly)
                .OrderByDescending(kvp => kvp.Sum(entry => entry.Value.Ticks));

            consoleOutput.WriteLine();

            getFormattedTime = d => d < 0.001 ?
                string.Format(culture, "{0,8:<0.000}", 0.001) :
                string.Format(culture, "{0,8:##0.000}", d);
            Func<int, string> getFormattedPercentage = i => string.Format("{0,5}", i < 1 ? "<1" : i.ToString());
            Func<string, string> getFormattedAnalyzerName = s => "   " + s;

            // Table header
            var analyzerTimeColumn = string.Format("{0,8}", CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader);
            var analyzerPercentageColumn = string.Format("{0,5}", "%");
            var analyzerNameColumn = getFormattedAnalyzerName(CodeAnalysisResources.AnalyzerNameColumnHeader);
            consoleOutput.WriteLine(analyzerTimeColumn + analyzerPercentageColumn + analyzerNameColumn);

            // Table rows grouped by assembly.
            foreach (var analyzerGroup in analyzersByAssembly)
            {
                var executionTime = analyzerGroup.Sum(kvp => kvp.Value.TotalSeconds);
                var percentage = (int)(executionTime * 100 / totalAnalyzerExecutionTime);

                analyzerTimeColumn = getFormattedTime(executionTime);
                analyzerPercentageColumn = getFormattedPercentage(percentage);
                analyzerNameColumn = getFormattedAnalyzerName(analyzerGroup.Key.FullName);

                consoleOutput.WriteLine(analyzerTimeColumn + analyzerPercentageColumn + analyzerNameColumn);

                // Rows for each diagnostic analyzer in the assembly.
                foreach (var kvp in analyzerGroup.OrderByDescending(kvp => kvp.Value))
                {
                    executionTime = kvp.Value.TotalSeconds;
                    percentage = (int)(executionTime * 100 / totalAnalyzerExecutionTime);

                    analyzerTimeColumn = getFormattedTime(executionTime);
                    analyzerPercentageColumn = getFormattedPercentage(percentage);
                    analyzerNameColumn = getFormattedAnalyzerName("   " + kvp.Key.ToString());

                    consoleOutput.WriteLine(analyzerTimeColumn + analyzerPercentageColumn + analyzerNameColumn);
                }

                consoleOutput.WriteLine();
            }
        }

        /// <summary>
        /// Given a compilation and a destination directory, determine three names:
        ///   1) The name with which the assembly should be output (default = null, which indicates that the compilation output name should be used).
        ///   2) The path of the assembly/module file (default = destination directory + compilation output name).
        ///   3) The path of the pdb file (default = assembly/module path with ".pdb" extension).
        /// </summary>
        /// <remarks>
        /// C# has a special implementation that implements idiosyncratic behavior of csc.
        /// </remarks>
        protected virtual string GetOutputFileName(Compilation compilation, CancellationToken cancellationToken)
        {
            return Arguments.OutputFileName;
        }

        /// <summary>
        /// Test hook for intercepting File.Open.
        /// </summary>
        internal Func<string, FileMode, FileAccess, FileShare, Stream> FileOpen
        {
            get { return _fileOpen ?? ((path, mode, access, share) => new FileStream(path, mode, access, share)); }
            set { _fileOpen = value; }
        }
        private Func<string, FileMode, FileAccess, FileShare, Stream> _fileOpen;

        private Stream OpenFile(
            string filePath,
            DiagnosticBag diagnostics,
            FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.ReadWrite,
            FileShare share = FileShare.None)
        {
            try
            {
                return FileOpen(filePath, mode, access, share);
            }
            catch (Exception e)
            {
                MessageProvider.ReportStreamWriteException(e, filePath, diagnostics);
                return null;
            }
        }

        // internal for testing
        internal static Stream GetWin32ResourcesInternal(
            CommonMessageProvider messageProvider,
            CommandLineArguments arguments,
            Compilation compilation,
            out IEnumerable<DiagnosticInfo> errors)
        {
            var diagnostics = DiagnosticBag.GetInstance();
            var stream = GetWin32Resources(messageProvider, arguments, compilation, diagnostics);
            errors = diagnostics.ToReadOnlyAndFree().SelectAsArray(diag => new DiagnosticInfo(messageProvider, diag.IsWarningAsError, diag.Code, (object[])diag.Arguments));
            return stream;
        }

        private static Stream GetWin32Resources(
            CommonMessageProvider messageProvider,
            CommandLineArguments arguments,
            Compilation compilation,
            DiagnosticBag diagnostics)
        {
            if (arguments.Win32ResourceFile != null)
            {
                return OpenStream(messageProvider, arguments.Win32ResourceFile, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Resource, diagnostics);
            }

            using (Stream manifestStream = OpenManifestStream(messageProvider, compilation.Options.OutputKind, arguments, diagnostics))
            {
                using (Stream iconStream = OpenStream(messageProvider, arguments.Win32Icon, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Icon, diagnostics))
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

        private static Stream OpenManifestStream(CommonMessageProvider messageProvider, OutputKind outputKind, CommandLineArguments arguments, DiagnosticBag diagnostics)
        {
            return outputKind.IsNetModule()
                ? null
                : OpenStream(messageProvider, arguments.Win32Manifest, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Manifest, diagnostics);
        }

        private static Stream OpenStream(CommonMessageProvider messageProvider, string path, string baseDirectory, int errorCode, DiagnosticBag diagnostics)
        {
            if (path == null)
            {
                return null;
            }

            string fullPath = ResolveRelativePath(messageProvider, path, baseDirectory, diagnostics);
            if (fullPath == null)
            {
                return null;
            }

            try
            {
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            }
            catch (Exception ex)
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(errorCode, Location.None, fullPath, ex.Message));
            }

            return null;
        }

        private static string ResolveRelativePath(CommonMessageProvider messageProvider, string path, string baseDirectory, DiagnosticBag diagnostics)
        {
            string fullPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
            if (fullPath == null)
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.FTL_InvalidInputFileName, Location.None, path));
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

        private static void EmitDeterminismKey(CommandLineArguments args, string[] rawArgs, string baseDirectory, CommandLineParser parser)
        {
            var key = CreateDeterminismKey(args, rawArgs, baseDirectory, parser);
            var filePath = Path.Combine(args.OutputDirectory, args.OutputFileName + ".key");
            using (var stream = File.Create(filePath))
            {
                var bytes = Encoding.UTF8.GetBytes(key);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// The string returned from this function represents the inputs to the compiler which impact determinism.  It is 
        /// meant to be inline with the specification here:
        /// 
        ///     - https://github.com/dotnet/roslyn/blob/master/docs/compilers/Deterministic%20Inputs.md
        /// 
        /// Issue #8193 tracks filling this out to the full specification. 
        /// 
        ///     https://github.com/dotnet/roslyn/issues/8193
        /// </summary>
        private static string CreateDeterminismKey(CommandLineArguments args, string[] rawArgs, string baseDirectory, CommandLineParser parser)
        {
            List<Diagnostic> diagnostics = new List<Diagnostic>();
            List<string> flattenedArgs = new List<string>();
            parser.FlattenArgs(rawArgs, diagnostics, flattenedArgs, null, baseDirectory);

            var builder = new StringBuilder();
            var name = !string.IsNullOrEmpty(args.OutputFileName)
                ? Path.GetFileNameWithoutExtension(Path.GetFileName(args.OutputFileName))
                : $"no-output-name-{Guid.NewGuid().ToString()}";

            builder.AppendLine($"{name}");
            builder.AppendLine($"Command Line:");
            foreach (var current in flattenedArgs)
            {
                builder.AppendLine($"\t{current}");
            }

            builder.AppendLine("Source Files:");
            var hash = MD5.Create();
            foreach (var sourceFile in args.SourceFiles)
            {
                var sourceFileName = Path.GetFileName(sourceFile.Path);

                string hashValue;
                try
                {
                    var bytes = File.ReadAllBytes(sourceFile.Path);
                    var hashBytes = hash.ComputeHash(bytes);
                    var data = BitConverter.ToString(hashBytes);
                    hashValue = data.Replace("-", "");
                }
                catch (Exception ex)
                {
                    hashValue = $"Could not compute {ex.Message}";
                }
                builder.AppendLine($"\t{sourceFileName} - {hashValue}");
            }

            return builder.ToString();
        }
    }
}
