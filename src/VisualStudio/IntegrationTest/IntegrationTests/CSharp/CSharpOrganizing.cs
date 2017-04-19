﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpOrganizing : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpOrganizing(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpOrganizing))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Organizing)]
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
            VisualStudio.ExecuteCommand("Edit.RemoveAndSort");
            VisualStudio.Editor.Verify.TextContains(@"
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