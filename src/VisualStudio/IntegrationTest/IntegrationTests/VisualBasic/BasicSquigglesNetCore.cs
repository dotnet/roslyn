// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicSquigglesNetCore : BasicSquigglesCommon
    {
        public BasicSquigglesNetCore()
            : base(WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
        {
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
