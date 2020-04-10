﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in source code.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public abstract class Location
    {
        internal Location()
        {
        }

        /// <summary>
        /// Location kind (None/SourceFile/MetadataFile).
        /// </summary>
        public abstract LocationKind Kind { get; }

        /// <summary>
        /// Returns true if the location represents a specific location in a source code file.
        /// </summary>
        public bool IsInSource { get { return SourceTree != null; } }

        /// <summary>
        /// Returns true if the location is in metadata.
        /// </summary>
        public bool IsInMetadata { get { return MetadataModuleInternal != null; } }

        /// <summary>
        /// The syntax tree this location is located in or <c>null</c> if not in a syntax tree.
        /// </summary>
        public virtual SyntaxTree? SourceTree { get { return null; } }

        /// <summary>
        /// Returns the metadata module the location is associated with or <c>null</c> if the module is not available.
        /// </summary>
        /// <remarks>
        /// Might return null even if <see cref="IsInMetadata"/> returns true. The module symbol might not be available anymore, 
        /// for example, if the location is serialized and deserialized.
        /// </remarks>
        public IModuleSymbol? MetadataModule { get { return (IModuleSymbol?)MetadataModuleInternal?.GetISymbol(); } }

        internal virtual IModuleSymbolInternal? MetadataModuleInternal { get { return null; } }

        /// <summary>
        /// The location within the syntax tree that this location is associated with.
        /// </summary>
        /// <remarks>
        /// If <see cref="IsInSource"/> returns False this method returns an empty <see cref="TextSpan"/> which starts at position 0.
        /// </remarks>
        public virtual TextSpan SourceSpan { get { return default(TextSpan); } }

        /// <summary>
        /// Gets the location in terms of path, line and column.
        /// </summary>
        /// <returns>
        /// <see cref="FileLinePositionSpan"/> that contains path, line and column information.
        /// 
        /// Returns an invalid span (see <see cref="FileLinePositionSpan.IsValid"/>) if the information is not available.
        /// 
        /// The values are not affected by line mapping directives (#line in C# or #ExternalSource in VB).
        /// </returns>
        public virtual FileLinePositionSpan GetLineSpan()
        {
            return default(FileLinePositionSpan);
        }

        /// <summary>
        /// Gets the location in terms of path, line and column after applying source line mapping directives
        /// (<c>#line</c> in C# or <c>#ExternalSource</c> in VB).
        /// </summary>
        /// <returns>
        /// <see cref="FileLinePositionSpan"/> that contains file, line and column information,
        /// or an invalid span (see <see cref="FileLinePositionSpan.IsValid"/>) if not available.
        /// </returns>
        public virtual FileLinePositionSpan GetMappedLineSpan()
        {
            return default(FileLinePositionSpan);
        }

        // Derived classes should provide value equality semantics.
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        public override string ToString()
        {
            string result = Kind.ToString();
            if (IsInSource)
            {
                result += "(" + this.SourceTree?.FilePath + this.SourceSpan + ")";
            }
            else if (IsInMetadata)
            {
                if (this.MetadataModuleInternal != null)
                {
                    result += "(" + this.MetadataModuleInternal.Name + ")";
                }
            }
            else
            {
                var pos = GetLineSpan();
                if (pos.Path != null)
                {
                    // user-visible line and column counts are 1-based, but internally are 0-based.
                    result += "(" + pos.Path + "@" + (pos.StartLinePosition.Line + 1) + ":" + (pos.StartLinePosition.Character + 1) + ")";
                }
            }

            return result;
        }

        public static bool operator ==(Location? left, Location? right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Location? left, Location? right)
        {
            return !(left == right);
        }

        protected virtual string GetDebuggerDisplay()
        {
            string result = this.GetType().Name;
            var pos = GetLineSpan();
            if (pos.Path != null)
            {
                // user-visible line and column counts are 1-based, but internally are 0-based.
                result += "(" + pos.Path + "@" + (pos.StartLinePosition.Line + 1) + ":" + (pos.StartLinePosition.Character + 1) + ")";
            }

            return result;
        }

        /// <summary>
        /// A location of kind LocationKind.None. 
        /// </summary>
        public static Location None { get { return NoLocation.Singleton; } }

        /// <summary>
        /// Creates an instance of a <see cref="Location"/> for a span in a <see cref="SyntaxTree"/>.
        /// </summary>
        public static Location Create(SyntaxTree syntaxTree, TextSpan textSpan)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            return new SourceLocation(syntaxTree, textSpan);
        }

        /// <summary>
        /// Creates an instance of a <see cref="Location"/> for a span in a file.
        /// </summary>
        public static Location Create(string filePath, TextSpan textSpan, LinePositionSpan lineSpan)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return new ExternalFileLocation(filePath, textSpan, lineSpan);
        }
    }
}
