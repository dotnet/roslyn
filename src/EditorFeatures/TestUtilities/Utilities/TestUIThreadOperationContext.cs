// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

internal sealed class TestUIThreadOperationContext : AbstractUIThreadOperationContext
{
    public TestUIThreadOperationContext()
        : base(allowCancellation: false, defaultDescription: "")
    {
    }
}
