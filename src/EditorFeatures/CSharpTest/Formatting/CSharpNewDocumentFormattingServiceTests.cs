// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities.Formatting;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class CSharpNewDocumentFormattingServiceTests : AbstractNewDocumentFormattingServiceTests
    {
        protected override string Language => LanguageNames.CSharp;
        protected override TestWorkspace CreateTestWorkspace(string testCode)
            => TestWorkspace.CreateCSharp(testCode);

        [Fact]
        public async Task TestOrganizeUsingsWithNoUsings()
        {
            var testCode = @"namespace Goo
{
}";
            await TestAsync(
                testCode: testCode,
                expected: testCode,
                options:
                    (CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption2.Error)));
        }

        [Fact]
        public async Task TestFileBanners()
        {
            await TestAsync(testCode: @"using System;

namespace Goo
{
}",
            expected: @"// This is a banner.

using System;

namespace Goo
{
}",
            options:
                (CodeStyleOptions2.FileHeaderTemplate, "This is a banner."));
        }

        [Fact]
        public async Task TestAccessibilityModifiers()
        {
            await TestAsync(testCode: @"using System;

namespace Goo
{
    class C
    {
    }
}",
            expected: @"using System;

namespace Goo
{
    internal class C
    {
    }
}",
            options:
                (CodeStyleOptions2.RequireAccessibilityModifiers, new CodeStyleOption2<AccessibilityModifiersRequired>(AccessibilityModifiersRequired.Always, NotificationOption2.Error)));
        }

        [Fact]
        public async Task TestUsingDirectivePlacement()
        {
            await TestAsync(testCode: @"using System;

namespace Goo
{
}",
            expected: @"namespace Goo
{
    using System;
}",
            options:
                (CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, new CodeStyleOption2<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption2.Error)));
        }
    }
}
