// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Text;

namespace Roslyn.VisualStudio.Test.Setup
{
    /// <summary>
    /// This class causes a crash if an exception is encountered by the editor.
    /// </summary>
    [Shared, Export(typeof(IExtensionErrorHandler)), Export(typeof(TestExtensionErrorHandler))]
    public class TestExtensionErrorHandler : IExtensionErrorHandler
    {
        public void HandleError(object sender, System.Exception exception)
        {
            FatalError.Report(exception);
        }
    }
}