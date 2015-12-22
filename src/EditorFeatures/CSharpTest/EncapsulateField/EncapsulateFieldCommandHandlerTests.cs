// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EncapsulateField
{
    public class EncapsulateFieldCommandHandlerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulatePrivateField()
        {
            var text = @"
class C
{
    private int f$$ield;

    private void foo()
    {
        field = 3;
    }
}";
            var expected = @"
class C
{
    private int field;

    public int Field
    {
        get
        {
            return field;
        }

        set
        {
            field = value;
        }
    }

    private void foo()
    {
        Field = 3;
    }
}";

            using (var state = await EncapsulateFieldTestState.CreateAsync(text))
            {
                state.AssertEncapsulateAs(expected);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateNonPrivateField()
        {
            var text = @"
class C
{
    protected int fi$$eld;

    private void foo()
    {
        field = 3;
    }
}";
            var expected = @"
class C
{
    private int field;

    protected int Field
    {
        get
        {
            return field;
        }

        set
        {
            field = value;
        }
    }

    private void foo()
    {
        Field = 3;
    }
}";

            using (var state = await EncapsulateFieldTestState.CreateAsync(text))
            {
                state.AssertEncapsulateAs(expected);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DialogShownIfNotFieldsFound()
        {
            var text = @"
class$$ C
{
    private int field;

    private void foo()
    {
        field = 3;
    }
}";

            using (var state = await EncapsulateFieldTestState.CreateAsync(text))
            {
                state.AssertError();
            }
        }

        [WorkItem(1086632)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateTwoFields()
        {
            var text = @"
class Program
{
    [|static int A = 1;
    static int B = A;|]
 
    static void Main(string[] args)
    {
        System.Console.WriteLine(A);
        System.Console.WriteLine(B);
    }
}
";
            var expected = @"
class Program
{
    static int A = 1;
    static int B = A1;

    public static int A1
    {
        get
        {
            return A;
        }

        set
        {
            A = value;
        }
    }

    public static int B1
    {
        get
        {
            return B;
        }

        set
        {
            B = value;
        }
    }

    static void Main(string[] args)
    {
        System.Console.WriteLine(A1);
        System.Console.WriteLine(B1);
    }
}
";

            using (var state = await EncapsulateFieldTestState.CreateAsync(text))
            {
                state.AssertEncapsulateAs(expected);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public async Task EncapsulateFieldCommandDisabledInSubmission()
        {
            var exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(typeof(InteractiveDocumentSupportsFeatureService)));

            using (var workspace = await TestWorkspaceFactory.CreateWorkspaceAsync(XElement.Parse(@"
                <Workspace>
                    <Submission Language=""C#"" CommonReferences=""true"">  
                        class C
                        {
                            object $$foo;
                        }
                    </Submission>
                </Workspace> "),
                workspaceKind: WorkspaceKind.Interactive,
                exportProvider: exportProvider))
            {
                // Force initialization.
                workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

                var textView = workspace.Documents.Single().GetTextView();

                var handler = new EncapsulateFieldCommandHandler(workspace.GetService<Host.IWaitIndicator>(), workspace.GetService<ITextBufferUndoManagerProvider>());
                var delegatedToNext = false;
                Func<CommandState> nextHandler = () =>
                {
                    delegatedToNext = true;
                    return CommandState.Unavailable;
                };

                var state = handler.GetCommandState(new Commands.EncapsulateFieldCommandArgs(textView, textView.TextBuffer), nextHandler);
                Assert.True(delegatedToNext);
                Assert.False(state.IsAvailable);
            }
        }
    }
}
