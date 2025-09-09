// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Setup;

/// <summary>This class causes a crash if an exception is encountered by the editor.</summary>
[Shared, Export(typeof(IExtensionErrorHandler)), Export(typeof(TestExtensionErrorHandler))]
public sealed class TestExtensionErrorHandler : IExtensionErrorHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestExtensionErrorHandler()
    {
    }

    public void HandleError(object sender, Exception exception)
    {
        FatalError.ReportAndPropagate(exception);
        TestTraceListener.Instance.AddException(exception);
    }
}
