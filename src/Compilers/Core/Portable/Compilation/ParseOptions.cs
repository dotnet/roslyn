// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents parse options common to C# and VB.
    /// </summary>
    public abstract class ParseOptions
    {
        private readonly Lazy<ImmutableArray<Diagnostic>> _lazyErrors;

        /// <summary>
        /// Specifies whether to parse as regular code files, script files or interactive code.
        /// </summary>
        public SourceCodeKind Kind { get; protected set; }

        /// <summary>
        /// Gets the specified source code kind, which is the value that was specified in
        /// the call to the constructor, or modified using the <see cref="WithKind(SourceCodeKind)"/> method.
        /// </summary>
        public SourceCodeKind SpecifiedKind { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the documentation comments are parsed and analyzed.
        /// </summary>
        public DocumentationMode DocumentationMode { get; protected set; }

        internal ParseOptions(SourceCodeKind kind, DocumentationMode documentationMode)
        {
            this.SpecifiedKind = kind;
            this.Kind = kind.MapSpecifiedToEffectiveKind();
            this.DocumentationMode = documentationMode;

            _lazyErrors = new Lazy<ImmutableArray<Diagnostic>>(() =>
            {
                var builder = ArrayBuilder<Diagnostic>.GetInstance();
                ValidateOptions(builder);
                return builder.ToImmutableAndFree();
            });
        }

        /// <summary>
        /// Gets the source language ("C#" or "Visual Basic").
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Errors collection related to an incompatible set of parse options
        /// </summary>
        public ImmutableArray<Diagnostic> Errors
        {
            get { return _lazyErrors.Value; }
        }

        /// <summary>
        /// Creates a new options instance with the specified source code kind.
        /// </summary>
        public ParseOptions WithKind(SourceCodeKind kind)
        {
            return CommonWithKind(kind);
        }

        /// <summary>
        /// Performs validation of options compatibilities and generates diagnostics if needed
        /// </summary>
        internal abstract void ValidateOptions(ArrayBuilder<Diagnostic> builder);

        internal void ValidateOptions(ArrayBuilder<Diagnostic> builder, CommonMessageProvider messageProvider)
        {
            // Validate SpecifiedKind not Kind, to catch deprecated specified kinds:
            if (!SpecifiedKind.IsValid())
            {
                builder.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_BadSourceCodeKind, Location.None, SpecifiedKind.ToString()));
            }

            if (!DocumentationMode.IsValid())
            {
                builder.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_BadDocumentationMode, Location.None, DocumentationMode.ToString()));
            }
        }

        // It was supposed to be a protected implementation detail. 
        // The "pattern" we have for these is the public With* method is the only public callable one, 
        // and that forwards to the protected Common* like all the other methods in the class. 
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract ParseOptions CommonWithKind(SourceCodeKind kind);

        /// <summary>
        /// Creates a new options instance with the specified documentation mode.
        /// </summary>
        public ParseOptions WithDocumentationMode(DocumentationMode documentationMode)
        {
            return CommonWithDocumentationMode(documentationMode);
        }

        protected abstract ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode);

        /// <summary>
        /// Enable some experimental language features for testing.
        /// </summary>
        // Note: use Feature entry for known feature flags.
        public ParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features)
        {
            return CommonWithFeatures(features);
        }

        protected abstract ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);

        /// <summary>
        /// Returns the experimental features.
        /// </summary>
        // Note: use Feature entry for known feature flags.
        public abstract IReadOnlyDictionary<string, string> Features
        {
            get;
        }

        internal bool HasFeature(string feature)
        {
            Feature.AssertValidFeature(feature);
            return Features.TryGetValue(feature, out var value) && value is not null;
        }

        /// <summary>
        /// Names of defined preprocessor symbols.
        /// </summary>
        public abstract IEnumerable<string> PreprocessorSymbolNames { get; }

        public abstract override bool Equals(object? obj);

        protected bool EqualsHelper([NotNullWhen(true)] ParseOptions? other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return
                this.SpecifiedKind == other.SpecifiedKind &&
                this.DocumentationMode == other.DocumentationMode &&
                FeaturesEqual(Features, other.Features) &&
                (this.PreprocessorSymbolNames == null ? other.PreprocessorSymbolNames == null : this.PreprocessorSymbolNames.SequenceEqual(other.PreprocessorSymbolNames, StringComparer.Ordinal));
        }

        public abstract override int GetHashCode();

        protected int GetHashCodeHelper()
        {
            return
                Hash.Combine((int)this.SpecifiedKind,
                Hash.Combine((int)this.DocumentationMode,
                Hash.Combine(HashFeatures(this.Features),
                Hash.Combine(Hash.CombineValues(this.PreprocessorSymbolNames, StringComparer.Ordinal), 0))));
        }

        private static bool FeaturesEqual(IReadOnlyDictionary<string, string> features, IReadOnlyDictionary<string, string> other)
        {
            if (ReferenceEquals(features, other))
            {
                return true;
            }

            if (features.Count != other.Count)
            {
                return false;
            }

            foreach (var (key, value) in features)
            {
                if (!other.TryGetValue(key, out var otherValue) || value != otherValue)
                {
                    return false;
                }
            }

            return true;
        }

        private static int HashFeatures(IReadOnlyDictionary<string, string> features)
        {
            int value = 0;
            foreach (var kv in features)
            {
                value = Hash.Combine(kv.Key.GetHashCode(),
                        Hash.Combine(kv.Value.GetHashCode(), value));
            }

            return value;
        }

        public static bool operator ==(ParseOptions? left, ParseOptions? right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(ParseOptions? left, ParseOptions? right)
        {
            return !object.Equals(left, right);
        }
    }
}
