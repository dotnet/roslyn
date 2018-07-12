// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class SolutionExplorer_InProc : InProcComponent
    {
        private SolutionExplorer_InProc() { }

        public static SolutionExplorer_InProc Create()
            => new SolutionExplorer_InProc();

        public void CleanUpOpenSolution()
            => InvokeOnUIThread(() =>
            {
                var dte = GetDTE();
                dte.Documents.CloseAll(EnvDTE.vsSaveChanges.vsSaveChangesNo);

                if (dte.Solution != null)
                {
                    var directoriesToDelete = new List<string>();

                    // Save the full path to each project in the solution. This is so we can
                    // cleanup any folders after the solution is closed.
                    foreach (EnvDTE.Project project in dte.Solution.Projects)
                    {
                        if (!string.IsNullOrEmpty(project.FullName))
                        {
                            directoriesToDelete.Add(Path.GetDirectoryName(project.FullName));
                        }
                    }

                    // Save the full path to the solution. This is so we can cleanup any folders after the solution is closed.
                    // The solution might be zero-impact and thus has no name, so deal with that
                    var solutionFullName = dte.Solution.FullName;

                    if (!string.IsNullOrEmpty(solutionFullName))
                    {
                        directoriesToDelete.Add(Path.GetDirectoryName(solutionFullName));
                    }

                    dte.Solution.Close(SaveFirst: false);

                    foreach (var directoryToDelete in directoriesToDelete)
                    {
                        IntegrationHelper.TryDeleteDirectoryRecursively(directoryToDelete);
                    }
                }
            });
    }
}
