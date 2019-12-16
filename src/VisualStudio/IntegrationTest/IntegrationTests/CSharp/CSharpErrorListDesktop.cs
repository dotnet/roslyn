// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpErrorListDesktop : CSharpErrorListCommon
    {
        public CSharpErrorListDesktop(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        public override void ErrorLevelWarning()
        {
            base.ErrorLevelWarning();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ErrorList)]
        [WorkItem(39902, "https://github.com/dotnet/roslyn/issues/39902")]
        public override void ErrorsAfterClosingFile()
        {
            base.ErrorsAfterClosingFile();
        }
    }
}
