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
        /// It will always be false if the refactoring is trigger from Quick Action.
        /// </summary>
        public readonly bool MakeDeclarationAtDestinationAbstract;

        /// <summary>
        /// Indicate whether pulling this member up would change the destination to abstract.
        /// </summary>
        public readonly bool ChangeDestinationToAbstract;

        /// <summary>
        /// Indicate whether it would cause error if we directly pull Member into destination.
        /// </summary>
        public bool PullMemberUpCausesError => ChangeOriginalToPublic || ChangeOriginalToNonStatic || ChangeDestinationToAbstract;

        internal MemberAnalysisResult(
            ISymbol member,
            bool changeOriginalToPublic,
            bool changeOriginalToNonStatic,
            bool makeDestinationDeclarationAbstract,
            bool changeDestinationToAbstract)
        {
            Member = member;
            ChangeOriginalToPublic = changeOriginalToPublic;
            ChangeOriginalToNonStatic = changeOriginalToNonStatic;
            MakeDeclarationAtDestinationAbstract = makeDestinationDeclarationAbstract;
            ChangeDestinationToAbstract = changeDestinationToAbstract;
        }
    }
}
