// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.VisualBasic
{
    internal class VisualBasicProjectFile : ProjectFile
    {
        public VisualBasicProjectFile(VisualBasicProjectFileLoader loader, MSB.Evaluation.Project loadedProject, ProjectBuildManager buildManager, DiagnosticLog log)
            : base(loader, loadedProject, buildManager, log)
        {
        }

        protected override SourceCodeKind GetSourceCodeKind(string documentFileName)
            => SourceCodeKind.Regular;

        public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
            => ".vb";

        protected override IEnumerable<MSB.Framework.ITaskItem> GetCompilerCommandLineArgs(MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.VbcCommandLineArgs);

        protected override ImmutableArray<string> ReadCommandLineArgs(MSB.Execution.ProjectInstance project)
            => VisualBasicCommandLineArgumentReader.Read(project);
    }
}
