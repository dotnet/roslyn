// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.AddObsoleteAttribute.CSharpAddObsoleteAttributeCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddObsoleteAttribute
{
    public class AddObsoleteAttributeTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassNoMessage()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete]
class Base {}

class Derived : {|CS0612:Base|} {
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
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete(""message"")]
class Base {}

class Derived : {|CS0618:Base|} {
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
        public async Task TestObsoleteClassWithMessageAndErrorFalse()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete(""message"", error: false)]
class Base {}

class Derived : {|CS0618:Base|} {
}
",
@"
[System.Obsolete(""message"", error: false)]
class Base {}

[System.Obsolete]
class Derived : Base {
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassWithMessageAndErrorTrue()
        {
            var code = @"
[System.Obsolete(""message"", error: true)]
class Base {}

class Derived : {|CS0619:Base|} {
}
";
            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteClassUsedInField()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    int i = {|CS0612:Base|}.i;
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
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = {|CS0612:Base|}.i;
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
            await VerifyCS.VerifyCodeFixAsync(
@"
class Base { 
    [System.Obsolete]
    protected virtual void ObMethod() { }
}

class Derived : Base {
    protected override void {|CS0672:ObMethod|}() { }
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
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = {|CS0612:Base|}.i;
        int j = {|CS0612:Base|}.i;
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
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = {|CS0612:Base|}.i;
        int j = {|CS0612:Base|}.i;
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
            await VerifyCS.VerifyCodeFixAsync(
@"
[System.Obsolete]
class Base { public static int i; }

class Derived {
    void Goo() {
        int i = {|CS0612:Base|}.i;
    }

    void Bar() {
        int j = {|CS0612:Base|}.i;
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteCollectionAddMethod()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class Collection : System.Collections.Generic.IEnumerable<int> {
    [System.Obsolete]
    public void Add(int i) { }

    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => throw null;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
}

class Derived {
    void Goo() {
        var c = new Collection {
            {|CS1064:1|}, {|CS1064:2|}, {|CS1064:3|}
        };
    }
}
",
@"
class Collection : System.Collections.Generic.IEnumerable<int> {
    [System.Obsolete]
    public void Add(int i) { }

    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => throw null;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
}

class Derived {
    [System.Obsolete]
    void Goo() {
        var c = new Collection {
            1, 2, 3
        };
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteCollectionAddMethodWithMessage()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class Collection : System.Collections.Generic.IEnumerable<int> {
    [System.Obsolete(""message"")]
    public void Add(int i) { }

    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => throw null;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
}

class Derived {
    void Goo() {
        var c = new Collection {
            {|CS1062:1|}, {|CS1062:2|}, {|CS1062:3|}
        };
    }
}
",
@"
class Collection : System.Collections.Generic.IEnumerable<int> {
    [System.Obsolete(""message"")]
    public void Add(int i) { }

    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => throw null;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
}

class Derived {
    [System.Obsolete]
    void Goo() {
        var c = new Collection {
            1, 2, 3
        };
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteCollectionAddMethodWithMessageAndErrorFalse()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"
class Collection : System.Collections.Generic.IEnumerable<int> {
    [System.Obsolete(""message"", error: false)]
    public void Add(int i) { }

    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => throw null;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
}

class Derived {
    void Goo() {
        var c = new Collection {
            {|CS1062:1|}, {|CS1062:2|}, {|CS1062:3|}
        };
    }
}
",
@"
class Collection : System.Collections.Generic.IEnumerable<int> {
    [System.Obsolete(""message"", error: false)]
    public void Add(int i) { }

    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => throw null;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
}

class Derived {
    [System.Obsolete]
    void Goo() {
        var c = new Collection {
            1, 2, 3
        };
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddObsoleteAttribute)]
        public async Task TestObsoleteCollectionAddMethodWithMessageAndErrorTrue()
        {
            var code = @"
class Collection : System.Collections.Generic.IEnumerable<int> {
    [System.Obsolete(""message"", error: true)]
    public void Add(int i) { }

    public System.Collections.Generic.IEnumerator<int> GetEnumerator() => throw null;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
}

class Derived {
    void Goo() {
        var c = new Collection {
            {|CS1063:1|}, {|CS1063:2|}, {|CS1063:3|}
        };
    }
}
";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }
    }
}
