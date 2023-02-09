// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal sealed class CSharpCodeGenerationContextInfo : CodeGenerationContextInfo
    {
        public readonly LanguageVersion LanguageVersion;

        public CSharpCodeGenerationContextInfo(CodeGenerationContext context, CSharpCodeGenerationOptions options, CSharpCodeGenerationService service, LanguageVersion languageVersion)
            : base(context)
        {
            Options = options;
            Service = service;
            LanguageVersion = languageVersion;
        }

        public new CSharpCodeGenerationOptions Options { get; }
        public new CSharpCodeGenerationService Service { get; }

        protected override SyntaxGenerator GeneratorImpl
            => Service.LanguageServices.GetRequiredService<SyntaxGenerator>();

        protected override CodeGenerationOptions OptionsImpl
            => Options;

        protected override ICodeGenerationService ServiceImpl
            => Service;

        public new CSharpCodeGenerationContextInfo WithContext(CodeGenerationContext value)
            => (Context == value) ? this : new(value, Options, Service, LanguageVersion);

        protected override CodeGenerationContextInfo WithContextImpl(CodeGenerationContext value)
            => WithContext(value);
    }
}
