﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
