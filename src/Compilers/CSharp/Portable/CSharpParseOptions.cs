﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
            IEnumerable<string> preprocessorSymbols = null)
            : this(languageVersion, 
                  documentationMode, 
                  kind, 
                  preprocessorSymbols.ToImmutableArrayOrEmpty(), 
                  ImmutableDictionary<string, string>.Empty)
        {
            // We test the mapped value, LanguageVersion, rather than the parameter, languageVersion,
            // which has not had "Latest" mapped to the latest version yet.
            if (!LanguageVersion.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(languageVersion));
            }

            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (preprocessorSymbols != null)
            {
                foreach (var preprocessorSymbol in preprocessorSymbols)
                {
                    if (!SyntaxFacts.IsValidIdentifier(preprocessorSymbol))
                    {
                        throw new ArgumentException($"{nameof(preprocessorSymbols)} contains a symbol that is not a valid identifier", nameof(preprocessorSymbols));
                    }
                }
            }
        }

        internal CSharpParseOptions(
            LanguageVersion languageVersion,
            DocumentationMode documentationMode,
            SourceCodeKind kind,
            IEnumerable<string> preprocessorSymbols,
            ImmutableDictionary<string, string> features)
            : this(languageVersion, documentationMode, kind, preprocessorSymbols)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = features;
        }

        private CSharpParseOptions(CSharpParseOptions other) : this(
            languageVersion: other.SpecifiedLanguageVersion,
            documentationMode: other.DocumentationMode,
            kind: other.Kind,
            preprocessorSymbols: other.PreprocessorSymbols,
            features: other.Features.ToImmutableDictionary())
        {
        }

        // No validation
        private CSharpParseOptions(
            LanguageVersion languageVersion,
            DocumentationMode documentationMode,
            SourceCodeKind kind,
            ImmutableArray<string> preprocessorSymbols,
            ImmutableDictionary<string, string> features)
            : base(kind, documentationMode)
        {
            Debug.Assert(!preprocessorSymbols.IsDefault);
            this.SpecifiedLanguageVersion = languageVersion;
            this.LanguageVersion = languageVersion.MapSpecifiedToEffectiveVersion();
            this.PreprocessorSymbols = preprocessorSymbols;
            _features = features;
        }

        public override string Language => LanguageNames.CSharp;

        public new CSharpParseOptions WithKind(SourceCodeKind kind)
        {
            if (kind == this.Kind)
            {
                return this;
            }

            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new CSharpParseOptions(this) { Kind = kind };
        }

        public CSharpParseOptions WithLanguageVersion(LanguageVersion version)
        {
            if (version == this.SpecifiedLanguageVersion)
            {
                return this;
            }

            var effectiveLanguageVersion = version.MapSpecifiedToEffectiveVersion();
            if (!effectiveLanguageVersion.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            return new CSharpParseOptions(this) { SpecifiedLanguageVersion = version, LanguageVersion = effectiveLanguageVersion };
        }

        public CSharpParseOptions WithPreprocessorSymbols(IEnumerable<string> preprocessorSymbols)
        {
            return WithPreprocessorSymbols(preprocessorSymbols.AsImmutableOrNull());
        }

        public CSharpParseOptions WithPreprocessorSymbols(params string[] preprocessorSymbols)
        {
            return WithPreprocessorSymbols(ImmutableArray.Create(preprocessorSymbols));
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

            if (!documentationMode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(documentationMode));
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

        protected override ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features)
        {
            return WithFeatures(features);
        }

        /// <summary>
        /// Enable some experimental language features for testing.
        /// </summary>
        public new CSharpParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features)
        {
            if (features == null)
            {
                throw new ArgumentNullException(nameof(features));
            }

            return new CSharpParseOptions(this) { _features = features.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase) };
        }

        public override IReadOnlyDictionary<string, string> Features
        {
            get
            {
                return _features;
            }
        }

        internal bool IsFeatureEnabled(MessageID feature)
        {
            string featureFlag = feature.RequiredFeature();
            if (featureFlag != null)
            {
                return Features.ContainsKey(featureFlag);
            }
            LanguageVersion availableVersion = LanguageVersion;
            LanguageVersion requiredVersion = feature.RequiredVersion();
            return availableVersion >= requiredVersion;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CSharpParseOptions);
        }

        public bool Equals(CSharpParseOptions other)
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
