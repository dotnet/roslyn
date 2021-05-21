// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a non source code file.
    /// </summary>
    internal sealed class AdditionalTextFile : AdditionalText
    {
        private readonly CommandLineSourceFile _sourceFile;
        private readonly CommonCompiler _compiler;
        private readonly Lazy<SourceText?> _text;
        private IList<DiagnosticInfo> _diagnostics;

        public AdditionalTextFile(CommandLineSourceFile sourceFile, CommonCompiler compiler)
        {
            if (compiler == null)
            {
                throw new ArgumentNullException(nameof(compiler));
            }

            _sourceFile = sourceFile;
            _compiler = compiler;
            _diagnostics = SpecializedCollections.EmptyList<DiagnosticInfo>();
            _text = new Lazy<SourceText?>(ReadText);
        }

        private SourceText? ReadText()
        {
            var diagnostics = new List<DiagnosticInfo>();
            var text = _compiler.TryReadFileContent(_sourceFile, diagnostics);
            _diagnostics = diagnostics;
            return text;
        }

        /// <summary>
        /// Path to the file.
        /// </summary>
        public override string Path => _sourceFile.Path;

        /// <summary>
        /// Returns a <see cref="SourceText"/> with the contents of this file, or <c>null</c> if
        /// there were errors reading the file.
        /// </summary>
        public override SourceText? GetText(CancellationToken cancellationToken = default) => _text.Value;

        /// <summary>
        /// Errors encountered when trying to read the additional file. Always empty if
        /// <see cref="GetText(CancellationToken)"/> has not been called.
        /// </summary>
        internal IList<DiagnosticInfo> Diagnostics => _diagnostics;
    }
}
