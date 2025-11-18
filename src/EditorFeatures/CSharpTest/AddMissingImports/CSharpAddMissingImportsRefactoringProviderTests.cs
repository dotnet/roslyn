// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddMissingImports;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.AddMissingImports;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.AddMissingImports)]
public sealed class CSharpAddMissingImportsRefactoringProviderTests : AbstractCSharpCodeActionTest
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(EditorTestWorkspace workspace, TestParameters parameters)
        => new CSharpAddMissingImportsRefactoringProvider();

    private static readonly CodeStyleOption2<AddImportPlacement> InsideNamespaceOption =
        new(AddImportPlacement.InsideNamespace, NotificationOption2.Error);

    private static readonly CodeStyleOption2<AddImportPlacement> OutsideNamespaceOption =
        new(AddImportPlacement.InsideNamespace, NotificationOption2.Error);

    protected override void InitializeWorkspace(EditorTestWorkspace workspace, TestParameters parameters)
    {
        // Treat the span being tested as the pasted span
        var hostDocument = workspace.Documents.First();
        var pastedTextSpan = hostDocument.SelectedSpans.FirstOrDefault();

        if (!pastedTextSpan.IsEmpty)
        {
            var pasteTrackingService = workspace.ExportProvider.GetExportedValue<PasteTrackingService>();

            // This tests the paste tracking service's resiliancy to failing when multiple pasted spans are
            // registered consecutively and that the last registered span wins.
            pasteTrackingService.RegisterPastedTextSpan(hostDocument.GetTextBuffer(), default);
            pasteTrackingService.RegisterPastedTextSpan(hostDocument.GetTextBuffer(), pastedTextSpan);
        }
    }

    private Task TestInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expectedMarkup,
        bool placeSystemNamespaceFirst,
        bool separateImportDirectiveGroups,
        bool placeInsideNamespace = false)
    {
        var options =
            new OptionsCollection(GetLanguage())
            {
                { GenerationOptions.PlaceSystemNamespaceFirst, placeSystemNamespaceFirst },
                { GenerationOptions.SeparateImportDirectiveGroups, separateImportDirectiveGroups },
                { CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, placeInsideNamespace ? InsideNamespaceOption : OutsideNamespaceOption },
            };
        return TestInRegularAndScriptAsync(initialMarkup, expectedMarkup, new(options: options));
    }

    [WpfFact]
    public Task AddMissingImports_AddImport_PasteContainsSingleMissingImport()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public [|D|] Foo { get; }
            }

            namespace A
            {
                public class D { }
            }
            """, """
            using A;

            class C
            {
                public D Foo { get; }
            }

            namespace A
            {
                public class D { }
            }
            """);

    [WpfFact]
    public Task AddMissingImports_AddImportsBelowSystem_PlaceSystemFirstPasteContainsMultipleMissingImports()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, placeSystemNamespaceFirst: true, separateImportDirectiveGroups: false);

    [WpfFact]
    public Task AddMissingImports_AddImportsAboveSystem_DoNotPlaceSystemFirstPasteContainsMultipleMissingImports()
        => TestInRegularAndScriptAsync("""
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
            """, """
            using A;
            using B;
            using System;

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
            """, placeSystemNamespaceFirst: false, separateImportDirectiveGroups: false);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/42221")]
    public Task AddMissingImports_AddImportsUngrouped_SeparateImportGroupsPasteContainsMultipleMissingImports()
        => TestInRegularAndScriptAsync("""
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
            """, """
            using A;

            using B;

            using System;

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
            """, placeSystemNamespaceFirst: false, separateImportDirectiveGroups: true);

    [WpfFact]
    public Task AddMissingImports_PartialFix_PasteContainsFixableAndAmbiguousMissingImports()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [WpfFact]
    public Task AddMissingImports_NoAction_NoPastedSpan()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                public D[||] Foo { get; }
            }

            namespace A
            {
                public class D { }
            }
            """);

    [WpfFact]
    public Task AddMissingImports_NoAction_PasteIsNotMissingImports()
        => TestMissingInRegularAndScriptAsync("""
            class [|C|]
            {
                public D Foo { get; }
            }

            namespace A
            {
                public class D { }
            }
            """);

    [WpfFact]
    public Task AddMissingImports_NoAction_PasteContainsAmibiguousMissingImport()
        => TestMissingInRegularAndScriptAsync("""
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
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31768")]
    public Task AddMissingImports_AddMultipleImports_NoPreviousImports()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """, placeSystemNamespaceFirst: false, separateImportDirectiveGroups: false);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/51844")]
    public Task TestOrdering1()
        => TestInRegularAndScriptAsync("""
            class C
            {
                [|List<Type> list;|]
            }
            """, """
            using System;
            using System.Collections.Generic;

            class C
            {
                List<Type> list;
            }
            """, placeSystemNamespaceFirst: true, separateImportDirectiveGroups: true);

    [WpfTheory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/54842")]
    public Task TestWithNamespace(bool placeInsideNamespace)
        => TestInRegularAndScriptAsync("""
            namespace N
            {
                using System;

                class C
                {
                    [|List<Type> list;|]
                }
            }
            """, """
            namespace N
            {
                using System;
                using System.Collections.Generic;

                class C
                {
                    List<Type> list;
                }
            }
            """, placeSystemNamespaceFirst: true, separateImportDirectiveGroups: true, placeInsideNamespace);
}
