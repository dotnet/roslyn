// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Indicates why the compiler accepted or rejected the member during overload resolution.
    /// </summary>
    public enum CommonMemberResolutionKind
    {
        /// <summary>
        /// The candidate member was accepted.
        /// </summary>
        Applicable,

        /// <summary>
        /// The candidate member was rejected because it is not supported by the language or cannot
        /// be used given the current set of assembly references.
        /// </summary>
        UseSiteError,

        /// <summary>
        /// The candidate member was rejected because type inference failed.
        /// </summary>
        TypeInferenceFailed,

        /// <summary>
        /// The candidate member was rejected because it was considered worse that another member.
        /// </summary>
        Worse,
    }
}