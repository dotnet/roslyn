// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.Editor.UnitTests.Structure;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure.MetadataAsSource
{
    /// <summary>
    /// Identifiers coming from IL can be just about any valid string and since C# doesn't have a way to escape all possible
    /// IL identifiers, we have to account for the possibility that an item's metadata name could lead to unparseable code.
    /// </summary>
    public class InvalidIdentifierStructureTests : AbstractSyntaxStructureProviderTests
    {
        protected override string LanguageName => LanguageNames.CSharp;
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;

        internal override async Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, int position)
        {
            var outliningService = document.GetLanguageService<BlockStructureService>();
            var options = GetOptions();
            return (await outliningService.GetBlockStructureAsync(document, options, CancellationToken.None)).Spans;
        }

        [WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task PrependedDollarSign()
        {
            const string code = @"
{|hint:$$class C{|textspan:
{
    public void $Invoke();
}|}|}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task SymbolsAndPunctuation()
        {
            const string code = @"
{|hint:$$class C{|textspan:
{
    public void !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`();
}|}|}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }

        [WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task IdentifierThatLooksLikeCode()
        {
            const string code = @"
{|hint1:$$class C{|textspan1:
{
    public void }|}|} } {|hint2:public class CodeInjection{|textspan2:{ }|}|} /* now everything is commented ();
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan1", "hint1", CSharpStructureHelpers.Ellipsis, autoCollapse: false),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: false));
        }
    }
}
