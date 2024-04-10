// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;

namespace Roslyn.VisualStudio.NewIntegrationTests
{
    public abstract class AbstractWebApplicationTest : AbstractEditorTest
    {
        protected static class GroupIds
        {
            public const string Server = "Microsoft.Web.Blazor.Server";
            public const string Wasm = "Microsoft.Web.Blazor.Wasm";
        }

        protected AbstractWebApplicationTest(string solutionName)
            : base(solutionName,
                  projectTemplate: WellKnownProjectTemplates.Blazor,
                  templateGroupId: GroupIds.Server)
        {
        }
    }
}
