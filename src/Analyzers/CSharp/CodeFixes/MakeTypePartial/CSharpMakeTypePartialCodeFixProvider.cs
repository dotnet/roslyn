// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MakeTypePartial;

namespace Microsoft.CodeAnalysis.CSharp.MakeTypePartial
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeTypePartial), Shared]
    internal sealed class CSharpMakeTypePartialCodeFixProvider : AbstractMakeTypePartialCodeFixProvider
    {
        private const string CS0260 = nameof(CS0260); // Missing partial modifier on declaration of type 'C'; another partial declaration of this type exists

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMakeTypePartialCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(CS0260);
    }
}
