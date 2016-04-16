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
        internal override bool ShouldPersistOption(OptionKey key)
        {
            return key.Option.Feature == CSharpFormattingOptions.NewLineFormattingFeatureName;
        }

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
            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.NewLineBraces });
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInTypes, CSharpVSResources.NewLinesBracesType, s_previewText, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInMethods, CSharpVSResources.NewLinesForBracesMethod, s_methodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInProperties, CSharpVSResources.NewLinesForBracesProperty, s_propertyPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInAccessors, CSharpVSResources.NewLinesForBracesAccessors, s_propertyPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, CSharpVSResources.NewLinesForBracesInAnonymousMethods, s_anonymousMethodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInControlBlocks, CSharpVSResources.NewLinesForBracesInControlBlocks, s_forBlockPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, CSharpVSResources.NewLinesForBracesInAnonymousTypes, s_anonymousTypePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, CSharpVSResources.NewLinesForBracesInObjectCollectionArrayInitializers, s_InitializerPreviewTrue, s_InitializerPreviewFalse, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, CSharpVSResources.NewLinesForBracesInLambdaExpressionBody, s_lambdaPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.NewLineKeywords });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForElse, CSharpVSResources.ElseOnNewLine, s_ifElsePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForCatch, CSharpVSResources.CatchOnNewLine, s_tryCatchFinallyPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForFinally, CSharpVSResources.FinallyOnNewLine, s_tryCatchFinallyPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.NewLineExpressions });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForMembersInObjectInit, CSharpVSResources.NewLineForMembersInObjectInit, s_objectInitializerPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, CSharpVSResources.NewLineForMembersInAnonymousTypes, s_anonymousTypePreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.NewLineForClausesInQuery, CSharpVSResources.NewLineForClausesInQuery, s_queryExpressionPreview, this, options));
        }
    }
}
