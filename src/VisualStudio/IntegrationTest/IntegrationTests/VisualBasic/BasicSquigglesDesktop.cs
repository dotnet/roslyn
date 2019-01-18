// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicSquigglesDesktop : BasicSquigglesCommon
    {
        public BasicSquigglesDesktop(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
