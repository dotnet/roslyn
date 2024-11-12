// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ExportUnitTestingIncrementalAnalyzerProviderAttribute : ExportAttribute
{
    public string Name { get; }
    public string[] WorkspaceKinds { get; }

    public ExportUnitTestingIncrementalAnalyzerProviderAttribute(string name, string[] workspaceKinds)
        : base(typeof(IUnitTestingIncrementalAnalyzerProvider))
    {
        this.WorkspaceKinds = workspaceKinds;
        this.Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
