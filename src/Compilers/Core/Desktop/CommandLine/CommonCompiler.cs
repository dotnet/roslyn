﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        /// <summary>
        /// Return the path in which to look for response files.  This should only be called 
        /// on EXE entry points as the implementation relies on managed entry points.
        /// </summary>
        /// <returns></returns>
        internal static string GetResponseFileDirectory()
        {
            var exePath = Assembly.GetEntryAssembly().Location;

            // This assert will fire when this method is called from places like xUnit and certain
            // types of AppDomains.  It should only be called on EXE entry points to help guarantee
            // this is being called from an executed assembly.
            Debug.Assert(exePath != null);
            return Path.GetDirectoryName(exePath);
        }

        /// <summary>
        /// Called from a compiler exe entry point to get the full path to the response file for
        /// the given name.  Will return a fully qualified path.
        /// </summary>
        internal static string GetResponseFileFullPath(string responseFileName)
        {
            return Path.Combine(GetResponseFileDirectory(), responseFileName);
        }

        public CommonMessageProvider MessageProvider { get; private set; }
        public CommandLineArguments Arguments { get; private set; }
        public abstract DiagnosticFormatter DiagnosticFormatter { get; }
        private readonly HashSet<Diagnostic> _reportedDiagnostics = new HashSet<Diagnostic>();

        protected abstract Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLogger);
        protected abstract void PrintLogo(TextWriter consoleOutput);
        protected abstract void PrintHelp(TextWriter consoleOutput);
        internal abstract string GetToolName();
        internal abstract Version GetAssemblyVersion();
        internal abstract string GetAssemblyFileVersion();

        protected abstract uint GetSqmAppID();
        protected abstract bool TryGetCompilerDiagnosticCode(string diagnosticId, out uint code);
        protected abstract void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession);
        protected abstract ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles);

        public CommonCompiler(CommandLineParser parser, string responseFile, string[] args, string baseDirectory, string additionalReferencePaths)
        {
            IEnumerable<string> allArgs = args;

            Debug.Assert(null == responseFile || PathUtilities.IsAbsolute(responseFile));

            if (!SuppressDefaultResponseFile(args) && File.Exists(responseFile))
            {
                allArgs = new[] { "@" + responseFile }.Concat(allArgs);
            }

            this.Arguments = parser.Parse(allArgs, baseDirectory, additionalReferencePaths);
            this.MessageProvider = parser.MessageProvider;
        }

        internal abstract bool SuppressDefaultResponseFile(IEnumerable<string> args);

        internal virtual MetadataFileReferenceProvider GetMetadataProvider()
        {
            return MetadataFileReferenceProvider.Default;
        }

        internal virtual MetadataFileReferenceResolver GetExternalMetadataResolver(TouchedFileLogger touchedFiles)
        {
            return new LoggingMetadataReferencesResolver(Arguments.ReferencePaths, Arguments.BaseDirectory, touchedFiles);
        }

        /// <summary>
        /// Resolves metadata references stored in command line arguments and reports errors for those that can't be resolved.
        /// </summary>
        internal List<MetadataReference> ResolveMetadataReferences(
            MetadataFileReferenceResolver externalReferenceResolver,
            MetadataFileReferenceProvider metadataProvider,
            List<DiagnosticInfo> diagnostics,
            AssemblyIdentityComparer assemblyIdentityComparer,
            TouchedFileLogger touchedFiles,
            out MetadataFileReferenceResolver referenceDirectiveResolver)
        {
            List<MetadataReference> resolved = new List<MetadataReference>();
            Arguments.ResolveMetadataReferences(new AssemblyReferenceResolver(externalReferenceResolver, metadataProvider), diagnostics, this.MessageProvider, resolved);

            if (Arguments.IsInteractive)
            {
                referenceDirectiveResolver = externalReferenceResolver;
            }
            else
            {
                // when compiling into an assembly (csc/vbc) we only allow #r that match references given on command line:
                referenceDirectiveResolver = new ExistingReferencesResolver(
                    resolved.Where(r => r.Properties.Kind == MetadataImageKind.Assembly).OfType<PortableExecutableReference>().AsImmutable(),
                    Arguments.ReferencePaths,
                    Arguments.BaseDirectory,
                    assemblyIdentityComparer,
                    touchedFiles);
            }

            return resolved;
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <param name="encoding">Encoding to use or 'null' for autodetect/default</param>
        /// <param name="checksumAlgorithm">Hash algorithm used to calculate file checksum.</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText ReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics, Encoding encoding, SourceHashAlgorithm checksumAlgorithm)
        {
            string discarded;
            return ReadFileContent(file, diagnostics, encoding, checksumAlgorithm, out discarded);
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <param name="encoding">Encoding to use or 'null' for autodetect/default</param>
        /// <param name="checksumAlgorithm">Hash algorithm used to calculate file checksum.</param>
        /// <param name="normalizedFilePath">If given <paramref name="file"/> opens successfully, set to normalized absolute path of the file, null otherwise.</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText ReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, out string normalizedFilePath)
        {
            try
            {
                // PERF: Using a very small buffer size for the FileStream opens up an optimization within EncodedStringText where
                // we read the entire FileStream into a byte array in one shot. For files that are actually smaller than the buffer
                // size, FileStream.Read still allocates the internal buffer.
                using (var data = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1))
                {
                    normalizedFilePath = data.Name;
                    return EncodedStringText.Create(data, encoding, checksumAlgorithm);
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(ToFileReadDiagnostics(e, file));
                normalizedFilePath = null;
                return null;
            }
        }

        private DiagnosticInfo ToFileReadDiagnostics(Exception e, CommandLineSourceFile file)
        {
            DiagnosticInfo diagnosticInfo;

            if (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                diagnosticInfo = new DiagnosticInfo(MessageProvider, MessageProvider.ERR_FileNotFound, file.Path);
            }
            else if (e is InvalidDataException)
            {
                diagnosticInfo = new DiagnosticInfo(MessageProvider, MessageProvider.ERR_BinaryFile, file.Path);
            }
            else
            {
                diagnosticInfo = new DiagnosticInfo(MessageProvider, MessageProvider.ERR_NoSourceFile, file.Path, e.Message);
            }

            return diagnosticInfo;
        }

        protected bool ReportErrors(IEnumerable<Diagnostic> diagnostics, TextWriter consoleOutput, ErrorLogger errorLogger)
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

                consoleOutput.WriteLine(DiagnosticFormatter.Format(diag, this.Culture));
                ErrorLogger.LogDiagnostic(diag, this.Culture, errorLogger);

                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    hasErrors = true;
                }

                _reportedDiagnostics.Add(diag);
            }

            return hasErrors;
        }

        protected bool ReportErrors(IEnumerable<DiagnosticInfo> diagnostics, TextWriter consoleOutput, ErrorLogger errorLogger)
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
                    ErrorLogger.LogDiagnostic(Diagnostic.Create(diagnostic), this.Culture, errorLogger);

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

        private ErrorLogger GetErrorLogger(TextWriter consoleOutput, CancellationToken cancellationToken)
        {
            Debug.Assert(Arguments.ErrorLogPath != null);

            var errorLog = OpenFile(Arguments.ErrorLogPath, consoleOutput, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
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
            var saveUICulture = Thread.CurrentThread.CurrentUICulture;
            ErrorLogger errorLogger = null;

            try
            {
                // Messages from exceptions can be used as arguments for errors and they are often localized.
                // Ensure they are localized to the right language.
                var culture = this.Culture;
                if (culture != null)
                {
                    Thread.CurrentThread.CurrentUICulture = culture;
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
                Thread.CurrentThread.CurrentUICulture = saveUICulture;
                errorLogger?.Dispose();
            }
        }

        private int RunCore(TextWriter consoleOutput, ErrorLogger errorLogger, CancellationToken cancellationToken)
        {
            Debug.Assert(!Arguments.IsInteractive);

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
            var additionalTextFiles = ResolveAdditionalFilesFromArguments(diagnostics, MessageProvider, touchedFilesLogger);
            if (ReportErrors(diagnostics, consoleOutput, errorLogger))
            {
                return Failed;
            }

            cancellationToken.ThrowIfCancellationRequested();

            CancellationTokenSource analyzerCts = null;
            try
            {
                Func<ImmutableArray<Diagnostic>> getAnalyzerDiagnostics = null;
                if (!analyzers.IsDefaultOrEmpty)
                {
                    analyzerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    var analyzerManager = new AnalyzerManager();
                    var analyzerExceptionDiagnostics = new ConcurrentSet<Diagnostic>();
                    Action<Diagnostic> addExceptionDiagnostic = diagnostic => analyzerExceptionDiagnostics.Add(diagnostic);
                    var analyzerOptions = new AnalyzerOptions(ImmutableArray.Create<AdditionalText, AdditionalTextFile>(additionalTextFiles));
                    var analyzerDriver = AnalyzerDriver.Create(compilation, analyzers, analyzerOptions, analyzerManager, addExceptionDiagnostic, out compilation, analyzerCts.Token);

                    getAnalyzerDiagnostics = () =>
                        {
                            var analyzerDiagnostics = analyzerDriver.GetDiagnosticsAsync().Result;
                            return analyzerDiagnostics.AddRange(analyzerExceptionDiagnostics);
                        };
                }

                // Print the diagnostics produced during the parsing stage and exit if there were any errors.
                if (ReportErrors(compilation.GetParseDiagnostics(), consoleOutput, errorLogger))
                {
                    return Failed;
                }

                if (ReportErrors(compilation.GetDeclarationDiagnostics(), consoleOutput, errorLogger))
                {
                    return Failed;
                }

                EmitResult emitResult;

                // NOTE: as native compiler does, we generate the documentation file
                // NOTE: 'in place', replacing the contents of the file if it exists

                string finalOutputPath;
                string finalPdbFilePath;
                string finalXmlFilePath;

                FileStream xml = null;

                cancellationToken.ThrowIfCancellationRequested();

                finalXmlFilePath = Arguments.DocumentationPath;
                if (finalXmlFilePath != null)
                {
                    xml = OpenFile(finalXmlFilePath, consoleOutput, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    if (xml == null)
                    {
                        return Failed;
                    }

                    xml.SetLength(0);
                }

                cancellationToken.ThrowIfCancellationRequested();

                IEnumerable<DiagnosticInfo> errors;
                using (var win32Res = GetWin32Resources(Arguments, compilation, out errors))
                using (xml)
                {
                    if (ReportErrors(errors, consoleOutput, errorLogger))
                    {
                        return Failed;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    string outputName = GetOutputFileName(compilation, cancellationToken);

                    finalOutputPath = Path.Combine(Arguments.OutputDirectory, outputName);
                    finalPdbFilePath = Arguments.PdbPath ?? Path.ChangeExtension(finalOutputPath, ".pdb");

                    // NOTE: Unlike the PDB path, the XML doc path is not embedded in the assembly, so we don't need to pass it to emit.
                    var emitOptions = Arguments.EmitOptions.
                        WithOutputNameOverride(outputName).
                        WithPdbFilePath(finalPdbFilePath);

                    var pdbOutputInfo = Arguments.EmitPdb
                        ? new Cci.PdbOutputInfo(finalPdbFilePath)
                        : Cci.PdbOutputInfo.None;

                    using (var peStreamProvider = new CompilerEmitStreamProvider(this, touchedFilesLogger, finalOutputPath))
                    {
                        emitResult = compilation.Emit(
                            peStreamProvider,
                            pdbOutputInfo,
                            xml,
                            win32Res,
                            Arguments.ManifestResources,
                            emitOptions,
                            getAnalyzerDiagnostics,
                            cancellationToken);

                        if (emitResult.Success && !pdbOutputInfo.IsNone && touchedFilesLogger != null)
                        {
                            touchedFilesLogger.AddWritten(pdbOutputInfo.FileName);
                        }
                    }
                }

                GenerateSqmData(Arguments.CompilationOptions, emitResult.Diagnostics);

                if (ReportErrors(emitResult.Diagnostics, consoleOutput, errorLogger))
                {
                    return Failed;
                }

                cancellationToken.ThrowIfCancellationRequested();

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

                    var readStream = OpenFile(Arguments.TouchedFilesPath + ".read", consoleOutput, FileMode.OpenOrCreate);
                    if (readStream == null)
                    {
                        return Failed;
                    }

                    using (var writer = new StreamWriter(readStream))
                    {
                        touchedFilesLogger.WriteReadPaths(writer);
                    }

                    var writtenStream = OpenFile(Arguments.TouchedFilesPath + ".write", consoleOutput, FileMode.OpenOrCreate);
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
                }
            }

            return Succeeded;
        }

        private ImmutableArray<AdditionalTextFile> ResolveAdditionalFilesFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFilesLogger)
        {
            var builder = ImmutableArray.CreateBuilder<AdditionalTextFile>();

            foreach (var file in Arguments.AdditionalFiles)
            {
                builder.Add(new AdditionalTextFile(file, this));
            }

            return builder.ToImmutableArray();
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
                    sqm = SqmServiceProvider.TryGetSqmService();
                    if (sqm != null)
                    {
                        sqm.BeginSession(this.GetSqmAppID(), false, out sqmSession);
                        sqm.SetGlobalSessionGuid(Arguments.SqmSessionGuid);

                        // Build Version
                        Assembly thisAssembly = typeof(CommonCompiler).Assembly;
                        var fileVersion = FileVersionInfo.GetVersionInfo(thisAssembly.Location).FileVersion;
                        sqm.SetStringDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_BUILDVERSION, fileVersion);

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
        internal Func<string, FileMode, FileAccess, FileShare, FileStream> FileOpen
        {
            get { return _fileOpen ?? File.Open; }
            set { _fileOpen = value; }
        }
        private Func<string, FileMode, FileAccess, FileShare, FileStream> _fileOpen;

        private FileStream OpenFile(string filePath, TextWriter consoleOutput, FileMode mode = FileMode.Open, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None)
        {
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

        private static FileStream OpenManifestStream(CommonMessageProvider messageProvider, OutputKind outputKind, CommandLineArguments arguments, List<DiagnosticInfo> errorList)
        {
            return outputKind.IsNetModule()
                ? null
                : OpenStream(messageProvider, arguments.Win32Manifest, arguments.BaseDirectory, messageProvider.ERR_CantOpenWin32Manifest, errorList);
        }

        private static FileStream OpenStream(CommonMessageProvider messageProvider, string path, string baseDirectory, int errorCode, IList<DiagnosticInfo> errors)
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
                return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
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
        /// csi.exe and vbi.exe entry point.
        /// </summary>
        internal int RunInteractive(TextWriter consoleOutput)
        {
            ErrorLogger errorLogger = null;
            if (Arguments.ErrorLogPath != null)
            {
                errorLogger = GetErrorLogger(consoleOutput, CancellationToken.None);
                if (errorLogger == null)
                {
                    return Failed;
                }
            }

            using (errorLogger)
            {
                return RunInteractiveCore(consoleOutput, errorLogger);
            }
        }

        /// <summary>
        /// csi.exe and vbi.exe entry point.
        /// </summary>
        private int RunInteractiveCore(TextWriter consoleOutput, ErrorLogger errorLogger)
        {
            Debug.Assert(Arguments.IsInteractive);

            var hasScriptFiles = Arguments.SourceFiles.Any(file => file.IsScript);

            if (Arguments.DisplayLogo && !hasScriptFiles)
            {
                PrintLogo(consoleOutput);
            }

            if (Arguments.DisplayHelp)
            {
                PrintHelp(consoleOutput);
                return 0;
            }

            // TODO (tomat):
            // When we have command line REPL enabled we'll launch it if there are no input files. 
            IEnumerable<Diagnostic> errors = Arguments.Errors;
            if (!hasScriptFiles)
            {
                errors = errors.Concat(new[] { Diagnostic.Create(MessageProvider, MessageProvider.ERR_NoScriptsSpecified) });
            }

            if (ReportErrors(errors, consoleOutput, errorLogger))
            {
                return Failed;
            }

            // arguments are always available when executing script code:
            Debug.Assert(Arguments.ScriptArguments != null);

            var compilation = CreateCompilation(consoleOutput, touchedFilesLogger: null, errorLogger: errorLogger);
            if (compilation == null)
            {
                return Failed;
            }

            byte[] compiledAssembly;
            using (MemoryStream output = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(output);
                if (ReportErrors(emitResult.Diagnostics, consoleOutput, errorLogger))
                {
                    return Failed;
                }

                compiledAssembly = output.ToArray();
            }

            var assembly = Assembly.Load(compiledAssembly);

            return Execute(assembly, Arguments.ScriptArguments.ToArray());
        }

        private static int Execute(Assembly assembly, string[] scriptArguments)
        {
            var parameters = assembly.EntryPoint.GetParameters();
            object[] arguments;

            if (parameters.Length == 0)
            {
                arguments = SpecializedCollections.EmptyObjects;
            }
            else
            {
                Debug.Assert(parameters.Length == 1);
                arguments = new object[] { scriptArguments };
            }

            object result = assembly.EntryPoint.Invoke(null, arguments);
            return result is int ? (int)result : Succeeded;
        }

        /// <summary>
        ///   When overriden by a derived class, this property can override the current thread's
        ///   CurrentUICulture property for diagnostic message resource lookups.
        /// </summary>
        protected virtual CultureInfo Culture
        {
            get
            {
                return Arguments.PreferredUILang ?? CultureInfo.CurrentUICulture;
            }
        }
#if REPL

        // let the assembly loader know about location of all files referenced by the compilation:
        foreach (AssemblyIdentity reference in compilation.ReferencedAssemblyNames)
        {
            if (reference.Location != null)
            {
                assemblyLoader.RegisterDependency(reference);
            }
}


        private void RunInteractiveLoop()
        {
            ShowLogo();

            var interactiveParseOptions = arguments.ParseOptions.Copy(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Interactive);
            var engine = new Engine(referenceResolver: assemblyLoader.GetReferenceResolver());

            // TODO: parse options, references, ...

            Session session = Session.Create();
            ObjectFormatter formatter = new ObjectFormatter(maxLineLength: Console.BufferWidth, memberIndentation: "  ");

            while (true)
            {
                Console.Write("> ");
                var input = new StringBuilder();
                string line;

                while (true)
                {
                    line = Console.ReadLine();
                    if (line == null)
                    {
                        return;
                    }

                    input.AppendLine(line);
                    if (Syntax.IsCompleteSubmission(input.ToString(), interactiveParseOptions))
                    {
                        break;
                    }

                    Console.Write("| ");
                }

                Submission<object> submission;
                object result;
                try
                {
                    submission = Compile(engine, session, input.ToString());
                    result = submission.Execute();
                }
                catch (CompilationErrorException e)
                {
                    DisplayInteractiveErrors(e.Diagnostics);
                    continue;
                }
                catch (Exception e)
                {
                    // TODO (tomat): stack pretty printing
                    Console.WriteLine(e);
                    continue;
                }

                bool hasValue;
                ITypeSymbol resultType = submission.Compilation.GetSubmissionResultType(out hasValue);
                if (hasValue)
                {
                    if (resultType != null && resultType.SpecialType == SpecialType.System_Void)
                    {
                        Console.Out.WriteLine(formatter.VoidDisplayString);
                    }
                    else
                    {
                        Console.Out.WriteLine(formatter.FormatObject(result));
                    }
                }
            }
        }

        private Submission<object> Compile(Engine engine, Session session, string text)
        {
            Submission<object> submission = engine.CompileSubmission<object>(text, session);

            foreach (MetadataReference reference in submission.Compilation.GetDirectiveReferences())
            {
                assemblyLoader.LoadReference(reference);
            }

            return submission;
        }

        private static void DisplayInteractiveErrors(ImmutableArray<IDiagnostic> diagnostics)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            try
            {
                DisplayInteractiveErrors(diagnostics, Console.Out);
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }

        private static void DisplayInteractiveErrors(ImmutableArray<IDiagnostic> diagnostics, TextWriter output)
        {
            var displayedDiagnostics = new List<IDiagnostic>();
            const int MaxErrorCount = 5;
            for (int i = 0, n = Math.Min(diagnostics.Count, MaxErrorCount); i < n; i++)
            {
                displayedDiagnostics.Add(diagnostics[i]);
            }
            displayedDiagnostics.Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);

            foreach (var diagnostic in displayedDiagnostics)
            {
                output.WriteLine(diagnostic.ToString(Culture));
            }

            if (diagnostics.Count > MaxErrorCount)
            {
                int notShown = diagnostics.Count - MaxErrorCount;
                output.WriteLine(" + additional {0} {1}", notShown, (notShown == 1) ? "error" : "errors");
            }
        }
#endif
    }
}
