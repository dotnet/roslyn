// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class EnumExtensions
    {
        public static T[] GetValues<T>() where T : struct
        {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}
