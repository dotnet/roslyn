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
        private readonly CSharpCodeGenerationOptions _options;

        public CSharpCodeGenerationContextInfo(CodeGenerationContext Context, CSharpCodeGenerationOptions Options)
            : base(Context)
        {
            _options = Options;
        }

        public new CSharpCodeGenerationOptions Options
            => _options;

        protected override CodeGenerationOptions OptionsImpl
            => _options;

        public new CSharpCodeGenerationContextInfo WithContext(CodeGenerationContext value)
            => (Context == value) ? this : new(value, Options);

        protected override CodeGenerationContextInfo WithContextImpl(CodeGenerationContext value)
            => WithContext(value);

        public static new async ValueTask<CSharpCodeGenerationContextInfo> FromDocumentAsync(CodeGenerationContext context, Document document, CancellationToken cancellationToken)
            => new(context, await CSharpCodeGenerationOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false));
    }
}
