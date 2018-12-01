// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpOrganizing : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpOrganizing() : base(nameof(CSharpOrganizing)) { }

        [TestMethod, TestCategory(Traits.Features.Organizing)]
        public void RemoveAndSort()
        {
            SetUpEditor(@"$$
using C;
using B;
using A;

class Test
{
    CA a = null;
    CC c = null;
}
namespace A { public class CA { } }
namespace B { public class CB { } }
namespace C { public class CC { } }");
            VisualStudioInstance.ExecuteCommand("Edit.RemoveAndSort");
            VisualStudioInstance.Editor.Verify.TextContains(@"
using A;
using C;

class Test
{
    CA a = null;
    CC c = null;
}
namespace A { public class CA { } }
namespace B { public class CB { } }
namespace C { public class CC { } }");

        }
    }
}
