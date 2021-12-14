// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal readonly record struct CSharpCodeGenerationOptions(
        CodeGenerationContext Context,
        CSharpCodeGenerationPreferences Preferences)
    {
        public static async ValueTask<CSharpCodeGenerationOptions> FromDocumentAsync(CodeGenerationContext context, Document document, CancellationToken cancellationToken)
            => new(context, await CSharpCodeGenerationPreferences.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false));

        public static implicit operator CodeGenerationOptions(CSharpCodeGenerationOptions options)
            => new(options.Context, options.Preferences);

        public static explicit operator CSharpCodeGenerationOptions(CodeGenerationOptions options)
            => new(options.Context, (CSharpCodeGenerationPreferences)options.Preferences);
    }
}
