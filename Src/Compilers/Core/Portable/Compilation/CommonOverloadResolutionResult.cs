// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Summarizes the results of an overload resolution analysis, as described in section 7.5 of
    /// the language specification. Describes whether overload resolution succeeded, and which
    /// method was selected if overload resolution succeeded, as well as detailed information about
    /// each method that was considered. 
    /// </summary>
    public struct CommonOverloadResolutionResult<TMember> where TMember : ISymbol
    {
        // Create an overload resolution result from a single result.
        internal CommonOverloadResolutionResult(
            bool succeeded,
            CommonMemberResolutionResult<TMember>? validResult,
            CommonMemberResolutionResult<TMember>? bestResult,
            ImmutableArray<CommonMemberResolutionResult<TMember>> results)
            : this()
        {
            this.Succeeded = succeeded;
            this.ValidResult = validResult;
            this.BestResult = bestResult;
            this.Results = results;
        }

        /// <summary>
        /// True if overload resolution successfully selected a single best method.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// If overload resolution successfully selected a single best method, returns information
        /// about that method. Otherwise returns null.
        /// </summary>
        public CommonMemberResolutionResult<TMember>? ValidResult { get; private set; }

        /// <summary>
        /// If there was a method that overload resolution considered better than all others,
        /// returns information about that method. A method may be returned even if that method was
        /// not considered a successful overload resolution, as long as it was better that any other
        /// potential method considered.
        /// </summary>
        public CommonMemberResolutionResult<TMember>? BestResult { get; private set; }

        /// <summary>
        /// Returns information about each method that was considered during overload resolution,
        /// and what the results of overload resolution were for that method.
        /// </summary>
        public ImmutableArray<CommonMemberResolutionResult<TMember>> Results { get; private set; }
    }
}