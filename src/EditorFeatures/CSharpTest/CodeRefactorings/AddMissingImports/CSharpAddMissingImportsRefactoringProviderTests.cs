// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddMissingImports;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.AddMissingImports)]
    public class CSharpAddMissingImportsRefactoringProviderTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
        {
            var testWorkspace = (TestWorkspace)workspace;
            var pasteTrackingService = testWorkspace.ExportProvider.GetExportedValue<PasteTrackingService>();
            return new CSharpAddMissingImportsRefactoringProvider(pasteTrackingService);
        }

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
        {
            var workspace = TestWorkspace.CreateCSharp(initialMarkup);

            // Treat the span being tested as the pasted span
            var hostDocument = workspace.Documents.First();
            var pastedTextSpan = hostDocument.SelectedSpans.FirstOrDefault();

            if (!pastedTextSpan.IsEmpty)
            {
                var pasteTrackingService = workspace.ExportProvider.GetExportedValue<PasteTrackingService>();
                pasteTrackingService.RegisterPastedTextSpan(hostDocument.TextBuffer, pastedTextSpan);
            }

            return workspace;
        }

        [WpfFact]
        public async Task AddMissingImports_AddImport_PasteContainsSingleMissingImport()
        {
            var code = @"
class C
{
    public [|D|] Foo { get; }
}

namespace A
{
    public class D { }
}
";

            var expected = @"
using A;

class C
{
    public D Foo { get; }
}

namespace A
{
    public class D { }
}
";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WpfFact]
        public async Task AddMissingImports_AddImports_PasteContainsMultipleMissingImports()
        {
            var code = @"
using System;

class C
{
    [|public D Foo { get; }
    public E Bar { get; }|]
}

namespace A
{
    public class D { }
}

namespace B
{
    public class E { }
}
";

            var expected = @"
using System;
using A;
using B;

class C
{
    public D Foo { get; }
    public E Bar { get; }
}

namespace A
{
    public class D { }
}

namespace B
{
    public class E { }
}
";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WpfFact]
        public async Task AddMissingImports_PartialFix_PasteContainsFixableAndAmbiguousMissingImports()
        {
            var code = @"
class C
{
    [|public D Foo { get; }
    public E Bar { get; }|]
}

namespace A
{
    public class D { }
}

namespace B
{
    public class D { }
    public class E { }
}
";

            var expected = @"
using B;

class C
{
    public D Foo { get; }
    public E Bar { get; }
}

namespace A
{
    public class D { }
}

namespace B
{
    public class D { }
    public class E { }
}
";

            await TestInRegularAndScriptAsync(code, expected);
        }

        [WpfFact]
        public async Task AddMissingImports_NoAction_NoPastedSpan()
        {
            var code = @"
class C
{
    public D[||] Foo { get; }
}

namespace A
{
    public class D { }
}
";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task AddMissingImports_NoAction_PasteIsNotMissingImports()
        {
            var code = @"
class [|C|]
{
    public D Foo { get; }
}

namespace A
{
    public class D { }
}
";

            await TestMissingInRegularAndScriptAsync(code);
        }

        [WpfFact]
        public async Task AddMissingImports_NoAction_PasteContainsAmibiguousMissingImport()
        {
            var code = @"
class C
{
    public [|D|] Foo { get; }
}

namespace A
{
    public class D { }
}

namespace B
{
    public class D { }
}
";

            await TestMissingInRegularAndScriptAsync(code);
        }
    }
}
