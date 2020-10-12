﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpProjectFile : ProjectFile
    {
        public CSharpProjectFile(CSharpProjectFileLoader loader, MSB.Evaluation.Project project, ProjectBuildManager buildManager, DiagnosticLog log)
            : base(loader, project, buildManager, log)
        {
        }

        protected override SourceCodeKind GetSourceCodeKind(string documentFileName)
            => SourceCodeKind.Regular;

        public override string GetDocumentExtension(SourceCodeKind sourceCodeKind)
            => ".cs";

        protected override IEnumerable<MSB.Framework.ITaskItem> GetCompilerCommandLineArgs(MSB.Execution.ProjectInstance executedProject)
            => executedProject.GetItems(ItemNames.CscCommandLineArgs);

        protected override ImmutableArray<string> ReadCommandLineArgs(MSB.Execution.ProjectInstance project)
            => CSharpCommandLineArgumentReader.Read(project);
    }
}
