// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator
{
    internal static class Utilities
    {
        public static string ToDisplayString(this TimeSpan timeSpan)
        {
            return timeSpan.TotalSeconds.ToString("N2") + " seconds";
        }
    }
}
