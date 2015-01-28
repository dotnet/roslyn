// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EncapsulateField
{
    public class EncapsulateFieldCommandHandlerTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulatePrivateField()
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

            using (var state = new EncapsulateFieldTestState(text))
            {
                state.AssertEncapsulateAs(expected);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateNonPrivateField()
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

            using (var state = new EncapsulateFieldTestState(text))
            {
                state.AssertEncapsulateAs(expected);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void DialogShownIfNotFieldsFound()
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

            using (var state = new EncapsulateFieldTestState(text))
            {
                state.AssertError();
            }
        }

        [WorkItem(1086632)]
        [Fact, Trait(Traits.Feature, Traits.Features.EncapsulateField)]
        public void EncapsulateTwoFields()
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

            using (var state = new EncapsulateFieldTestState(text))
            {
                state.AssertEncapsulateAs(expected);
            }
        }
    }
}
