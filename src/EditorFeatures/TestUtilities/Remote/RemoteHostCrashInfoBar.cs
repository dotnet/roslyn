﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    /// <summary>
    /// Mock for test framework
    /// </summary>
    internal static class RemoteHostCrashInfoBar
    {
        public static void ShowInfoBar(Workspace workspace, Exception ex = null)
        {
            // do nothing
        }
    }
}
