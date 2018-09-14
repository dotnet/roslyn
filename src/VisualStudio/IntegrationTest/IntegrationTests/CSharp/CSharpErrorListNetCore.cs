// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpErrorListNetCore : CSharpErrorListCommon
    {
        public CSharpErrorListNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/18996"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/18996"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorLevelWarning()
        {
            base.ErrorLevelWarning();
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/19090"), Trait(Traits.Feature, Traits.Features.ErrorList)]
        [Trait(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
