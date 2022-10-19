// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal readonly record struct ExtractMethodOptions(
    bool DontPutOutOrRefOnStruct = true)
{
    public ExtractMethodOptions()
        : this(DontPutOutOrRefOnStruct: true)
    {
    }

    public static readonly ExtractMethodOptions Default = new();
}
