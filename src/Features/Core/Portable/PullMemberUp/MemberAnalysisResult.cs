// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal readonly struct MemberAnalysisResult
    {
        /// <summary>
        /// The member needs to be pulled up.
        /// </summary>
        public readonly ISymbol Member;

        /// <summary>
        /// Indicate whether this member needs to be changed to public so it won't cause error after it is pulled up to destination.
        /// </summary>
        public readonly bool ChangeOriginalToPublic;

        /// <summary>
        /// Indicate whether this member needs to be changed to non-static so it won't cause error after it is pulled up to destination.
        /// </summary>
        public readonly bool ChangeOriginalToNonStatic;

        /// <summary>
        /// Indicate whether it would cause error if we directly pull Member into destination.
        /// </summary>
        public bool PullMemberUpCausesError => ChangeOriginalToPublic || ChangeOriginalToNonStatic;

        internal MemberAnalysisResult(ISymbol member, bool changeOriginalToPublic, bool changeOriginalToNonStatic)
        {
            Member = member;
            ChangeOriginalToPublic = changeOriginalToPublic;
            ChangeOriginalToNonStatic = changeOriginalToNonStatic;
        }
    }
}
