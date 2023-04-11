// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal sealed partial class SyntaxTreeIndex : AbstractSyntaxIndex<SyntaxTreeIndex>
    {
        private readonly LiteralInfo _literalInfo;
        private readonly IdentifierInfo _identifierInfo;
        private readonly ContextInfo _contextInfo;
        private readonly HashSet<(string alias, string name, int arity)>? _globalAliasInfo;

        private SyntaxTreeIndex(
            Checksum? checksum,
            LiteralInfo literalInfo,
            IdentifierInfo identifierInfo,
            ContextInfo contextInfo,
            HashSet<(string alias, string name, int arity)>? globalAliasInfo)
            : base(checksum)
        {
            _literalInfo = literalInfo;
            _identifierInfo = identifierInfo;
            _contextInfo = contextInfo;
            _globalAliasInfo = globalAliasInfo;
        }

        public static ValueTask<SyntaxTreeIndex> GetRequiredIndexAsync(Document document, CancellationToken cancellationToken)
            => GetRequiredIndexAsync(document.Project, (DocumentState)document.State, cancellationToken);

        public static ValueTask<SyntaxTreeIndex> GetRequiredIndexAsync(Project project, DocumentState document, CancellationToken cancellationToken)
            => GetRequiredIndexAsync(project, document, ReadIndex, CreateIndex, cancellationToken);

        public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, CancellationToken cancellationToken)
            => GetIndexAsync(document.Project, (DocumentState)document.State, cancellationToken); 

        public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Project project, DocumentState document, CancellationToken cancellationToken)
            => GetIndexAsync(project, document, loadOnly: false, cancellationToken);

        public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, bool loadOnly, CancellationToken cancellationToken)
            => GetIndexAsync(document.Project, (DocumentState)document.State, loadOnly, cancellationToken);

        public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Project project, DocumentState document, bool loadOnly, CancellationToken cancellationToken)
            => GetIndexAsync(project, document, loadOnly, ReadIndex, CreateIndex, cancellationToken);
    }
}
