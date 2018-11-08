// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog
{
    internal class PullMemberDialogResult
    {
        public static PullMemberDialogResult CanceledResult { get; } = new PullMemberDialogResult(true);

        public bool IsCanceled { get; }

        public AnalysisResult PullMembersAnalysisResult { get; }

        internal PullMemberDialogResult(AnalysisResult result)
        {
            PullMembersAnalysisResult = result;
            IsCanceled = false;
        }

        private PullMemberDialogResult(bool isCanceled)
        {
            IsCanceled = isCanceled;
        }
    }
}
