// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.CSharp;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.LanguageServerProtocol
{
    /// <summary>
    /// A set of LSP specific goto definition tests.
    /// These tests test behavior that only applies to the LSP version of goto definition.
    /// </summary>
    [Collection(nameof(SharedIntegrationHostFixture))]
    [Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
    public class LspGoToDefinition : CSharpGoToDefinition
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public LspGoToDefinition(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        /// <summary>
        /// We need to pass in a different window name to look for declarations in as the LSP client
        /// uses a different name to create the references window.
        /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1286575
        /// </summary>
        [WpfFact, Trait(Traits.Feature, Traits.Features.GoToDefinition), Trait(Traits.Editor, Traits.Editors.LanguageServerProtocol)]
        [ConditionalFact]
        public override void GoToDefinitionWithMultipleResults()
        {
            TestGoToDefinitionWithMultipleResults(declarationWindowName: "'PartialClass' references");
        }
    }
}
