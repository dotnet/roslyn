// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    [Trait(Traits.Feature, Traits.Features.Organizing)]
    public class CSharpOrganizing : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpOrganizing()
            : base(nameof(CSharpOrganizing))
        {
        }

        [IdeFact]
        public async Task RemoveAndSort()
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
namespace C { public class CC { } }", HangMitigatingCancellationToken);
            await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.RemoveAndSort, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
using A;
using C;

class Test
{
    CA a = null;
    CC c = null;
}
namespace A { public class CA { } }
namespace B { public class CB { } }
namespace C { public class CC { } }", cancellationToken: HangMitigatingCancellationToken);
        }
    }
}
