﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public void HandleError(object sender, Exception exception)
        {
            if (exception is ArgumentOutOfRangeException argumentOutOfRangeException
                && argumentOutOfRangeException.ParamName == "index"
                && argumentOutOfRangeException.StackTrace.Contains("Microsoft.NodejsTools.Repl.ReplOutputClassifier.GetClassificationSpans"))
            {
                // Known issue https://github.com/Microsoft/nodejstools/issues/2138
                return;
            }

            FatalError.Report(exception);
        }
    }
}
