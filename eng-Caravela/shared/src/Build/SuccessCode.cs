// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Engineering.BuildTools.Build
{
    public enum SuccessCode
    {
        /// <summary>
        /// Success.
        /// </summary>
        Success,

        /// <summary>
        /// Error, but we can try to continue to the next item.
        /// </summary>
        Error,

        /// <summary>
        /// Error, and we have to stop immediately.
        /// </summary>
        Fatal
    }
}