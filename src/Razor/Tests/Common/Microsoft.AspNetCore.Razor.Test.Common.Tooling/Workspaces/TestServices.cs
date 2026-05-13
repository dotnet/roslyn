// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.AspNetCore.Razor.Test.Common.Workspaces;

public class TestServices : HostServices
{
    private readonly IEnumerable<IWorkspaceService> _workspaceServices;
    private readonly IEnumerable<ILanguageService> _razorLanguageServices;

    private TestServices(IEnumerable<IWorkspaceService> workspaceServices, IEnumerable<ILanguageService> razorLanguageServices)
    {
        _workspaceServices = workspaceServices;
        _razorLanguageServices = razorLanguageServices;
    }

    protected override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
    {
        return new TestWorkspaceServices(this, _workspaceServices, _razorLanguageServices, workspace);
    }

    public static HostServices Create(IEnumerable<ILanguageService> razorLanguageServices)
        => Create(Enumerable.Empty<IWorkspaceService>(), razorLanguageServices);

    public static HostServices Create(IEnumerable<IWorkspaceService> workspaceServices, IEnumerable<ILanguageService> razorLanguageServices)
        => new TestServices(workspaceServices, razorLanguageServices);
}
