// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal class PullMemberDialogResult
    {
        public IEnumerable<(ISymbol member, bool makeAbstract)> SelectedMembers { get; }

        public static PullMemberDialogResult CanceledResult { get; } = new PullMemberDialogResult(true);

        public bool IsCanceled { get; }

        public INamedTypeSymbol Target { get; }

        internal PullMemberDialogResult(IEnumerable<(ISymbol member, bool makeAbstract)> selectMembers, INamedTypeSymbol target)
        {
            SelectedMembers = selectMembers;
            Target = target;
            IsCanceled = false;
        }

        private PullMemberDialogResult(bool isCanceled)
        {
            IsCanceled = isCanceled;
        }
    }

}
