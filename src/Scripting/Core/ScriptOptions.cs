// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting
{
    using static ParameterValidationHelpers;

    /// <summary>
    /// Options for creating and running scripts.
    /// </summary>
    public sealed class ScriptOptions
    {
        public static ScriptOptions Default { get; } = new ScriptOptions(
            filePath: string.Empty,
            references: GetDefaultMetadataReferences(),
            namespaces: ImmutableArray<string>.Empty,
            metadataResolver: ScriptMetadataResolver.Default,
            sourceResolver: SourceFileResolver.Default,
            emitDebugInformation: false,
            fileEncoding: null,
            OptimizationLevel.Debug,
            checkOverflow: false,
            allowUnsafe: true,
            warningLevel: 4,
            parseOptions: null);

        private static ImmutableArray<MetadataReference> GetDefaultMetadataReferences()
        {
            if (GacFileResolver.IsAvailable)
            {
                return ImmutableArray<MetadataReference>.Empty;
            }

            // These references are resolved lazily. Keep in sync with list in core csi.rsp.
            var files = new[]
            {
                "System.Collections",
                "System.Collections.Concurrent",
                "System.Console",
                "System.Diagnostics.Debug",
                "System.Diagnostics.Process",
                "System.Diagnostics.StackTrace",
                "System.Globalization",
                "System.IO",
                "System.IO.FileSystem",
                "System.IO.FileSystem.Primitives",
                "System.Reflection",
                "System.Reflection.Extensions",
                "System.Reflection.Primitives",
                "System.Runtime",
                "System.Runtime.Extensions",
                "System.Runtime.InteropServices",
                "System.Text.Encoding",
                "System.Text.Encoding.CodePages",
                "System.Text.Encoding.Extensions",
                "System.Text.RegularExpressions",
                "System.Threading",
                "System.Threading.Tasks",
                "System.Threading.Tasks.Parallel",
                "System.Threading.Thread",
                "System.ValueTuple",
            };

            return ImmutableArray.CreateRange(files.Select(CreateUnresolvedReference));
        }

        /// <summary>
        /// An array of <see cref="MetadataReference"/>s to be added to the script.
        /// </summary>
        /// <remarks>
        /// The array may contain both resolved and unresolved references (<see cref="UnresolvedMetadataReference"/>).
        /// Unresolved references are resolved when the script is about to be executed 
        /// (<see cref="Script.RunAsync(object, CancellationToken)"/>.
        /// Any resolution errors are reported at that point through <see cref="CompilationErrorException"/>.
        /// </remarks>
        public ImmutableArray<MetadataReference> MetadataReferences { get; private set; }

        /// <summary>
        /// <see cref="MetadataReferenceResolver"/> to be used to resolve missing dependencies, unresolved metadata references and #r directives.
        /// </summary>
        public MetadataReferenceResolver MetadataResolver { get; private set; }

        /// <summary>
        /// <see cref="SourceReferenceResolver"/> to be used to resolve source of scripts referenced via #load directive.
        /// </summary>
        public SourceReferenceResolver SourceResolver { get; private set; }

        /// <summary>
        /// The namespaces, static classes and aliases imported by the script.
        /// </summary>
        public ImmutableArray<string> Imports { get; private set; }

        /// <summary>
        /// Specifies whether debugging symbols should be emitted.
        /// </summary>
        public bool EmitDebugInformation { get; private set; } = false;

        /// <summary>
        /// Specifies the encoding to be used when debugging scripts loaded from a file, or saved to a file for debugging purposes.
        /// If it's null, the compiler will attempt to detect the necessary encoding for debugging
        /// </summary>
        public Encoding FileEncoding { get; private set; }

        /// <summary>
        /// The path to the script source if it originated from a file, empty otherwise.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Specifies whether or not optimizations should be performed on the output IL.
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; private set; }

        /// <summary>
        /// Whether bounds checking on integer arithmetic is enforced by default or not.
        /// </summary>
        public bool CheckOverflow { get; private set; }

        /// <summary>
        /// Allow unsafe regions (i.e. unsafe modifiers on members and unsafe blocks).
        /// </summary>
        public bool AllowUnsafe { get; private set; }

        /// <summary>
        /// Global warning level (from 0 to 4).
        /// </summary>
        public int WarningLevel { get; private set; }

        internal ParseOptions ParseOptions { get; private set; }

        internal ScriptOptions(
            string filePath,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<string> namespaces,
            MetadataReferenceResolver metadataResolver,
            SourceReferenceResolver sourceResolver,
            bool emitDebugInformation,
            Encoding fileEncoding,
            OptimizationLevel optimizationLevel,
            bool checkOverflow,
            bool allowUnsafe,
            int warningLevel,
            ParseOptions parseOptions)
        {
            Debug.Assert(filePath != null);
            Debug.Assert(!references.IsDefault);
            Debug.Assert(!namespaces.IsDefault);
            Debug.Assert(metadataResolver != null);
            Debug.Assert(sourceResolver != null);

            FilePath = filePath;
            MetadataReferences = references;
            Imports = namespaces;
            MetadataResolver = metadataResolver;
            SourceResolver = sourceResolver;
            EmitDebugInformation = emitDebugInformation;
            FileEncoding = fileEncoding;
            OptimizationLevel = optimizationLevel;
            CheckOverflow = checkOverflow;
            AllowUnsafe = allowUnsafe;
            WarningLevel = warningLevel;
            ParseOptions = parseOptions;
        }

        private ScriptOptions(ScriptOptions other)
            : this(filePath: other.FilePath,
                   references: other.MetadataReferences,
                   namespaces: other.Imports,
                   metadataResolver: other.MetadataResolver,
                   sourceResolver: other.SourceResolver,
                   emitDebugInformation: other.EmitDebugInformation,
                   fileEncoding: other.FileEncoding,
                   optimizationLevel: other.OptimizationLevel,
                   checkOverflow: other.CheckOverflow,
                   allowUnsafe: other.AllowUnsafe,
                   warningLevel: other.WarningLevel,
                   parseOptions: other.ParseOptions)
        {
        }

        // a reference to an assembly should by default be equivalent to #r, which applies recursive global alias:
        private static readonly MetadataReferenceProperties s_assemblyReferenceProperties =
            MetadataReferenceProperties.Assembly.WithRecursiveAliases(true);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="FilePath"/> changed.
        /// </summary>
        public ScriptOptions WithFilePath(string filePath)
            => (FilePath == filePath) ? this : new ScriptOptions(this) { FilePath = filePath ?? "" };

        private static MetadataReference CreateUnresolvedReference(string reference)
            => new UnresolvedMetadataReference(reference, s_assemblyReferenceProperties);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        private ScriptOptions WithReferences(ImmutableArray<MetadataReference> references)
            => MetadataReferences.Equals(references) ? this : new ScriptOptions(this) { MetadataReferences = CheckImmutableArray(references, nameof(references)) };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(IEnumerable<MetadataReference> references)
            => WithReferences(ToImmutableArrayChecked(references, nameof(references)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(params MetadataReference[] references)
            => WithReferences((IEnumerable<MetadataReference>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(IEnumerable<MetadataReference> references)
            => WithReferences(ConcatChecked(MetadataReferences, references, nameof(references)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params MetadataReference[] references)
            => AddReferences((IEnumerable<MetadataReference>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions WithReferences(IEnumerable<Assembly> references)
            => WithReferences(SelectChecked(references, nameof(references), CreateReferenceFromAssembly));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions WithReferences(params Assembly[] references)
            => WithReferences((IEnumerable<Assembly>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions AddReferences(IEnumerable<Assembly> references)
            => AddReferences(SelectChecked(references, nameof(references), CreateReferenceFromAssembly));

        private static MetadataReference CreateReferenceFromAssembly(Assembly assembly)
        {
            return MetadataReference.CreateFromAssemblyInternal(assembly, s_assemblyReferenceProperties);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions AddReferences(params Assembly[] references)
            => AddReferences((IEnumerable<Assembly>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(IEnumerable<string> references)
            => WithReferences(SelectChecked(references, nameof(references), CreateUnresolvedReference));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(params string[] references)
            => WithReferences((IEnumerable<string>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(IEnumerable<string> references)
            => AddReferences(SelectChecked(references, nameof(references), CreateUnresolvedReference));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params string[] references)
            => AddReferences((IEnumerable<string>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="MetadataResolver"/>.
        /// </summary>
        public ScriptOptions WithMetadataResolver(MetadataReferenceResolver resolver)
            => MetadataResolver == resolver ? this : new ScriptOptions(this) { MetadataResolver = resolver };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="SourceResolver"/>.
        /// </summary>
        public ScriptOptions WithSourceResolver(SourceReferenceResolver resolver)
            => SourceResolver == resolver ? this : new ScriptOptions(this) { SourceResolver = resolver };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Imports"/> changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        private ScriptOptions WithImports(ImmutableArray<string> imports)
            => Imports.Equals(imports) ? this : new ScriptOptions(this) { Imports = CheckImmutableArray(imports, nameof(imports)) };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Imports"/> changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions WithImports(IEnumerable<string> imports)
            => WithImports(ToImmutableArrayChecked(imports, nameof(imports)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Imports"/> changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions WithImports(params string[] imports)
            => WithImports((IEnumerable<string>)imports);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="Imports"/> added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions AddImports(IEnumerable<string> imports)
            => WithImports(ConcatChecked(Imports, imports, nameof(imports)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="Imports"/> added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions AddImports(params string[] imports)
            => AddImports((IEnumerable<string>)imports);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with debugging information enabled.
        /// </summary>
        public ScriptOptions WithEmitDebugInformation(bool emitDebugInformation)
            => emitDebugInformation == EmitDebugInformation ? this : new ScriptOptions(this) { EmitDebugInformation = emitDebugInformation };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="FileEncoding"/>.
        /// </summary>
        public ScriptOptions WithFileEncoding(Encoding encoding)
            => encoding == FileEncoding ? this : new ScriptOptions(this) { FileEncoding = encoding };

        /// <summary>
        /// Create a new <see cref="ScriptOptions"/> with the specified <see cref="OptimizationLevel"/>.
        /// </summary>
        /// <returns></returns>
        public ScriptOptions WithOptimizationLevel(OptimizationLevel optimizationLevel)
            => optimizationLevel == OptimizationLevel ? this : new ScriptOptions(this) { OptimizationLevel = optimizationLevel };

        /// <summary>
        /// Create a new <see cref="ScriptOptions"/> with unsafe code regions allowed.
        /// </summary>
        public ScriptOptions WithAllowUnsafe(bool allowUnsafe)
            => allowUnsafe == AllowUnsafe ? this : new ScriptOptions(this) { AllowUnsafe = allowUnsafe };

        /// <summary>
        /// Create a new <see cref="ScriptOptions"/> with bounds checking on integer arithmetic enforced.
        /// </summary>
        public ScriptOptions WithCheckOverflow(bool checkOverflow)
            => checkOverflow == CheckOverflow ? this : new ScriptOptions(this) { CheckOverflow = checkOverflow };

        /// <summary>
        /// Create a new <see cref="ScriptOptions"/> with the specific <see cref="WarningLevel"/>.
        /// </summary>
        public ScriptOptions WithWarningLevel(int warningLevel)
            => warningLevel == WarningLevel ? this : new ScriptOptions(this) { WarningLevel = warningLevel };

        internal ScriptOptions WithParseOptions(ParseOptions parseOptions)
            => parseOptions == ParseOptions ? this : new ScriptOptions(this) { ParseOptions = parseOptions };
    }
}
