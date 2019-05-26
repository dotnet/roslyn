// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Composition;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Editor
{
    [Shared]
    [ExportLanguageService(typeof(IGoToDefinitionService), LanguageNames.FSharp)]
    internal class FSharpGoToDefinitionService : IGoToDefinitionService
    {
        private readonly IFSharpGoToDefinitionService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpGoToDefinitionService(IFSharpGoToDefinitionService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var items = await _service.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
            return items.Select(x => new InternalFSharpNavigableItem(x));
        }

        public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
        {
            return _service.TryGoToDefinition(document, position, cancellationToken);
        }

        private class InternalFSharpNavigableItem : INavigableItem
        {
            public InternalFSharpNavigableItem(FSharpNavigableItem item)
            {
                Document = item.Document;
                SourceSpan = item.SourceSpan;
            }

            public Glyph Glyph => Glyph.BasicFile;

            public ImmutableArray<TaggedText> DisplayTaggedParts => ImmutableArray<TaggedText>.Empty;

            public bool DisplayFileLocation => true;

            public bool IsImplicitlyDeclared => false;

            public Document Document { get; private set; }

            public TextSpan SourceSpan { get; private set; }

            public ImmutableArray<INavigableItem> ChildItems => ImmutableArray<INavigableItem>.Empty;
        }
    }
}
