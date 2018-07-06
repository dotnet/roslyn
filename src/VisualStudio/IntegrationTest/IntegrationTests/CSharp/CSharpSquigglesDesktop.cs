// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSquigglesDesktop : CSharpSquigglesCommon
    {
        public CSharpSquigglesDesktop()
            : base(WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override async Task VerifySyntaxErrorSquigglesAsync()
        {
            await base.VerifySyntaxErrorSquigglesAsync();
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override async Task VerifySemanticErrorSquigglesAsync()
        {
            await base.VerifySemanticErrorSquigglesAsync();
        }
    }
}
