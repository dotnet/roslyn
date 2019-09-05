// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents both a source text and its version stamp.
    /// </summary>
    public sealed class TextAndVersion
    {
        /// <summary>
        /// The source text.
        /// </summary>
        public SourceText Text { get; }

        /// <summary>
        /// The version of the source text
        /// </summary>
        public VersionStamp Version { get; }

        /// <summary>
        /// An optional file path that identifies the origin of the source text
        /// </summary>
        public string FilePath { get; }

        private TextAndVersion(SourceText text, VersionStamp version, string? filePath)
        {
            this.Text = text;
            this.Version = version;
            this.FilePath = filePath ?? string.Empty;
        }

        /// <summary>
        /// Create a new TextAndVersion instance.
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="version">The version</param>
        /// <param name="filePath">An optional file path that identifies the original of the source text.</param>
        /// <returns></returns>
        public static TextAndVersion Create(SourceText text, VersionStamp version, string? filePath = null)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return new TextAndVersion(text, version, filePath);
        }
    }
}
