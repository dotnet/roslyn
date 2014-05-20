// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Instrumentation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CSharpCompiler : CommonCompiler
    {
        private static string responseFileName;
        private CommandLineDiagnosticFormatter diagnosticFormatter;

        protected CSharpCompiler(CSharpCommandLineParser parser, string responseFile, string[] args, string baseDirectory, string additionalReferencePaths)
            : base(parser, responseFile, args, baseDirectory, additionalReferencePaths)
        {
            this.diagnosticFormatter = new CommandLineDiagnosticFormatter(baseDirectory, Arguments.PrintFullPaths, Arguments.ShouldIncludeErrorEndLocation);
        }

        public override DiagnosticFormatter DiagnosticFormatter { get { return this.diagnosticFormatter; } }
        protected internal new CSharpCommandLineArguments Arguments { get { return (CSharpCommandLineArguments)base.Arguments; } }

        // Name of the response file to load by default (unless /noconfig is specified)
        protected static string CSharpResponseFileName
        {
            get
            {
                // TODO: Bug 872626 (angocke)
                // WORKAROUND: REMOVE BEFORE SHIPPING
                // First look for csc.rsp, then look for rcsc.rsp.
                if (String.IsNullOrEmpty(responseFileName))
                {
                    responseFileName = Path.Combine(ResponseFileDirectory, "csc.rsp");
                    if (!File.Exists(responseFileName))
                    {
                        responseFileName = Path.Combine(ResponseFileDirectory, "rcsc.rsp");
                    }
                }
                return responseFileName;
            }
        }

        public override int Run(TextWriter consoleOutput, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return base.Run(consoleOutput, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                DiagnosticInfo diag = new DiagnosticInfo(MessageProvider, (int)ErrorCode.ERR_CompileCancelled);
                PrintErrors(new[] { diag }, consoleOutput);
                return 1;
            }
        }

        protected override Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger)
        {
            var parseOptions = Arguments.ParseOptions;
            var scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script);

            bool hadErrors = false;

            var sourceFiles = Arguments.SourceFiles;
            var trees = new SyntaxTree[sourceFiles.Length];
            var normalizedFilePaths = new String[sourceFiles.Length];

            if (Arguments.CompilationOptions.ConcurrentBuild)
            {
                Parallel.For(0, sourceFiles.Length, i =>
                {
                    var file = sourceFiles[i];

                    //NOTE: order of trees is important!!
                    trees[i] = ParseFile(consoleOutput, parseOptions, scriptParseOptions, ref hadErrors, file, out normalizedFilePaths[i]);
                });
            }
            else
            {
                for (int i = 0; i < sourceFiles.Length; i++)
                {
                    var file = sourceFiles[i];

                    //NOTE: order of trees is important!!
                    trees[i] = ParseFile(consoleOutput, parseOptions, scriptParseOptions, ref hadErrors, file, out normalizedFilePaths[i]);
                }
            }

            // If errors had been reported in ParseFile, while trying to read files, then we should simply exit.
            if (hadErrors)
            {
                return null;
            }

            var diagnostics = new List<DiagnosticInfo>();

            var uniqueFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                var normalizedFilePath = normalizedFilePaths[i];
                Debug.Assert(normalizedFilePath != null);
                Debug.Assert(PathUtilities.IsAbsolute(normalizedFilePath));

                if (!uniqueFilePaths.Add(normalizedFilePath))
                {
                    // warning CS2002: Source file '{0}' specified multiple times
                    diagnostics.Add(new DiagnosticInfo(MessageProvider, (int)ErrorCode.WRN_FileAlreadyIncluded,
                        Arguments.PrintFullPaths ? normalizedFilePath : this.diagnosticFormatter.RelativizeNormalizedPath(normalizedFilePath)));

                    trees[i] = null;
                }
            }

            if (Arguments.TouchedFilesPath != null)
            {
                foreach (var path in uniqueFilePaths)
                {
                    touchedFilesLogger.AddRead(path);
                }
            }

            var assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            var appConfigPath = this.Arguments.AppConfigPath;
            if (appConfigPath != null)
            {
                try
                {
                    using (var appConfigStream = new FileStream(appConfigPath, FileMode.Open, FileAccess.Read))
                    {
                        assemblyIdentityComparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfigStream);
                    }

                    if (touchedFilesLogger != null)
                    {
                        touchedFilesLogger.AddRead(appConfigPath);
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.Add(new DiagnosticInfo(MessageProvider, (int)ErrorCode.ERR_CantReadConfigFile, appConfigPath, ex.Message));
                }
            }

            var metadataProvider = GetMetadataProvider();
            var xmlFileResolver = new LoggingXmlFileResolver(Arguments.BaseDirectory, touchedFilesLogger);
            var sourceFileResolver = new LoggingSourceFileResolver(ImmutableArray<string>.Empty, Arguments.BaseDirectory, touchedFilesLogger);

            var externalReferenceResolver = GetExternalMetadataResolver(touchedFilesLogger);
            MetadataReferenceResolver referenceDirectiveResolver;
            var resolvedReferences = ResolveMetadataReferences(externalReferenceResolver, metadataProvider, diagnostics, assemblyIdentityComparer, touchedFilesLogger, out referenceDirectiveResolver);
            if (PrintErrors(diagnostics, consoleOutput))    
            {
                return null;
            }

            var strongNameProvider = new LoggingStrongNameProvider(Arguments.KeyFileSearchPaths, touchedFilesLogger);

            var compilation = CSharpCompilation.Create(
                Arguments.CompilationName,
                trees.WhereNotNull(),
                resolvedReferences,
                Arguments.CompilationOptions.
                    WithMetadataReferenceResolver(referenceDirectiveResolver).
                    WithMetadataReferenceProvider(metadataProvider).
                    WithAssemblyIdentityComparer(assemblyIdentityComparer).
                    WithStrongNameProvider(strongNameProvider).
                    WithXmlReferenceResolver(xmlFileResolver).
                    WithSourceReferenceResolver(sourceFileResolver));

            // Print the diagnostics produced during the parsing stage and exit if there were any errors.
            if (PrintErrors(compilation.GetParseDiagnostics(), consoleOutput))
            {
                return null;
            }

            if (PrintErrors(compilation.GetDeclarationDiagnostics(), consoleOutput))
            {
                return null;
            }

            return compilation;
        }

        private SyntaxTree ParseFile(
            TextWriter consoleOutput,
            CSharpParseOptions parseOptions,
            CSharpParseOptions scriptParseOptions,
            ref bool hadErrors,
            CommandLineSourceFile file,
            out string normalizedFilePath)
        {
            var fileReadDiagnostics = new List<DiagnosticInfo>();
            var content = ReadFileContent(file, fileReadDiagnostics, Arguments.Encoding, out normalizedFilePath);

            if (content == null)
            {
                PrintErrors(fileReadDiagnostics, consoleOutput);
                fileReadDiagnostics.Clear();
                hadErrors = true;
                return null;
            }
            else
            {
                return ParseFile(parseOptions, scriptParseOptions, content, file);
            }
        }

        private static SyntaxTree ParseFile(
            CSharpParseOptions parseOptions, 
            CSharpParseOptions scriptParseOptions,
            SourceText content,
            CommandLineSourceFile file)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(
                content,
                file.IsScript ? scriptParseOptions : parseOptions,
                file.Path);

            // prepopulate line tables.
            // we will need line tables anyways and it is better to not wait until we are in emit
            // where things run sequentially.
            bool isHiddenDummy;
            tree.GetMappedLineSpanAndVisibility(default(TextSpan), out isHiddenDummy);

            return tree;
        }

        /// <summary>
        /// Given a compilation and a destination directory, determine three names:
        ///   1) The name with which the assembly should be output.
        ///   2) The path of the assembly/module file.
        ///   3) The path of the pdb file.
        /// 
        /// When csc produces an executable, but the name of the resulting assembly
        /// is not specified using the "/out" switch, the name is taken from the name
        /// of the file (note: file, not class) containing the assembly entrypoint
        /// (as determined by binding and the "/main" switch).
        /// 
        /// For example, if the command is "csc /target:exe a.cs b.cs" and b.cs contains the
        /// entrypoint, then csc will produce "b.exe" and "b.pdb" in the output directory,
        /// with assembly name "b" and module name "b.exe" embedded in the file.
        /// </summary>
        protected override string GetOutputFileName(Compilation compilation, CancellationToken cancellationToken)
        {
            if (Arguments.OutputFileName == null)
            {
                Debug.Assert(Arguments.CompilationOptions.OutputKind.IsApplication());

                ISymbol entryPoint = (ISymbol)compilation.ScriptClass ?? compilation.GetEntryPoint(cancellationToken);
                if (entryPoint != null)
                {
                    string entryPointFileName = PathUtilities.GetFileName(entryPoint.Locations.First().SourceTree.FilePath);
                    return Path.ChangeExtension(entryPointFileName, ".exe");
                }
                else
                {
                    // no entrypoint found - an error will be reported and the compilation won't be emitted
                    return "error";
                }
            }
            else
            {
                return base.GetOutputFileName(compilation, cancellationToken);
            }
        }

        internal override bool SuppressDefaultResponseFile(IEnumerable<string> args)
        {
            return args.Any(arg => new[] { "/noconfig", "-noconfig" }.Contains(arg.ToLowerInvariant()));
        }

        /// <summary>
        /// Print compiler logo
        /// </summary>
        /// <param name="consoleOutput"></param>
        protected override void PrintLogo(TextWriter consoleOutput)
        {
            Assembly thisAssembly = GetType().Assembly;
            consoleOutput.WriteLine(CSharpResources.LogoLine1, FileVersionInfo.GetVersionInfo(thisAssembly.Location).FileVersion);
            consoleOutput.WriteLine(CSharpResources.LogoLine2);
            consoleOutput.WriteLine();
        }

        /// <summary>
        /// Print Commandline help message (up to 80 English characters per line)
        /// </summary>
        /// <param name="consoleOutput"></param>
        protected override void PrintHelp(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(
@"                        Visual C# Compiler Options

                        - OUTPUT FILES -
/out:<file>                    Specify output file name (default: base name of 
                               file with main class or first file)
/target:exe                    Build a console executable (default) (Short 
                               form: /t:exe)
/target:winexe                 Build a Windows executable (Short form: 
                               /t:winexe)
/target:library                Build a library (Short form: /t:library)
/target:module                 Build a module that can be added to another 
                               assembly (Short form: /t:module)
/target:appcontainerexe        Build an Appcontainer executable (Short form: 
                               /t:appcontainerexe)
/target:winmdobj               Build a Windows Runtime intermediate file that 
                               is consumed by WinMDExp (Short form: /t:winmdobj)
/doc:<file>                    XML Documentation file to generate
/platform:<string>             Limit which platforms this code can run on: x86,
                               Itanium, x64, arm, anycpu32bitpreferred, or 
                               anycpu. The default is anycpu.

                        - INPUT FILES -
/recurse:<wildcard>            Include all files in the current directory and 
                               subdirectories according to the wildcard 
                               specifications
/reference:<alias>=<file>      Reference metadata from the specified assembly 
                               file using the given alias (Short form: /r)
/reference:<file list>         Reference metadata from the specified assembly 
                               files (Short form: /r)
/addmodule:<file list>         Link the specified modules into this assembly
/link:<file list>              Embed metadata from the specified interop 
                               assembly files (Short form: /l)
/analyzer:<file list>          Run the analyzers from this assembly
                               (Short form: /a)

                        - RESOURCES -
/win32res:<file>               Specify a Win32 resource file (.res)
/win32icon:<file>              Use this icon for the output
/win32manifest:<file>          Specify a Win32 manifest file (.xml)
/nowin32manifest               Do not include the default Win32 manifest
/resource:<resinfo>            Embed the specified resource (Short form: /res)
/linkresource:<resinfo>        Link the specified resource to this assembly 
                               (Short form: /linkres) Where the resinfo format 
                               is <file>[,<string name>[,public|private]]

                        - CODE GENERATION -
/debug[+|-]                    Emit debugging information
/debug:{full|pdbonly}          Specify debugging type ('full' is default, and 
                               enables attaching a debugger to a running 
                               program)
/optimize[+|-]                 Enable optimizations (Short form: /o)

                        - ERRORS AND WARNINGS -
/warnaserror[+|-]              Report all warnings as errors
/warnaserror[+|-]:<warn list>  Report specific warnings as errors
/warn:<n>                      Set warning level (0-4) (Short form: /w)
/nowarn:<warn list>            Disable specific warning messages
/ruleset:<file>                Specify a ruleset file that disables specific
                               diagnostics.

                        - LANGUAGE -
/checked[+|-]                  Generate overflow checks
/unsafe[+|-]                   Allow 'unsafe' code
/define:<symbol list>          Define conditional compilation symbol(s) (Short 
                               form: /d)
/langversion:<string>          Specify language version mode: ISO-1, ISO-2, 3, 
                               4, 5, 6, or Default

                        - SECURITY -
/delaysign[+|-]                Delay-sign the assembly using only the public 
                               portion of the strong name key
/keyfile:<file>                Specify a strong name key file
/keycontainer:<string>         Specify a strong name key container
/highentropyva[+|-]            Enable high-entropy ASLR

                        - MISCELLANEOUS -
@<file>                        Read response file for more options
/help                          Display this usage message (Short form: /?)
/nologo                        Suppress compiler copyright message
/noconfig                      Do not auto include CSC.RSP file
/parallel[+|-]                 Concurrent build. 

                        - ADVANCED -
/baseaddress:<address>         Base address for the library to be built
/bugreport:<file>              Create a 'Bug Report' file
/codepage:<n>                  Specify the codepage to use when opening source 
                               files
/utf8output                    Output compiler messages in UTF-8 encoding
/main:<type>                   Specify the type that contains the entry point 
                               (ignore all other possible entry points) (Short 
                               form: /m)
/fullpaths                     Compiler generates fully qualified paths
/filealign:<n>                 Specify the alignment used for output file 
                               sections
/pdb:<file>                    Specify debug information file name (default: 
                               output file name with .pdb extension)
/errorendlocation              Output line and column of the end location of 
                               each error
/preferreduilang               Specify the preferred output language name.
/nostdlib[+|-]                 Do not reference standard library (mscorlib.dll)
/subsystemversion:<string>     Specify subsystem version of this assembly
/lib:<file list>               Specify additional directories to search in for 
                               references
/errorreport:<string>          Specify how to handle internal compiler errors: 
                               prompt, send, queue, or none. The default is 
                               queue.
/appconfig:<file>              Specify an application configuration file 
                               containing assembly binding settings
/moduleassemblyname:<string>   Name of the assembly which this module will be 
                               a part of
");
        }

    }
}
