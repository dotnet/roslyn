// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicErrorListDesktop : BasicErrorListCommon
    {
        public BasicErrorListDesktop()
            : base(WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        public override async Task ErrorListAsync()
        {
            await base.ErrorListAsync();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        public override async Task ErrorsDuringMethodBodyEditingAsync()
        {
            await base.ErrorsDuringMethodBodyEditingAsync();
        }
    }
}
