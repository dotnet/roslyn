// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
