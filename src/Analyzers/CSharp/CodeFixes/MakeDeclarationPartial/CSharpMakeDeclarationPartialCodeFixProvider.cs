// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MakeDeclarationPartial;

namespace Microsoft.CodeAnalysis.CSharp.MakeDeclarationPartial
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeDeclarationPartial), Shared]
    internal sealed class CSharpMakeDeclarationPartialCodeFixProvider : AbstractMakeDeclarationPartialCodeFixProvider
    {
        private const string CS0260 = nameof(CS0260);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMakeDeclarationPartialCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS0260);
    }
}
