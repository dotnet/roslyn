// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    /// <summary>
    /// Mock to make test project build
    /// </summary>
    internal class WatsonReporter
    {
        public static void Report(string description, Exception exception)
        {
            // do nothing
        }

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

    // Mock resource to make test project build
    internal static class ServicesVSResources
    {
        public const string Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio = "";
    }
}
