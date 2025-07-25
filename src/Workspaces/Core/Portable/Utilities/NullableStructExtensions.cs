// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Utilities;

internal static class NullableStructExtensions
{
    extension<T>(T? value) where T : struct
    {
        public void Deconstruct(out T valueOrDefault, out bool hasValue)
        {
            valueOrDefault = value.GetValueOrDefault();
            hasValue = value.HasValue;
        }
    }
}
