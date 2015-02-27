// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [Flags]
    public enum ResetOptions
    {
        /// <summary>
        /// No options.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Initialize IInteractiveEvaluator process.
        /// </summary>
        Initialize = 0x1,

        /// <summary>
        /// Print command text to IInteractiveWindow.
        /// </summary>
        Print = 0x2,
    }
}
