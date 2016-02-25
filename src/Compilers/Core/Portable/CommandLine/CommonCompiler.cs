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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
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
        private readonly HashSet<Diagnostic> _reportedDiagnostics = new HashSet<Diagnostic>();

        public abstract Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLoggerOpt);
        public abstract void PrintLogo(TextWriter consoleOutput);
        public abstract void PrintHelp(TextWriter consoleOutput);
        internal abstract string GetToolName();

        protected abstract uint GetSqmAppID();
        protected abstract bool TryGetCompilerDiagnosticCode(string diagnosticId, out uint code);
        protected abstract void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession);
        protected abstract ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles);
        protected abstract ImmutableArray<SourceGenerator> ResolveGeneratorsFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles);

        public CommonCompiler(CommandLineParser parser, string responseFile, string[] args, string clientDirectory, string baseDirectory, string sdkDirectoryOpt, string additionalReferenceDirectories, IAnalyzerAssemblyLoader assemblyLoader)
        {
            IEnumerable<string> allArgs = args;
            _clientDirectory = clientDirectory;

            Debug.Assert(null == responseFile || PathUtilities.IsAbsolute(responseFile));
            if (!SuppressDefaultResponseFile(args) && PortableShim.File.Exists(responseFile))
            {
                allArgs = new[] { "@" + responseFile }.Concat(allArgs);
            }

            this.Arguments = parser.Parse(allArgs, baseDirectory, sdkDirectoryOpt, additionalReferenceDirectories);
            this.MessageProvider = parser.MessageProvider;
            this.AssemblyLoader = assemblyLoader;

            if (Arguments.ParseOptions.Features.ContainsKey("debug-determinism"))
            {
                EmitDeterminismKey(Arguments, args, baseDirectory, parser);
            }
        }

        internal abstract bool SuppressDefaultResponseFile(IEnumerable<string> args);

        internal string GetAssemblyFileVersion()
        {
            if (_clientDirectory != null)
            {
                var name = $"{typeof(CommonCompiler).GetTypeInfo().Assembly.GetName().Name}.dll";
                var filePath = Path.Combine(_clientDirectory, name);
                return PortableShim.Misc.GetFileVersion(filePath);
            }

            return "";
        }

        internal Version GetAssemblyVersion()
        {
            return typeof(CommonCompiler).GetTypeInfo().Assembly.GetName().Version;
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
        internal SourceText ReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics)
        {
            string discarded;
            return ReadFileContent(file, diagnostics, out discarded);
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <param name="normalizedFilePath">If given <paramref name="file"/> opens successfully, set to normalized absolute path of the file, null otherwise.</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText ReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics, out string normalizedFilePath)
        {
            var filePath = file.Path;
            try
            {
                // PERF: Using a very small buffer size for the FileStream opens up an optimization within EncodedStringText where
                // we read the entire FileStream into a byte array in one shot. For files that are actually smaller than the buffer
                // size, FileStream.Read still allocates the internal buffer.
                using (var data = PortableShim.FileStream.Create(filePath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read, PortableShim.FileShare.ReadWrite, bufferSize: 1, options: PortableShim.FileOptions.None))
                {
                    normalizedFilePath = (string)PortableShim.FileStream.Name.GetValue(data);
                    return EncodedStringText.Create(data, Arguments.Encoding, Arguments.ChecksumAlgorithm);
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(ToFileReadDiagnostics(this.MessageProvider, e, filePath));
                normalizedFilePath = null;
                return null;
            }
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

        public bool ReportErrors(IEnumerable<Diagnostic> diagnostics, TextWriter consoleOutput, ErrorLogger errorLoggerOpt)
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
                errorLoggerOpt?.LogDiagnostic(diag, this.Culture);
                if (diag.IsSuppressed)
                {
                    continue;
                }

                consoleOutput.WriteLine(DiagnosticFormatter.Format(diag, this.Culture));

                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                }

                Debug.Assert(IsReportedError(diag));
                _reportedDiagnostics.Add(diag);
            }

            return hasErrors;
        }

        /// <summary>
        /// Returns true if the diagnostic is an error that should be reported.
        /// </summary>
        private static bool IsReportedError(Diagnostic diagnostic)
        {
            return (diagnostic.Severity == DiagnosticSeverity.Error) && !diagnostic.IsSuppressed;
        }

        public bool ReportErrors(IEnumerable<DiagnosticInfo> diagnostics, TextWriter consoleOutput, ErrorLogger errorLoggerOpt)
        {
            bool hasErrors = false;
            if (diagnostics != null && diagnostics.Any())
            {
                foreach (var diagnostic in diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Hidden)
                    {
                        // Not reported from the command-line compiler.
                        continue;
                    }

                    PrintError(diagnostic, consoleOutput);
                    errorLoggerOpt?.LogDiagnostic(Diagnostic.Create(diagnostic), this.Culture);

                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        hasErrors = true;
                    }
                }
            }

            return hasErrors;
        }

        protected virtual void PrintError(DiagnosticInfo diagnostic, TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(diagnostic.ToString(Culture));
        }

        public ErrorLogger GetErrorLogger(TextWriter consoleOutput, CancellationToken cancellationToken)
        {
            Debug.Assert(Arguments.ErrorLogPath != null);

            var errorLog = OpenFile(Arguments.ErrorLogPath, consoleOutput, PortableShim.FileMode.Create, PortableShim.FileAccess.Write, PortableShim.FileShare.ReadWriteBitwiseOrDelete);
            if (errorLog == null)
            {
                return null;
            }

            return new ErrorLogger(errorLog, GetToolName(), GetAssemblyFileVersion(), GetAssemblyVersion());
        }

        /// <summary>
        /// csc.exe and vbc.exe entry point.
        /// </summary>
        public virtual int Run(TextWriter consoleOutput, CancellationToken cancellationToken = default(CancellationToken))
        {
            var saveUICulture = CultureInfo.CurrentUICulture;
            ErrorLogger errorLogger = null;

            try
            {
                // Messages from exceptions can be used as arguments for errors and they are often localized.
                // Ensure they are localized to the right language.
                var culture = this.Culture;
                if (culture != null)
                {
                    PortableShim.Misc.SetCurrentUICulture(culture);
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
                PortableShim.Misc.SetCurrentUICulture(saveUICulture);
                errorLogger?.Dispose();
            }
        }

        private int RunCore(TextWriter consoleOutput, ErrorLogger errorLogger, CancellationToken cancellationToken)
        {
            Debug.Assert(!Arguments.IsScriptRunner);

            cancellationToken.ThrowIfCancellationRequested();

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

            var diagnostics = new List<DiagnosticInfo>();
            var analyzers = ResolveAnalyzersFromArguments(diagnostics, MessageProvider, touchedFilesLogger);
            var sourceGenerators = ResolveGeneratorsFromArguments(diagnostics, MessageProvider, touchedFilesLogger);
            var additionalTextFiles = ResolveAdditionalFilesFromArguments(diagnostics, MessageProvider, touchedFilesLogger);
            if (ReportErrors(diagnostics, consoleOutput, errorLogger))
            {
                return Failed;
            }

            bool includeGenerators = !sourceGenerators.IsEmpty;

retry:
            bool reportAnalyzer = false;
            CancellationTokenSource analyzerCts = null;
            AnalyzerManager analyzerManager = null;
            AnalyzerDriver analyzerDriver = null;

            try
            {
                Func<DiagnosticBag, bool, bool> getAnalyzerDiagnostics = null;
                ConcurrentSet<Diagnostic> analyzerExceptionDiagnostics = null;

                if (!analyzers.IsEmpty)
                {
                    analyzerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    analyzerManager = new AnalyzerManager();
                    analyzerExceptionDiagnostics = new ConcurrentSet<Diagnostic>();
                    Action<Diagnostic> addExceptionDiagnostic = diagnostic => analyzerExceptionDiagnostics.Add(diagnostic);
                    var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.CastUp(additionalTextFiles));

                    analyzerDriver = AnalyzerDriver.CreateAndAttachToCompilation(compilation, analyzers, analyzerOptions, analyzerManager, addExceptionDiagnostic, Arguments.ReportAnalyzer, out compilation, analyzerCts.Token);
                    reportAnalyzer = Arguments.ReportAnalyzer && !analyzers.IsEmpty;
                }

                Compilation newCompilation = null;
                if ((analyzerDriver != null) || includeGenerators)
                {
                    getAnalyzerDiagnostics = (diags, result) =>
                    {
                        if (result && (analyzerDriver != null))
                        {
                            var hostDiagnostics = analyzerDriver.GetDiagnosticsAsync(compilation).Result;
                            diags.AddRange(hostDiagnostics);
                            if (hostDiagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
                            {
                                result = false;
                            }
                        }
                        if (includeGenerators)
                        {
                            newCompilation = AddGeneratedSource(compilation, sourceGenerators, cancellationToken);
                            includeGenerators = false;
                            if (newCompilation != compilation)
                            {
                                result = false;
                            }
                        }
                        return result;
                    };
                }

                // Print the diagnostics produced during the parsing stage and exit if there were any errors.
                if (ReportErrors(compilation.GetParseDiagnostics(), consoleOutput, errorLogger))
                {
                    return Failed;
                }

                var declarationDiagnostics = compilation.GetDeclarationDiagnostics();
                if (includeGenerators && declarationDiagnostics.Any(IsReportedError))
                {
                    // Run generators even if there are declaration errors since
                    // the errors may be the resolved by the generated code.
                    newCompilation = AddGeneratedSource(compilation, sourceGenerators, cancellationToken);
                    includeGenerators = false;
                    if (newCompilation != compilation)
                    {
                        compilation = newCompilation;
                        goto retry;
                    }
                }

                if (ReportErrors(declarationDiagnostics, consoleOutput, errorLogger))
                {
                    return Failed;
                }

                EmitResult emitResult;

                // NOTE: as native compiler does, we generate the documentation file
                // NOTE: 'in place', replacing the contents of the file if it exists

                string finalPeFilePath;
                string finalPdbFilePath;
                string finalXmlFilePath;

                Stream xmlStreamOpt = null;

                cancellationToken.ThrowIfCancellationRequested();

                finalXmlFilePath = Arguments.DocumentationPath;
                if (finalXmlFilePath != null)
                {
                    xmlStreamOpt = OpenFile(finalXmlFilePath, consoleOutput, PortableShim.FileMode.OpenOrCreate, PortableShim.FileAccess.Write, PortableShim.FileShare.ReadWriteBitwiseOrDelete);
                    if (xmlStreamOpt == null)
                    {
                        return Failed;
                    }

                    xmlStreamOpt.SetLength(0);
                }

                cancellationToken.ThrowIfCancellationRequested();

                IEnumerable<DiagnosticInfo> errors;
                using (var win32ResourceStreamOpt = GetWin32Resources(Arguments, compilation, out errors))
                using (xmlStreamOpt)
                {
                    if (ReportErrors(errors, consoleOutput, errorLogger))
                    {
                        return Failed;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    string outputName = GetOutputFileName(compilation, cancellationToken);

                    finalPeFilePath = Path.Combine(Arguments.OutputDirectory, outputName);
                    finalPdbFilePath = Arguments.PdbPath ?? Path.ChangeExtension(finalPeFilePath, ".pdb");

                    // NOTE: Unlike the PDB path, the XML doc path is not embedded in the assembly, so we don't need to pass it to emit.
                    var emitOptions = Arguments.EmitOptions.
                        WithOutputNameOverride(outputName).
                        WithPdbFilePath(finalPdbFilePath);

                    using (var peStreamProvider = new CompilerEmitStreamProvider(this, finalPeFilePath))
                    using (var pdbStreamProviderOpt = Arguments.EmitPdb ? new CompilerEmitStreamProvider(this, finalPdbFilePath) : null)
                    {
                        emitResult = compilation.Emit(
                            peStreamProvider,
                            pdbStreamProviderOpt,
                            (xmlStreamOpt != null) ? new Compilation.SimpleEmitStreamProvider(xmlStreamOpt) : null,
                            (win32ResourceStreamOpt != null) ? new Compilation.SimpleEmitStreamProvider(win32ResourceStreamOpt) : null,
                            Arguments.ManifestResources,
                            emitOptions,
                            debugEntryPoint: null,
                            testData: null,
                            getHostDiagnostics: getAnalyzerDiagnostics,
                            cancellationToken: cancellationToken);

                        if (!emitResult.Success && (newCompilation != null) && (newCompilation != compilation))
                        {
                            compilation = newCompilation;
                            goto retry;
                        }

                        if (emitResult.Success && touchedFilesLogger != null)
                        {
                            if (pdbStreamProviderOpt != null)
                            {
                                touchedFilesLogger.AddWritten(finalPdbFilePath);
                            }

                            touchedFilesLogger.AddWritten(finalPeFilePath);
                        }
                    }
                }

                GenerateSqmData(Arguments.CompilationOptions, emitResult.Diagnostics);

                if (ReportErrors(emitResult.Diagnostics, consoleOutput, errorLogger))
                {
                    return Failed;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (analyzerExceptionDiagnostics != null && ReportErrors(analyzerExceptionDiagnostics, consoleOutput, errorLogger))
                {
                    return Failed;
                }

                bool errorsReadingAdditionalFiles = false;
                foreach (var additionalFile in additionalTextFiles)
                {
                    if (ReportErrors(additionalFile.Diagnostics, consoleOutput, errorLogger))
                    {
                        errorsReadingAdditionalFiles = true;
                    }
                }

                if (errorsReadingAdditionalFiles)
                {
                    return Failed;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (Arguments.TouchedFilesPath != null)
                {
                    Debug.Assert(touchedFilesLogger != null);

                    if (finalXmlFilePath != null)
                    {
                        touchedFilesLogger.AddWritten(finalXmlFilePath);
                    }

                    var readStream = OpenFile(Arguments.TouchedFilesPath + ".read", consoleOutput, mode: PortableShim.FileMode.OpenOrCreate);
                    if (readStream == null)
                    {
                        return Failed;
                    }

                    using (var writer = new StreamWriter(readStream))
                    {
                        touchedFilesLogger.WriteReadPaths(writer);
                    }

                    var writtenStream = OpenFile(Arguments.TouchedFilesPath + ".write", consoleOutput, mode: PortableShim.FileMode.OpenOrCreate);
                    if (writtenStream == null)
                    {
                        return Failed;
                    }

                    using (var writer = new StreamWriter(writtenStream))
                    {
                        touchedFilesLogger.WriteWrittenPaths(writer);
                    }
                }
            }
            finally
            {
                // At this point analyzers are already complete in which case this is a no-op.  Or they are 
                // still running because the compilation failed before all of the compilation events were 
                // raised.  In the latter case the driver, and all its associated state, will be waiting around 
                // for events that are never coming.  Cancel now and let the clean up process begin.
                if (analyzerCts != null)
                {
                    analyzerCts.Cancel();

                    if (analyzerManager != null)
                    {
                        // Clear cached analyzer descriptors and unregister exception handlers hooked up to the LocalizableString fields of the associated descriptors.
                        analyzerManager.ClearAnalyzerState(analyzers);
                    }

                    if (reportAnalyzer)
                    {
                        ReportAnalyzerExecutionTime(consoleOutput, analyzerDriver, Culture, compilation.Options.ConcurrentBuild);
                    }
                }
            }

            return Succeeded;
        }

        private Compilation AddGeneratedSource(
            Compilation compilation,
            ImmutableArray<SourceGenerator> sourceGenerators,
            CancellationToken cancellationToken)
        {
            // TODO: Generated source path should include a "GeneratedFiles" subdirectory
            // so that "build clean" can be modified to delete the entire directory.
            var trees = compilation.GenerateSource(sourceGenerators, this.Arguments.OutputDirectory, writeToDisk: true, cancellationToken: cancellationToken);
            return compilation.AddSyntaxTrees(trees);
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

        private void GenerateSqmData(CompilationOptions compilationOptions, ImmutableArray<Diagnostic> diagnostics)
        {
            // Generate SQM data file for Compilers
            if (Arguments.SqmSessionGuid != Guid.Empty)
            {
                IVsSqmMulti sqm = null;
                uint sqmSession = 0u;
                try
                {
                    sqm = SqmServiceProvider.TryGetSqmService(_clientDirectory);
                    if (sqm != null)
                    {
                        sqm.BeginSession(this.GetSqmAppID(), false, out sqmSession);
                        sqm.SetGlobalSessionGuid(Arguments.SqmSessionGuid);

                        // Build Version
                        sqm.SetStringDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_BUILDVERSION, GetAssemblyFileVersion());

                        // Write Errors and Warnings from build
                        foreach (var diagnostic in diagnostics)
                        {
                            switch (diagnostic.Severity)
                            {
                                case DiagnosticSeverity.Error:
                                    sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_ERRORNUMBERS, (uint)diagnostic.Code);
                                    break;

                                case DiagnosticSeverity.Warning:
                                    sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNINGNUMBERS, (uint)diagnostic.Code);
                                    break;

                                case DiagnosticSeverity.Hidden:
                                case DiagnosticSeverity.Info:
                                    break;

                                default:
                                    throw ExceptionUtilities.UnexpectedValue(diagnostic.Severity);
                            }
                        }

                        //Suppress Warnings / warningCode as error / warningCode as warning
                        foreach (var item in compilationOptions.SpecificDiagnosticOptions)
                        {
                            uint code;
                            if (TryGetCompilerDiagnosticCode(item.Key, out code))
                            {
                                ReportDiagnostic options = item.Value;
                                switch (options)
                                {
                                    case ReportDiagnostic.Suppress:
                                        sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_SUPPRESSWARNINGNUMBERS, code);      // Suppress warning
                                        break;

                                    case ReportDiagnostic.Error:
                                        sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNASERRORS_NUMBERS, code);       // Warning as errors
                                        break;

                                    case ReportDiagnostic.Warn:
                                        sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNASWARNINGS_NUMBERS, code);     // Warning as warnings
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                        sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_OUTPUTKIND, (uint)compilationOptions.OutputKind);
                        CompilerSpecificSqm(sqm, sqmSession);
                    }
                }
                finally
                {
                    if (sqm != null)
                    {
                        sqm.EndSession(sqmSession);
                    }
                }
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
        internal Func<string, object, object, object, Stream> FileOpen
        {
            get { return _fileOpen ?? PortableShim.FileStream.Create_String_FileMode_FileAccess_FileShare; }
            set { _fileOpen = value; }
        }
        private Func<string, object, object, object, Stream> _fileOpen;

        private Stream OpenFile(string filePath, TextWriter consoleOutput, object mode = null, object access = null, object share = null)
        {
            mode = mode ?? PortableShim.FileMode.Open;
            access = access ?? PortableShim.FileAccess.ReadWrite;
            share = share ?? PortableShim.FileShare.None;

            try
            {
                return FileOpen(filePath, mode, access, share);
            }
            catch (Exception e)
            {
                if (consoleOutput != null)
                {
                    // TODO: distinct error message?
                    DiagnosticInfo diagnosticInfo = new DiagnosticInfo(MessageProvider, (int)MessageProvider.ERR_OutputWriteFailed, filePath, e.Message);
                    consoleOutput.WriteLine(diagnosticInfo.ToString(Culture));
                }

                return null;
            }
        }

        protected Stream GetWin32Resources(CommandLineArguments arguments, Compilation compilation, out IEnumerable<DiagnosticInfo> errors)
        {
            return GetWin32ResourcesInternal(MessageProvider, arguments, compilation, out errors);
        }

        // internal for testing
        internal static Stream GetWin32ResourcesInternal(CommonMessageProvider messageProvider, CommandLineArguments arguments, Compilation compilation, out IEnumerable<DiagnosticInfo> errors)
        {
            List<DiagnosticInfo> errorList = new List<DiagnosticInfo>();
            errors = errorList;

            if (arguments.Win32ResourceFile != null)
            {
                return OpenStream(messageProvider, arguments.Win32ResourceFile, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Resource, errorList);
            }

            using (Stream manifestStream = OpenManifestStream(messageProvider, compilation.Options.OutputKind, arguments, errorList))
            {
                using (Stream iconStream = OpenStream(messageProvider, arguments.Win32Icon, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Icon, errorList))
                {
                    try
                    {
                        return compilation.CreateDefaultWin32Resources(true, arguments.NoWin32Manifest, manifestStream, iconStream);
                    }
                    catch (ResourceException ex)
                    {
                        errorList.Add(new DiagnosticInfo(messageProvider, messageProvider.ERR_ErrorBuildingWin32Resource, ex.Message));
                    }
                    catch (OverflowException ex)
                    {
                        errorList.Add(new DiagnosticInfo(messageProvider, messageProvider.ERR_ErrorBuildingWin32Resource, ex.Message));
                    }
                }
            }

            return null;
        }

        private static Stream OpenManifestStream(CommonMessageProvider messageProvider, OutputKind outputKind, CommandLineArguments arguments, List<DiagnosticInfo> errorList)
        {
            return outputKind.IsNetModule()
                ? null
                : OpenStream(messageProvider, arguments.Win32Manifest, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Manifest, errorList);
        }

        private static Stream OpenStream(CommonMessageProvider messageProvider, string path, string baseDirectory, int errorCode, IList<DiagnosticInfo> errors)
        {
            if (path == null)
            {
                return null;
            }

            string fullPath = ResolveRelativePath(messageProvider, path, baseDirectory, errors);
            if (fullPath == null)
            {
                return null;
            }

            try
            {
                return PortableShim.FileStream.Create(fullPath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read);
            }
            catch (Exception ex)
            {
                errors.Add(new DiagnosticInfo(messageProvider, errorCode, fullPath, ex.Message));
            }

            return null;
        }

        private static string ResolveRelativePath(CommonMessageProvider messageProvider, string path, string baseDirectory, IList<DiagnosticInfo> errors)
        {
            string fullPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
            if (fullPath == null)
            {
                errors.Add(new DiagnosticInfo(messageProvider, messageProvider.FTL_InputFileNameTooLong, path));
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
            using (var stream = PortableShim.File.Create(filePath))
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
            var hash = new MD5CryptoServiceProvider();
            foreach (var sourceFile in args.SourceFiles)
            {
                var sourceFileName = Path.GetFileName(sourceFile.Path);

                string hashValue;
                try
                {
                    var bytes = PortableShim.File.ReadAllBytes(sourceFile.Path);
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
