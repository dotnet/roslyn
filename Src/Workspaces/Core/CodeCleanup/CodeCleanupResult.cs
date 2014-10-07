using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.CodeCleanup
{
    internal class CodeCleanupResult : ICodeCleanupResult
    {
        private readonly CancellableLazy<IList<TextChange>> lazyChanges;
        private readonly CancellableLazy<CommonSyntaxNode> lazyNode;

        public CodeCleanupResult(CommonSyntaxNode node)
        {
            this.lazyChanges = new CancellableLazy<IList<TextChange>>(SpecializedCollections.EmptyList<TextChange>());
            this.lazyNode = new CancellableLazy<CommonSyntaxNode>(node);
        }

        public bool ContainsChanges
        {
            get { return this.lazyChanges.GetValue(CancellationToken.None).Count > 0; }
        }

        public IList<TextChange> GetTextChanges(CancellationToken cancellation = default(CancellationToken))
        {
            return this.lazyChanges.GetValue(cancellation);
        }

        public CommonSyntaxNode GetFormattedRoot(CancellationToken cancellation = default(CancellationToken))
        {
            return this.lazyNode.GetValue(cancellation);
        }
    }
}
