// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents the results of overload resolution for a single member.
    /// </summary>
    [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
    internal struct MemberResolutionResult<TMember> where TMember : Symbol
    {
        private readonly TMember _member;
        private readonly TMember _leastOverriddenMember;
        private readonly MemberAnalysisResult _result;

        internal MemberResolutionResult(TMember member, TMember leastOverriddenMember, MemberAnalysisResult result)
        {
            _member = member;
            _leastOverriddenMember = leastOverriddenMember;
            _result = result;
        }

        internal bool IsNull
        {
            get { return (object)_member == null; }
        }

        internal bool IsNotNull
        {
            get { return (object)_member != null; }
        }

        /// <summary>
        /// The member considered during overload resolution.
        /// </summary>
        public TMember Member
        {
            get { return _member; }
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
            get { return _leastOverriddenMember; }
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
                return _result.HasUseSiteDiagnosticToReportFor(_member);
            }
        }

        /// <summary>
        /// The result of member analysis.
        /// </summary>
        internal MemberAnalysisResult Result
        {
            get { return _result; }
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
