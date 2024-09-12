// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EncapsulateField
{
    [UseExportProvider]
    public class EncapsulateFieldCommandHandlerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulatePrivateField()
        {
            var text = """
                class C
                {
                    private int f$$ield;

                    private void goo()
                    {
                        field = 3;
                    }
                }
                """;
            var expected = """
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

                    private void goo()
                    {
                        Field = 3;
                    }
                }
                """;

            using var state = EncapsulateFieldTestState.Create(text);
            await state.AssertEncapsulateAsAsync(expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateNonPrivateField()
        {
            var text = """
                class C
                {
                    protected int fi$$eld;

                    private void goo()
                    {
                        field = 3;
                    }
                }
                """;
            var expected = """
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

                    private void goo()
                    {
                        Field = 3;
                    }
                }
                """;

            using var state = EncapsulateFieldTestState.Create(text);
            await state.AssertEncapsulateAsAsync(expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task DialogShownIfNotFieldsFound()
        {
            var text = """
                class$$ C
                {
                    private int field;

                    private void goo()
                    {
                        field = 3;
                    }
                }
                """;

            using var state = EncapsulateFieldTestState.Create(text);
            await state.AssertErrorAsync();
        }

        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1086632")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public async Task EncapsulateTwoFields()
        {
            var text = """
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
                """;
            var expected = """
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
                """;

            using var state = EncapsulateFieldTestState.Create(text);
            await state.AssertEncapsulateAsAsync(expected);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void EncapsulateFieldCommandDisabledInSubmission()
        {
            using var workspace = EditorTestWorkspace.Create(XElement.Parse("""
                <Workspace>
                    <Submission Language="C#" CommonReferences="true">  
                        class C
                        {
                            object $$goo;
                        }
                    </Submission>
                </Workspace>
                """),
                workspaceKind: WorkspaceKind.Interactive,
                composition: EditorTestCompositions.EditorFeaturesWpf);
            // Force initialization.
            workspace.GetOpenDocumentIds().Select(id => workspace.GetTestDocument(id).GetTextView()).ToList();

            var textView = workspace.Documents.Single().GetTextView();

            var handler = workspace.ExportProvider.GetCommandHandler<EncapsulateFieldCommandHandler>(PredefinedCommandHandlerNames.EncapsulateField, ContentTypeNames.CSharpContentType);

            var state = handler.GetCommandState(new EncapsulateFieldCommandArgs(textView, textView.TextBuffer));
            Assert.True(state.IsUnspecified);
        }
    }
}
