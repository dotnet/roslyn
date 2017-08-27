﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicSquigglesNetCore : BasicSquigglesCommon
    {
        public BasicSquigglesNetCore(VisualStudioInstanceFactory instanceFactory)
            :base(instanceFactory, WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [Test.Utilities.WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [Fact(Skip = "1825"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [Test.Utilities.WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [Fact(Skip = "1825"), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
