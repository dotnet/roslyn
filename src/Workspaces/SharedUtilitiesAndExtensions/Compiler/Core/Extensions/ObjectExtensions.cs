﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ObjectExtensions
    {
        public static string GetTypeDisplayName(this object? obj)
        {
            return obj == null ? "null" : obj.GetType().Name;
        }
    }
}
