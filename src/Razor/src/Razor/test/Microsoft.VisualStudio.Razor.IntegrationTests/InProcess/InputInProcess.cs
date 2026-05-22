// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Forms;

namespace Microsoft.VisualStudio.Extensibility.Testing;

[TestService]
internal partial class InputInProcess
{
    internal void Send(string keys)
    {
        SendKeys.Send(keys);
    }
}

