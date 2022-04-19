// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal sealed class CSharpCodeGenerationContextInfo : CodeGenerationContextInfo
    {
        public readonly LanguageVersion LanguageVersion;

        public CSharpCodeGenerationContextInfo(CodeGenerationContext context, CSharpCodeGenerationOptions options, LanguageVersion languageVersion)
            : base(context)
        {
            Options = options;
            LanguageVersion = languageVersion;
        }

        public new CSharpCodeGenerationOptions Options { get; }

        protected override CodeGenerationOptions OptionsImpl
            => Options;

        public new CSharpCodeGenerationContextInfo WithContext(CodeGenerationContext value)
            => (Context == value) ? this : new(value, Options, LanguageVersion);

        protected override CodeGenerationContextInfo WithContextImpl(CodeGenerationContext value)
            => WithContext(value);
    }
}
