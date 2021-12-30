// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSquigglesDesktop : CSharpSquigglesCommon
    {
        protected override bool SupportsGlobalUsings => false;

        public CSharpSquigglesDesktop(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [ConditionalWpfFact(typeof(DesktopServiceHubHostOnly))]
        [Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [ConditionalWpfFact(typeof(DesktopServiceHubHostOnly))]
        [Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
