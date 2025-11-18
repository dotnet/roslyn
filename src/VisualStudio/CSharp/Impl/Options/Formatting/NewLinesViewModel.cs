// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;

/// <summary>
/// Interaction logic for FormattingNewLinesOptionControl.xaml
/// </summary>
internal sealed class NewLinesViewModel : AbstractOptionPreviewViewModel
{
    private static readonly Conversions<NewLineBeforeOpenBracePlacement, int> s_newLinesForBracesConversions = new(v => (int)v, v => (NewLineBeforeOpenBracePlacement)v);

    private const string s_previewText = """
        //[
        class C {
        }
        //]
        """;

    private const string s_methodPreview = """
        class c {
        //[
            void Goo(){
                Console.WriteLine();

                int LocalFunction(int x) {
                    return 2 * x;
                }

                Console.ReadLine();
            }
        //]
        }
        """;

    private const string s_propertyPreview = """
        class c {
        //[
            public int Property {
                get {
                    return 42;
                }
                set {
                }
            }
        //]
        }
        """;

    private const string s_tryCatchFinallyPreview = """
        using System;
        class C {
            void Goo() {
        //[
                try {
                }
                catch (Exception e) {
                }
                finally {
                }
        //]
            }
        }
        """;

    private const string s_ifElsePreview = """
        class C {
            void Goo() {
        //[
                if (false) {
                }
                else {
                }
        //]
            }
        }
        """;

    private const string s_forBlockPreview = """
        class C {
            void Goo() {
        //[
                for (int i; i < 10; i++){
                }
        //]
            }
        }
        """;

    private const string s_lambdaPreview = """
        using System;
        class C {
            void Goo() {
        //[
                Func<int, int> f = x => {
                    return 2 * x;
                };
        //]
            }
        }
        """;
    private const string s_anonymousMethodPreview = """
        using System;

        delegate int D(int x);

        class C {
            void Goo() {
        //[
                D d = delegate(int x) {
                    return 2 * x;
                };
            //]
            }
        }
        """;

    private const string s_anonymousTypePreview = """
        using System;
        class C {
            void Goo() {
        //[
                var z = new {
                    A = 3, B = 4
                };
        //]
            }
        }
        """;
    private const string s_InitializerPreviewTrue = """
        using System;
        using System.Collections.Generic;

        class C {
            void Goo() {
        //[
                var z = new B()
                {
                    A = 3, B = 4
                };

                // During Brace Completion or Only if Empty Body 
                var collectionVariable = new List<int> 
                {
                }

                // During Brace Completion
                var arrayVariable = new int[] 
                {
                }
        //]
            }
        }

        class B {
            public int A { get; set; }
            public int B { get; set; }
        }
        """;
    private const string s_InitializerPreviewFalse = """
        using System;
        using System.Collections.Generic;

        class C {
            void Goo() {
        //[
                var z = new B() {
                    A = 3, B = 4
                };

                // During Brace Completion or Only if Empty Body 
                var collectionVariable = new List<int> {
                }

                // During Brace Completion
                var arrayVariable = new int[] {
                }
        //]
            }
        }

        class B {
            public int A { get; set; }
            public int B { get; set; }
        }
        """;
    private const string s_objectInitializerPreview = """
        using System;
        class C {
            void Goo() {
        //[
                var z = new B() {
                    A = 3, B = 4
                };
        //]
            }
        }

        class B {
            public int A { get; set; }
            public int B { get; set; }
        }
        """;
    private const string s_queryExpressionPreview = """
        using System;
        using System.Linq;
        using System.Collections.Generic;
        class C {
            void Goo(IEnumerable<int> e) {
        //[
                var q = from a in e from b in e
                        select a * b;
        //]
        }
        class B {
            public int A { get; set; }
            public int B { get; set; }
        }
        """;

    public NewLinesViewModel(OptionStore optionStore, IServiceProvider serviceProvider) : base(optionStore, serviceProvider, LanguageNames.CSharp)
    {
        Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.New_line_options_for_braces });

        var newLineBeforeOpenBraceValue = new StrongBox<NewLineBeforeOpenBracePlacement>();
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Types, CSharpVSResources.Place_open_brace_on_new_line_for_types, s_previewText, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Methods, CSharpVSResources.Place_open_brace_on_new_line_for_methods_local_functions, s_methodPreview, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Properties, CSharpVSResources.Place_open_brace_on_new_line_for_properties_indexers_and_events, s_propertyPreview, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.Accessors, CSharpVSResources.Place_open_brace_on_new_line_for_property_indexer_and_event_accessors, s_propertyPreview, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.AnonymousMethods, CSharpVSResources.Place_open_brace_on_new_line_for_anonymous_methods, s_anonymousMethodPreview, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.ControlBlocks, CSharpVSResources.Place_open_brace_on_new_line_for_control_blocks, s_forBlockPreview, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.AnonymousTypes, CSharpVSResources.Place_open_brace_on_new_line_for_anonymous_types, s_anonymousTypePreview, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, CSharpVSResources.Place_open_brace_on_new_line_for_object_collection_array_and_with_initializers, s_InitializerPreviewTrue, s_InitializerPreviewFalse, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));
        Items.Add(new CheckBoxEnumFlagsOptionViewModel<NewLineBeforeOpenBracePlacement>(CSharpFormattingOptions2.NewLineBeforeOpenBrace, (int)NewLineBeforeOpenBracePlacement.LambdaExpressionBody, CSharpVSResources.Place_open_brace_on_new_line_for_lambda_expression, s_lambdaPreview, this, optionStore, newLineBeforeOpenBraceValue, s_newLinesForBracesConversions));

        Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.New_line_options_for_keywords });

        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.NewLineForElse, CSharpVSResources.Place_else_on_new_line, s_ifElsePreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.NewLineForCatch, CSharpVSResources.Place_catch_on_new_line, s_tryCatchFinallyPreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.NewLineForFinally, CSharpVSResources.Place_finally_on_new_line, s_tryCatchFinallyPreview, this, optionStore));

        Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.New_line_options_for_expressions });

        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.NewLineForMembersInObjectInit, CSharpVSResources.Place_members_in_object_initializers_on_new_line, s_objectInitializerPreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes, CSharpVSResources.Place_members_in_anonymous_types_on_new_line, s_anonymousTypePreview, this, optionStore));
        Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions2.NewLineForClausesInQuery, CSharpVSResources.Place_query_expression_clauses_on_new_line, s_queryExpressionPreview, this, optionStore));
    }
}
