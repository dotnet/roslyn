// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddObsoleteAttribute;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.AddObsoleteAttribute;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddObsoleteAttribute), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpAddObsoleteAttributeCodeFixProvider()
    : AbstractAddObsoleteAttributeCodeFixProvider(CSharpSyntaxFacts.Instance, CSharpCodeFixesResources.Add_Obsolete)
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [
            "CS0612",
            "CS0618",
            "CS0672",
            "CS1062",
            "CS1064",
        ];
}
