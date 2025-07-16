// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CSharpDeterministicKeyBuilder : DeterministicKeyBuilder
    {
        internal static readonly CSharpDeterministicKeyBuilder Instance = new();

        private CSharpDeterministicKeyBuilder()
        {
        }

        protected override void WriteCompilationOptionsCore(JsonWriter writer, CompilationOptions options)
        {
            if (options is not CSharpCompilationOptions csharpOptions)
            {
                throw new ArgumentException(null, nameof(options));
            }

            base.WriteCompilationOptionsCore(writer, options);

            writer.Write("unsafe", csharpOptions.AllowUnsafe);
            writer.Write("topLevelBinderFlags", csharpOptions.TopLevelBinderFlags);
            writer.WriteKey("usings");
            writer.WriteArrayStart();
            foreach (var name in csharpOptions.Usings)
            {
                writer.Write(name);
            }
            writer.WriteArrayEnd();
        }

        protected override void WriteParseOptionsCore(JsonWriter writer, ParseOptions parseOptions)
        {
            if (parseOptions is not CSharpParseOptions csharpOptions)
            {
                throw new ArgumentException(null, nameof(parseOptions));
            }

            base.WriteParseOptionsCore(writer, parseOptions);

            writer.Write("languageVersion", csharpOptions.LanguageVersion);
            writer.Write("specifiedLanguageVersion", csharpOptions.SpecifiedLanguageVersion);

            writer.WriteKey("preprocessorSymbols");
            writer.WriteArrayStart();

            // Even though tools like the command line parser don't explicitly order the symbols 
            // here the order doesn't actually impact determinism.
            foreach (var symbol in csharpOptions.PreprocessorSymbols.OrderBy(StringComparer.Ordinal))
            {
                writer.Write(symbol);
            }

            writer.WriteArrayEnd();
        }
    }
}
