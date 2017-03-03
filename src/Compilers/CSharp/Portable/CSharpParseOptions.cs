// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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
        }

        internal CSharpParseOptions(
            LanguageVersion languageVersion,
            DocumentationMode documentationMode,
            SourceCodeKind kind,
            IEnumerable<string> preprocessorSymbols,
            IReadOnlyDictionary<string, string> features)
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

        /// <summary>
        /// Parse a LanguageVersion from a string input, as the command-line compiler does.
        /// </summary>
        public static bool TryParseLanguageVersion(string version, out LanguageVersion result)
        {
            if (version == null)
            {
                result = LanguageVersion.Default;
                return true;
            }

            switch (version.ToLowerInvariant())
            {
                case "iso-1":
                    result = LanguageVersion.CSharp1;
                    return true;

                case "iso-2":
                    result = LanguageVersion.CSharp2;
                    return true;

                case "7":
                    result = LanguageVersion.CSharp7;
                    return true;

                case "default":
                    result = LanguageVersion.Default;
                    return true;

                case "latest":
                    result = LanguageVersion.Latest;
                    return true;

                default:
                    // We are likely to introduce minor version numbers after C# 7, thus breaking the
                    // one-to-one correspondence between the integers and the corresponding
                    // LanguageVersion enum values. But for compatibility we continue to accept any
                    // integral value parsed by int.TryParse for its corresponding LanguageVersion enum
                    // value for language version C# 6 and earlier (e.g. leading zeros are allowed)
                    int versionNumber;
                    if (int.TryParse(version, NumberStyles.None, CultureInfo.InvariantCulture, out versionNumber) &&
                        versionNumber <= 6 &&
                        ((LanguageVersion)versionNumber).IsValid())
                    {
                        result = (LanguageVersion)versionNumber;
                        return true;
                    }

                    result = LanguageVersion.Default;
                    return false;
            }
        }
    }
}
