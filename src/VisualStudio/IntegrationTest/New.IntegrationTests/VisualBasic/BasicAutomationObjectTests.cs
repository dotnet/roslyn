// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.VisualBasic.Options;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public class BasicAutomationObjectTests : AbstractAutomationObjectTests
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        protected override AbstractAutomationObject CreateAutomationObject(Workspace workspace)
            => new AutomationObject(workspace);
    }
}
