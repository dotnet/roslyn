// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SignatureHelp
{
    public abstract class AbstractCSharpSignatureHelpProviderTests : AbstractSignatureHelpProviderTests<CSharpTestWorkspaceFixture>
    {
        protected override ParseOptions CreateExperimentalParseOptions()
        {
            return new CSharpParseOptions().WithFeatures(ImmutableArray<string>.Empty); // no experimental features to enable
        }
    }
}
