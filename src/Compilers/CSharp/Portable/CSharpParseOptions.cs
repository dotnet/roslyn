// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This class stores several source parsing related options and offers access to their values.
    /// </summary>
    public sealed class CSharpParseOptions : ParseOptions, IEquatable<CSharpParseOptions>
    {
        /// <summary>
        /// The default parse options.
        /// </summary>
        public static CSharpParseOptions Default { get; } = new CSharpParseOptions();

        private ImmutableDictionary<string, string> _features;
        private ImmutableArray<ImmutableArray<string>> _interceptorsNamespaces;

        /// <summary>
        /// Gets the effective language version, which the compiler uses to select the
        /// language rules to apply to the program.
        /// </summary>
        public LanguageVersion LanguageVersion { get; private set; }

        /// <summary>
        /// Gets the specified language version, which is the value that was specified in
        /// the call to the constructor, or modified using the <see cref="WithLanguageVersion"/> method,
        /// or provided on the command line.
        /// </summary>
        public LanguageVersion SpecifiedLanguageVersion { get; private set; }

        internal ImmutableArray<string> PreprocessorSymbols { get; private set; }

        /// <summary>
        /// Gets the names of defined preprocessor symbols.
        /// </summary>
        public override IEnumerable<string> PreprocessorSymbolNames
        {
            get { return PreprocessorSymbols; }
        }

        public CSharpParseOptions(
            LanguageVersion languageVersion = LanguageVersion.Default,
            DocumentationMode documentationMode = DocumentationMode.Parse,
            SourceCodeKind kind = SourceCodeKind.Regular,
            IEnumerable<string>? preprocessorSymbols = null)
            : this(languageVersion,
                  documentationMode,
                  kind,
                  preprocessorSymbols.ToImmutableArrayOrEmpty(),
                  ImmutableDictionary<string, string>.Empty)
        {
        }

        internal CSharpParseOptions(
            LanguageVersion languageVersion,
            DocumentationMode documentationMode,
            SourceCodeKind kind,
            ImmutableArray<string> preprocessorSymbols,
            IReadOnlyDictionary<string, string>? features)
            : base(kind, documentationMode)
        {
            this.SpecifiedLanguageVersion = languageVersion;
            this.LanguageVersion = languageVersion.MapSpecifiedToEffectiveVersion();
            this.PreprocessorSymbols = preprocessorSymbols.ToImmutableArrayOrEmpty();
            _features = features?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty;
        }

        private CSharpParseOptions(CSharpParseOptions other) : this(
            languageVersion: other.SpecifiedLanguageVersion,
            documentationMode: other.DocumentationMode,
            kind: other.Kind,
            preprocessorSymbols: other.PreprocessorSymbols,
            features: other.Features)
        {
        }

        public override string Language => LanguageNames.CSharp;

        public new CSharpParseOptions WithKind(SourceCodeKind kind)
        {
            if (kind == this.SpecifiedKind)
            {
                return this;
            }

            var effectiveKind = kind.MapSpecifiedToEffectiveKind();
            return new CSharpParseOptions(this) { SpecifiedKind = kind, Kind = effectiveKind };
        }

        public CSharpParseOptions WithLanguageVersion(LanguageVersion version)
        {
            if (version == this.SpecifiedLanguageVersion)
            {
                return this;
            }

            var effectiveLanguageVersion = version.MapSpecifiedToEffectiveVersion();
            return new CSharpParseOptions(this) { SpecifiedLanguageVersion = version, LanguageVersion = effectiveLanguageVersion };
        }

        public CSharpParseOptions WithPreprocessorSymbols(IEnumerable<string>? preprocessorSymbols)
        {
            return WithPreprocessorSymbols(preprocessorSymbols.AsImmutableOrNull());
        }

        public CSharpParseOptions WithPreprocessorSymbols(params string[]? preprocessorSymbols)
        {
            return WithPreprocessorSymbols(preprocessorSymbols.AsImmutableOrNull());
        }

        public CSharpParseOptions WithPreprocessorSymbols(ImmutableArray<string> symbols)
        {
            if (symbols.IsDefault)
            {
                symbols = ImmutableArray<string>.Empty;
            }

            if (symbols.Equals(this.PreprocessorSymbols))
            {
                return this;
            }

            return new CSharpParseOptions(this) { PreprocessorSymbols = symbols };
        }

        public new CSharpParseOptions WithDocumentationMode(DocumentationMode documentationMode)
        {
            if (documentationMode == this.DocumentationMode)
            {
                return this;
            }

            return new CSharpParseOptions(this) { DocumentationMode = documentationMode };
        }

        public override ParseOptions CommonWithKind(SourceCodeKind kind)
        {
            return WithKind(kind);
        }

        protected override ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode)
        {
            return WithDocumentationMode(documentationMode);
        }

        protected override ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>>? features)
        {
            return WithFeatures(features);
        }

        /// <summary>
        /// Enable some experimental language features for testing.
        /// </summary>
        public new CSharpParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>>? features)
        {
            ImmutableDictionary<string, string> dictionary =
                features?.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase)
                ?? ImmutableDictionary<string, string>.Empty;

            return new CSharpParseOptions(this) { _features = dictionary };
        }

        public override IReadOnlyDictionary<string, string> Features
        {
            get
            {
                return _features;
            }
        }

        internal ImmutableArray<ImmutableArray<string>> InterceptorsNamespaces
        {
            get
            {
                if (!_interceptorsNamespaces.IsDefault)
                {
                    return _interceptorsNamespaces;
                }

                // e.g. [["System", "Threading"], ["System", "Collections"]]
                ImmutableArray<ImmutableArray<string>> previewNamespaces = Features.TryGetValue("InterceptorsNamespaces", out var namespaces) && namespaces.Length > 0
                    ? makeNamespaces(namespaces)
                    : ImmutableArray<ImmutableArray<string>>.Empty;

                ImmutableInterlocked.InterlockedInitialize(ref _interceptorsNamespaces, previewNamespaces);
                return previewNamespaces;

                static ImmutableArray<ImmutableArray<string>> makeNamespaces(string namespaces)
                {
                    var builder = ArrayBuilder<ImmutableArray<string>>.GetInstance();
                    var singleNamespaceBuilder = ArrayBuilder<string>.GetInstance();
                    int currentIndex = 0;
                    while (currentIndex < namespaces.Length && namespaces.IndexOf(';', currentIndex) is not -1 and var semicolonIndex)
                    {
                        addSingleNamespaceParts(builder, singleNamespaceBuilder, namespaces.AsSpan(currentIndex, semicolonIndex - currentIndex));
                        currentIndex = semicolonIndex + 1;
                    }

                    addSingleNamespaceParts(builder, singleNamespaceBuilder, namespaces.AsSpan(currentIndex));
                    singleNamespaceBuilder.Free();
                    return builder.ToImmutableAndFree();
                }

                static void addSingleNamespaceParts(ArrayBuilder<ImmutableArray<string>> namespacesBuilder, ArrayBuilder<string> singleNamespaceBuilder, ReadOnlySpan<char> @namespace)
                {
                    int currentIndex = 0;
                    while (currentIndex < @namespace.Length && @namespace.IndexOf('.', currentIndex) is not -1 and var dotIndex)
                    {
                        singleNamespaceBuilder.Add(@namespace.Slice(currentIndex, dotIndex - currentIndex).ToString());
                        currentIndex = dotIndex + 1;
                    }
                    singleNamespaceBuilder.Add(@namespace.Slice(currentIndex).ToString());

                    if (!namespacesBuilder.Any(ns => ns.SequenceEqual(singleNamespaceBuilder)))
                    {
                        namespacesBuilder.Add(singleNamespaceBuilder.ToImmutable());
                    }

                    singleNamespaceBuilder.Clear();
                }
            }
        }

        internal override void ValidateOptions(ArrayBuilder<Diagnostic> builder)
        {
            ValidateOptions(builder, MessageProvider.Instance);

            // Validate LanguageVersion not SpecifiedLanguageVersion, after Latest/Default has been converted:
            if (!LanguageVersion.IsValid())
            {
                builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_BadLanguageVersion, LanguageVersion.ToString()));
            }

            if (!PreprocessorSymbols.IsDefaultOrEmpty)
            {
                foreach (var symbol in PreprocessorSymbols)
                {
                    if (symbol == null)
                    {
                        builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_InvalidPreprocessingSymbol, "null"));
                    }
                    else if (!SyntaxFacts.IsValidIdentifier(symbol))
                    {
                        builder.Add(Diagnostic.Create(MessageProvider.Instance, (int)ErrorCode.ERR_InvalidPreprocessingSymbol, symbol));
                    }
                }
            }
        }

        internal bool IsFeatureEnabled(MessageID feature)
        {
            string? featureFlag = feature.RequiredFeature();
            if (featureFlag != null)
            {
                return Features.ContainsKey(featureFlag);
            }
            LanguageVersion availableVersion = LanguageVersion;
            LanguageVersion requiredVersion = feature.RequiredVersion();
            return availableVersion >= requiredVersion;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as CSharpParseOptions);
        }

        public bool Equals(CSharpParseOptions? other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (!base.EqualsHelper(other))
            {
                return false;
            }

            return this.SpecifiedLanguageVersion == other.SpecifiedLanguageVersion;
        }

        public override int GetHashCode()
        {
            return
                Hash.Combine(base.GetHashCodeHelper(),
                Hash.Combine((int)this.SpecifiedLanguageVersion, 0));
        }
    }
}
