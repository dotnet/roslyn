// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the results of overload resolution for a single member.
    /// </summary>
    public struct CommonMemberResolutionResult<TMember> where TMember : ISymbol
    {
        /// <summary>
        /// The member considered during overload resolution.
        /// </summary>
        public TMember Member { get; private set; }

        /// <summary>
        /// Indicates why the compiler accepted or rejected the member during overload resolution.
        /// </summary>
        public CommonMemberResolutionKind Resolution { get; private set; }

        /// <summary>
        /// Returns true if the compiler accepted this member as the sole correct result of overload resolution.
        /// </summary>
        public bool IsValid { get; private set; }

        internal CommonMemberResolutionResult(
            TMember member,
            CommonMemberResolutionKind resolution,
            bool isValid)
            : this()
        {
            this.Member = member;
            this.Resolution = resolution;
            this.IsValid = isValid;
        }
    }
}