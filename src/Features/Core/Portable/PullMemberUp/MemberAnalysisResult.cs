// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

namespace Microsoft.CodeAnalysis.PullMemberUp
{
    internal readonly struct MemberAnalysisResult(
        ISymbol member,
        bool changeOriginalToPublic,
        bool changeOriginalToNonStatic,
        bool makeMemberDeclarationAbstract,
        bool changeDestinationTypeToAbstract)
    {
        /// <summary>
        /// The member needs to be pulled up.
        /// </summary>
        public readonly ISymbol Member = member;

        /// <summary>
        /// Indicate whether this member needs to be changed to public so it won't cause error after it is pulled up to destination.
        /// </summary>
        public readonly bool ChangeOriginalToPublic = changeOriginalToPublic;

        /// <summary>
        /// Indicate whether this member needs to be changed to non-static so it won't cause error after it is pulled up to destination.
        /// </summary>
        public readonly bool ChangeOriginalToNonStatic = changeOriginalToNonStatic;

        /// <summary>
        /// Indicate whether this member's declaration in destination needs to be made to abstract. It is only used by the dialog UI.
        /// If this property is true, then pull a member up to a class will only generate a abstract declaration in the destination.
        /// It will always be false if the refactoring is triggered from Quick Action.
        /// </summary>
        public readonly bool MakeMemberDeclarationAbstract = makeMemberDeclarationAbstract;

        /// <summary>
        /// Indicate whether pulling this member up would change the destination to abstract. It will be true if:
        /// 1. Pull an abstract member to a non-abstract class
        /// 2. The 'Make abstract' check box of a member is checked, and the destination is a non-abstract class
        /// </summary>
        public readonly bool ChangeDestinationTypeToAbstract = changeDestinationTypeToAbstract;

        /// <summary>
        /// Indicate whether it would cause error if we directly pull Member into destination.
        /// </summary>
        public bool PullMemberUpNeedsToDoExtraChanges => ChangeOriginalToPublic || ChangeOriginalToNonStatic || ChangeDestinationTypeToAbstract;
    }
}
