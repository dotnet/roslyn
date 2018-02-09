// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    /// <summary>
    /// Explicitly indicates result is void
    /// </summary>
    internal sealed class VoidResult
    {
        /// <summary>
        /// Use this in case default(VoidResult) is not desirable
        /// </summary>
        public static readonly VoidResult Instance = new VoidResult();

        // prevent someone from newing this type
        private VoidResult()
        {

        }
    }
}
