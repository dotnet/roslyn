// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

[Export(typeof(IExtensionErrorHandler))]
[Export(typeof(ITestErrorHandler))]
internal sealed class TestExtensionErrorHandler : IExtensionErrorHandler, ITestErrorHandler
{
    public ImmutableList<Exception> Exceptions { get; private set; } = [];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestExtensionErrorHandler()
    {
    }

    public void HandleError(object sender, Exception exception)
    {
        // This exception is unexpected and as such we want the containing test case to
        // fail. Unfortuntately throwing an exception here is not going to help because
        // the editor is going to catch and swallow it. Store it here and wait for the 
        // containing workspace to notice it and throw.
        Exceptions = Exceptions.Add(exception);
    }
}
