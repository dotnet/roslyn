// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents parse options common to C# and VB.
    /// </summary>
    public abstract class ParseOptions
    {
        /// <summary>
        /// Specifies whether to parse as regular code files, script files or interactive code.
        /// </summary>
        public SourceCodeKind Kind { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the documentation comments are parsed.
        /// </summary>
        /// <value><c>true</c> if documentation comments are parsed, <c>false</c> otherwise.</value>
        public DocumentationMode DocumentationMode { get; protected set; }

        internal ParseOptions(SourceCodeKind kind, DocumentationMode documentationMode)
        {
            this.Kind = kind;
            this.DocumentationMode = documentationMode;
        }

        /// <summary>
        /// Creates a new options instance with the specified source code kind.
        /// </summary>
        public ParseOptions WithKind(SourceCodeKind kind)
        {
            return CommonWithKind(kind);
        }

        // It was supposed to be a protected implementation detail. 
        // The "pattern" we have for these is the public With* method is the only public callable one, 
        // and that forwards to the protected Common* like all the other methods in the class (see PR #10304 comments by Jason Malinowski) 
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
        public ParseOptions WithFeatures(IEnumerable<KeyValuePair<string, string>> features)
        {
            return CommonWithFeatures(features);
        }

        protected abstract ParseOptions CommonWithFeatures(IEnumerable<KeyValuePair<string, string>> features);

        /// <summary>
        /// Returns the experimental features.
        /// </summary>
        public abstract IReadOnlyDictionary<string, string> Features
        {
            get;
        }

        /// <summary>
        /// Names of defined preprocessor symbols.
        /// </summary>
        public abstract IEnumerable<string> PreprocessorSymbolNames { get; }

        public abstract override bool Equals(object obj);

        protected bool EqualsHelper(ParseOptions other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return
                this.Kind == other.Kind &&
                this.DocumentationMode == other.DocumentationMode &&
                this.Features.SequenceEqual(other.Features) &&
                (this.PreprocessorSymbolNames == null ? other.PreprocessorSymbolNames == null : this.PreprocessorSymbolNames.SequenceEqual(other.PreprocessorSymbolNames, StringComparer.Ordinal));
        }

        public abstract override int GetHashCode();

        protected int GetHashCodeHelper()
        {
            return
                Hash.Combine((int)this.Kind,
                Hash.Combine((int)this.DocumentationMode,
                Hash.Combine(HashFeatures(this.Features),
                Hash.Combine(Hash.CombineValues(this.PreprocessorSymbolNames, StringComparer.Ordinal), 0))));
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

        public static bool operator ==(ParseOptions left, ParseOptions right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(ParseOptions left, ParseOptions right)
        {
            return !object.Equals(left, right);
        }
    }
}
