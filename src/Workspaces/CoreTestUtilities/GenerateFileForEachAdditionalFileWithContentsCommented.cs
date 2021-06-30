// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal sealed class GenerateFileForEachAdditionalFileWithContentsCommented : IIncrementalGenerator
    {
        /// <remarks>
        /// This should only be updated with Interlocked APIs.
        /// </remarks>
        private int _additionalFilesConvertedCount;

        /// <summary>
        /// The number of additional files we converted to a source file. This can be used to assert incrementality.
        /// </summary>
        public int AdditionalFilesConvertedCount => _additionalFilesConvertedCount;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterSourceOutput(context.AdditionalTextsProvider, (context, additionalText) =>
                context.AddSource(
                    GetGeneratedFileName(additionalText.Path),
                    GenerateSourceForAdditionalFile(additionalText, context.CancellationToken)));
        }

        private SourceText GenerateSourceForAdditionalFile(AdditionalText file, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _additionalFilesConvertedCount);

            // We're going to "comment" out the contents of the file when generating this
            var sourceText = file.GetText(cancellationToken);
            Contract.ThrowIfNull(sourceText, "Failed to fetch the text of an additional file.");

            var changes = sourceText.Lines.SelectAsArray(l => new TextChange(new TextSpan(l.Start, length: 0), "// "));
            var generatedText = sourceText.WithChanges(changes);

            return SourceText.From(generatedText.ToString(), encoding: Encoding.UTF8);
        }

        private static string GetGeneratedFileName(string path) => $"{Path.GetFileNameWithoutExtension(path)}.generated";
    }
}
