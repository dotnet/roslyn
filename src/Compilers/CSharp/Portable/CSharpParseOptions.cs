// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Gets the language version.
        /// </summary>
        public LanguageVersion LanguageVersion { get; private set; }

        internal ImmutableArray<string> PreprocessorSymbols { get; private set; }

        /// <summary>
        /// Gets the names of defined preprocessor symbols.
        /// </summary>
        public override IEnumerable<string> PreprocessorSymbolNames
        {
            get { return PreprocessorSymbols; }
        }

        public CSharpParseOptions(
            LanguageVersion languageVersion = LanguageVersion.CSharp6,
            DocumentationMode documentationMode = DocumentationMode.Parse,
            SourceCodeKind kind = SourceCodeKind.Regular,
            IEnumerable<string> preprocessorSymbols = null)
            : this(languageVersion, documentationMode, kind, preprocessorSymbols.ToImmutableArrayOrEmpty())
        {
            if (!languageVersion.IsValid())
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
                        throw new ArgumentException("preprocessorSymbols");
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
            languageVersion: other.LanguageVersion,
            documentationMode: other.DocumentationMode,
            kind: other.Kind,
            preprocessorSymbols: other.PreprocessorSymbols,
            features: other.Features.ToImmutableDictionary())
        {
        }

        // No validation
        internal CSharpParseOptions(
            LanguageVersion languageVersion,
            DocumentationMode documentationMode,
            SourceCodeKind kind,
            ImmutableArray<string> preprocessorSymbols)
            : base(kind, documentationMode)
        {
            Debug.Assert(!preprocessorSymbols.IsDefault);
            this.LanguageVersion = languageVersion;
            this.PreprocessorSymbols = preprocessorSymbols;
            _features = ImmutableDictionary<string, string>.Empty;
        }

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
            if (version == this.LanguageVersion)
            {
                return this;
            }

            if (!version.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            return new CSharpParseOptions(this) { LanguageVersion = version };
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
            switch (feature)
            {
                case MessageID.IDS_FeatureBinaryLiteral:
                case MessageID.IDS_FeatureDigitSeparator:
                case MessageID.IDS_FeatureLocalFunctions:
                case MessageID.IDS_FeatureRefLocalsReturns:
                case MessageID.IDS_FeaturePatternMatching:
                    // in "demo" mode enable proposed new C# 7 language features.
                    if (PreprocessorSymbols.Contains("__DEMO__") ||
                        PreprocessorSymbols.Contains("__DEMO_EXPERIMENTAL__"))
                    {
                        return true;
                    }
                    break;
                case MessageID.IDS_FeaturePatternMatching2:
                    // in "experimental" mode enable experimental and proposed new C# 7 language features.
                    if (PreprocessorSymbols.Contains("__DEMO_EXPERIMENTAL__"))
                    {
                        return true;
                    }
                    break;
                default:
                    break;
            }

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

            return this.LanguageVersion == other.LanguageVersion;
        }

        public override int GetHashCode()
        {
            return
                Hash.Combine(base.GetHashCodeHelper(),
                Hash.Combine((int)this.LanguageVersion, 0));
        }
    }
}
