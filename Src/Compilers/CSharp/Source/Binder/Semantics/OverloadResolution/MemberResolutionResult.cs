// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents the results of overload resolution for a single member.
    /// </summary>
    internal struct MemberResolutionResult<TMember> where TMember : Symbol
    {
        private readonly TMember member;
        private readonly TMember leastOverriddenMember;
        private readonly MemberAnalysisResult result;

        internal MemberResolutionResult(TMember member, TMember leastOverriddenMember, MemberAnalysisResult result)
        {
            this.member = member;
            this.leastOverriddenMember = leastOverriddenMember;
            this.result = result;
        }

        internal bool IsNull
        {
            get { return (object)member == null; }
        }

        internal bool IsNotNull
        {
            get { return (object)member != null; }
        }

        /// <summary>
        /// The member considered during overload resolution.
        /// </summary>
        public TMember Member
        {
            get { return member; }
        }

        /// <summary>
        /// The least overridden member that is accessible from the call site that performed overload resolution. 
        /// Typically a virtual or abstract method (but not necessarily).
        /// </summary>
        /// <remarks>
        /// The member whose parameter types and params modifiers were considered during overload resolution.
        /// </remarks>
        internal TMember LeastOverriddenMember
        {
            get { return leastOverriddenMember; }
        }

        /// <summary>
        /// Indicates why the compiler accepted or rejected the member during overload resolution.
        /// </summary>
        public MemberResolutionKind Resolution
        {
            get
            {
                return Result.Kind;
            }
        }

        /// <summary>
        /// Returns true if the compiler accepted this member as the sole correct result of overload resolution.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return Result.IsValid;
            }
        }

        public bool IsApplicable
        {
            get
            {
                return Result.IsApplicable;
            }
        }

        internal bool HasUseSiteDiagnosticToReport
        {
            get
            {
                return this.result.HasUseSiteDiagnosticToReportFor(this.member);
            }
        }

        /// <summary>
        /// The result of member analysis.
        /// </summary>
        internal MemberAnalysisResult Result
        {
            get { return result; }
        }

        internal CommonMemberResolutionResult<TSymbol> ToCommon<TSymbol>()
            where TSymbol : ISymbol
        {
            return new CommonMemberResolutionResult<TSymbol>(
                (TSymbol)(ISymbol)this.Member,
                ConvertKind(this.Resolution),
                this.IsValid);
        }

        private static CommonMemberResolutionKind ConvertKind(MemberResolutionKind kind)
        {
            switch (kind)
            {
                case MemberResolutionKind.ApplicableInExpandedForm:
                case MemberResolutionKind.ApplicableInNormalForm:
                    return CommonMemberResolutionKind.Applicable;
                case MemberResolutionKind.UseSiteError:
                case MemberResolutionKind.UnsupportedMetadata:
                    return CommonMemberResolutionKind.UseSiteError;
                case MemberResolutionKind.TypeInferenceFailed:
                case MemberResolutionKind.TypeInferenceExtensionInstanceArgument:
                    return CommonMemberResolutionKind.TypeInferenceFailed;
                default:
                    return CommonMemberResolutionKind.Worse;
            }
        }

        public override bool Equals(object obj)
        {
            throw new NotSupportedException();
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }
    }
}