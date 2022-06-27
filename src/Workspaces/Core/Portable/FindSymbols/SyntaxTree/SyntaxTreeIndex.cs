// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

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

        public static Task PrecalculateAsync(Document document, CancellationToken cancellationToken)
            => PrecalculateAsync(document, CreateIndex, cancellationToken);

        public static ValueTask<SyntaxTreeIndex> GetRequiredIndexAsync(Document document, CancellationToken cancellationToken)
            => GetRequiredIndexAsync(document, ReadIndex, CreateIndex, cancellationToken);

        public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, CancellationToken cancellationToken)
            => GetIndexAsync(document, loadOnly: false, cancellationToken);

        [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1224834", OftenCompletesSynchronously = true)]
        public static ValueTask<SyntaxTreeIndex?> GetIndexAsync(Document document, bool loadOnly, CancellationToken cancellationToken)
            => GetIndexAsync(document, loadOnly, ReadIndex, CreateIndex, cancellationToken);
    }
}
