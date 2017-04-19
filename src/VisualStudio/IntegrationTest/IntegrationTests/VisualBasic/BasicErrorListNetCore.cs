// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    public class BasicErrorListNetCore : BasicErrorListCommon
    {
        public BasicErrorListNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
        {
        }

        [Test.Utilities.WorkItem(1825 , "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [Test.Utilities.WorkItem(1825 , "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [Fact(Skip = "https://github.com/dotnet/roslyn-project-system/issues/1825"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
