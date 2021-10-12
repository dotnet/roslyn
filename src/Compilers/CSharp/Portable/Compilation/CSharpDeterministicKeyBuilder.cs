// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CSharpDeterministicKeyBuilder : DeterministicKeyBuilder
    {
        public CSharpDeterministicKeyBuilder(DeterministicKeyOptions options = DeterministicKeyOptions.Default)
            : base(options)
        {

        }

        protected override void WriteCompilationOptionsCore(CompilationOptions options)
        {
            if (options is not CSharpCompilationOptions csharpOptions)
            {
                throw new InvalidOperationException();
            }

            base.WriteCompilationOptionsCore(options);

            Writer.Write("unsafe", csharpOptions.AllowUnsafe);
            Writer.Write("topLevelBinderFlags", csharpOptions.TopLevelBinderFlags);

            if (csharpOptions.Usings.Length > 0)
            {
                Writer.WriteKey("globalUsings");
                Writer.WriteArrayStart();
                foreach (var name in csharpOptions.Usings)
                {
                    Writer.Write(name);
                }
                Writer.WriteArrayEnd();
            }
        }

        protected override void WriteParseOptionsCore(ParseOptions parseOptions)
        {
            if (parseOptions is not CSharpParseOptions csharpOptions)
            {
                throw new InvalidOperationException();
            }

            Writer.Write("languageVersion", csharpOptions.LanguageVersion);
            Writer.Write("specifiedLanguageVersion", csharpOptions.SpecifiedLanguageVersion);

            if (csharpOptions.PreprocessorSymbols is { Length: > 0 } symbols)
            {
                Writer.WriteKey("preprocessorSymbols");
                Writer.WriteArrayStart();
                foreach (var symbol in symbols)
                {
                    Writer.Write(symbol);
                }
                Writer.WriteArrayEnd();
            }
        }
    }
}
