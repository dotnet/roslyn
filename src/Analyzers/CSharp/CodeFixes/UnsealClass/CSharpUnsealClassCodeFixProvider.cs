// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnsealClass;

namespace Microsoft.CodeAnalysis.CSharp.UnsealClass;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UnsealClass), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUnsealClassCodeFixProvider() : AbstractUnsealClassCodeFixProvider
{
    private const string CS0509 = nameof(CS0509); // 'D': cannot derive from sealed type 'C'

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [CS0509];

    protected override string TitleFormat => CSharpCodeFixesResources.Unseal_class_0;
}
