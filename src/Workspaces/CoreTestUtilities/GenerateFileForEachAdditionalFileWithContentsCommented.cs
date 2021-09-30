// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal sealed class GenerateFileForEachAdditionalFileWithContentsCommented : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var file in context.AdditionalFiles)
            {
                AddSourceForAdditionalFile(context, file);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // TODO: context.RegisterForAdditionalFileChanges(UpdateContext);
        }

        private static void AddSourceForAdditionalFile(GeneratorExecutionContext context, AdditionalText file)
        {
            // We're going to "comment" out the contents of the file when generating this
            var sourceText = file.GetText(context.CancellationToken);
            Contract.ThrowIfNull(sourceText, "Failed to fetch the text of an additional file.");

            var changes = sourceText.Lines.SelectAsArray(l => new TextChange(new TextSpan(l.Start, length: 0), "// "));
            var generatedText = sourceText.WithChanges(changes);

            // TODO: remove the generatedText.ToString() when I don't have to specify the encoding
            context.AddSource(GetGeneratedFileName(file.Path), SourceText.From(generatedText.ToString(), encoding: Encoding.UTF8));
        }

        private static string GetGeneratedFileName(string path) => $"{Path.GetFileNameWithoutExtension(path)}.generated";
    }
}
