﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UnusedReferences
{
    internal sealed class ReferenceUpdate
    {
        /// <summary>
        /// Indicates action to perform on the reference.
        /// </summary>
        public UpdateAction Action { get; set; }

        /// <summary>
        /// Gets the reference to be updated.
        /// </summary>
        public ReferenceInfo ReferenceInfo { get; }

        public ReferenceUpdate(UpdateAction action, ReferenceInfo referenceInfo)
        {
            Action = action;
            ReferenceInfo = referenceInfo;
        }
    }
}
