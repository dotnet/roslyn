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
        private readonly CommandLineSourceFile sourceFile;
        private readonly CommonCompiler compiler;
        private SourceText text;
        private IList<DiagnosticInfo> diagnostics;

        private readonly object lockObject = new object();

        public AdditionalTextFile(CommandLineSourceFile sourceFile, CommonCompiler compiler)
        {
            if (compiler == null)
            {
                throw new ArgumentNullException(nameof(compiler));
            }

            this.sourceFile = sourceFile;
            this.compiler = compiler;
            this.diagnostics = SpecializedCollections.EmptyList<DiagnosticInfo>();
        }

        /// <summary>
        /// Path to the file.
        /// </summary>
        public override string Path => this.sourceFile.Path;

        /// <summary>
        /// Returns a <see cref="SourceText"/> with the contents of this file, or <c>null</c> if
        /// there were errors reading the file.
        /// </summary>
        public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (lockObject)
            {
                if (this.text == null)
                {
                    var diagnostics = new List<DiagnosticInfo>();
                    this.text = this.compiler.ReadFileContent(
                        this.sourceFile,
                        diagnostics,
                        this.compiler.Arguments.Encoding,
                        this.compiler.Arguments.ChecksumAlgorithm);

                    this.diagnostics = diagnostics;
                }
            }

            return this.text;
        }

        /// <summary>
        /// Errors encountered when trying to read the additional file. Always empty if
        /// <see cref="GetText(CancellationToken)"/> has not been called.
        /// </summary>
        internal IList<DiagnosticInfo> Diagnostics => this.diagnostics;
    }
}
