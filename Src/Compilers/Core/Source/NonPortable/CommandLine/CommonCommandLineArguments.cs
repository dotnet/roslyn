// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The base class for representing command line arguments to a
    /// <see cref="CommonCompiler"/>.
    /// </summary>
    public abstract class CommandLineArguments
    {
        internal bool IsInteractive { get; set; }

        /// <summary>
        /// Directory used to resolve relative paths stored in the arguments.
        /// </summary>
        /// <remarks>
        /// Except for paths stored in <see cref="MetadataReferences"/>, all
        /// paths stored in the properties of this class are resolved and
        /// absolute. This is the directory that relative paths specified on
        /// command line were resolved against.
        /// </remarks>
        public string BaseDirectory { get; internal set; }

        /// <summary>
        /// Sequence of absolute paths used to search for references.
        /// </summary>
        public ImmutableArray<string> ReferencePaths { get; internal set; }

        /// <summary>
        /// Sequence of absolute paths used to search for key files.
        /// </summary>
        public ImmutableArray<string> KeyFileSearchPaths { get; internal set; }

        /// <summary>
        /// If true, use UTF8 for output.
        /// </summary>
        public bool Utf8Output { get; internal set; }

        /// <summary>
        /// Compilation name or null if not specified.
        /// </summary>
        public string CompilationName { get; internal set; }

        /// <summary>
        /// Name of the output file or null if not specified.
        /// </summary>
        public string OutputFileName { get; internal set; }

        /// <summary>
        /// Path of the PDB file or null if not specified.
        /// </summary>
        public string PdbPath { get; internal set; }

        /// <summary>
        /// Absolute path of the output directory.
        /// </summary>
        public string OutputDirectory { get; internal set; }

        /// <summary>
        /// Absolute path of the documentation comment XML file or null if not specified.
        /// </summary>
        public string DocumentationPath { get; internal set; }

        /// <summary>
        /// An absolute path of the App.config file or null if not specified.
        /// </summary>
        public string AppConfigPath { get; internal set; }

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
        public ImmutableArray<CommandLineAnalyzer> Analyzers { get; internal set; }

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
        /// The path to a Win32 resource.
        /// </summary>
        public string Win32ResourceFile { get; internal set; }

        /// <summary>
        /// The path to a .ico icon file.
        /// </summary>
        public string Win32Icon { get; internal set; }

        /// <summary>
        /// The path to a Win32 manifest file to embed
        /// into the output portable executable (PE) file.
        /// </summary>
        public string Win32Manifest { get; internal set; }

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
        public Encoding Encoding { get; internal set; }

        /// <summary>
        /// Arguments following script argument separator "--" or null if <see cref="IsInteractive"/> is false.
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
        /// The base path to a file to write the recorded output of the
        /// <see cref="Instrumentation.TouchedFileLogger"/>. The output file
        /// paths will be the base path with ".read" appended for the read files
        /// and ".write" appended for the written files.
        /// </summary>
        public string TouchedFilesPath { get; internal set; }

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
        public CultureInfo PreferredUILang { get; internal set; }

        internal Guid SqmSessionGuid { get; set; }

        internal CommandLineArguments()
        {
        }

        /// <summary>
        /// Resolves metadata references stored in <see cref="P:MetadataReferences"/> using given file resolver and metadata provider.
        /// </summary>
        /// <param name="fileResolver">The file resolver to use for assembly name and relative path resolution, or null to use a default.</param>
        /// <param name="metadataProvider">Uses to create metadata references from resolved full paths, or null to use a default.</param>
        /// <returns>Yields resolved metadata references or <see cref="UnresolvedMetadataReference"/>.</returns>
        public IEnumerable<MetadataReference> ResolveMetadataReferences(FileResolver fileResolver = null, MetadataReferenceProvider metadataProvider = null)
        {
            return ResolveMetadataReferences(fileResolver, metadataProvider, diagnosticsOpt: null, messageProviderOpt: null);
        }

        /// <summary>
        /// Resolves metadata references stored in <see cref="P:MetadataReferences"/> using given file resolver and metadata provider.
        /// If a non-null diagnostic bag <paramref name="diagnosticsOpt"/> is provided, it catches exceptions that may be generated while reading the metadata file and
        /// reports appropriate diagnostics.
        /// Otherwise, if <paramref name="diagnosticsOpt"/> is null, the exceptions are unhandled.
        /// </summary>
        internal IEnumerable<MetadataReference> ResolveMetadataReferences(FileResolver fileResolver, MetadataReferenceProvider metadataProvider, List<DiagnosticInfo> diagnosticsOpt, CommonMessageProvider messageProviderOpt)
        {
            if (fileResolver == null)
            {
                fileResolver = new FileResolver(ReferencePaths, BaseDirectory);
            }

            if (metadataProvider == null)
            {
                metadataProvider = MetadataReferenceProvider.Default;
            }

            foreach (CommandLineReference cmdLineReference in MetadataReferences)
            {
                yield return cmdLineReference.Resolve(fileResolver, metadataProvider, diagnosticsOpt, messageProviderOpt);
            }
        }
    }
}
