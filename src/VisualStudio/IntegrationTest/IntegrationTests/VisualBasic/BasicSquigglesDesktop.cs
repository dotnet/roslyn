// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicSquigglesDesktop : BasicSquigglesCommon
    {
        public BasicSquigglesDesktop()
            : base(WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
