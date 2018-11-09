// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddObsoleteAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddObsoleteAttribute
{
    public class AddObsoleteAttributeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddObsoleteAttributeCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassNoMessage()
        {
            await TestInRegularAndScript1Async(
@"
[System.Obsolete]
class Base {}

class Derived : [||]Base {
}
",
@"
[System.Obsolete]
class Base {}

[System.Obsolete]
class Derived : Base {
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassWithMessage()
        {
            await TestInRegularAndScript1Async(
@"
[System.Obsolete(""message"")]
class Base {}

class Derived : [||]Base {
}
",
@"
[System.Obsolete(""message"")]
class Base {}

[System.Obsolete]
class Derived : Base {
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassUsedInField()
        {
            await TestInRegularAndScript1Async(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    int i = [||]Base.i;
}
",
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    [System.Obsolete]
    int i = Base.i;
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassUsedInMethod()
        {
            await TestInRegularAndScript1Async(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = [||]Base.i;
    }
}
",
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    [System.Obsolete]
    void Goo() {
        int i = Base.i;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteOverride()
        {
            await TestInRegularAndScript1Async(
@"
class Base { 
    [System.Obsolete]
    protected virtual void ObMethod() { }
}

class Derived : Base {
    protected override void [||]ObMethod() { }
}
",
@"
class Base { 
    [System.Obsolete]
    protected virtual void ObMethod() { }
}

class Derived : Base {
    [System.Obsolete]
    protected override void ObMethod() { }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassFixAll1()
        {
            await TestInRegularAndScript1Async(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = {|FixAllInDocument:|}Base.i;
        int j = Base.i;
    }
}
",
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    [System.Obsolete]
    void Goo() {
        int i = Base.i;
        int j = Base.i;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassFixAll2()
        {
            await TestInRegularAndScript1Async(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = Base.i;
        int j = {|FixAllInDocument:|}Base.i;
    }
}
",
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    [System.Obsolete]
    void Goo() {
        int i = Base.i;
        int j = Base.i;
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassFixAll3()
        {
            await TestInRegularAndScript1Async(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = {|FixAllInDocument:|}Base.i;
    }

    void Bar() {
        int j = Base.i;
    }
}
",
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    [System.Obsolete]
    void Goo() {
        int i = Base.i;
    }

    [System.Obsolete]
    void Bar() {
        int j = Base.i;
    }
}
");
        }
    }
}
