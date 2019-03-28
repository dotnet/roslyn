// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting
{
    public static class ScriptOptionsExtensions
    {
        public static ScriptOptions WithLanguageVersion(this ScriptOptions options, LanguageVersion languageVersion)
        {
            var parseOptions = (options.ParseOptions is null)
                ? CSharpScriptCompiler.DefaultParseOptions
                : (options.ParseOptions is CSharpParseOptions existing) ? existing : throw new InvalidOperationException(string.Format(ScriptingResources.CannotSetLanguageSpecificOption, LanguageNames.CSharp, nameof(LanguageVersion)));

            return options.WithParseOptions(parseOptions.WithLanguageVersion(languageVersion));
        }
    }
}
