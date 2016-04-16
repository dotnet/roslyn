// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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
        private SourceText _text;
        private IList<DiagnosticInfo> _diagnostics;

        private readonly object _lockObject = new object();

        public AdditionalTextFile(CommandLineSourceFile sourceFile, CommonCompiler compiler)
        {
            if (compiler == null)
            {
                throw new ArgumentNullException(nameof(compiler));
            }

            _sourceFile = sourceFile;
            _compiler = compiler;
            _diagnostics = SpecializedCollections.EmptyList<DiagnosticInfo>();
        }

        /// <summary>
        /// Path to the file.
        /// </summary>
        public override string Path => _sourceFile.Path;

        /// <summary>
        /// Returns a <see cref="SourceText"/> with the contents of this file, or <c>null</c> if
        /// there were errors reading the file.
        /// </summary>
        public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (_lockObject)
            {
                if (_text == null)
                {
                    var diagnostics = new List<DiagnosticInfo>();
                    _text = _compiler.ReadFileContent(_sourceFile, diagnostics);
                    _diagnostics = diagnostics;
                }
            }

            return _text;
        }

        /// <summary>
        /// Errors encountered when trying to read the additional file. Always empty if
        /// <see cref="GetText(CancellationToken)"/> has not been called.
        /// </summary>
        internal IList<DiagnosticInfo> Diagnostics => _diagnostics;
    }
}
