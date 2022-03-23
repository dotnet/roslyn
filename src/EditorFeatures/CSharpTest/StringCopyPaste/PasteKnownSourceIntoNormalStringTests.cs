// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public class PasteKnownSourceIntoNormalStringTests : StringCopyPasteCommandHandlerKnownSourceTests
    {
        [WpfFact]
        public void TestPasteSimpleNormalLiteralContent()
        {
            TestCopyPaste(
@"var v = ""{|Copy:goo|}"";",
@"
var dest =
    ""[||]"";",
@"
var dest =
    ""goo[||]"";",
@"
var dest =
    ""[||]"";");
        }
    }
}
