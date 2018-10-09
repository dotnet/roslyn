using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp
{
    internal class PushTargetsResult
    {
        public IEnumerable<ISymbol> SelectedMembers { get; }

        public static PushTargetsResult CanceledResult { get; } = new PushTargetsResult(true);

        public bool IsCanceled { get; }

        public INamedTypeSymbol Target { get; }

        internal PushTargetsResult(IEnumerable<ISymbol> selectMembers, INamedTypeSymbol target)
        {
            SelectedMembers = selectMembers;
            Target = target;
            IsCanceled = false;
        }

        private PushTargetsResult(bool isCanceled)
        {
            IsCanceled = isCanceled;
        }
    }
}
