// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToImplementedMethod()
        {
            var markup = @"
interface I
{
    void M(int x, string y);
}

class C : I
{
    $$public void M(int x, string y)
    { }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
interface I
{
    void M(string y, int x);
}

class C : I
{
    public void M(string y, int x)
    { }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToImplementingMethod()
        {
            var markup = @"
interface I
{
    $$void M(int x, string y);
}

class C : I
{
    public void M(int x, string y)
    { }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
interface I
{
    void M(string y, int x);
}

class C : I
{
    public void M(string y, int x)
    { }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToOverriddenMethod()
        {
            var markup = @"
class B
{
    public virtual void M(int x, string y)
    { }
}

class D : B
{
    $$public override void M(int x, string y)
    { }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class B
{
    public virtual void M(string y, int x)
    { }
}

class D : B
{
    public override void M(string y, int x)
    { }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToOverridingMethod()
        {
            var markup = @"
class B
{
    $$public virtual void M(int x, string y)
    { }
}

class D : B
{
    public override void M(int x, string y)
    { }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class B
{
    public virtual void M(string y, int x)
    { }
}

class D : B
{
    public override void M(string y, int x)
    { }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToOverriddenMethod_Transitive()
        {
            var markup = @"
class B
{
    public virtual void M(int x, string y)
    { }
}

class D : B
{
    public override void M(int x, string y)
    { }
}

class D2 : D
{
    $$public override void M(int x, string y)
    { }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class B
{
    public virtual void M(string y, int x)
    { }
}

class D : B
{
    public override void M(string y, int x)
    { }
}

class D2 : D
{
    public override void M(string y, int x)
    { }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToOverridingMethod_Transitive()
        {
            var markup = @"
class B
{
    $$public virtual void M(int x, string y)
    { }
}

class D : B
{
    public override void M(int x, string y)
    { }
}

class D2 : D
{
    public override void M(int x, string y)
    { }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class B
{
    public virtual void M(string y, int x)
    { }
}

class D : B
{
    public override void M(string y, int x)
    { }
}

class D2 : D
{
    public override void M(string y, int x)
    { }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToMethods_Complex()
        {
            ////     B   I   I2
            ////      \ / \ /
            ////       D  (I3)
            ////      / \   \
            ////   $$D2  D3  C

            var markup = @"
class B { public virtual void M(int x, string y) { } }
class D : B, I { public override void M(int x, string y) { } }
class D2 : D { public override void $$M(int x, string y) { } }
class D3 : D { public override void M(int x, string y) { } }
interface I { void M(int x, string y); }
interface I2 { void M(int x, string y); }
interface I3 : I, I2 { }
class C : I3 { public void M(int x, string y) { } }";

            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class B { public virtual void M(string y, int x) { } }
class D : B, I { public override void M(string y, int x) { } }
class D2 : D { public override void M(string y, int x) { } }
class D3 : D { public override void M(string y, int x) { } }
interface I { void M(string y, int x); }
interface I2 { void M(string y, int x); }
interface I3 : I, I2 { }
class C : I3 { public void M(string y, int x) { } }";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderParameters_Cascade_ToMethods_WithDifferentParameterNames()
        {
            var markup = @"
public class B
{
    /// <param name=""x""></param>
    /// <param name=""y""></param>
    public virtual int M(int x, string y)
    {
        return 1;
    }
}

public class D : B
{
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    public override int M(int a, string b)
    {
        return 1;
    }
}

public class D2 : D
{
    /// <param name=""y""></param>
    /// <param name=""x""></param>
    public override int $$M(int y, string x)
    {
        M(1, ""Two"");
        ((D)this).M(1, ""Two"");
        ((B)this).M(1, ""Two"");

        M(1, x: ""Two"");
        ((D)this).M(1, b: ""Two"");
        ((B)this).M(1, y: ""Two"");

        return 1;
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
public class B
{
    /// <param name=""y""></param>
    /// <param name=""x""></param>
    public virtual int M(string y, int x)
    {
        return 1;
    }
}

public class D : B
{
    /// <param name=""b""></param>
    /// <param name=""a""></param>
    public override int M(string b, int a)
    {
        return 1;
    }
}

public class D2 : D
{
    /// <param name=""x""></param>
    /// <param name=""y""></param>
    public override int M(string x, int y)
    {
        M(""Two"", 1);
        ((D)this).M(""Two"", 1);
        ((B)this).M(""Two"", 1);

        M(x: ""Two"", y: 1);
        ((D)this).M(b: ""Two"", a: 1);
        ((B)this).M(y: ""Two"", x: 1);

        return 1;
    }
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
