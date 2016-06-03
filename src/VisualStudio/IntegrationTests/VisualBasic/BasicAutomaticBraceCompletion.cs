// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicAutomaticBraceCompletion : EditorTestFixture
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicAutomaticBraceCompletion(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicAutomaticBraceCompletion))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Braces_InsertionAndTabCompleting()
        {
            SetUpEditor(@"
Class C
    Sub Foo()
        $$
    End Sub
End Class");

            SendKeys("Dim x = {");
            VerifyCurrentLineText("Dim x = {$$}");

            SendKeys(
                "New Object",
                VirtualKey.Escape,
                VirtualKey.Tab);

            VerifyCurrentLineText("Dim x = {New Object}$$");
        }
    }
}
