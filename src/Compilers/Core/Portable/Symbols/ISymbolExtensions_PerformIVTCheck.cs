// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis
{
    public static partial class ISymbolExtensions
    {
        /// <summary>
        /// Given that an assembly with identity assemblyGrantingAccessIdentity granted access to assemblyWantingAccess,
        /// check the public keys to ensure the internals-visible-to check should succeed. This is used by both the
        /// C# and VB implementations as a helper to implement `bool IAssemblySymbol.GivesAccessTo(IAssemblySymbol toAssembly)`.
        /// </summary>
        internal static IVTConclusion PerformIVTCheck(
            this AssemblyIdentity assemblyGrantingAccessIdentity,
            ImmutableArray<byte> assemblyWantingAccessKey,
            ImmutableArray<byte> grantedToPublicKey)
        {
            // This gets a bit complicated. Let's break it down.
            //
            // First off, let's assume that the "other" assembly is Smith.DLL, that the "this"
            // assembly is "Jones.DLL", and that Smith has named Jones as a friend (that is a precondition
            // to calling this method). Whether we allow Jones to see internals of Smith depends on these four factors:
            //
            // q1) Is Smith strong-named?
            // q2) Did Smith name Jones as a friend via a strong name?
            // q3) Is Jones strong-named?
            // q4) Does Smith give a strong-name for Jones that matches our strong name?
            //
            // Before we dive into the details, we should mention two additional facts:
            //
            // * If the answer to q1 is "yes", and Smith was compiled by a Roslyn compiler, then q2 must be "yes" also.
            //   Strong-named Smith must only be friends with strong-named Jones. See the blog article
            //   http://blogs.msdn.com/b/ericlippert/archive/2009/06/04/alas-smith-and-jones.aspx
            //   for an explanation of why this feature is desirable.
            //
            //   Now, just because the compiler enforces this rule does not mean that we will never run into
            //   a scenario where Smith is strong-named and names Jones via a weak name. Not all assemblies
            //   were compiled with a Roslyn compiler. We still need to deal sensibly with this situation.
            //   We do so by ignoring the problem; if strong-named Smith extends friendship to weak-named
            //   Jones then we're done; any assembly named Jones is a friend of Smith.
            //
            //   Incidentally, the C# compiler produces error CS1726, ERR_FriendAssemblySNReq, and VB produces
            //   the error VB31535, ERR_FriendAssemblyStrongNameRequired, when compiling 
            //   a strong-named Smith that names a weak-named Jones as its friend.
            //
            // * If the answer to q1 is "no" and the answer to q3 is "yes" then we are in a situation where
            //   strong-named Jones is referencing weak-named Smith, which is illegal. In the dev10 compiler
            //   we do not give an error about this until emit time. In Roslyn we have a new error, CS7029,
            //   which we give before emit time when we detect that weak-named Smith has given friend access
            //   to strong-named Jones, which then references Smith. However, we still want to give friend
            //   access to Jones for the purposes of semantic analysis.
            //
            // TODO: Roslyn C# does not yet give an error in other circumstances whereby a strong-named assembly
            // TODO: references a weak-named assembly.
            //
            // Let's make a chart that illustrates all the possible answers to these four questions, and
            // what the resulting accessibility should be:
            //
            // case q1  q2  q3  q4  Result                 Explanation
            // 1    YES YES YES YES SUCCESS          Smith has named this strong-named Jones as a friend.
            // 2    YES YES YES NO  NO MATCH         Smith has named a different strong-named Jones as a friend.
            // 3    YES YES NO  NO  NO MATCH         Smith has named a strong-named Jones as a friend, but this Jones is weak-named.
            // 4    YES NO  YES NO  SUCCESS          Smith has improperly (*) named any Jones as its friend. But we honor its offer of friendship.
            // 5    YES NO  NO  NO  SUCCESS          Smith has improperly (*) named any Jones as its friend. But we honor its offer of friendship.
            // 6    NO  YES YES YES SUCCESS, BAD REF Smith has named this strong-named Jones as a friend, but Jones should not be referring to a weak-named Smith.
            // 7    NO  YES YES NO  NO MATCH         Smith has named a different strong-named Jones as a friend.
            // 8    NO  YES NO  NO  NO MATCH         Smith has named a strong-named Jones as a friend, but this Jones is weak-named.
            // 9    NO  NO  YES NO  SUCCESS, BAD REF Smith has named any Jones as a friend, but Jones should not be referring to a weak-named Smith.
            // 10   NO  NO  NO  NO  SUCCESS          Smith has named any Jones as its friend.
            //                                     
            // (*) Smith was not built with a Roslyn compiler, which would have prevented this.
            //
            // This method never returns NoRelationshipClaimed because if control got here, then we assume
            // (as a precondition) that Smith named Jones as a friend somehow.

            bool q1 = assemblyGrantingAccessIdentity.IsStrongName;
            bool q2 = !grantedToPublicKey.IsDefaultOrEmpty;
            bool q3 = !assemblyWantingAccessKey.IsDefaultOrEmpty;
            bool q4 = (q2 & q3) && ByteSequenceComparer.Equals(grantedToPublicKey, assemblyWantingAccessKey);

            // Cases 2, 3, 7 and 8:
            if (q2 && !q4)
            {
                return IVTConclusion.PublicKeyDoesntMatch;
            }

            // Cases 6 and 9:
            if (!q1 && q3)
            {
                return IVTConclusion.OneSignedOneNot;
            }

            // Cases 1, 4, 5 and 10:
            return IVTConclusion.Match;
        }
    }
}
