// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CSharpCompiler : CommonCompiler
    {
        internal const string ResponseFileName = "csc.rsp";

        private readonly CommandLineDiagnosticFormatter _diagnosticFormatter;
        private readonly string? _tempDirectory;
        
        // <Metalama>
        static CSharpCompiler()
        {
            // Ensure that our Metalama.Compiler.Interfaces (the one with type forwarders) get loaded first, and not the user-facing one, which
            // is a reference assembly.
            MetalamaCompilerInterfaces.Initialize();
        }
        // </Metalama>

        protected CSharpCompiler(CSharpCommandLineParser parser, string? responseFile, string[] args, BuildPaths buildPaths, string? additionalReferenceDirectories, IAnalyzerAssemblyLoader assemblyLoader, GeneratorDriverCache? driverCache = null)
            : base(parser, responseFile, args, buildPaths, additionalReferenceDirectories, assemblyLoader, driverCache)
        {
            _diagnosticFormatter = new CommandLineDiagnosticFormatter(buildPaths.WorkingDirectory, Arguments.PrintFullPaths, Arguments.ShouldIncludeErrorEndLocation);
            _tempDirectory = buildPaths.TempDirectory;
        }
        
        
        // <Metalama>
        protected abstract bool IsLongRunningProcess { get; }
        // </Metalama>

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
        
        

        private (string[] AdditionalLicenses, bool SkipUserLicenses) GetLicensingOptions(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
        {
            // Load license keys from build options.
            string[] additionalLicenses;
            if (analyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.MetalamaLicense", out var licenseProperty))
            {
                additionalLicenses = licenseProperty.Trim()
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            }
            else
            {
                additionalLicenses = Array.Empty<string>();
            }

            if (!(analyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.MetalamaIgnoreUserLicenses",
                out var ignoreUserLicensesProperty) && bool.TryParse(ignoreUserLicensesProperty, out var ignoreUserLicenses)))
            {
                ignoreUserLicenses = false;
            }
            
            return (additionalLicenses, ignoreUserLicenses);


        }

        protected virtual bool RequiresMetalamaSupportServices => true;
        protected virtual bool RequiresMetalamaLicensingServices => true;

        private protected override TransformersResult RunTransformers(
            Compilation inputCompilation, ImmutableArray<ISourceTransformer> transformers, SourceOnlyAnalyzersOptions sourceOnlyAnalyzersOptions,
            ImmutableArray<object> plugins, AnalyzerConfigOptionsProvider analyzerConfigProvider, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            // If there are no transformers, don't do anything, not even annotating
            if (transformers.IsEmpty)
            {
                return TransformersResult.Empty(inputCompilation);
            }

            var services = new ServiceCollection();
            var serviceProviderBuilder = new ServiceProviderBuilder(
                (type, instance) => services.AddService(type, instance),
                () => services);

            if (this.RequiresMetalamaLicensingServices)
            {
                var licenseOptions = this.GetLicensingOptions(analyzerConfigProvider);
                serviceProviderBuilder.AddBackstageServices(
                    new MetalamaCompilerApplicationInfo(this.IsLongRunningProcess),
                    inputCompilation.AssemblyName,
                    licenseOptions.SkipUserLicenses,
                    licenseOptions.AdditionalLicenses,
                    this.RequiresMetalamaSupportServices);
            }
            else
            {
                if ( this.RequiresMetalamaSupportServices )
                {
                    throw new InvalidOperationException();
                }

                serviceProviderBuilder.AddMinimalBackstageServices();
            }

            // Initialize licensing.
            var licenseManager = services.GetService<ILicenseConsumptionManager>();
            try
            {
                if (licenseManager != null)
                {
                    string? consumerNamespace = inputCompilation.AssemblyName ?? "";

                    if (!licenseManager.CanConsumeFeatures(LicensedFeatures.Community, consumerNamespace))
                    {
                        diagnostics.Add(Diagnostic.Create(MetalamaCompilerMessageProvider.Instance,
                            (int)MetalamaErrorCode.ERR_InvalidLicenseOverall));
                        return TransformersResult.Failure(inputCompilation);
                    }

                    bool shouldDebugTransformedCode = ShouldDebugTransformedCode(analyzerConfigProvider);

                    if (shouldDebugTransformedCode)
                    {
                        if (!licenseManager.CanConsumeFeatures(LicensedFeatures.Metalama, consumerNamespace))
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
                    analyzerConfigProvider, diagnostics, resources, AssemblyLoader, services, cancellationToken);

                Arguments.ManifestResources = resources.AddRange(result.AdditionalResources);

                return result;
            }
            finally
            {
                // Write all licensing messages that may have been emitted during the compilation.
                if (licenseManager != null)
                {
                    foreach (var licensingMessage in licenseManager.Messages)
                    {
                        int messageId = (int) (licensingMessage.IsError ? MetalamaErrorCode.ERR_LicensingMessage : MetalamaErrorCode.WRN_LicensingMessage);
                        diagnostics.Add(Diagnostic.Create(MetalamaCompilerMessageProvider.Instance,
                            messageId, licensingMessage.Text));
                    }
                }
                
                // Close logs.
                services.GetLoggerFactory().Dispose();
            }
        }

        internal static TransformersResult RunTransformers(
            Compilation inputCompilation,
            ImmutableArray<ISourceTransformer> transformers,
            SourceOnlyAnalyzersOptions? sourceOnlyAnalyzersOptions,
            ImmutableArray<object> plugins,
            AnalyzerConfigOptionsProvider analyzerConfigProvider,
            DiagnosticBag diagnostics,
            ImmutableArray<ResourceDescription> manifestResources,
            IAnalyzerAssemblyLoader assemblyLoader,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            // If there are no transformers, don't do anything, not even annotating
            if (transformers.IsEmpty)
            {
                return TransformersResult.Empty(inputCompilation);
            }

            Dictionary<SyntaxTree, (SyntaxTree NewTree,bool IsModified)> oldTreeToNewTrees = new();
            Dictionary<SyntaxTree, SyntaxTree?> newTreesToOldTrees = new();
            HashSet<SyntaxTree> addedTrees = new();
            var inputResources = manifestResources.SelectAsArray(m => new ManagedResource(m));
            List<ManagedResource> addedResources = new();
            var diagnosticFiltersBuilder = ImmutableArray.CreateBuilder<DiagnosticFilter>();

            // Add tracking annotations to the input tree.
            var annotatedInputCompilation = inputCompilation;
            bool shouldDebugTransformedCode = ShouldDebugTransformedCode(analyzerConfigProvider);

            if (!shouldDebugTransformedCode)
            {
                // mark old trees as debuggable
                foreach (var tree in inputCompilation.SyntaxTrees)
                {
                    SyntaxTree annotatedTree = tree.WithRootAndOptions(TreeTracker.AnnotateNodeAndChildren(tree.GetRoot(cancellationToken)), tree.Options);
                    SyntaxTreeHistory.Update(tree, annotatedTree );
                    annotatedInputCompilation = annotatedInputCompilation.ReplaceSyntaxTree(tree, annotatedTree);
                    oldTreeToNewTrees[tree] = (annotatedTree,false);
                    newTreesToOldTrees[annotatedTree] = tree;
                }
            }

            // We source-only analyzers
            if (sourceOnlyAnalyzersOptions != null)
            {
                // Executing the analyzers can realize most of the compilation, so we pay attention to execute them on the same compilation
                // as the one we give as the input for transformations.
                annotatedInputCompilation = ExecuteSourceOnlyAnalyzers(sourceOnlyAnalyzersOptions, annotatedInputCompilation, diagnostics, cancellationToken);
            }

            // Execute the transformers.
            var outputCompilation = annotatedInputCompilation;

            foreach (var transformer in transformers)
            {
                try
                {
                    var context = new TransformerContext(outputCompilation,
                        plugins,
                        analyzerConfigProvider.GlobalOptions,
                        inputResources.AddRange(addedResources),
                        services,
                        diagnostics,
                        assemblyLoader);
                    transformer.Execute(context);

                    diagnosticFiltersBuilder.AddRange(context.DiagnosticFilters);
                    addedResources.AddRange(context.AddedResources);

                    foreach (var transformedTree in context.TransformedTrees)
                    {
                        SyntaxTree newTree = transformedTree.NewTree;
                        
                        // Annotate the new tree.
                        /*
                        if (!shouldDebugTransformedCode)
                        {
                            // mark new trees as not debuggable
                            // in Debug mode, also mark transformed trees as undebuggable "poison", which triggers assert if used in a sequence point
                            if (TreeTracker.IsAnnotated(newTree.GetRoot()))
                            {
#if DEBUG
                                TreeTracker.MarkAsUndebuggable(newTree);
#endif
                            }
                            else
                            {
                                // TODO: this is at most not efficient because we may have reference to hundreds of compilations.
                                newTree = newTree.WithRootAndOptions(
                                    TreeTracker.AnnotateNodeAndChildren(newTree.GetRoot(), null, outputCompilation),
                                    newTree.Options);
                            }
                        }
                        */

                        // Update the compilation and indices.
                        if (transformedTree.OldTree != null)
                        {
                            // Find the original tree.
                            if (!newTreesToOldTrees.TryGetValue(transformedTree.OldTree, out SyntaxTree? oldTree))
                            {
                                oldTree = transformedTree.OldTree;
                            }

                            // Updates the index that allows to find the original tree.
                            newTreesToOldTrees[newTree] = oldTree;

                            if (oldTree != null)
                            {
                                // Update the index mapping old trees to new trees.
                                oldTreeToNewTrees[oldTree] = (newTree,true);
                            }
                            else
                            {
                                // We are updating a tree that was added by a previous transformer.
                                addedTrees.Remove(transformedTree.OldTree);
                                addedTrees.Add(newTree);
                            }

                            outputCompilation =
                                outputCompilation.ReplaceSyntaxTree(transformedTree.OldTree, newTree);
                        }
                        else
                        {
                            addedTrees.Add(newTree);
                            newTreesToOldTrees[newTree] = null;
                            outputCompilation = outputCompilation.AddSyntaxTrees(newTree);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var diagnostic = Diagnostic.Create(new DiagnosticInfo(
                        MetalamaCompilerMessageProvider.Instance, (int)global::Metalama.Compiler.MetalamaErrorCode.ERR_TransformerFailed, transformer.GetType().Name, ex.ToString()));
                    diagnostics.Add(diagnostic);
                }
            }


            var replacements = oldTreeToNewTrees
                .Where( p => p.Value.IsModified )
                .Select(p => new SyntaxTreeTransformation(p.Value.NewTree, p.Key))
                .Concat(addedTrees.Select(t => new SyntaxTreeTransformation(t, null)))
                .ToImmutableArray();

            foreach ( var resource in addedResources.Where( r => r.IncludeInRefAssembly ) )
            {
                AttachedProperties.Add(resource.Resource, RefAssemblyResourceMarker.Instance);
            }


            return new TransformersResult(annotatedInputCompilation, outputCompilation,
                 replacements, new DiagnosticFilters(diagnosticFiltersBuilder.ToImmutable()), addedResources.SelectAsArray( m => m.Resource) );
            
    
        }
        // </Metalama>
    }
}
