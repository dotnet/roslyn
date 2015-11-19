// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
