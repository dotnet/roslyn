// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup
{
    internal class CSharpCodeCleanerService : AbstractCodeCleanerService
    {
        private static readonly ImmutableArray<ICodeCleanupProvider> s_defaultProviders = ImmutableArray.Create<ICodeCleanupProvider>(
            new SimplificationCodeCleanupProvider(),
            new FormatCodeCleanupProvider());

        public override ImmutableArray<ICodeCleanupProvider> GetDefaultProviders()
            => s_defaultProviders;

        protected override ImmutableArray<TextSpan> GetSpansToAvoid(SyntaxNode root)
            => ImmutableArray<TextSpan>.Empty;
    }
}
