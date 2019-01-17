// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicErrorListNetCore : BasicErrorListCommon
    {
        public BasicErrorListNetCore()
            : base(WellKnownProjectTemplates.VisualBasicNetCoreClassLibrary)
        {
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorList)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorList()
        {
            base.ErrorList();
        }

        [WorkItem(1825, "https://github.com/dotnet/roslyn-project-system/issues/1825")]
        [TestMethod, TestProperty(Traits.Feature, Traits.Features.ErrorList)]
        [TestProperty(Traits.Feature, Traits.Features.NetCore)]
        public override void ErrorsDuringMethodBodyEditing()
        {
            base.ErrorsDuringMethodBodyEditing();
        }
    }
}
