// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
