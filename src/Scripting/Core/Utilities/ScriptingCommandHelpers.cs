// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class ScriptingCommandHelpers
    {
        internal static async Task WriteReferencesAsync(TextWriter outputWriter, Compilation compilation)
        {
            if (compilation == null)
            {
                await outputWriter.WriteLineAsync(ScriptingResources.FoundNoReferences);
                return;
            }

            var references = compilation.References;
            if (!references.Any())
            {
                await outputWriter.WriteLineAsync(ScriptingResources.FoundNoReferences);
                return;
            }

            // NB: Specifically not reporting previous submission as a reference.
            foreach (var reference in references)
            {
                await outputWriter.WriteAsync(reference.Display);

                var aliases = reference.Properties.Aliases;
                if (aliases.Length > 0)
                {
                    await outputWriter.WriteAsync($" ({string.Join(", ", aliases)})");
                }

                await outputWriter.WriteLineAsync();
            }
        }

        internal static async Task WriteDistinctFilesAsync(TextWriter outputWriter, IEnumerable<string> filePaths)
        {
            if (!filePaths.Any())
            {
                await outputWriter.WriteLineAsync(ScriptingResources.FoundNoFiles);
                return;
            }

            var seen = PooledHashSet<string>.GetInstance();

            foreach (var filePath in filePaths)
            {
                if (seen.Add(filePath))
                {
                    await outputWriter.WriteLineAsync(filePath);
                }
            }

            seen.Free();
        }

        internal static async Task WriteImportsAsync(TextWriter outputWriter, ImmutableArray<string> globalImports, ImmutableArray<string> localImports)
        {
            await WriteDistinctImportsAsync(outputWriter, globalImports, ScriptingResources.GlobalImportsHeader, ScriptingResources.FoundNoGlobalImports);
            await WriteDistinctImportsAsync(outputWriter, localImports, ScriptingResources.LocalImportsHeader, ScriptingResources.FoundNoLocalImports);
        }

        private static async Task WriteDistinctImportsAsync(TextWriter outputWriter, IEnumerable<string> imports, string headerMessage, string foundNoneMessage)
        {
            const string indentation = "  ";

            await outputWriter.WriteLineAsync(headerMessage);
            if (!imports.Any())
            {
                await outputWriter.WriteLineAsync(indentation + foundNoneMessage);
                return;
            }

            foreach (var import in imports)
            {
                await outputWriter.WriteLineAsync(indentation + import);
            }
        }
    }
}
