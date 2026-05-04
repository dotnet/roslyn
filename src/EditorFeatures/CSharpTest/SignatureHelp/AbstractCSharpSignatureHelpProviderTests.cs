// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

public abstract class AbstractCSharpSignatureHelpProviderTests : AbstractSignatureHelpProviderTests<CSharpTestWorkspaceFixture>
{
    protected override ParseOptions CreateExperimentalParseOptions()
        => new CSharpParseOptions().WithFeatures([]); // no experimental features to enable

    protected override Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        IEnumerable<SignatureHelpTestItem>? expectedOrderedItemsOrNull = null,
        bool usePreviousCharAsTrigger = false,
        SourceCodeKind? sourceCodeKind = null,
        bool experimental = false)
    {
        return base.TestAsync(markup, expectedOrderedItemsOrNull, usePreviousCharAsTrigger, sourceCodeKind, experimental);
    }
}
