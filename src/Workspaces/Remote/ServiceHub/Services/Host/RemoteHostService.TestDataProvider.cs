// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteHostService
    {
        internal sealed class TestDataProvider
        {
            public bool IsInProc { get; private set; }

            public TestDataProvider(bool isInProc)
            {
                IsInProc = isInProc;
            }
        }
    }
}
