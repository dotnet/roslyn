// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Extensibility.Testing;

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

internal interface ITextViewWindowVerifierInProcess
{
    TestServices TestServices { get; }

    ITextViewWindowInProcess TextViewWindow { get; }
}
