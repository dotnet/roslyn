// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
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

            return (await outliningService.GetBlockStructureAsync(document, CancellationToken.None)).Spans;
        }

        [WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task PrependedDollarSign()
        {
            const string code = @"
$$class C
{
    public void $Invoke();
}";

            await VerifyNoBlockSpansAsync(code);
        }

        [WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task SymbolsAndPunctuation()
        {
            const string code = @"
$$class C
{
    public void !#$%^&*(()_-+=|\}]{[""':;?/>.<,~`();
}";

            await VerifyNoBlockSpansAsync(code);
        }

        [WorkItem(1174405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174405")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task IdentifierThatLooksLikeCode()
        {
            const string code = @"
$$class C
{
    public void } } public class CodeInjection{ } /* now everything is commented ();
}";

            await VerifyNoBlockSpansAsync(code);
        }
    }
}
