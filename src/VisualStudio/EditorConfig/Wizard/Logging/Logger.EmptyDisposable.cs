// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging;

public partial class Logger
{
    private class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new EmptyDisposable();

        public void Dispose() { }
    }
}
