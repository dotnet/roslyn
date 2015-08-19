// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Organizing
{
    public class OrganizeModifiersTests : AbstractOrganizerTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public void TestTypes1()
        {
            var initial =
@"static public class C {
}";
            var final =
@"public static class C {
}";

            Check(initial, final);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public void TestTypes2()
        {
            var initial =
@"public static class D {
}";
            var final =
@"public static class D {
}";

            Check(initial, final);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public void TestTypes3()
        {
            var initial =
@"public static partial class E {
}";
            var final =
@"public static partial class E {
}";

            Check(initial, final);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public void TestTypes4()
        {
            var initial =
@"static public partial class F {
}";
            var final =
@"public static partial class F {
}";

            Check(initial, final);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Organizing)]
        public void TestTypes5()
        {
            var initial =
@"unsafe public static class F {
}";
            var final =
@"public static unsafe class F {
}";

            Check(initial, final);
        }
    }
}
