// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpOrganizing : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpOrganizing()
            : base(nameof(CSharpOrganizing))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public async Task RemoveAndSortAsync()
        {
            await SetUpEditorAsync(@"$$
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
            await VisualStudio.VisualStudio.ExecuteCommandAsync(WellKnownCommandNames.Edit_RemoveAndSort);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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
