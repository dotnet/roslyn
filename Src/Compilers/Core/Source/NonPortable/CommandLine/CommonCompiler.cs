// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Instrumentation;
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
        protected const int Failed = 1;
        protected const int Succeeded = 0;
        private readonly ObjectPool<MemoryStream> memoryStreamPool = new ObjectPool<MemoryStream>(() => new MemoryStream(), 4);

        protected static string ResponseFileDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_responseFileDirectory))
                {
                    _responseFileDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                }
                return _responseFileDirectory;
            }
        }
        private static string _responseFileDirectory = null;


        public CommonMessageProvider MessageProvider { get; private set; }
        public CommandLineArguments Arguments { get; private set; }
        public abstract DiagnosticFormatter DiagnosticFormatter { get; }
        private readonly HashSet<Diagnostic> reportedDiagnostics = new HashSet<Diagnostic>();

        protected abstract Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger);
        protected abstract void PrintLogo(TextWriter consoleOutput);
        protected abstract void PrintHelp(TextWriter consoleOutput);
        protected abstract uint GetSqmAppID();
        protected abstract void CompilerSpecificSqm(IVsSqmMulti sqm, uint sqmSession);

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
            MetadataReferenceResolver externalReferenceResolver,
            MetadataReferenceProvider metadataProvider,
            List<DiagnosticInfo> diagnostics,
            AssemblyIdentityComparer assemblyIdentityComparer,
            TouchedFileLogger touchedFiles,
            out MetadataReferenceResolver referenceDirectiveResolver)
        {
            using (Logger.LogBlock(FunctionId.Common_CommandLineCompiler_ResolveMetadataReferences))
            {
                List<MetadataReference> resolved = new List<MetadataReference>();
                ResolveMetadataReferencesFromArguments(externalReferenceResolver, metadataProvider, diagnostics, resolved);

                if (Arguments.IsInteractive)
                {
                    referenceDirectiveResolver = externalReferenceResolver;
                }
                else
                {
                    // when compiling into an assembly (csc/vbc) we only allow #r that match references given on command line:
                    referenceDirectiveResolver = new ExistingReferencesResolver(
                        resolved.Where(r => r.Properties.Kind == MetadataImageKind.Assembly).OfType<MetadataFileReference>().AsImmutable(),
                        Arguments.ReferencePaths,
                        Arguments.BaseDirectory,
                        assemblyIdentityComparer,
                        touchedFiles);
                }

                return resolved;
            }
        }

        /// <summary>
        /// Returns false if there were unresolved references in arguments, true otherwise.
        /// </summary>
        protected virtual bool ResolveMetadataReferencesFromArguments(MetadataReferenceResolver externalReferenceResolver, MetadataReferenceProvider metadataProvider, List<DiagnosticInfo> diagnostics, List<MetadataReference> resolved)
        {
            bool result = true;

            foreach (var reference in Arguments.ResolveMetadataReferences(externalReferenceResolver, metadataProvider, diagnostics, MessageProvider))
            {
                if (!reference.IsUnresolved)
                {
                    resolved.Add(reference);
                }
                else
                {
                    result = false;
                    Debug.Assert(diagnostics.Any());
                }
            }

            return result;
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <param name="encoding">Encoding to use or 'null' for autodetect/default</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText ReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics, Encoding encoding)
        {
            string discarded;
            return ReadFileContent(file, diagnostics, encoding, out discarded);
        }

        /// <summary>
        /// Reads content of a source file.
        /// </summary>
        /// <param name="file">Source file information.</param>
        /// <param name="diagnostics">Storage for diagnostics.</param>
        /// <param name="encoding">Encoding to use or 'null' for autodetect/default</param>
        /// <param name="normalizedFilePath">If given <paramref name="file"/> opens successfully, set to normalized absolute path of the file, null otherwise.</param>
        /// <returns>File content or null on failure.</returns>
        internal SourceText ReadFileContent(CommandLineSourceFile file, IList<DiagnosticInfo> diagnostics, Encoding encoding, out string normalizedFilePath)
        {
            try
            {
                using (var data = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    normalizedFilePath = data.Name;
                    return EncodedStringText.Create(data, encoding);
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(ToFileReadDiagostics(e, file));
                normalizedFilePath = null;
                return null;
            }
        }

        private DiagnosticInfo ToFileReadDiagostics(Exception e, CommandLineSourceFile file)
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


        internal bool PrintErrors(IEnumerable<Diagnostic> diagnostics, TextWriter consoleOutput)
        {
            bool hasErrors = false;
            foreach (var diag in diagnostics)
            {
                if (reportedDiagnostics.Contains(diag))
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
                else if (diag.Severity == DiagnosticSeverity.Info)
                {
                    // Not reported from the command-line compiler.
                    continue;
                }

                consoleOutput.WriteLine(DiagnosticFormatter.Format(diag, this.Culture));

                if (diag.Severity == DiagnosticSeverity.Error || diag.IsWarningAsError)
                {
                    hasErrors = true;
                }

                reportedDiagnostics.Add(diag);
            }

            return hasErrors;
        }

        internal bool PrintErrors(IEnumerable<DiagnosticInfo> diagnostics, TextWriter consoleOutput)
        {
            bool hasErrors = false;
            if (diagnostics != null && diagnostics.Any())
            {
                foreach (var diagnostic in diagnostics)
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Info)
                    {
                        // Not reported from the command-line compiler.
                        continue;
                    }

                    PrintError(diagnostic, consoleOutput);
                    if (diagnostic.Severity == DiagnosticSeverity.Error || diagnostic.IsWarningAsError)
                    {
                        hasErrors = true;
                    }
                }
            }

            return hasErrors;
        }

        internal virtual void PrintError(DiagnosticInfo diagnostic, TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(diagnostic.ToString(Culture));
        }

        /// <summary>
        /// csc.exe and vbc.exe entry point.
        /// </summary>
        public virtual int Run(TextWriter consoleOutput, CancellationToken cancellationToken)
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

            if (PrintErrors(Arguments.Errors, consoleOutput))
            {
                return Failed;
            }

            var touchedFilesLogger = (Arguments.TouchedFilesPath != null) ? new TouchedFileLogger() : null;

            Compilation compilation = CreateCompilation(consoleOutput, touchedFilesLogger);
            if (compilation == null)
            {
                return Failed;
            }

            var diagnostics = new List<DiagnosticInfo>();
            var analyzers = Arguments.ResolveAnalyzersFromArguments(diagnostics, MessageProvider, touchedFilesLogger);
            if (PrintErrors(diagnostics, consoleOutput))
            {
                return Failed;
            }

            cancellationToken.ThrowIfCancellationRequested();

            EmitResult emitResult;

            // EDMAURER: Don't yet know if there are method body errors. don't overwrite
            // any existing output files until the compilation is known to be successful.
            string tempExeFilename = null;
            string tempPdbFilename = null;

            // NOTE: as native compiler does, we generate the documentation file
            // NOTE: 'in place', replacing the contents of the file if it exists

            try
            {
                tempExeFilename = CreateTempFile(consoleOutput);

                // Can happen when temp directory is "full"
                if (tempExeFilename == null)
                {
                    return Failed;
                }

                FileStream output = OpenFile(tempExeFilename, consoleOutput);
                if (output == null)
                {
                    return Failed;
                }

                string finalOutputPath;
                string finalPdbFilePath;
                string finalXmlFilePath;

                using (output)
                {
                    FileStream pdb = null;
                    FileStream xml = null;

                    cancellationToken.ThrowIfCancellationRequested();

                    if (Arguments.CompilationOptions.DebugInformationKind != DebugInformationKind.None)
                    {
                        tempPdbFilename = CreateTempFile(consoleOutput);

                        if (tempPdbFilename == null)
                        {
                            return Failed;
                        }

                        pdb = OpenFile(tempPdbFilename, consoleOutput);
                        if (pdb == null)
                        {
                            return Failed;
                        }
                    }

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
                    using (pdb)
                    using (xml)
                    {
                        if (PrintErrors(errors, consoleOutput))
                        {
                            return Failed;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        string outputName = GetOutputFileName(compilation, cancellationToken);

                        finalOutputPath = Path.Combine(Arguments.OutputDirectory, outputName);
                        finalPdbFilePath = Arguments.PdbPath ?? Path.ChangeExtension(finalOutputPath, ".pdb");

                        // NOTE: Unlike the PDB path, the XML doc path is not embedded in the assembly, so we don't need to pass it to emit.
                        emitResult = compilation.Emit(output, outputName, finalPdbFilePath, pdb, xml, cancellationToken, win32Res, Arguments.ManifestResources);
                    }
                }

                GenerateSqmData(Arguments.CompilationOptions, emitResult.Diagnostics);

                if (PrintErrors(emitResult.Diagnostics, consoleOutput))
                {
                    return Failed;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var analyzerDiagnostics = AnalyzerDriver.GetDiagnostics(compilation, analyzers, default(CancellationToken));
                if (PrintErrors(analyzerDiagnostics, consoleOutput))
                {
                    return Failed;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (!TryDeleteFile(finalOutputPath, consoleOutput) || !TryMoveFile(tempExeFilename, finalOutputPath, consoleOutput))
                {
                    return Failed;
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (tempPdbFilename != null)
                {
                    if (!TryDeleteFile(finalPdbFilePath, consoleOutput) || !TryMoveFile(tempPdbFilename, finalPdbFilePath, consoleOutput))
                    {
                        return Failed;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (Arguments.TouchedFilesPath != null)
                {
                    Debug.Assert(touchedFilesLogger != null);

                    touchedFilesLogger.AddWritten(tempExeFilename);
                    touchedFilesLogger.AddWritten(finalOutputPath);
                    if (tempPdbFilename != null)
                    {
                        touchedFilesLogger.AddWritten(tempPdbFilename);
                        touchedFilesLogger.AddWritten(finalPdbFilePath);
                    }
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


                return Succeeded;
            }
            finally
            {
                if (tempExeFilename != null)
                {
                    TryDeleteFile(tempExeFilename, consoleOutput: null);
                }

                if (tempPdbFilename != null)
                {
                    TryDeleteFile(tempPdbFilename, consoleOutput: null);
                }
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
                                if (diagnostic.IsWarningAsError)
                                {
                                    sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_ERRORNUMBERS, (uint)diagnostic.Code);
                                }
                                else
                                {
                                    sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNINGNUMBERS, (uint)diagnostic.Code);
                                }
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
                            uint code = uint.Parse(item.Key.Substring(2));
                            ReportDiagnostic options = item.Value;
                            switch (options)
                            {
                            case ReportDiagnostic.Suppress:
                                sqm.AddItemToStream(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_SUPPRESSWARNINGNUMBERS, code);      // Supress warning
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
            get { return fileOpen ?? File.Open; }
            set { fileOpen = value; }
        }
        private Func<string, FileMode, FileAccess, FileShare, FileStream> fileOpen;

        /// <summary>
        /// Test hook for intercepting File.Delete.
        /// </summary>
        internal Action<string> FileDelete
        {
            get { return fileDelete ?? File.Delete; }
            set { fileDelete = value; }
        }
        private Action<string> fileDelete;

        /// <summary>
        /// Test hook for intercepting File.Move.
        /// </summary>
        internal Action<string, string> FileMove
        {
            get { return fileMove ?? File.Move; }
            set { fileMove = value; }
        }
        private Action<string, string> fileMove;

        /// <summary>
        /// Test hook for intercepting Path.GetTempFileName.
        /// </summary>
        internal Func<string> PathGetTempFileName
        {
            get { return pathGetTempFileName ?? Path.GetTempFileName; }
            set { pathGetTempFileName = value; }
        }
        private Func<string> pathGetTempFileName;

        private string CreateTempFile(TextWriter consoleOutput)
        {
            string result = null;

            // now catching in response to watson bucket 148019219
            try
            {
                result = PathGetTempFileName();
            }
            catch (IOException ex)
            {
                if (consoleOutput != null)
                {
                    DiagnosticInfo diagnosticInfo = new DiagnosticInfo(MessageProvider, (int)MessageProvider.ERR_FailedToCreateTempFile, ex.Message);
                    consoleOutput.WriteLine(diagnosticInfo.ToString(Culture));
                }
            }

            return result;
        }

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

        private bool TryDeleteFile(string filePath, TextWriter consoleOutput)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    FileDelete(filePath);
                }

                return true;
            }
            catch (Exception e)
            {
                // Treat all possible exceptions uniformly, so we report 
                // "Could not write to output file"/"can't open '***' for writing" 
                // for all as in the native CS/VB compiler.

                if (consoleOutput != null)
                {
                    DiagnosticInfo diagnosticInfo = new DiagnosticInfo(MessageProvider, (int)MessageProvider.ERR_OutputWriteFailed, filePath, e.Message);
                    consoleOutput.WriteLine(diagnosticInfo.ToString(Culture));
                }

                return false;
            }
        }

        private bool TryMoveFile(string sourcePath, string destinationPath, TextWriter consoleOutput)
        {
            try
            {
                FileMove(sourcePath, destinationPath);

                return true;
            }
            catch (Exception e)
            {
                // There can be various exceptions caught here including:
                //  - DirectoryNotFoundException when a given path is not found
                //  - IOException when a device like a:\ is not ready
                //  - UnauthorizedAccessException when a given path is not accessible 
                //  - NotSupportedException when a given path is in an invalid format
                //
                // Treat them uniformly, so we report "Cannot open 'filename' for writing" for all as in the native VB compiler.

                if (consoleOutput != null)
                {
                    DiagnosticInfo diagnosticInfo = new DiagnosticInfo(MessageProvider, (int)MessageProvider.ERR_CantOpenFileWrite, destinationPath, e.Message);
                    consoleOutput.WriteLine(diagnosticInfo.ToString(Culture));
                }

                return false;
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

        /// <summary>
        /// csi.exe and vbi.exe entry point.
        /// </summary>
        internal int RunInteractive(TextWriter consoleOutput)
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

            if (PrintErrors(errors, consoleOutput))
            {
                return Failed;
            }

            // arguments are always available when executing script code:
            Debug.Assert(Arguments.ScriptArguments != null);

            var compilation = CreateCompilation(consoleOutput, touchedFilesLogger: null);
            if (compilation == null)
            {
                return Failed;
            }

            byte[] compiledAssembly;
            using (MemoryStream output = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(output);
                if (PrintErrors(emitResult.Diagnostics, consoleOutput))
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
                arguments = new object[0];
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
       ...

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
