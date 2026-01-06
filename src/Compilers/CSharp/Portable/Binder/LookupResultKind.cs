// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Classifies the different ways in which a found symbol might be incorrect.
    /// Higher values are considered "better" than lower values. These values are used
    /// in a few different places:
    ///    1) Inside a LookupResult to indicate the quality of a symbol from lookup.
    ///    2) Inside a bound node (for example, BoundBadExpression), to indicate
    ///       the "binding quality" of the symbols referenced by that bound node.
    ///    3) Inside an error type symbol, to indicate the reason that the candidate symbols
    ///       in the error type symbols were not good.
    ///       
    /// While most of the values can occur in all places, some of the problems are not
    /// detected at lookup time (e.g., NotAVariable), so only occur in bound nodes.
    /// </summary>
    /// <remarks>
    /// This enumeration is parallel to and almost the same as the CandidateReason enumeration.
    /// Changes to one should usually result in changes to the other.
    /// 
    /// There are two enumerations because:
    ///   1) CandidateReason in language-independent, while this enum is language specific.
    ///   2) The name "CandidateReason" didn't make much sense in the way LookupResultKind is used internally.
    ///   3) Viable isn't used in CandidateReason, but we need it in LookupResultKind, and there isn't a 
    ///      a way to have internal enumeration values.
    /// </remarks>
    internal enum LookupResultKind : byte
    {
        // Note: order is important! High values take precedence over lower values. 

        Empty,
        NotATypeOrNamespace,
        NotAnAttributeType,
        WrongArity,
        NotCreatable,      // E.g., new of an interface or static class
        Inaccessible,
        NotReferencable,   // E.g., get_Goo binding to an accessor.
        NotAValue,
        NotAVariable,      // used for several slightly different places, e.g. LHS of =, out/ref parameters, etc.
        NotInvocable,
        NotLabel,          // used when a label is required
        StaticInstanceMismatch,
        OverloadResolutionFailure,

        // Note: within LookupResult, LookupResultKind.Ambiguous is currently not used (in C#). Instead
        // ambiguous results are determined later by examining multiple viable results to determine if
        // they are ambiguous or overloaded. Thus, LookupResultKind.Ambiguous does not occur in a LookupResult,
        // but can occur within a BoundBadExpression.
        Ambiguous,

        // Indicates a set of symbols, and they are totally fine.
        MemberGroup,

        // Indicates a single symbol is totally fine.
        Viable,
    }

    internal static class LookupResultKindExtensions
    {
        /// <summary>
        /// Maps a LookupResultKind to a CandidateReason. Should not be called on LookupResultKind.Viable!
        /// </summary>
        public static CandidateReason ToCandidateReason(this LookupResultKind resultKind)
        {
            switch (resultKind)
            {
                case LookupResultKind.Empty: return CandidateReason.None;
                case LookupResultKind.NotATypeOrNamespace: return CandidateReason.NotATypeOrNamespace;
                case LookupResultKind.NotAnAttributeType: return CandidateReason.NotAnAttributeType;
                case LookupResultKind.WrongArity: return CandidateReason.WrongArity;
                case LookupResultKind.Inaccessible: return CandidateReason.Inaccessible;
                case LookupResultKind.NotCreatable: return CandidateReason.NotCreatable;
                case LookupResultKind.NotReferencable: return CandidateReason.NotReferencable;
                case LookupResultKind.NotAValue: return CandidateReason.NotAValue;
                case LookupResultKind.NotAVariable: return CandidateReason.NotAVariable;
                case LookupResultKind.NotInvocable: return CandidateReason.NotInvocable;
                case LookupResultKind.StaticInstanceMismatch: return CandidateReason.StaticInstanceMismatch;
                case LookupResultKind.OverloadResolutionFailure: return CandidateReason.OverloadResolutionFailure;
                case LookupResultKind.Ambiguous: return CandidateReason.Ambiguous;
                case LookupResultKind.MemberGroup: return CandidateReason.MemberGroup;

                case LookupResultKind.Viable:
                    Debug.Assert(false, "Should not call this on LookupResultKind.Viable");
                    return CandidateReason.None;

                default:
                    throw ExceptionUtilities.UnexpectedValue(resultKind);
            }
        }

        // Return the lowest non-empty result kind
        public static LookupResultKind WorseResultKind(this LookupResultKind resultKind1, LookupResultKind resultKind2)
        {
            if (resultKind1 == LookupResultKind.Empty)
                return resultKind2;
            if (resultKind2 == LookupResultKind.Empty)
                return resultKind1;
            if (resultKind1 < resultKind2)
                return resultKind1;
            else
                return resultKind2;
        }
    }
}
