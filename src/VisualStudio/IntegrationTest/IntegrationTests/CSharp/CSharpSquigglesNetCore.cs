// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpSquigglesNetCore : CSharpSquigglesCommon
    {
        public CSharpSquigglesNetCore()
            : base(nameof(CSharpSquigglesNetCore), WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySyntaxErrorSquiggles()
        {
            base.VerifySyntaxErrorSquiggles();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorSquiggles)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void VerifySemanticErrorSquiggles()
        {
            base.VerifySemanticErrorSquiggles();
        }
    }
}
