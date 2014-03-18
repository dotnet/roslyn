// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents parse options common to C# and VB.
    /// </summary>
    [Serializable]
    public abstract class ParseOptions : ISerializable
    {
        /// <summary>
        /// Specifies whether to parse as regular code files, script files or interactive code.
        /// </summary>
        public readonly SourceCodeKind Kind;

        /// <summary>
        /// Gets a value indicating whether the documentation comments are parsed.
        /// </summary>
        /// <value><c>true</c> if documentation comments are parsed, <c>false</c> otherwise.</value>
        public readonly DocumentationMode DocumentationMode;

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

        protected abstract ParseOptions CommonWithKind(SourceCodeKind kind);

        /// <summary>
        /// Creates a new options instance with the specified documentation mode.
        /// </summary>
        public ParseOptions WithDocumentationMode(DocumentationMode documentationMode)
        {
            return CommonWithDocumentationMode(documentationMode);
        }

        protected abstract ParseOptions CommonWithDocumentationMode(DocumentationMode documentationMode);

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
                (this.PreprocessorSymbolNames == null ? other.PreprocessorSymbolNames == null : this.PreprocessorSymbolNames.SequenceEqual(other.PreprocessorSymbolNames, StringComparer.Ordinal));
        }

        public abstract override int GetHashCode();

        protected int GetHashCodeHelper()
        {
            return
                Hash.Combine((int)this.Kind,
                Hash.Combine((int)this.DocumentationMode,
                Hash.Combine(Hash.CombineValues(this.PreprocessorSymbolNames, StringComparer.Ordinal), 0)));
        }

        public static bool operator ==(ParseOptions left, ParseOptions right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(ParseOptions left, ParseOptions right)
        {
            return !object.Equals(left, right);
        }

        #region "serialization"

        protected ParseOptions(SerializationInfo info, StreamingContext context)
        {
            //public readonly SourceCodeKind Kind;
            this.Kind = (SourceCodeKind)info.GetValue("Kind", typeof(SourceCodeKind));

            //public readonly DocumentationMode DocumentationMode;
            this.DocumentationMode = (DocumentationMode)info.GetValue("DocumentationMode", typeof(DocumentationMode));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //public readonly SourceCodeKind Kind;
            info.AddValue("Kind", Kind, typeof(SourceCodeKind));

            //public readonly DocumentationMode DocumentationMode;
            info.AddValue("DocumentationMode", DocumentationMode, typeof(DocumentationMode));
        }

        #endregion
    }
}
