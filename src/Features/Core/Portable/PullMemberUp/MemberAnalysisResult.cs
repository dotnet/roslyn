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
        /// Indicate whether this member's declaration in destination needs to be made to abstract. It is only used by the dialog UI.
        /// If this property is true, then pull a member up to a class will only generate a abstract declaration in the destination.
        /// It will always be false if the refactoring is triggered from Quick Action.
        /// </summary>
        public readonly bool MakeMemberDeclarationAbstract;

        /// <summary>
        /// Indicate whether pulling this member up would change the destination to abstract. It will be true if:
        /// 1. Pull an abstract member to a non-abstract class
        /// 2. The 'Make abstract' check box of a member is checked, and the destination is a non-abstract class
        /// </summary>
        public readonly bool ChangeDestinationTypeToAbstract;


        /// <summary>
        /// Indicate whether it would cause error if we directly pull Member into destination.
        /// </summary>
        public bool PullMemberUpNeedsToDoExtraChanges => ChangeOriginalToPublic || ChangeOriginalToNonStatic || ChangeDestinationTypeToAbstract;

        public MemberAnalysisResult(
            ISymbol member,
            bool changeOriginalToPublic,
            bool changeOriginalToNonStatic,
            bool makeMemberDeclarationAbstract,
            bool changeDestinationTypeToAbstract)
        {
            Member = member;
            ChangeOriginalToPublic = changeOriginalToPublic;
            ChangeOriginalToNonStatic = changeOriginalToNonStatic;
            MakeMemberDeclarationAbstract = makeMemberDeclarationAbstract;
            ChangeDestinationTypeToAbstract = changeDestinationTypeToAbstract;
        }
    }
}
