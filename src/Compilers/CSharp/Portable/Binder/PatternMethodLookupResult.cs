// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
        /// One or more errors occured while performing the lookup
        /// </summary>
        ResultHasErrors
    }

}

