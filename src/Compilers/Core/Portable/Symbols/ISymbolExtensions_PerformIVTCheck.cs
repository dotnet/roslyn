// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            // First off, let's assume that the "other" assembly is GrantingAssembly.DLL, that the "this"
            // assembly is "WantingAssembly.DLL", and that GrantingAssembly has named WantingAssembly as a friend (that is a precondition
            // to calling this method). Whether we allow WantingAssembly to see internals of GrantingAssembly depends on these four factors:
            //
            // q1) Is GrantingAssembly strong-named?
            // q2) Did GrantingAssembly name WantingAssembly as a friend via a strong name?
            // q3) Is WantingAssembly strong-named?
            // q4) Does GrantingAssembly give a strong-name for WantingAssembly that matches our strong name?
            //
            // Before we dive into the details, we should mention two additional facts:
            //
            // * If the answer to q1 is "yes", and GrantingAssembly was compiled by a Roslyn compiler, then q2 must be "yes" also.
            //   Strong-named GrantingAssembly must only be friends with strong-named WantingAssembly. See the blog article
            //   http://blogs.msdn.com/b/ericlippert/archive/2009/06/04/alas-smith-and-jones.aspx
            //   for an explanation of why this feature is desirable.
            //
            //   Now, just because the compiler enforces this rule does not mean that we will never run into
            //   a scenario where GrantingAssembly is strong-named and names WantingAssembly via a weak name. Not all assemblies
            //   were compiled with a Roslyn compiler. We still need to deal sensibly with this situation.
            //   We do so by ignoring the problem; if strong-named GrantingAssembly extends friendship to weak-named
            //   WantingAssembly then we're done; any assembly named WantingAssembly is a friend of GrantingAssembly.
            //
            //   Incidentally, the C# compiler produces error CS1726, ERR_FriendAssemblySNReq, and VB produces
            //   the error VB31535, ERR_FriendAssemblyStrongNameRequired, when compiling 
            //   a strong-named GrantingAssembly that names a weak-named WantingAssembly as its friend.
            //
            // * If the answer to q1 is "no" and the answer to q3 is "yes" then we are in a situation where
            //   strong-named WantingAssembly is referencing weak-named GrantingAssembly, which is illegal. In the dev10 compiler
            //   we do not give an error about this until emit time. In Roslyn we have a new error, CS7029,
            //   which we give before emit time when we detect that weak-named GrantingAssembly has given friend access
            //   to strong-named WantingAssembly, which then references GrantingAssembly. However, we still want to give friend
            //   access to WantingAssembly for the purposes of semantic analysis.
            //
            // Roslyn C# does not yet give an error in other circumstances whereby a strong-named assembly
            // references a weak-named assembly. See https://github.com/dotnet/roslyn/issues/26722
            //
            // Let's make a chart that illustrates all the possible answers to these four questions, and
            // what the resulting accessibility should be:
            //
            // case q1  q2  q3  q4  Result                 Explanation
            // 1    YES YES YES YES SUCCESS          GrantingAssembly has named this strong-named WantingAssembly as a friend.
            // 2    YES YES YES NO  NO MATCH         GrantingAssembly has named a different strong-named WantingAssembly as a friend.
            // 3    YES YES NO  NO  NO MATCH         GrantingAssembly has named a strong-named WantingAssembly as a friend, but this WantingAssembly is weak-named.
            // 4    YES NO  YES NO  SUCCESS          GrantingAssembly has improperly (*) named any WantingAssembly as its friend. But we honor its offer of friendship.
            // 5    YES NO  NO  NO  SUCCESS          GrantingAssembly has improperly (*) named any WantingAssembly as its friend. But we honor its offer of friendship.
            // 6    NO  YES YES YES SUCCESS, BAD REF GrantingAssembly has named this strong-named WantingAssembly as a friend, but WantingAssembly should not be referring to a weak-named GrantingAssembly.
            // 7    NO  YES YES NO  NO MATCH         GrantingAssembly has named a different strong-named WantingAssembly as a friend.
            // 8    NO  YES NO  NO  NO MATCH         GrantingAssembly has named a strong-named WantingAssembly as a friend, but this WantingAssembly is weak-named.
            // 9    NO  NO  YES NO  SUCCESS, BAD REF GrantingAssembly has named any WantingAssembly as a friend, but WantingAssembly should not be referring to a weak-named GrantingAssembly.
            // 10   NO  NO  NO  NO  SUCCESS          GrantingAssembly has named any WantingAssembly as its friend.
            //                                     
            // (*) GrantingAssembly was not built with a Roslyn compiler, which would have prevented this.
            //
            // This method never returns NoRelationshipClaimed because if control got here, then we assume
            // (as a precondition) that GrantingAssembly named WantingAssembly as a friend somehow.

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
