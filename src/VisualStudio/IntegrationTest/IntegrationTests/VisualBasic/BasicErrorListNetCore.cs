// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicErrorListNetCore : BasicErrorListCommon
    {
        private const string _targetFrameworkMoniker = "netcoreapp3.0";

        public BasicErrorListNetCore(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary, _targetFrameworkMoniker)
        {
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
