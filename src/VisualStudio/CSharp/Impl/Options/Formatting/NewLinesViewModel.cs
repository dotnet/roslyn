// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    /// <summary>
    /// Interaction logic for FormattingNewLinesOptionControl.xaml
    /// </summary>
    internal class NewLinesViewModel : AbstractOptionPreviewViewModel
    {
        private static string s_previewText = @"//[
class C {
}
//]";

        private static string s_methodPreview = @"class c {
//[
    void Foo(){
    }
//]
}";

        private static string s_propertyPreview = @"class c {
//[
    public int Property {
        get {
            return 42;
        }
        set {
        }
    }
//]
}";

        private static readonly string s_tryCatchFinallyPreview = @"using System;
class C {
    void Foo() {
//[
        try {
        }
        catch (Exception e) {
        }
        finally {
        }
//]
    }
}";

        private static readonly string s_ifElsePreview = @"class C {
    void Foo() {
//[
        if (false) {
        }
        else {
        }
//]
    }
}";

        private static readonly string s_forBlockPreview = @"class C {
    void Foo() {
//[
        for (int i; i < 10; i++){
        }
//]
    }
}";

        private static readonly string s_lambdaPreview = @"using System;
class C {
    void Foo() {
//[
        Func<int, int> f = (x) => {
            return 2 * x;
        };
//]
    }
}";
        private static readonly string s_anonymousMethodPreview = @"using System;

delegate int D(int x);

class C {
    void Foo() {
//[
        D d = delegate(int x) {
            return 2 * x;
        };
    //]
    }
}";

        private static readonly string s_anonymousTypePreview = @"using System;
class C {
    void Foo() {
//[
        var z = new {
            A = 3, B = 4
        };
//]
    }
}";
        private static readonly string s_InitializerPreviewTrue = @"using System;
using System.Collections.Generic;

class C {
    void Foo() {
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
}";
        private static readonly string s_InitializerPreviewFalse = @"using System;
using System.Collections.Generic;

class C {
    void Foo() {
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
}";
        private static readonly string s_objectInitializerPreview = @"using System;
class C {
    void Foo() {
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
}";
        private static readonly string s_queryExpressionPreview = @"using System;
using System.Linq;
using System.Collections.Generic;
class C {
    void Foo(IEnumerable<int> e) {
//[
        var q = from a in e from b in e
                select a * b;
//]
}
class B {
    public int A { get; set; }
    public int B { get; set; }
}";

        public NewLinesViewModel(OptionSet options, IServiceProvider serviceProvider) : base(options, serviceProvider, LanguageNames.CSharp)
        {
            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.New_line_options_for_braces });
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInTypes, CSharpVSResources.Place_open_brace_on_new_line_for_types, s_previewText, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInMethods, CSharpVSResources.Place_open_brace_on_new_line_for_methods, s_methodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInProperties, CSharpVSResources.Place_open_brace_on_new_line_for_properties_indexers_and_events, s_propertyPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInAccessors, CSharpVSResources.Place_open_brace_on_new_line_for_property_indexer_and_event_accessors, s_propertyPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, CSharpVSResources.Place_open_brace_on_new_line_for_anonymous_methods, s_anonymousMethodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, CSharpVSResources.Place_open_brace_on_new_line_for_control_blocks, s_forBlockPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, CSharpVSResources.Place_open_brace_on_new_line_for_anonymous_types, s_anonymousTypePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, CSharpVSResources.Place_open_brace_on_new_line_for_object_collection_and_array_initializers, s_InitializerPreviewTrue, s_InitializerPreviewFalse, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, CSharpVSResources.Place_open_brace_on_new_line_for_lambda_expression, s_lambdaPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.New_line_options_for_keywords });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForElse, CSharpVSResources.Place_else_on_new_line, s_ifElsePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForCatch, CSharpVSResources.Place_catch_on_new_line, s_tryCatchFinallyPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForFinally, CSharpVSResources.Place_finally_on_new_line, s_tryCatchFinallyPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.New_line_options_for_expressions });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForMembersInObjectInit, CSharpVSResources.Place_members_in_object_initializers_on_new_line, s_objectInitializerPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, CSharpVSResources.Place_members_in_anonymous_types_on_new_line, s_anonymousTypePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForClausesInQuery, CSharpVSResources.Place_query_expression_clauses_on_new_line, s_queryExpressionPreview, this, options));
        }
    }
}
