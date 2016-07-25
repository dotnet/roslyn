using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ReplacePropertyWithMethods
{
    public class ReplacePropertyWithMethodsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
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
        public async Task TestGetterAndSetterAccessibilityChange()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get { return 0; } private set { var v = value; } } 
}",
@"class C {
    public int GetProp() { return 0; }
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
        public async Task TestComputedPropWithTrailingTrivia()
        {
            await TestAsync(
@"class C
{
    int [||]Prop => 1; // Comment
}",
@"class C
{
    private int GetProp()
    {
        return 1; // Comment
    }
}", compareTokens: false);
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
    private readonly int prop;
    public int GetProp() { return prop; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty2()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get; }
    public C() {
        this.Prop++;
    }
}",
@"class C {
    private readonly int prop;
    public int GetProp() { return prop; }
    public C() {
        this.prop = this.GetProp() + 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty3()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get; }
    public C() {
        this.Prop *= x + y;
    }
}",
@"class C {
    private readonly int prop;
    public int GetProp() { return prop; }
    public C() {
        this.prop = this.GetProp() * (x + y);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty4()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get; } = 1;
}",
@"class C {
    private readonly int prop = 1;
    public int GetProp() { return prop; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty5()
        {
            await TestAsync(
@"class C {
    private int prop;
    public int [||]Prop { get; } = 1;
}",
@"class C {
    private int prop;
    private readonly int prop1 = 1;
    public int GetProp() { return prop1; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty6()
        {
            await TestAsync(
@"class C {
    public int [||]PascalCase { get; }
}",
@"class C {
    private readonly int pascalCase;
    public int GetPascalCase() { return pascalCase; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUniqueName1()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { get { return 0; } }
    public abstract int GetProp();
}",
@"class C {
    public int GetProp1() { return 0; }
    public abstract int GetProp();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUniqueName2()
        {
            await TestAsync(
@"class C {
    public int [||]Prop { set { } }
    public abstract void SetProp(int i);
}",
@"class C {
    public void SetProp1(int value) { }
    public abstract void SetProp(int i);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUniqueName3()
        {
            await TestAsync(
@"class C {
    public object [||]Prop { set { } }
    public abstract void SetProp(dynamic i);
}",
@"class C {
    public void SetProp1(object value) { }
    public abstract void SetProp(dynamic i);
}");
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestTrivia1()
        {
            await TestAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {

        Prop++;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {

        SetProp(GetProp() + 1);
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestTrivia2()
        {
            await TestAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        /* Leading */
        Prop++; /* Trailing */
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        /* Leading */
        SetProp(GetProp() + 1); /* Trailing */
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestTrivia3()
        {
            await TestAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        /* Leading */
        Prop += 1 /* Trailing */ ;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        /* Leading */
        SetProp(GetProp() + 1 /* Trailing */ );
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task ReplaceReadInsideWrite1()
        {
            await TestAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        Prop = Prop + 1;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        SetProp(GetProp() + 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task ReplaceReadInsideWrite2()
        {
            await TestAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        Prop *= Prop + 1;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        SetProp(GetProp() * (GetProp() + 1));
    }
}");
        }
    }
}
