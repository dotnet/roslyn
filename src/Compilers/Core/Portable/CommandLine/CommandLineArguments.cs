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
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using Metalama.Compiler;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The base class for representing command line arguments to a
    /// <see cref="CommonCompiler"/>.
    /// </summary>
    public abstract class CommandLineArguments
    {
        internal bool IsScriptRunner { get; set; }

        /// <summary>
        /// Drop to an interactive loop. If a script is specified in <see cref="SourceFiles"/> executes the script first.
        /// </summary>
        public bool InteractiveMode { get; internal set; }

        /// <summary>
        /// Directory used to resolve relative paths stored in the arguments.
        /// </summary>
        /// <remarks>
        /// Except for paths stored in <see cref="MetadataReferences"/>, all
        /// paths stored in the properties of this class are resolved and
        /// absolute. This is the directory that relative paths specified on
        /// command line were resolved against.
        /// </remarks>
        public string? BaseDirectory { get; internal set; }

        /// <summary>
        /// A list of pairs of paths. This stores the value of the command-line compiler
        /// option /pathMap:X1=Y1;X2=Y2... which causes a prefix of X1 followed by a path
        /// separator to be replaced by Y1 followed by a path separator, and so on for each following pair.
        /// </summary>
        /// <remarks>
        /// This option is used to help get build-to-build determinism even when the build
        /// directory is different from one build to the next.  The prefix matching is case sensitive.
        /// </remarks>
        public ImmutableArray<KeyValuePair<string, string>> PathMap { get; internal set; }

        /// <summary>
        /// Sequence of absolute paths used to search for references.
        /// </summary>
        public ImmutableArray<string> ReferencePaths { get; internal set; }

        /// <summary>
        /// Sequence of absolute paths used to search for sources specified as #load directives.
        /// </summary>
        public ImmutableArray<string> SourcePaths { get; internal set; }

        /// <summary>
        /// Sequence of absolute paths used to search for key files.
        /// </summary>
        public ImmutableArray<string> KeyFileSearchPaths { get; internal set; }

        /// <summary>
        /// If true, use UTF-8 for output.
        /// </summary>
        public bool Utf8Output { get; internal set; }

        /// <summary>
        /// Compilation name or null if not specified.
        /// </summary>
        public string? CompilationName { get; internal set; }

        /// <summary>
        /// Gets the emit options.
        /// </summary>
        public EmitOptions EmitOptions { get; internal set; } = null!; // initialized by Parse

        /// <summary>
        /// Name of the output file or null if not specified.
        /// </summary>
        public string? OutputFileName { get; internal set; }

        /// <summary>
        /// Path of the output ref assembly or null if not specified.
        /// </summary>
        public string? OutputRefFilePath { get; internal set; }

        /// <summary>
        /// Path of the PDB file or null if same as output binary path with .pdb extension.
        /// </summary>
        public string? PdbPath { get; internal set; }

        /// <summary>
        /// Path of the file containing information linking the compilation to source server that stores 
        /// a snapshot of the source code included in the compilation.
        /// </summary>
        public string? SourceLink { get; internal set; }

        /// <summary>
        /// Absolute path of the .ruleset file or null if not specified.
        /// </summary>
        public string? RuleSetPath { get; internal set; }

        /// <summary>
        /// True to emit PDB information (to a standalone PDB file or embedded into the PE file).
        /// </summary>
        public bool EmitPdb { get; internal set; }

        /// <summary>
        /// Absolute path of the output directory (could only be null if there is an error reported).
        /// </summary>
        public string OutputDirectory { get; internal set; } = null!; // initialized by Parse

        /// <summary>
        /// Absolute path of the documentation comment XML file or null if not specified.
        /// </summary>
        public string? DocumentationPath { get; internal set; }

        /// <summary>
        /// Absolute path of the directory to place generated files in, or <c>null</c> to not emit any generated files.
        /// </summary>
        public string? GeneratedFilesOutputDirectory { get; internal set; }

        /// <summary>
        /// Options controlling the generation of a SARIF log file containing compilation or
        /// analysis diagnostics, or null if no log file is desired.
        /// </summary>
        public ErrorLogOptions? ErrorLogOptions { get; internal set; }

        /// <summary>
        /// Options controlling the generation of a SARIF log file containing compilation or
        /// analysis diagnostics, or null if no log file is desired.
        /// </summary>
        public string? ErrorLogPath => ErrorLogOptions?.Path;

        /// <summary>
        /// An absolute path of the app.config file or null if not specified.
        /// </summary>
        public string? AppConfigPath { get; internal set; }

        /// <summary>
        /// Errors while parsing the command line arguments.
        /// </summary>
        public ImmutableArray<Diagnostic> Errors { get; internal set; }

        /// <summary>
        /// References to metadata supplied on the command line. 
        /// Includes assemblies specified via /r and netmodules specified via /addmodule.
        /// </summary>
        public ImmutableArray<CommandLineReference> MetadataReferences { get; internal set; }

        /// <summary>
        /// References to analyzers supplied on the command line.
        /// </summary>
        public ImmutableArray<CommandLineAnalyzerReference> AnalyzerReferences { get; internal set; }

        /// <summary>
        /// A set of paths to EditorConfig-compatible analyzer config files.
        /// </summary>
        public ImmutableArray<string> AnalyzerConfigPaths { get; internal set; }

        /// <summary>
        /// A set of additional non-code text files that can be used by analyzers.
        /// </summary>
        public ImmutableArray<CommandLineSourceFile> AdditionalFiles { get; internal set; }

        /// <summary>
        /// A set of files to embed in the PDB.
        /// </summary>
        public ImmutableArray<CommandLineSourceFile> EmbeddedFiles { get; internal set; }

        /// <value>
        /// Report additional information related to analyzers, such as analyzer execution time.
        /// </value>
        public bool ReportAnalyzer { get; internal set; }

        /// <summary>
        /// Report additional information related to InternalsVisibleToAttributes for all assemblies the compiler sees in this compilation.
        /// </summary>
        public bool ReportInternalsVisibleToAttributes { get; internal set; }

        /// <value>
        /// Skip execution of <see cref="DiagnosticAnalyzer"/>s.
        /// </value>
        public bool SkipAnalyzers { get; internal set; }

        /// <summary>
        /// If true, prepend the command line header logo during 
        /// <see cref="CommonCompiler.Run"/>.
        /// </summary>
        public bool DisplayLogo { get; internal set; }

        /// <summary>
        /// If true, append the command line help during
        /// <see cref="CommonCompiler.Run"/>
        /// </summary>
        public bool DisplayHelp { get; internal set; }

        /// <summary>
        /// If true, append the compiler version during
        /// <see cref="CommonCompiler.Run"/>
        /// </summary>
        public bool DisplayVersion { get; internal set; }

        /// <summary>
        /// If true, prepend the compiler-supported language versions during
        /// <see cref="CommonCompiler.Run"/>
        /// </summary>
        public bool DisplayLangVersions { get; internal set; }

        /// <summary>
        /// The path to a Win32 resource.
        /// </summary>
        public string? Win32ResourceFile { get; internal set; }

        /// <summary>
        /// The path to a .ico icon file.
        /// </summary>
        public string? Win32Icon { get; internal set; }

        /// <summary>
        /// The path to a Win32 manifest file to embed
        /// into the output portable executable (PE) file.
        /// </summary>
        public string? Win32Manifest { get; internal set; }

        /// <summary>
        /// If true, do not embed any Win32 manifest, including
        /// one specified by <see cref="Win32Manifest"/> or any
        /// default manifest.
        /// </summary>
        public bool NoWin32Manifest { get; internal set; }

        /// <summary>
        /// Resources specified as arguments to the compilation.
        /// </summary>
        public ImmutableArray<ResourceDescription> ManifestResources { get; internal set; }

        /// <summary>
        /// Encoding to be used for source files or 'null' for autodetect/default.
        /// </summary>
        public Encoding? Encoding { get; internal set; }

        /// <summary>
        /// Hash algorithm to use to calculate source file debug checksums and PDB checksum.
        /// </summary>
        public SourceHashAlgorithm ChecksumAlgorithm { get; internal set; }

        /// <summary>
        /// Arguments following a script file or separator "--". Null if the command line parser is not interactive.
        /// </summary>
        public ImmutableArray<string> ScriptArguments { get; internal set; }

        /// <summary>
        /// Source file paths.
        /// </summary>
        /// <remarks>
        /// Includes files specified directly on command line as well as files matching patterns specified 
        /// on command line using '*' and '?' wildcards or /recurse option.
        /// </remarks>
        public ImmutableArray<CommandLineSourceFile> SourceFiles { get; internal set; }

        /// <summary>
        /// Full path of a log of file paths accessed by the compiler, or null if file logging should be suppressed.
        /// </summary>
        /// <remarks>
        /// Two log files will be created: 
        /// One with path <see cref="TouchedFilesPath"/> and extension ".read" logging the files read,
        /// and second with path <see cref="TouchedFilesPath"/> and extension ".write" logging the files written to during compilation.
        /// </remarks>
        public string? TouchedFilesPath { get; internal set; }

        /// <summary>
        /// If true, prints the full path of the file containing errors or
        /// warnings in diagnostics.
        /// </summary>
        public bool PrintFullPaths { get; internal set; }

        /// <summary>
        /// Options to the <see cref="CommandLineParser"/>.
        /// </summary>
        /// <returns></returns>
        public ParseOptions ParseOptions
        {
            get { return ParseOptionsCore; }
        }

        /// <summary>
        /// Options to the <see cref="Compilation"/>.
        /// </summary>
        public CompilationOptions CompilationOptions
        {
            get { return CompilationOptionsCore; }
        }

        protected abstract ParseOptions ParseOptionsCore { get; }
        protected abstract CompilationOptions CompilationOptionsCore { get; }

        /// <summary>
        /// Specify the preferred output language name.
        /// </summary>
        public CultureInfo? PreferredUILang { get; internal set; }

        internal StrongNameProvider GetStrongNameProvider(StrongNameFileSystem fileSystem)
            => new DesktopStrongNameProvider(KeyFileSearchPaths, fileSystem);

        internal CommandLineArguments()
        {
        }

        /// <summary>
        /// Returns a full path of the file that the compiler will generate the assembly to if compilation succeeds.
        /// </summary>
        /// <remarks>
        /// The method takes <paramref name="outputFileName"/> rather than using the value of <see cref="OutputFileName"/> 
        /// since the latter might be unspecified, in which case actual output path can't be determined for C# command line
        /// without creating a compilation and finding an entry point. VB does not allow <see cref="OutputFileName"/> to 
        /// be unspecified.
        /// </remarks>
        public string GetOutputFilePath(string outputFileName)
        {
            if (outputFileName == null)
            {
                throw new ArgumentNullException(nameof(outputFileName));
            }

            return Path.Combine(OutputDirectory, outputFileName);
        }

        /// <summary>
        /// Returns a full path of the PDB file that the compiler will generate the debug symbols to 
        /// if <see cref="EmitPdbFile"/> is true and the compilation succeeds.
        /// </summary>
        /// <remarks>
        /// The method takes <paramref name="outputFileName"/> rather than using the value of <see cref="OutputFileName"/> 
        /// since the latter might be unspecified, in which case actual output path can't be determined for C# command line
        /// without creating a compilation and finding an entry point. VB does not allow <see cref="OutputFileName"/> to 
        /// be unspecified.
        /// </remarks>
        public string GetPdbFilePath(string outputFileName)
        {
            if (outputFileName == null)
            {
                throw new ArgumentNullException(nameof(outputFileName));
            }

            return PdbPath ?? Path.Combine(OutputDirectory, Path.ChangeExtension(outputFileName, ".pdb"));
        }

        /// <summary>
        /// Returns true if the PDB is generated to a PDB file, as opposed to embedded to the output binary and not generated at all.
        /// </summary>
        public bool EmitPdbFile
            => EmitPdb && EmitOptions.DebugInformationFormat != DebugInformationFormat.Embedded;

        #region Metadata References

        /// <summary>
        /// Resolves metadata references stored in <see cref="MetadataReferences"/> using given file resolver and metadata provider.
        /// </summary>
        /// <param name="metadataResolver"><see cref="MetadataReferenceResolver"/> to use for assembly name and relative path resolution.</param>
        /// <returns>Yields resolved metadata references or <see cref="UnresolvedMetadataReference"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="metadataResolver"/> is null.</exception>
        public IEnumerable<MetadataReference> ResolveMetadataReferences(MetadataReferenceResolver metadataResolver)
        {
            if (metadataResolver == null)
            {
                throw new ArgumentNullException(nameof(metadataResolver));
            }

            return ResolveMetadataReferences(metadataResolver, diagnosticsOpt: null, messageProviderOpt: null);
        }

        /// <summary>
        /// Resolves metadata references stored in <see cref="MetadataReferences"/> using given file resolver and metadata provider.
        /// If a non-null diagnostic bag <paramref name="diagnosticsOpt"/> is provided, it catches exceptions that may be generated while reading the metadata file and
        /// reports appropriate diagnostics.
        /// Otherwise, if <paramref name="diagnosticsOpt"/> is null, the exceptions are unhandled.
        /// </summary>
        /// <remarks>
        /// called by CommonCompiler with diagnostics and message provider
        /// </remarks>
        internal IEnumerable<MetadataReference> ResolveMetadataReferences(MetadataReferenceResolver metadataResolver, List<DiagnosticInfo>? diagnosticsOpt, CommonMessageProvider? messageProviderOpt)
        {
            RoslynDebug.Assert(metadataResolver != null);

            var resolved = new List<MetadataReference>();
            this.ResolveMetadataReferences(metadataResolver, diagnosticsOpt, messageProviderOpt, resolved);

            return resolved;
        }

        internal virtual bool ResolveMetadataReferences(MetadataReferenceResolver metadataResolver, List<DiagnosticInfo>? diagnosticsOpt, CommonMessageProvider? messageProviderOpt, List<MetadataReference> resolved)
        {
            bool result = true;

            foreach (CommandLineReference cmdReference in MetadataReferences)
            {
                var references = ResolveMetadataReference(cmdReference, metadataResolver, diagnosticsOpt, messageProviderOpt);
                if (!references.IsDefaultOrEmpty)
                {
                    resolved.AddRange(references);
                }
                else
                {
                    result = false;
                    if (diagnosticsOpt == null)
                    {
                        // no diagnostic, so leaved unresolved reference in list
                        resolved.Add(new UnresolvedMetadataReference(cmdReference.Reference, cmdReference.Properties));
                    }
                }
            }

            return result;
        }

        internal static ImmutableArray<PortableExecutableReference> ResolveMetadataReference(CommandLineReference cmdReference, MetadataReferenceResolver metadataResolver, List<DiagnosticInfo>? diagnosticsOpt, CommonMessageProvider? messageProviderOpt)
        {
            RoslynDebug.Assert(metadataResolver != null);
            Debug.Assert((diagnosticsOpt == null) == (messageProviderOpt == null));

            ImmutableArray<PortableExecutableReference> references;
            try
            {
                references = metadataResolver.ResolveReference(cmdReference.Reference, baseFilePath: null, properties: cmdReference.Properties);
            }
            catch (Exception e) when (diagnosticsOpt != null && (e is BadImageFormatException || e is IOException))
            {
                var diagnostic = PortableExecutableReference.ExceptionToDiagnostic(e, messageProviderOpt!, Location.None, cmdReference.Reference, cmdReference.Properties.Kind);
                diagnosticsOpt.Add(((DiagnosticWithInfo)diagnostic).Info);
                return ImmutableArray<PortableExecutableReference>.Empty;
            }

            if (references.IsDefaultOrEmpty && diagnosticsOpt != null)
            {
                RoslynDebug.AssertNotNull(messageProviderOpt);
                diagnosticsOpt.Add(new DiagnosticInfo(messageProviderOpt, messageProviderOpt.ERR_MetadataFileNotFound, cmdReference.Reference));
                return ImmutableArray<PortableExecutableReference>.Empty;
            }

            return references;
        }

        #endregion

        #region Analyzer References

        /// <summary>
        /// Resolves analyzer references stored in <see cref="AnalyzerReferences"/> using given file resolver.
        /// </summary>
        /// <param name="analyzerLoader">Load an assembly from a file path</param>
        /// <returns>Yields resolved <see cref="AnalyzerFileReference"/> or <see cref="UnresolvedAnalyzerReference"/>.</returns>
        public IEnumerable<AnalyzerReference> ResolveAnalyzerReferences(IAnalyzerAssemblyLoader analyzerLoader)
        {
            foreach (CommandLineAnalyzerReference cmdLineReference in AnalyzerReferences)
            {
                yield return ResolveAnalyzerReference(cmdLineReference, analyzerLoader)
                    ?? (AnalyzerReference)new UnresolvedAnalyzerReference(cmdLineReference.FilePath);
            }
        }

        internal void ResolveAnalyzersFromArguments(
            string language,
            List<DiagnosticInfo> diagnostics,
            CommonMessageProvider messageProvider,
            IAnalyzerAssemblyLoader analyzerLoader,
            CompilationOptions compilationOptions,
            bool skipAnalyzers,
            // <Metalama>
            ImmutableArray<string?> transformerOrder,
            // </Metalama>
            out ImmutableArray<DiagnosticAnalyzer> analyzers,
            out ImmutableArray<ISourceGenerator> generators,
            // <Metalama>
            out ImmutableArray<ISourceTransformer> transfomers
            // </Metalama>
            )
        {
            var analyzerBuilder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            var generatorBuilder = ImmutableArray.CreateBuilder<ISourceGenerator>();

            // <Metalama>
            var transformerBuilder = ImmutableArray.CreateBuilder<ISourceTransformer>();
            var transformerOrders = new List<ImmutableArray<string?>>();
            // </Metalama>

            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) =>
            {
                var analyzerReference = o as AnalyzerFileReference;
                RoslynDebug.Assert(analyzerReference is object);
                DiagnosticInfo? diagnostic;

                // <Metalama>
                if (e.Exception != null)
                {
                    CrashReporter.WriteCrashReport(e.Exception);
                }
                // </Metalama>

                switch (e.ErrorCode)
                {
                    case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer:
                        diagnostic = new DiagnosticInfo(messageProvider, messageProvider.WRN_UnableToLoadAnalyzer, analyzerReference.FullPath, e.Message);
                        break;
                    case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer:
                        diagnostic = new DiagnosticInfo(messageProvider, messageProvider.WRN_AnalyzerCannotBeCreated, e.TypeName ?? "", analyzerReference.FullPath, e.Message);
                        break;
                    case AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers:
                        diagnostic = new DiagnosticInfo(messageProvider, messageProvider.WRN_NoAnalyzerInAssembly, analyzerReference.FullPath);
                        break;
                    case AnalyzerLoadFailureEventArgs.FailureErrorCode.ReferencesFramework:
                        diagnostic = new DiagnosticInfo(messageProvider, messageProvider.WRN_AnalyzerReferencesFramework, analyzerReference.FullPath, e.TypeName!);
                        break;
                    case AnalyzerLoadFailureEventArgs.FailureErrorCode.ReferencesNewerCompiler:
                        diagnostic = new DiagnosticInfo(messageProvider, messageProvider.WRN_AnalyzerReferencesNewerCompiler, analyzerReference.FullPath, e.ReferencedCompilerVersion!.ToString(), typeof(AnalyzerFileReference).Assembly.GetName().Version!.ToString());
                        break;
                    case AnalyzerLoadFailureEventArgs.FailureErrorCode.None:
                    default:
                        return;
                }

                // Filter this diagnostic based on the compilation options so that /nowarn and /warnaserror etc. take effect.
                diagnostic = messageProvider.FilterDiagnosticInfo(diagnostic, compilationOptions);

                if (diagnostic != null)
                {
                    diagnostics.Add(diagnostic);
                }
            };

            var resolvedReferencesSet = PooledHashSet<AnalyzerFileReference>.GetInstance();
            var resolvedReferencesList = ArrayBuilder<AnalyzerFileReference>.GetInstance();
            foreach (var reference in AnalyzerReferences.Distinct()) // <Metalama />: add Distinct()
            {
                // <Metalama>
                var resolvedReference = ResolveAnalyzerReference(reference, analyzerLoader, messageProvider, diagnostics);
                if (resolvedReference != null)
                {
                    var isAdded = resolvedReferencesSet.Add(resolvedReference);
                    if (isAdded)
                    {
                        // In Metalama, we always load analyzer assemblies even if they don't contain analyzer types because
                        // they may contain other compile-time types.
                        resolvedReference.LoadAssembly();

                        // register the reference to the analyzer loader:
                        analyzerLoader.AddDependencyLocation(resolvedReference.FullPath);

                        resolvedReferencesList.Add(resolvedReference);
                    }
                    else
                    {
                        // https://github.com/dotnet/roslyn/issues/63856
                        //diagnostics.Add(new DiagnosticInfo(messageProvider, messageProvider.WRN_DuplicateAnalyzerReference, reference.FilePath));
                    }
                }
                // </Metalama>
            }

            // All analyzer references are registered now, we can start loading them.
            foreach (var resolvedReference in resolvedReferencesList)
            {
                resolvedReference.AnalyzerLoadFailed += errorHandler;
                resolvedReference.AddAnalyzers(analyzerBuilder, language, shouldIncludeAnalyzer);
                resolvedReference.AddGenerators(generatorBuilder, language);

                // <Metalama>
                resolvedReference.AddTransformers(transformerBuilder, language);
                resolvedReference.AddTransformerOrder(transformerOrders);
                // </Metalama>

                resolvedReference.AnalyzerLoadFailed -= errorHandler;
            }

            resolvedReferencesList.Free();
            resolvedReferencesSet.Free();

            // <Metalama>
            if (!transformerOrder.IsDefaultOrEmpty)
                transformerOrders.Add(transformerOrder);

            TransformerDependencyResolver.Sort(ref transformerBuilder, transformerOrders, diagnostics);

            transfomers = transformerBuilder.ToImmutable();
            // </Metalama>

            generators = generatorBuilder.ToImmutable();
            analyzers = analyzerBuilder.ToImmutable();

            // If we are skipping analyzers, ensure that we only add suppressors.
            bool shouldIncludeAnalyzer(DiagnosticAnalyzer analyzer) => !skipAnalyzers || analyzer is DiagnosticSuppressor;
        }

        // <Metalama>
        private readonly HashSet<string> _additionalRedirectedReferences = [];

        private static bool ContainsSourceGenerators(AssemblyMetadata assembly)
        {
            var metadataReader = assembly.GetModules().First().MetadataReader;

            foreach (var typeDefinitionHandle in metadataReader.TypeDefinitions)
            {
                var typeDefinition = metadataReader.GetTypeDefinition(typeDefinitionHandle);

                if (!typeDefinition.Attributes.HasFlag(TypeAttributes.Public))
                {
                    continue;
                }

                foreach (var attributeHandle in typeDefinition.GetCustomAttributes())
                {
                    var attributeConstructorHandle = metadataReader.GetCustomAttribute(attributeHandle).Constructor;

                    if (attributeConstructorHandle.Kind != HandleKind.MemberReference)
                    {
                        continue;
                    }

                    var attributeConstructor = metadataReader.GetMemberReference((MemberReferenceHandle)attributeConstructorHandle);

                    var parentHandle = attributeConstructor.Parent;

                    if (parentHandle.Kind != HandleKind.TypeReference)
                    {
                        continue;
                    }

                    var parent = metadataReader.GetTypeReference((TypeReferenceHandle)parentHandle);

                    if (metadataReader.GetString(parent.Name) != "GeneratorAttribute" || metadataReader.GetString(parent.Namespace) != "Microsoft.CodeAnalysis")
                    {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private static readonly Version? s_metalamaRoslynVersion = GetAssemblyMetadataVersion("RoslynVersion", normalizeVersion: true);

        private static readonly Version? s_dotnetSdkVersion = GetAssemblyMetadataVersion("DotnetSdkVersion", normalizeVersion: false);

        private static Version? GetAssemblyMetadataVersion(string key, bool normalizeVersion)
        {
            var versionString = typeof(CommandLineArguments).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .SingleOrDefault(a => a.Key == key)
                ?.Value;

            if (!Version.TryParse(versionString, out var version))
            {
                return null;
            }

            if (normalizeVersion)
            {
                // Replace empty version components with zeroes. Version says that e.g. 4.6.0.0 > 4.6.0, but we want those two to be considered equal.
                if (version.Build == -1)
                {
                    version = new Version(version.Major, version.Minor, 0, 0);
                }
                else if (version.Revision == -1)
                {
                    version = new Version(version.Major, version.Minor, version.Build, 0);
                }
            }

            return version;
        }
        // </Metalama>

        // <Metalama> modified
        private AnalyzerFileReference? ResolveAnalyzerReference(CommandLineAnalyzerReference reference, IAnalyzerAssemblyLoader analyzerLoader, CommonMessageProvider? messageProvider = null, List<DiagnosticInfo>? diagnostics = null)
        {
            string? resolvedPath = FileUtilities.ResolveRelativePath(reference.FilePath, basePath: null, baseDirectory: BaseDirectory, searchPaths: ReferencePaths, fileExists: File.Exists);
            if (resolvedPath != null)
            {
                resolvedPath = FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
            }

            if (resolvedPath == null)
            {
                diagnostics?.Add(new DiagnosticInfo(messageProvider!, messageProvider!.ERR_MetadataFileNotFound, reference.FilePath));

                return null;
            }

            if (_additionalRedirectedReferences.Contains(Path.GetFileNameWithoutExtension(reference.FilePath)))
            {
                // The set contains references that can't be redirected (e.g. netstandard, Roslyn), so it's fine if GetRedirectedPath returns null.
                resolvedPath = AnalyzerAssemblyRedirector.GetRedirectedPath(resolvedPath) ?? resolvedPath;
            }
            else if (s_metalamaRoslynVersion is { } metalamaRoslynVersion)
            {
                using var assembly = AssemblyMetadata.CreateFromFile(resolvedPath);

                var referencedAssemblies = assembly.GetModules().First().Module.ReferencedAssemblies;
                var referencedRoslynVersion = referencedAssemblies.FirstOrDefault(a => a.Name == "Microsoft.CodeAnalysis")?.Version;

                if (referencedRoslynVersion != null)
                {
                    if (referencedRoslynVersion.Major >= 2023)
                    {
                        // If the version is year-based, assume the referenced version of Roslyn is from Metalama and so do nothing.
                        // Though this should only happen in tests.
                        RoslynDebug.Assert(AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.StartsWith("xunit") == true));
                    }
                    else if (referencedRoslynVersion > metalamaRoslynVersion)
                    {
                        if (AnalyzerAssemblyRedirector.GetRedirectedPath(resolvedPath) is { } redirectedPath)
                        {
                            // We're redirecting assemblies to their older versions, which means the behavior shouldn't change much.
                            // So a single generic warning should be enough.
                            if (diagnostics?.Contains(diagnostic => diagnostic.MessageProvider == MetalamaCompilerMessageProvider.Instance && diagnostic.Code == (int)MetalamaErrorCode.WRN_AnalyzerAssembliesRedirected) == false)
                            {
                                diagnostics.Add(new DiagnosticInfo(MetalamaCompilerMessageProvider.Instance, (int)MetalamaErrorCode.WRN_AnalyzerAssembliesRedirected, referencedRoslynVersion.ToString(), metalamaRoslynVersion.ToString()));
                            }

                            // References of this assembly might not reference Roslyn, but still have to be redirected, so add them to a set.
                            foreach (var referencedAssembly in referencedAssemblies)
                            {
                                _additionalRedirectedReferences.Add(referencedAssembly.Name);
                            }

                            resolvedPath = redirectedPath;
                        }
                        else
                        {
                            // We were unable to redirect, so we're disabling this assembly and informing the user.
                            var errorCode = ContainsSourceGenerators(assembly)
                                ? MetalamaErrorCode.WRN_GeneratorAssemblyCantRedirect
                                : MetalamaErrorCode.WRN_AnalyzerAssemblyCantRedirect;

                            diagnostics?.Add(
                                new DiagnosticInfo(
                                    MetalamaCompilerMessageProvider.Instance,
                                    (int)errorCode,
                                    reference.FilePath,
                                    referencedRoslynVersion.ToString(),
                                    metalamaRoslynVersion.ToString(),
                                    s_dotnetSdkVersion?.ToString() ?? string.Empty));

                            return null;
                        }
                    }
                }
            }

            return new AnalyzerFileReference(resolvedPath, analyzerLoader);
        }
        // </Metalama>
        #endregion
    }
}
