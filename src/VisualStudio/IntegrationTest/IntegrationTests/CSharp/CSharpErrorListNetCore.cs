// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpErrorListNetCore : CSharpErrorListCommon
    {
        public CSharpErrorListNetCore(VisualStudioInstanceFactory instanceFactory)
            : base(WellKnownProjectTemplates.CSharpNetCoreClassLibrary)
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorList)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorList)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorLevelWarning()
        {
            base.ErrorLevelWarning();
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorList)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
