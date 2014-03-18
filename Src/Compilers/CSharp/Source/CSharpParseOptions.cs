// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This class stores several source parsing related options and offers access to their values.
    /// </summary>
    [Serializable]
    public sealed class CSharpParseOptions : ParseOptions, IEquatable<CSharpParseOptions>, ISerializable
    {
        /// <summary>
        /// The default parse options.
        /// </summary>
        public static readonly CSharpParseOptions Default = new CSharpParseOptions();

        /// <summary>
        /// Gets the language version.
        /// </summary>
        public readonly LanguageVersion LanguageVersion;

        internal readonly ImmutableArray<string> PreprocessorSymbols;

        /// <summary>
        /// Gets the names of defined preprocessor symbols.
        /// </summary>
        public override IEnumerable<string> PreprocessorSymbolNames
        {
            get { return PreprocessorSymbols; }
        }

        // NOTE: warnaserror[+|-], warnaserror[+|-]:<warn list>, unsafe[+|-], warn:<n>, nowarn:<warn list>

        public CSharpParseOptions(
            LanguageVersion languageVersion = LanguageVersion.CSharp6,
            DocumentationMode documentationMode = DocumentationMode.Parse,
            SourceCodeKind kind = SourceCodeKind.Regular,
            params string[] preprocessorSymbols)
            : this(languageVersion, documentationMode, kind, preprocessorSymbols.AsImmutableOrEmpty())
        {
        }

        public CSharpParseOptions(
            LanguageVersion languageVersion = LanguageVersion.CSharp6,
            DocumentationMode documentationMode = DocumentationMode.Parse,
            SourceCodeKind kind = SourceCodeKind.Regular,
            ImmutableArray<string> preprocessorSymbols = default(ImmutableArray<string>))
            : this(languageVersion, documentationMode, kind, preprocessorSymbols.NullToEmpty(), privateCtor: true)
        {
            if (!languageVersion.IsValid())
            {
                throw new ArgumentOutOfRangeException("languageVersion");
            }

            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException("kind");
            }

            if (!preprocessorSymbols.IsDefaultOrEmpty)
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

        // No validation
        internal CSharpParseOptions(
            LanguageVersion languageVersion,
            DocumentationMode documentationMode,
            SourceCodeKind kind,
            ImmutableArray<string> preprocessorSymbols,
            bool privateCtor) //dummy param to distinguish from public ctor
            : base(kind, documentationMode)
        {
            Debug.Assert(!preprocessorSymbols.IsDefault);
            this.LanguageVersion = languageVersion;
            this.PreprocessorSymbols = preprocessorSymbols;
        }

        public new CSharpParseOptions WithKind(SourceCodeKind kind)
        {
            if (kind == this.Kind)
            {
                return this;
            }

            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException("kind");
            }

            return new CSharpParseOptions(
                this.LanguageVersion,
                this.DocumentationMode,
                kind,
                this.PreprocessorSymbols,
                privateCtor: true
            );
        }

        protected override ParseOptions CommonWithKind(SourceCodeKind kind)
        {
            return WithKind(kind);
        }

        protected override ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode)
        {
            return WithDocumentationMode(documentationMode);
        }

        public CSharpParseOptions WithLanguageVersion(LanguageVersion version)
        {
            if (version == this.LanguageVersion)
            {
                return this;
            }

            if (!version.IsValid())
            {
                throw new ArgumentOutOfRangeException("version");
            }

            return new CSharpParseOptions(
                version,
                this.DocumentationMode,
                this.Kind,
                this.PreprocessorSymbols,
                privateCtor: true
            );
        }

        public CSharpParseOptions WithPreprocessorSymbols(IEnumerable<string> preprocessorSymbols)
        {
            return WithPreprocessorSymbols(preprocessorSymbols.AsImmutableOrNull());
        }

        public CSharpParseOptions WithPreprocessorSymbols(params string[] preprocessorSymbols)
        {
            return WithPreprocessorSymbols(ImmutableArray.Create<string>(preprocessorSymbols));
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

            return new CSharpParseOptions(
                this.LanguageVersion,
                this.DocumentationMode,
                this.Kind,
                symbols,
                privateCtor: true
            );
        }

        public new CSharpParseOptions WithDocumentationMode(DocumentationMode documentationMode)
        {
            if (documentationMode == this.DocumentationMode)
            {
                return this;
            }

            if (!documentationMode.IsValid())
            {
                throw new ArgumentOutOfRangeException("documentationMode");
            }

            return new CSharpParseOptions(
                this.LanguageVersion,
                documentationMode,
                this.Kind,
                this.PreprocessorSymbols,
                privateCtor: true
            );
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

        #region "serialization"

        private CSharpParseOptions(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            //public readonly LanguageVersion LanguageVersion;
            this.LanguageVersion = (LanguageVersion)info.GetValue("LanguageVersion", typeof(LanguageVersion));

            //internal readonly ImmutableArray<string> PreprocessorSymbols;
            this.PreprocessorSymbols = info.GetArray<string>("PreprocessorSymbols");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            //public readonly LanguageVersion LanguageVersion;
            info.AddValue("LanguageVersion", this.LanguageVersion, typeof(LanguageVersion));

            //internal readonly ImmutableArray<string> PreprocessorSymbols;
            info.AddArray("PreprocessorSymbols", this.PreprocessorSymbols);
        }

        #endregion
    }
}