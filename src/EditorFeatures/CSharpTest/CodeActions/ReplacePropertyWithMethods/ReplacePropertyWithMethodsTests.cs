using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ReplacePropertyWithMethods
{
    public class ReplacePropertyWithMethodsTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new ReplacePropertyWithMethodsCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestGetWithBody()
        {
            await TestAsync(
@"class C { int [||]Prop { get { return 0; } } }",
@"class C { private int GetProp() { return 0; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestPublicProperty()
        {
            await TestAsync(
@"class C { public int [||]Prop { get { return 0; } } }",
@"class C { public int GetProp() { return 0; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAnonyousType1()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get { return 0; } } 
    public void M() { var v = new { P = this.Prop } }
}",
@"class C {
    public int GetProp() { return 0; } 
    public void M() { var v = new { P = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAnonyousType2()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get { return 0; } } 
    public void M() { var v = new { this.Prop } }
}",
@"class C {
    public int GetProp() { return 0; } 
    public void M() { var v = new { Prop = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestPassedToRef1()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get { return 0; } }
    public void RefM(ref int i) { }
    public void M() { RefM(ref this.Prop); }
}",
@"class C {
    public int GetProp() { return 0; } 
    public void RefM(ref int i) { }
    public void M() { RefM(ref this.{|Conflict:GetProp|}()); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestPassedToOut1()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get { return 0; } }
    public void OutM(out int i) { }
    public void M() { OutM(out this.Prop); }
}",
@"class C {
    public int GetProp() { return 0; } 
    public void OutM(out int i) { }
    public void M() { OutM(out this.{|Conflict:GetProp|}()); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUsedInAttribute1()
        {
            await TestAsync(
@"
using System;

class CAttribute : Attribute {
    public int [||]Prop { get { return 0; } }
}

[C(Prop = 1)]
class D
{
}
",
@"
using System;

class CAttribute : Attribute {
    public int GetProp() { return 0; }
}

[C({|Conflict:Prop|} = 1)]
class D
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestSetWithBody1()
        {
            await TestAsync(
@"class C { int [||]Prop { set { var v = value; } } }",
@"class C { private void SetProp(int value) { var v = value; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestSetReference1()
        {
            await TestAsync(
@"class C {
    int [||]Prop { set { var v = value; } } 
    void M() { this.Prop = 1; }
}",
@"class C {
    private void SetProp(int value) { var v = value; }
    void M() { this.SetProp(1); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestGetterAndSetter()
        {
            await TestAsync(
@"class C {
    int [||]Prop { get { return 0; } set { var v = value; } } 
}",
@"class C {
    private int GetProp() { return 0; }
    private void SetProp(int value) { var v = value; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestIncrement1()
        {
            await TestAsync(
@"class C {
    int [||]Prop { get { return 0; } set { var v = value; } } 
    void M() { this.Prop++; }
}",
@"class C {
    private int GetProp() { return 0; }
    private void SetProp(int value) { var v = value; }
    void M() { this.SetProp(this.GetProp() + 1); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestDecrement2()
        {
            await TestAsync(
@"class C {
    int [||]Prop { get { return 0; } set { var v = value; } } 
    void M() { this.Prop--; }
}",
@"class C {
    private int GetProp() { return 0; }
    private void SetProp(int value) { var v = value; }
    void M() { this.SetProp(this.GetProp() - 1); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestRecursiveGet()
        {
            await TestAsync(
@"class C {
    int [||]Prop { get { return this.Prop + 1; } } 
}",
@"class C {
    private int GetProp() { return this.GetProp() + 1; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestRecursiveSet()
        {
            await TestAsync(
@"class C {
    int [||]Prop { set { this.Prop = value + 1; } } 
}",
@"class C {
    private void SetProp(int value) { this.SetProp(value + 1); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCompoundAssign1()
        {
            await TestAsync(
@"class C {
    int [||]Prop { get { return 0; } set { var v = value; } } 
    void M() { this.Prop *= x; }
}",
@"class C {
    private int GetProp() { return 0; }
    private void SetProp(int value) { var v = value; }
    void M() { this.SetProp(this.GetProp() * x); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCompoundAssign2()
        {
            await TestAsync(
@"class C {
    int [||]Prop { get { return 0; } set { var v = value; } } 
    void M() { this.Prop *= x + y; }
}",
@"class C {
    private int GetProp() { return 0; }
    private void SetProp(int value) { var v = value; }
    void M() { this.SetProp(this.GetProp() * (x + y)); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestMissingAccessors()
        {
            await TestAsync(
@"class C {
    int [||]Prop { }
    void M() { var v = this.Prop; }
}",
@"class C {
    void M() { var v = this.GetProp(); }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestComputedProp()
        {
            await TestAsync(
@"class C {
    int [||]Prop => 1;
}",
@"class C {
    private int GetProp() { return 1; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAbstractProperty()
        {
            await TestAsync(
@"class C {
    public abstract int [||]Prop { get; } 
    public void M() { var v = new { P = this.Prop } }
}",
@"class C {
    public abstract int GetProp();
    public void M() { var v = new { P = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestVirtualProperty()
        {
            await TestAsync(
@"class C {
    public virtual int [||]Prop { get { return 1; } } 
    public void M() { var v = new { P = this.Prop } }
}",
@"class C {
    public virtual int GetProp() { return 1; }
    public void M() { var v = new { P = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestInterfaceProperty()
        {
            await TestAsync(
@"interface I {
    int [||]Prop { get; }
}",
@"interface I {
    int GetProp();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty1()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get; }
}",
@"class C {
    private readonly int _prop;
    public int GetProp() { return _prop; }
}");
        }
    }
}
