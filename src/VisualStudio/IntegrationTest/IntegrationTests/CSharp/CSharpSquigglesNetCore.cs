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

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpSquigglesNetCore : CSharpSquigglesCommon
    {
        protected override bool SupportsGlobalUsings => true;

        public CSharpSquigglesNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);

            // The CSharpNetCoreClassLibrary template does not open a file automatically.
            VisualStudio.SolutionExplorer.OpenFile(new Project(ProjectName), WellKnownProjectTemplates.CSharpNetCoreClassLibraryClassFileName);
        }

        [ConditionalWpfFact(typeof(DesktopServiceHubHostOnly))]
        [Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [ConditionalWpfFact(typeof(DesktopServiceHubHostOnly))]
        [Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
