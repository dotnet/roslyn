﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SolutionExtensions
    {
        public static void WriteTo(this IObjectWritable @object, ObjectWriter writer)
        {
            @object.WriteTo(writer);
        }
    }
}
