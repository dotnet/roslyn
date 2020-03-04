﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UnsealClass;

namespace Microsoft.CodeAnalysis.CSharp.UnsealClass
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UnsealClass), Shared]
    internal sealed class CSharpUnsealClassCodeFixProvider : AbstractUnsealClassCodeFixProvider
    {
        private const string CS0509 = nameof(CS0509); // 'D': cannot derive from sealed type 'C'

        [ImportingConstructor]
        public CSharpUnsealClassCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS0509);

        protected override string TitleFormat => CSharpFeaturesResources.Unseal_class_0;
    }
}
