// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Xunit;

internal class CriticalIdeTheoryAttribute : IdeTheoryAttribute
{
    [Obsolete("Critical tests cannot be skipped.", error: true)]
    public new string Skip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }
}
