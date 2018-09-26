// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.PasteTracking
{
    [UseExportProvider]
    public class PasteTrackingServiceTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void PasteTracking_MissingTextSpan_WhenNothingPasted()
        {
            var code = @"
class C
{
$$
}";

            using (var testState = PasteTrackingTestState.Create(code, LanguageNames.CSharp))
            {
                testState.AssertMissingPastedTextSpan();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void PasteTracking_HasTextSpan_AfterPaste()
        {
            var code = @"
class C
{
$$
}";

            var pasted = @"
public void Main(string[] args)
{
}
";

            using (var testState = PasteTrackingTestState.Create(code, LanguageNames.CSharp))
            {
                var start = testState.HostDocument.CursorPosition.GetValueOrDefault();
                var length = pasted.Length;
                var expectedTextSpan = new Text.TextSpan(start, length);

                testState.SendPaste(pasted);
                testState.AssertHasPastedTextSpan(expectedTextSpan);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void PasteTracking_MissingTextSpan_AfterPasteThenEdit()
        {
            var code = @"
class C
{
$$
}";

            var pasted = @"
public void Main(string[] args)
{
}
";

            using (var testState = PasteTrackingTestState.Create(code, LanguageNames.CSharp))
            {
                testState.SendPaste(pasted);
                testState.AssertHasPastedTextSpan();

                testState.InsertText(Environment.NewLine);
                testState.AssertMissingPastedTextSpan();
            }
        }


        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void PasteTracking_MissingTextSpan_AfterPasteThenClose()
        {
            var code = @"
class C
{
$$
}";

            var pasted = @"
public void Main(string[] args)
{
}
";

            using (var testState = PasteTrackingTestState.Create(code, LanguageNames.CSharp))
            {
                testState.SendPaste(pasted);
                testState.AssertHasPastedTextSpan();

                testState.CloseView();
                testState.AssertMissingPastedTextSpan();
            }
        }
    }
}
