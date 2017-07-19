// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// Mock to make test project build
    /// </summary>
    internal class WatsonReporter
    {
        public static void Report(string description, Exception exception, Func<IFaultUtility, int> callback)
        {
            // do nothing
        }

        public interface IFaultUtility
        {
            void AddProcessDump(int pid);
            void AddFile(string fullpathname);
        }
    }
}
