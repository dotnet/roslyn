// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Text;

namespace Microsoft.CodeAnalysis.Scripting
{
    using static ParameterValidationHelpers;

    /// <summary>
    /// Options for creating and running scripts.
    /// </summary>
    public sealed class ScriptOptions
    {
        public static ScriptOptions Default { get; } = new ScriptOptions(
            filePath: "",
            references: GetDefaultMetadataReferences(),
            namespaces: ImmutableArray<string>.Empty,
            metadataResolver: RuntimeMetadataReferenceResolver.Default,
            sourceResolver: SourceFileResolver.Default,
            emitDebugInformation: false,
            fileEncoding: null);

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

        internal ScriptOptions(
            string filePath,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<string> namespaces,
            MetadataReferenceResolver metadataResolver,
            SourceReferenceResolver sourceResolver,
            bool emitDebugInformation,
            Encoding fileEncoding)
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
        }

        private ScriptOptions(ScriptOptions other)
            : this(filePath: other.FilePath,
                   references: other.MetadataReferences,
                   namespaces: other.Imports,
                   metadataResolver: other.MetadataResolver,
                   sourceResolver: other.SourceResolver,
                   emitDebugInformation: other.EmitDebugInformation,
                   fileEncoding: other.FileEncoding)
        {
        }

        // a reference to an assembly should by default be equivalent to #r, which applies recursive global alias:
        private static readonly MetadataReferenceProperties s_assemblyReferenceProperties =
            MetadataReferenceProperties.Assembly.WithRecursiveAliases(true);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="FilePath"/> changed.
        /// </summary>
        public ScriptOptions WithFilePath(string filePath) =>
            (FilePath == filePath) ? this : new ScriptOptions(this) { FilePath = filePath ?? "" };

        private static MetadataReference CreateUnresolvedReference(string reference) =>
            new UnresolvedMetadataReference(reference, s_assemblyReferenceProperties);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        private ScriptOptions WithReferences(ImmutableArray<MetadataReference> references) =>
            MetadataReferences.Equals(references) ? this : new ScriptOptions(this) { MetadataReferences = CheckImmutableArray(references, nameof(references)) };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(IEnumerable<MetadataReference> references) =>
            WithReferences(ToImmutableArrayChecked(references, nameof(references)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(params MetadataReference[] references) =>
            WithReferences((IEnumerable<MetadataReference>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(IEnumerable<MetadataReference> references) =>
            WithReferences(ConcatChecked(MetadataReferences, references, nameof(references)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params MetadataReference[] references) =>
            AddReferences((IEnumerable<MetadataReference>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions WithReferences(IEnumerable<Assembly> references) =>
            WithReferences(SelectChecked(references, nameof(references), CreateReferenceFromAssembly));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions WithReferences(params Assembly[] references) =>
            WithReferences((IEnumerable<Assembly>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions AddReferences(IEnumerable<Assembly> references) =>
            AddReferences(SelectChecked(references, nameof(references), CreateReferenceFromAssembly));

        private static MetadataReference CreateReferenceFromAssembly(Assembly assembly)
        {
            return MetadataReference.CreateFromAssemblyInternal(assembly, s_assemblyReferenceProperties);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        /// <exception cref="NotSupportedException">Specified assembly is not supported (e.g. it's a dynamic assembly).</exception>
        public ScriptOptions AddReferences(params Assembly[] references) =>
            AddReferences((IEnumerable<Assembly>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(IEnumerable<string> references) =>
            WithReferences(SelectChecked(references, nameof(references), CreateUnresolvedReference));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(params string[] references) =>
            WithReferences((IEnumerable<string>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(IEnumerable<string> references) =>
            AddReferences(SelectChecked(references, nameof(references), CreateUnresolvedReference));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params string[] references) =>
            AddReferences((IEnumerable<string>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="MetadataResolver"/>.
        /// </summary>
        public ScriptOptions WithMetadataResolver(MetadataReferenceResolver resolver) =>
            MetadataResolver == resolver ? this : new ScriptOptions(this) { MetadataResolver = resolver };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="SourceResolver"/>.
        /// </summary>
        public ScriptOptions WithSourceResolver(SourceReferenceResolver resolver) =>
            SourceResolver == resolver ? this : new ScriptOptions(this) { SourceResolver = resolver };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Imports"/> changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        private ScriptOptions WithImports(ImmutableArray<string> imports) =>
            Imports.Equals(imports) ? this : new ScriptOptions(this) { Imports = CheckImmutableArray(imports, nameof(imports)) };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Imports"/> changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions WithImports(IEnumerable<string> imports) =>
            WithImports(ToImmutableArrayChecked(imports, nameof(imports)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Imports"/> changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions WithImports(params string[] imports) =>
            WithImports((IEnumerable<string>)imports);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="Imports"/> added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions AddImports(IEnumerable<string> imports) =>
            WithImports(ConcatChecked(Imports, imports, nameof(imports)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="Imports"/> added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="imports"/> is null or contains a null reference.</exception>
        public ScriptOptions AddImports(params string[] imports) =>
            AddImports((IEnumerable<string>)imports);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with debugging information enabled.
        /// </summary>
        public ScriptOptions WithEmitDebugInformation(bool emitDebugInformation) =>
            emitDebugInformation == EmitDebugInformation ? this : new ScriptOptions(this) { EmitDebugInformation = emitDebugInformation };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="FileEncoding"/>.
        /// </summary>
        public ScriptOptions WithFileEncoding(Encoding encoding) =>
            encoding == FileEncoding ? this : new ScriptOptions(this) { FileEncoding = encoding };
    }
}
