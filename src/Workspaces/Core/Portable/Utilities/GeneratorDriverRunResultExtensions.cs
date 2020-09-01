// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Utilities
{
    internal static class GeneratorDriverRunResultExtensions
    {
        public static bool TryGetGeneratorAndHint(
            this GeneratorDriverRunResult? generatorRunResult,
            SyntaxTree tree,
            [NotNullWhen(true)] out ISourceGenerator? generator,
            [NotNullWhen(true)] out string? generatedSourceHintName)
        {
            if (generatorRunResult != null)
            {
                foreach (var generatorResult in generatorRunResult.Results)
                {
                    foreach (var generatedSource in generatorResult.GeneratedSources)
                    {
                        if (generatedSource.SyntaxTree == tree)
                        {
                            generator = generatorResult.Generator;
                            generatedSourceHintName = generatedSource.HintName;

                            return true;
                        }
                    }
                }
            }

            generator = null;
            generatedSourceHintName = null;
            return false;
        }
    }
}
