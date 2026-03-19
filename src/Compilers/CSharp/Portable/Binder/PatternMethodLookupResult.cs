// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum PatternLookupResult
    {
        /// <summary>
        /// The Lookup was successful
        /// </summary>
        Success,

        /// <summary>
        /// A member was found, but it was not a method
        /// </summary>
        NotAMethod,

        /// <summary>
        /// A member was found, but it was not callable
        /// </summary>
        NotCallable,

        /// <summary>
        /// The lookup failed to find anything
        /// </summary>
        NoResults,

        /// <summary>
        /// One or more errors occurred while performing the lookup
        /// </summary>
        ResultHasErrors
    }

}

