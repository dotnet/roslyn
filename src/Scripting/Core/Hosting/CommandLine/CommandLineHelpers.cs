// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal static class CommandLineHelpers
    {
        // TODO (https://github.com/dotnet/roslyn/issues/5854): remove 
        public static ImmutableArray<string> GetImports(CommandLineArguments args)
        {
            return args.CompilationOptions.GetImports();
        }

        internal static ScriptOptions RemoveImportsAndReferences(this ScriptOptions options)
        {
            return options.WithReferences(Array.Empty<MetadataReference>()).WithImports(Array.Empty<string>());
        }
    }
}
