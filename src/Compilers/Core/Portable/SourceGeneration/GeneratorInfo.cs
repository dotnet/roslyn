// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal readonly struct GeneratorInfo
    {
        internal EditCallback<AdditionalFileEdit>? EditCallback { get; }

        internal GeneratorInfo(EditCallback<AdditionalFileEdit>? editCallback)
        {
            EditCallback = editCallback;
        }

        internal class Builder
        {
            internal EditCallback<AdditionalFileEdit>? EditCallback { get; set; }

            public GeneratorInfo ToImmutable() => new GeneratorInfo(EditCallback);
        }
    }
}
