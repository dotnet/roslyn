// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicSquigglesNetCore : BasicSquigglesCommon
    {
        private const string _targetFrameworkMoniker = "netcoreapp3.0";

        public BasicSquigglesNetCore(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary, _targetFrameworkMoniker)
        {
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
