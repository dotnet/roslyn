// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
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
