// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator;

internal unsafe struct SampleFixedBuffer
{
    public fixed byte Buffer[4];

    public static SampleFixedBuffer Create()
    {
        SampleFixedBuffer val = default;
        val.Buffer[0] = 0;
        val.Buffer[1] = 1;
        val.Buffer[2] = 2;
        val.Buffer[3] = 3;

        return val;
    }
}
