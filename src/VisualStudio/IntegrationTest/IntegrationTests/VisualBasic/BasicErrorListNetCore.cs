// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicErrorListNetCore : BasicErrorListCommon
    {
        public BasicErrorListNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);

            // The VisualBasicNetCoreClassLibrary template does not open a file automatically.
            VisualStudio.SolutionExplorer.OpenFile(new Project(ProjectName), WellKnownProjectTemplates.VisualBasicNetCoreClassLibraryClassFileName);
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
