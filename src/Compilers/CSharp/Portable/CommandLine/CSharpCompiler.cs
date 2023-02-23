// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using Metalama.Compiler;
using Metalama.Compiler.Interface.TypeForwards;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Telemetry;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class MetalamaInitializer
    {
        // This method exists so that the caller can be debugged even if the JIT compilation of the caller fails.
        public static void Initialize()
        {
            // Ensure that our Metalama.Compiler.Interfaces (the one with type forwarders) get loaded first, and not the user-facing one, which
            // is a reference assembly.
            MetalamaCompilerInterfaces.Initialize();
        }
    }
    
    internal abstract class CSharpCompiler : CommonCompiler
    {
        internal const string ResponseFileName = "csc.rsp";

        private readonly CommandLineDiagnosticFormatter _diagnosticFormatter;
        private readonly string? _tempDirectory;

        // <Metalama>
        static CSharpCompiler()
        {
            // Debugger.Launch();

            // Ensure that our Metalama.Compiler.Interfaces (the one with type forwarders) get loaded first, and not the user-facing one, which
            // is a reference assembly.
            MetalamaInitializer.Initialize();
        }
        // </Metalama>

        protected CSharpCompiler(CSharpCommandLineParser parser, string? responseFile, string[] args, BuildPaths buildPaths, string? additionalReferenceDirectories, IAnalyzerAssemblyLoader assemblyLoader, GeneratorDriverCache? driverCache = null, ICommonCompilerFileSystem? fileSystem = null)
            : base(parser, responseFile, args, buildPaths, additionalReferenceDirectories, assemblyLoader, driverCache, fileSystem)
        {
            _diagnosticFormatter = new CommandLineDiagnosticFormatter(buildPaths.WorkingDirectory, Arguments.PrintFullPaths, Arguments.ShouldIncludeErrorEndLocation);
            _tempDirectory = buildPaths.TempDirectory;
        }


        public override DiagnosticFormatter DiagnosticFormatter { get { return _diagnosticFormatter; } }
        protected internal new CSharpCommandLineArguments Arguments { get { return (CSharpCommandLineArguments)base.Arguments; } }

        public override Compilation? CreateCompilation(
            TextWriter consoleOutput,
            TouchedFileLogger? touchedFilesLogger,
            ErrorLogger? errorLogger,
            ImmutableArray<AnalyzerConfigOptionsResult> analyzerConfigOptions,
            AnalyzerConfigOptionsResult globalConfigOptions)
        {
            var parseOptions = Arguments.ParseOptions;

            // We compute script parse options once so we don't have to do it repeatedly in
            // case there are many script files.
            var scriptParseOptions = parseOptions.WithKind(SourceCodeKind.Script);

            bool hadErrors = false;

            var sourceFiles = Arguments.SourceFiles;
            var trees = new SyntaxTree?[sourceFiles.Length];
            var normalizedFilePaths = new string?[sourceFiles.Length];
            var diagnosticBag = DiagnosticBag.GetInstance();

            if (Arguments.CompilationOptions.ConcurrentBuild)
            {
                RoslynParallel.For(
                    0,
                    sourceFiles.Length,
                    UICultureUtilities.WithCurrentUICulture<int>(i =>
                    {
                        //NOTE: order of trees is important!!
                        trees[i] = ParseFile(
                            parseOptions,
                            scriptParseOptions,
                            ref hadErrors,
                            sourceFiles[i],
                            diagnosticBag,
                            out normalizedFilePaths[i]);
                    }),
                    CancellationToken.None);
            }
            else
            {
                for (int i = 0; i < sourceFiles.Length; i++)
                {
                    //NOTE: order of trees is important!!
                    trees[i] = ParseFile(
                        parseOptions,
                        scriptParseOptions,
                        ref hadErrors,
                        sourceFiles[i],
                        diagnosticBag,
                        out normalizedFilePaths[i]);
                }
            }

            // If errors had been reported in ParseFile, while trying to read files, then we should simply exit.
            if (ReportDiagnostics(diagnosticBag.ToReadOnlyAndFree(), consoleOutput, errorLogger, compilation: null))
            {
                Debug.Assert(hadErrors);
                return null;
            }

            var diagnostics = new List<DiagnosticInfo>();
            var uniqueFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < sourceFiles.Length; i++)
            {
                var normalizedFilePath = normalizedFilePaths[i];
                Debug.Assert(normalizedFilePath != null);
                Debug.Assert(sourceFiles[i].IsInputRedirected || PathUtilities.IsAbsolute(normalizedFilePath));

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
                Debug.Assert(touchedFilesLogger is object);
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

            var xmlFileResolver = new LoggingXmlFileResolver(Arguments.BaseDirectory, touchedFilesLogger);
            var sourceFileResolver = new LoggingSourceFileResolver(ImmutableArray<string>.Empty, Arguments.BaseDirectory, Arguments.PathMap, touchedFilesLogger);

            MetadataReferenceResolver referenceDirectiveResolver;
            var resolvedReferences = ResolveMetadataReferences(diagnostics, touchedFilesLogger, out referenceDirectiveResolver);
            if (ReportDiagnostics(diagnostics, consoleOutput, errorLogger, compilation: null))
            {
                return null;
            }

            var loggingFileSystem = new LoggingStrongNameFileSystem(touchedFilesLogger, _tempDirectory);
            var optionsProvider = new CompilerSyntaxTreeOptionsProvider(trees, analyzerConfigOptions, globalConfigOptions);

            return CSharpCompilation.Create(
                Arguments.CompilationName,
                trees.WhereNotNull(),
                resolvedReferences,
                Arguments.CompilationOptions
                    .WithMetadataReferenceResolver(referenceDirectiveResolver)
                    .WithAssemblyIdentityComparer(assemblyIdentityComparer)
                    .WithXmlReferenceResolver(xmlFileResolver)
                    .WithStrongNameProvider(Arguments.GetStrongNameProvider(loggingFileSystem))
                    .WithSourceReferenceResolver(sourceFileResolver)
                    .WithSyntaxTreeOptionsProvider(optionsProvider));
        }

        private SyntaxTree? ParseFile(
            CSharpParseOptions parseOptions,
            CSharpParseOptions scriptParseOptions,
            ref bool addedDiagnostics,
            CommandLineSourceFile file,
            DiagnosticBag diagnostics,
            out string? normalizedFilePath)
        {
            var fileDiagnostics = new List<DiagnosticInfo>();
            var content = TryReadFileContent(file, fileDiagnostics, out normalizedFilePath);

            if (content == null)
            {
                foreach (var info in fileDiagnostics)
                {
                    diagnostics.Add(MessageProvider.CreateDiagnostic(info));
                }
                fileDiagnostics.Clear();
                addedDiagnostics = true;
                return null;
            }
            else
            {
                Debug.Assert(fileDiagnostics.Count == 0);
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
            if (Arguments.OutputFileName is object)
            {
                return Arguments.OutputFileName;
            }

            Debug.Assert(Arguments.CompilationOptions.OutputKind.IsApplication());

            var comp = (CSharpCompilation)compilation;

            Symbol? entryPoint = comp.ScriptClass;
            if (entryPoint is null)
            {
                var method = comp.GetEntryPoint(cancellationToken);
                if (method is object)
                {
                    entryPoint = method.PartialImplementationPart ?? method;
                }
                else
                {
                    // no entrypoint found - an error will be reported and the compilation won't be emitted
                    return "error";
                }
            }

            string entryPointFileName = PathUtilities.GetFileName(entryPoint.Locations.First().SourceTree!.FilePath);
            return Path.ChangeExtension(entryPointFileName, ".exe");
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
            consoleOutput.WriteLine(ErrorFacts.GetMessage(MessageID.IDS_LogoLine1, Culture), GetToolName(), GetCompilerVersion());
            consoleOutput.WriteLine(ErrorFacts.GetMessage(MessageID.IDS_LogoLine2, Culture));
            consoleOutput.WriteLine();
            // <Metalama> Print out copyright line
            consoleOutput.WriteLine(ErrorFacts.GetMessage(MessageID.IDS_LogoLine3, Culture));
            consoleOutput.WriteLine();
            // </Metalama>
        }

        public override void PrintLangVersions(TextWriter consoleOutput)
        {
            consoleOutput.WriteLine(ErrorFacts.GetMessage(MessageID.IDS_LangVersions, Culture));
            var defaultVersion = LanguageVersion.Default.MapSpecifiedToEffectiveVersion();
            var latestVersion = LanguageVersion.Latest.MapSpecifiedToEffectiveVersion();
            foreach (var v in (LanguageVersion[])Enum.GetValues(typeof(LanguageVersion)))
            {
                if (v == defaultVersion)
                {
                    consoleOutput.WriteLine($"{v.ToDisplayString()} (default)");
                }
                else if (v == latestVersion)
                {
                    consoleOutput.WriteLine($"{v.ToDisplayString()} (latest)");
                }
                else
                {
                    consoleOutput.WriteLine(v.ToDisplayString());
                }
            }
            consoleOutput.WriteLine();
        }

        internal override Type Type
        {
            get
            {
                // We do not use this.GetType() so that we don't break mock subtypes
                return typeof(CSharpCompiler);
            }
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

        protected override void ResolveAnalyzersFromArguments(
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider,
            bool skipAnalyzers,
            // <Metalama>
            ImmutableArray<string?> transformerOrder,
            // </Metalama>
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators,
            // <Metalama>
            out ImmutableArray<ISourceTransformer> transformers,
            out ImmutableArray<object> plugins
            // </Metalama>
            )
        {
            // <Metalama>
            Arguments.ResolveAnalyzersFromArguments(LanguageNames.CSharp, diagnostics, messageProvider, AssemblyLoader, skipAnalyzers, transformerOrder, out analyzers, out generators, out transformers, out plugins);
            // </Metalama>
        }

        protected override void ResolveEmbeddedFilesFromExternalSourceDirectives(
            SyntaxTree tree,
            SourceReferenceResolver resolver,
            OrderedSet<string> embeddedFiles,
            DiagnosticBag diagnostics)
        {
            foreach (LineDirectiveTriviaSyntax directive in tree.GetRoot().GetDirectives(
                d => d.IsActive && !d.HasErrors && d.Kind() == SyntaxKind.LineDirectiveTrivia))
            {
                var path = (string?)directive.File.Value;
                if (path == null)
                {
                    continue;
                }

                string? resolvedPath = resolver.ResolveReference(path, tree.FilePath);
                if (resolvedPath == null)
                {
                    diagnostics.Add(
                        MessageProvider.CreateDiagnostic(
                            (int)ErrorCode.ERR_NoSourceFile,
                            directive.File.GetLocation(),
                            path,
                            CSharpResources.CouldNotFindFile));

                    continue;
                }

                embeddedFiles.Add(resolvedPath);
            }
        }

        private protected override GeneratorDriver CreateGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, ImmutableArray<AdditionalText> additionalTexts)
        {
            return CSharpGeneratorDriver.Create(generators, additionalTexts, (CSharpParseOptions)parseOptions, analyzerConfigOptionsProvider);
        }

        // <Metalama>

        private protected override TransformersResult RunTransformers(
            Compilation inputCompilation, IServiceProvider serviceProvider, ImmutableArray<ISourceTransformer> transformers, SourceOnlyAnalyzersOptions sourceOnlyAnalyzersOptions,
            ImmutableArray<object> plugins, AnalyzerConfigOptionsProvider analyzerConfigProvider, TransformerOptions transformerOptions, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            // If there are no transformers, don't do anything, not even annotate.
            if (transformers.IsEmpty)
            {
                return TransformersResult.Empty(inputCompilation, analyzerConfigProvider);
            }

            // Enforce licensing.
            var licenseManager = serviceProvider.GetService<ILicenseConsumptionService>();
            if (licenseManager != null)
            {
                if (!licenseManager.CanConsume(LicenseRequirement.Free))
                {
                    // We only emit the generic invalid license error when no specific error message
                    // comes from the license manager to avoid confusion of users.
                    if (!licenseManager.Messages.Any(m => m.IsError))
                    {
                        diagnostics.Add(Diagnostic.Create(MetalamaCompilerMessageProvider.Instance,
                            (int)MetalamaErrorCode.ERR_InvalidLicenseOverall));
                    }
                    else
                    {
                       // Errors are emitted by the CommonCompiler.DisposeServices() method.
                    }

                    return TransformersResult.Failure(inputCompilation);
                }

                var application = serviceProvider.GetRequiredService<IApplicationInfoProvider>().CurrentApplication;
                var sdkLicenseMissing = false;

                foreach (var component in application.Components)
                {
                    // Metalama SDK is required for 3rd-party transformers.
                    if (component.Company != "PostSharp Technologies" && !licenseManager.CanConsume(LicenseRequirement.Professional))
                    {
                        diagnostics.Add(Diagnostic.Create(MetalamaCompilerMessageProvider.Instance,
                            (int)MetalamaErrorCode.ERR_InvalidLicenseForSdk, component.Name));
                        sdkLicenseMissing = true;
                    }
                }

                if (sdkLicenseMissing)
                {
                    return TransformersResult.Failure(inputCompilation);
                }

                bool shouldDebugTransformedCode = ShouldDebugTransformedCode(analyzerConfigProvider);

                if (shouldDebugTransformedCode)
                {
                    if (!licenseManager.CanConsume(LicenseRequirement.Starter))
                    {
                        diagnostics.Add(Diagnostic.Create(MetalamaCompilerMessageProvider.Instance,
                            (int)MetalamaErrorCode.ERR_InvalidLicenseForProducingTransformedOutput));
                        return TransformersResult.Failure(inputCompilation);
                    }
                }
            }

            // Run transformers.
            ImmutableArray<ResourceDescription> resources = Arguments.ManifestResources;

            var result = RunTransformers(inputCompilation, transformers, sourceOnlyAnalyzersOptions, plugins,
                analyzerConfigProvider, transformerOptions, diagnostics, resources, AssemblyLoader, serviceProvider, cancellationToken);

            Arguments.ManifestResources = resources.AddRange(result.AdditionalResources);

            return result;
        }

        internal static TransformersResult RunTransformers(
            Compilation inputCompilation,
            ImmutableArray<ISourceTransformer> transformers,
            SourceOnlyAnalyzersOptions? sourceOnlyAnalyzersOptions,
            ImmutableArray<object> plugins,
            AnalyzerConfigOptionsProvider analyzerConfigProvider,
            TransformerOptions? transformerOptions,
            DiagnosticBag diagnostics,
            ImmutableArray<ResourceDescription> manifestResources,
            IAnalyzerAssemblyLoader assemblyLoader,
            IServiceProvider? services,
            CancellationToken cancellationToken)
        {
            // If there are no transformers, don't do anything, not even annotating
            if (transformers.IsEmpty)
            {
                return TransformersResult.Empty(inputCompilation, analyzerConfigProvider);
            }

            transformerOptions ??= TransformerOptions.Default;

            Dictionary<SyntaxTree, (SyntaxTree NewTree,bool IsModified)> oldTreeToNewTrees = new();
            Dictionary<SyntaxTree, SyntaxTree?> newTreesToOldTrees = new();
            HashSet<SyntaxTree> addedTrees = new();
            
            AnalyzerConfigOptionsProvider GetMappedAnalyzerConfigOptionsProvider(AnalyzerConfigOptionsProvider optionsProvider) 
                => CompilerAnalyzerConfigOptionsProvider.MapSyntaxTrees( 
                    optionsProvider,
                    oldTreeToNewTrees.Select(x => (x.Key, x.Value.NewTree)) );

            var inputResources = manifestResources.SelectAsArray(m => new ManagedResource(m));
            List<ManagedResource> addedResources = new();
            var diagnosticFiltersBuilder = ImmutableArray.CreateBuilder<DiagnosticFilter>();

            // Add tracking annotations to the input tree.
            var annotatedInputCompilation = inputCompilation;
            bool shouldDebugTransformedCode = ShouldDebugTransformedCode(analyzerConfigProvider);

            if (!shouldDebugTransformedCode)
            {
                foreach (var tree in inputCompilation.SyntaxTrees)
                {
                    SyntaxTree annotatedTree = tree.WithRootAndOptions(TreeTracker.AnnotateNodeAndChildren(tree.GetRoot(cancellationToken)), tree.Options);
                    
                    SyntaxTreeHistory.Update(tree, annotatedTree );
                    annotatedInputCompilation = annotatedInputCompilation.ReplaceSyntaxTree(tree, annotatedTree);
                    oldTreeToNewTrees[tree] = (annotatedTree,false);
                    newTreesToOldTrees[annotatedTree] = tree;
                }
            }

            // Execute source-only analyzers before any transformation.
            if (sourceOnlyAnalyzersOptions != null)
            {
                // Executing the analyzers can realize most of the compilation, so we pay attention to execute them on the same compilation
                // as the one we give as the input for transformations.

                // Map the options provider to the annotated syntax trees.
                var mappedOptionProvider =
                    GetMappedAnalyzerConfigOptionsProvider(sourceOnlyAnalyzersOptions.AnalyzerOptions
                        .AnalyzerConfigOptionsProvider);
                var mappedOptions = new AnalyzerOptions(sourceOnlyAnalyzersOptions.AnalyzerOptions.AdditionalFiles,
                    mappedOptionProvider);

                annotatedInputCompilation = ExecuteSourceOnlyAnalyzers(
                    sourceOnlyAnalyzersOptions with { AnalyzerOptions = mappedOptions}, 
                    annotatedInputCompilation,
                    diagnostics,
                    GetLogger(services),
                    cancellationToken);
            }

            // Execute the transformers.
            var outputCompilation = annotatedInputCompilation;


            foreach (var transformer in transformers)
            {
                try
                {
                    var transformerDiagnostics = new DiagnosticBag();

                    var context = new TransformerContext(outputCompilation,
                        plugins,
                        analyzerConfigProvider,
                        transformerOptions,
                        inputResources.AddRange(addedResources),
                        services,
                        transformerDiagnostics,
                        assemblyLoader);
                    transformer.Execute(context);

                    diagnosticFiltersBuilder.AddRange(context.DiagnosticFilters);
                    addedResources.AddRange(context.AddedResources);

                    // Filter the diagnostics.
                    foreach ( var transformerDiagnostic in transformerDiagnostics .AsEnumerableWithoutResolution() )
                    {
                        var filteredDiagnostic = outputCompilation.Options.FilterDiagnostic(transformerDiagnostic, cancellationToken);
                        if ( filteredDiagnostic != null )
                        {
                            diagnostics.Add(filteredDiagnostic);
                        }
                    }

                    // Apply the transformations.
                    foreach (var transformedTree in context.TransformedTrees)
                    {
                        var newTree = transformedTree.NewTree;
                        
                        // Update the compilation and indices.
                        if (transformedTree.OldTree != null)
                        {
                            // Find the original tree.
                            if (!newTreesToOldTrees.TryGetValue(transformedTree.OldTree, out SyntaxTree? oldTree))
                            {
                                oldTree = transformedTree.OldTree;
                            }

                            // Updates the index that allows to find the original tree.
                            if (newTree != null)
                            {
                                newTreesToOldTrees[newTree] = oldTree;
                            }

                            if (oldTree != null)
                            {
                                // Update the index mapping old trees to new trees.
                                oldTreeToNewTrees[oldTree] = (newTree!,true);
                            }
                            else
                            {
                                // We are updating a tree that was added by a previous transformer.
                                addedTrees.Remove(transformedTree.OldTree);
                                if (newTree != null)
                                {
                                    addedTrees.Add(newTree);
                                }
                            }

                            if (newTree != null)
                            {
                                outputCompilation =
                                    outputCompilation.ReplaceSyntaxTree(transformedTree.OldTree, newTree);
                            }
                            else
                            {
                                outputCompilation =
                                    outputCompilation.RemoveSyntaxTrees(transformedTree.OldTree);
                            }
                        }
                        else
                        {
                            addedTrees.Add(newTree!);
                            newTreesToOldTrees[newTree!] = null;
                            outputCompilation = outputCompilation.AddSyntaxTrees(newTree!);
                        }
                    }


                }
                catch (Exception ex)
                {
                    var crashReportPath = CrashReporter.WriteCrashReport(ex);

                    // Report a diagnostic.
                    var diagnostic = Diagnostic.Create(new DiagnosticInfo(
                        MetalamaCompilerMessageProvider.Instance, (int)MetalamaErrorCode.ERR_TransformerFailed, transformer.GetType().Name, ex.Message, crashReportPath));

                    diagnostics.Add(diagnostic);
                }
            }


            var replacements = oldTreeToNewTrees
                .Where( p => p.Value.IsModified )
                .Select(p => SyntaxTreeTransformation.ReplaceTree(p.Key, p.Value.NewTree))
                .Concat(addedTrees.Select(t => SyntaxTreeTransformation.AddTree(t)))
                .ToImmutableArray();

            foreach ( var resource in addedResources.Where( r => r.IncludeInRefAssembly ) )
            {
                AttachedProperties.Add(resource.Resource, RefAssemblyResourceMarker.Instance);
            }

            return new TransformersResult(
                annotatedInputCompilation, 
                outputCompilation,
                 replacements, 
                new DiagnosticFilters(diagnosticFiltersBuilder.ToImmutable()), 
                addedResources.SelectAsArray( m => m.Resource),
                GetMappedAnalyzerConfigOptionsProvider(analyzerConfigProvider) );
        }
        // </Metalama>
    }
}
