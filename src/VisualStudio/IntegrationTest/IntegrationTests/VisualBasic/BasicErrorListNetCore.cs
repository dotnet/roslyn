// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicErrorListNetCore : BasicErrorListCommon
    {
        public BasicErrorListNetCore()
            : base(WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
        {
        }

        [WorkItem(1825 , "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [IdeFact(Skip = "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/627280"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task ErrorListAsync()
        {
            await base.ErrorListAsync();
        }

        [WorkItem(1825 , "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [IdeFact(Skip = "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/627280"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override async Task ErrorsDuringMethodBodyEditingAsync()
        {
            await base.ErrorsDuringMethodBodyEditingAsync();
        }
    }
}
