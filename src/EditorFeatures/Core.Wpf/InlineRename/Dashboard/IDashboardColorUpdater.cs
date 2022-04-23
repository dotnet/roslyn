﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal interface IDashboardColorUpdater
    {
        /// <summary>
        /// Implemented by a host to set the properties on <see cref="DashboardColors"/>.
        /// </summary>
        public void UpdateColors();
    }
}
