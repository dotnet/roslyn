// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp;

public abstract class AbstractCSharpSignatureHelpProviderTests : AbstractSignatureHelpProviderTests<CSharpTestWorkspaceFixture>
{
    protected override ParseOptions CreateExperimentalParseOptions()
    {
        return new CSharpParseOptions().WithFeatures(new Dictionary<string, string>()); // no experimental features to enable
    }
}
