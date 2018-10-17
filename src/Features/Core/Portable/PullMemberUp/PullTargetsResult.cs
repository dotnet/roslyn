using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    internal class PullTargetsResult
    {
        public IEnumerable<(ISymbol member, bool makeAbstract)> SelectedMembers { get; }

        public static PullTargetsResult CanceledResult { get; } = new PullTargetsResult(true);

        public bool IsCanceled { get; }

        public INamedTypeSymbol Target { get; }

        internal PullTargetsResult(IEnumerable<(ISymbol member, bool makeAbstract)> selectMembers, INamedTypeSymbol target)
        {
            SelectedMembers = selectMembers;
            Target = target;
            IsCanceled = false;
        }

        private PullTargetsResult(bool isCanceled)
        {
            IsCanceled = isCanceled;
        }
    }
}
