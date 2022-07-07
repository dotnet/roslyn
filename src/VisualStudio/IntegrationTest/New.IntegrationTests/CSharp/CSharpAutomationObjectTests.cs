// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpAutomationObjectTests : AbstractAutomationObjectTests
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override AbstractAutomationObject CreateAutomationObject(Workspace workspace)
            => new AutomationObject(workspace);
    }
}
