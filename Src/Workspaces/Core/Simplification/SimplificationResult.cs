using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Simplification
{
    public struct SimplificationResult
    {
        /// <summary>
        /// The nodes in the old document that have been simplified.
        /// </summary>
        public IEnumerable<CommonSyntaxNode> SimplifiedNodes { get; private set; }

        /// <summary>
        /// The simplfied document.
        /// </summary>
        public Document Document { get; private set; }

        internal SimplificationResult(Document document, IEnumerable<CommonSyntaxNode> simplifiedNodes)
            : this()
        {
            this.Document = document;
            this.SimplifiedNodes = simplifiedNodes;
        }
    }
}
