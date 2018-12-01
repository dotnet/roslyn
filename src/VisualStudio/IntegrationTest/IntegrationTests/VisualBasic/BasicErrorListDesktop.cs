// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicErrorListDesktop : BasicErrorListCommon
    {
        public BasicErrorListDesktop( )
            : base( WellKnownProjectTemplates.ClassLibrary)
        {
        }

        [TestMethod, TestCategory(Traits.Features.ErrorList)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [TestMethod, TestCategory(Traits.Features.ErrorList)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
