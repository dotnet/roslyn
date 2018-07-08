// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpErrorListNetCore : CSharpErrorListCommon
    {
        public CSharpErrorListNetCore()
            : base(WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [IdeFact(Isolate = "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/627280"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task ErrorListAsync()
        {
            await base.ErrorListAsync();
        }

        [IdeFact(Isolate = "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/627280"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task ErrorLevelWarningAsync()
        {
            await base.ErrorLevelWarningAsync();
        }

        [IdeFact(Isolate = "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/627280"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task ErrorsDuringMethodBodyEditingAsync()
        {
            await base.ErrorsDuringMethodBodyEditingAsync();
        }
    }
}
