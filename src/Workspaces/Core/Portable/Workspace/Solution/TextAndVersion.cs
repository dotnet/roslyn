// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Obsolete.
        /// </summary>
        [Obsolete("Use Document.FilePath instead", false)]
        public string FilePath { get; }

        /// <summary>
        /// If an error occurred while loading the text the corresponding diagnostic, otherwise null.
        /// </summary>
        internal Diagnostic? LoadDiagnostic { get; }

        private TextAndVersion(SourceText text, VersionStamp version, string? filePath, Diagnostic? loadDiagnostic)
        {
            Text = text;
            Version = version;
            LoadDiagnostic = loadDiagnostic;

#pragma warning disable CS0618 // Type or member is obsolete
            FilePath = filePath ?? string.Empty;
#pragma warning restore
        }

        /// <summary>
        /// Create a new <see cref="TextAndVersion"/> instance.
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="version">The version</param>
        /// <param name="filePath">Obsolete.</param>
        /// <returns></returns>
        public static TextAndVersion Create(SourceText text, VersionStamp version, string? filePath = null)
            => new(text ?? throw new ArgumentNullException(nameof(text)), version, filePath, loadDiagnostic: null);

        /// <summary>
        /// Create a new <see cref="TextAndVersion"/> instance.
        /// </summary>
        /// <param name="text">The text</param>
        /// <param name="version">The version</param>
        /// <param name="loadDiagnostic">Diagnostic describing failure to load the source text.</param>
        /// <returns></returns>
        internal static TextAndVersion Create(SourceText text, VersionStamp version, Diagnostic? loadDiagnostic)
            => new(text, version, filePath: null, loadDiagnostic);
    }
}
