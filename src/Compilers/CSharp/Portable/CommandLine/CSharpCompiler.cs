// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CSharpCompiler : CommonCompiler
    {
        internal const string ResponseFileName = "csc.rsp";

        private readonly CommandLineDiagnosticFormatter _diagnosticFormatter;

        protected CSharpCompiler(CSharpCommandLineParser parser, string responseFile, string[] args, string clientDirectory, string baseDirectory, string sdkDirectoryOpt, string additionalReferenceDirectories, IAnalyzerAssemblyLoader analyzerLoader)
            : base(parser, responseFile, args, clientDirectory, baseDirectory, sdkDirectoryOpt, additionalReferenceDirectories, analyzerLoader)
        {
            _diagnosticFormatter = new CommandLineDiagnosticFormatter(baseDirectory, Arguments.PrintFullPaths, Arguments.ShouldIncludeErrorEndLocation);
        }

        public override DiagnosticFormatter DiagnosticFormatter { get { return _diagnosticFormatter; } }
        protected internal new CSharpCommandLineArguments Arguments { get { return (CSharpCommandLineArguments)base.Arguments; } }

        public override Compilation CreateCompilation(TextWriter consoleOutput, TouchedFileLogger touchedFilesLogger, ErrorLogger errorLogger)
        {
            var parseOptions = Arguments.ParseOptions;

            // We compute script parse options once so we don't have to do it repeatedly in
            // case there are many script files.
            var scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script);

            bool hadErrors = false;

            var sourceFiles = Arguments.SourceFiles;
            var trees = new SyntaxTree[sourceFiles.Length];
            var normalizedFilePaths = new String[sourceFiles.Length];

            if (Arguments.CompilationOptions.ConcurrentBuild)
            {
                Parallel.For(0, sourceFiles.Length, UICultureUtilities.WithCurrentUICulture<int>(i =>
                {
                    //NOTE: order of trees is important!!
                    trees[i] = ParseFile(consoleOutput, parseOptions, scriptParseOptions, ref hadErrors, sourceFiles[i], errorLogger, out normalizedFilePaths[i]);
                }));
            }
            else
            {
                for (int i = 0; i < sourceFiles.Length; i++)
                {
                    //NOTE: order of trees is important!!
                    trees[i] = ParseFile(consoleOutput, parseOptions, scriptParseOptions, ref hadErrors, sourceFiles[i], errorLogger, out normalizedFilePaths[i]);
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
                        Arguments.PrintFullPaths ? normalizedFilePath : _diagnosticFormatter.RelativizeNormalizedPath(normalizedFilePath)));

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
                    using (var appConfigStream = PortableShim.FileStream.Create(appConfigPath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read))
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

            var xmlFileResolver = new LoggingXmlFileResolver(Arguments.BaseDirectory, touchedFilesLogger);
            var sourceFileResolver = new LoggingSourceFileResolver(ImmutableArray<string>.Empty, Arguments.BaseDirectory, Arguments.PathMap, touchedFilesLogger);

            MetadataReferenceResolver referenceDirectiveResolver;
            var resolvedReferences = ResolveMetadataReferences(diagnostics, touchedFilesLogger, out referenceDirectiveResolver);
            if (ReportErrors(diagnostics, consoleOutput, errorLogger))
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
                    WithAssemblyIdentityComparer(assemblyIdentityComparer).
                    WithStrongNameProvider(strongNameProvider).
                    WithXmlReferenceResolver(xmlFileResolver).
                    WithSourceReferenceResolver(sourceFileResolver));

            return compilation;
        }

        private SyntaxTree ParseFile(
            TextWriter consoleOutput,
            CSharpParseOptions parseOptions,
            CSharpParseOptions scriptParseOptions,
            ref bool hadErrors,
            CommandLineSourceFile file,
            ErrorLogger errorLogger,
            out string normalizedFilePath)
        {
            var fileReadDiagnostics = new List<DiagnosticInfo>();
            var content = ReadFileContent(file, fileReadDiagnostics, out normalizedFilePath);

            if (content == null)
            {
                ReportErrors(fileReadDiagnostics, consoleOutput, errorLogger);
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

                var comp = (CSharpCompilation)compilation;

                Symbol entryPoint = comp.ScriptClass;
                if ((object)entryPoint == null)
                {
                    var method = comp.GetEntryPoint(cancellationToken);
                    if ((object)method != null)
                    {
                        entryPoint = method.PartialImplementationPart ?? method;
                    }
                }

                if ((object)entryPoint != null)
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
        public override void PrintLogo(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(ErrorFacts.GetMessage(MessageID.IDS_LogoLine1, Culture), GetToolName(), GetAssemblyFileVersion());
            consoleOutput.WriteLine(ErrorFacts.GetMessage(MessageID.IDS_LogoLine2, Culture));
            consoleOutput.WriteLine();
        }

        internal override string GetToolName()
        {
            return ErrorFacts.GetMessage(MessageID.IDS_ToolName, Culture);
        }

        /// <summary>
        /// Print Commandline help message (up to 80 English characters per line)
        /// </summary>
        /// <param name="consoleOutput"></param>
        public override void PrintHelp(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(ErrorFacts.GetMessage(MessageID.IDS_CSCHelp, Culture));
        }

        protected override bool TryGetCompilerDiagnosticCode(string diagnosticId, out uint code)
        {
            return CommonCompiler.TryGetCompilerDiagnosticCode(diagnosticId, "CS", out code);
        }

        protected override ImmutableArray<DiagnosticAnalyzer> ResolveAnalyzersFromArguments(List<DiagnosticInfo> diagnostics, CommonMessageProvider messageProvider, TouchedFileLogger touchedFiles)
        {
            return Arguments.ResolveAnalyzersFromArguments(LanguageNames.CSharp, diagnostics, messageProvider, touchedFiles, AnalyzerLoader);
        }
    }
}
