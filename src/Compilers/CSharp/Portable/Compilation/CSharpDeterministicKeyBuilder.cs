// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.Compilation
{
    internal sealed class CSharpDeterministicKeyBuilder : DeterministicKeyBuilder
    {
        internal override void AppendCompilationOptions(CompilationOptions options)
        {
            if (options is not CSharpCompilationOptions csharpOptions)
            {
                throw new InvalidOperationException();
            }

            base.AppendCompilationOptions(options);

            AppendBool(nameof(CSharpCompilationOptions.AllowUnsafe), csharpOptions.AllowUnsafe);
            AppendEnum(nameof(CSharpCompilationOptions.TopLevelBinderFlags), csharpOptions.TopLevelBinderFlags);

            if (csharpOptions.Usings.Length > 0)
            {
                AppendLine("Global Usings");
                foreach (var name in csharpOptions.Usings)
                {
                    AppendSpaces(spaceCount: 4);
                    AppendLine(name);
                }
            }
        }

        internal override void AppendParseOptions(ParseOptions parseOptions)
        {
            if (parseOptions is not CSharpParseOptions csharpOptions)
            {
                throw new InvalidOperationException();
            }

            AppendEnum(nameof(CSharpParseOptions.LanguageVersion), csharpOptions.LanguageVersion);
            AppendEnum(nameof(CSharpParseOptions.SpecifiedLanguageVersion), csharpOptions.SpecifiedLanguageVersion);

            if (csharpOptions.PreprocessorSymbols is { Length: > 0 } symbols)
            {
                AppendLine("Preprocessor Symbols");
                foreach (var symbol in symbols)
                {
                    AppendSpaces(spaceCount: 4);
                    AppendLine(symbol);
                }
            }
        }
    }
}
