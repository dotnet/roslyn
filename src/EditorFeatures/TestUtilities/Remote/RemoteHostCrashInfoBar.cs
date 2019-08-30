// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
