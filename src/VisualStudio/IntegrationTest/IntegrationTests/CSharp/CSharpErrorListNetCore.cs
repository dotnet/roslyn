// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpErrorListNetCore : CSharpErrorListCommon
    {
        private const string _targetFrameworkMoniker = "netcoreapp3.0";

        public CSharpErrorListNetCore(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, WellKnownProjectTemplates.CSharpNetCoreClassLibrary, _targetFrameworkMoniker)
        {
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorLevelWarning()
        {
            base.ErrorLevelWarning();
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
