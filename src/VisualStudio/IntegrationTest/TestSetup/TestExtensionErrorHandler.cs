﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    /// <summary>This class causes a crash if an exception is encountered by the editor.</summary>
    [Shared, Export(typeof(IExtensionErrorHandler)), Export(typeof(TestExtensionErrorHandler))]
    public class TestExtensionErrorHandler : IExtensionErrorHandler
    {
        [ImportingConstructor]
        public TestExtensionErrorHandler()
        {
        }

        public void HandleError(object sender, Exception exception)
        {
            if (exception is ArgumentOutOfRangeException argumentOutOfRangeException
                && argumentOutOfRangeException.ParamName == "index"
                && argumentOutOfRangeException.StackTrace.Contains("Microsoft.NodejsTools.Repl.ReplOutputClassifier.GetClassificationSpans"))
            {
                // Known issue https://github.com/Microsoft/nodejstools/issues/2138
                return;
            }

            if (exception is ArgumentException argumentException
                && argumentException.Message.Contains("SnapshotPoint")
                && argumentException.StackTrace.Contains("Microsoft.VisualStudio.Text.Editor.Implementation.WpfTextView.ValidateBufferPosition"))
            {
                // Known issue https://github.com/dotnet/roslyn/issues/35123
                return;
            }

            FatalError.Report(exception);
        }
    }
}
