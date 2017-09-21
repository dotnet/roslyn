// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.LinkedFileDiffMerging
{
    public partial class LinkedFileDiffMergingTests
    {
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18432")]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestChangeSignature()
        {
            TestLinkedFileSet(
                @"public class Class1
{
    void M(int x, string y, int z)
    {
    }
#if LIB1
    void N()
    {
        M(2, ""A"", 1);
    }
#elif LIB2
    void N()
    {
        M(4, ""B"", 3);
    }
#endif
}",
                new List<string>
                {
                    @"public class Class1
{
    void M(int z, int x)
    {
    }
#if LIB1
    void N()
    {
        M(1, 2);
    }
#elif LIB2
    void N()
    {
        M(4, ""B"", 3);
    }
#endif
}",
                    @"public class Class1
{
    void M(int z, int x)
    {
    }
#if LIB1
    void N()
    {
        M(2, ""A"", 1);
    }
#elif LIB2
    void N()
    {
        M(3, 4);
    }
#endif
}"
                },
                @"public class Class1
{
    void M(int z, int x)
    {
    }
#if LIB1
    void N()
    {
        M(1, 2);
    }
#elif LIB2
    void N()
    {
        M(3, 4);
    }
#endif
}",
                LanguageNames.CSharp);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
        public void TestRename()
        {
            TestLinkedFileSet(
                @"public class Class1
{
    void M()
    {
    }
#if LIB1
    void N()
    {
        M();
    }
#elif LIB2
    void N()
    {
        M();
    }
#endif
}",
                new List<string>
                {
                    @"public class Class1
{
    void Method()
    {
    }
#if LIB1
    void N()
    {
        Method();
    }
#elif LIB2
    void N()
    {
        M();
    }
#endif
}",
                    @"public class Class1
{
    void Method()
    {
    }
#if LIB1
    void N()
    {
        M();
    }
#elif LIB2
    void N()
    {
        Method();
    }
#endif
}"
                },
                @"public class Class1
{
    void Method()
    {
    }
#if LIB1
    void N()
    {
        Method();
    }
#elif LIB2
    void N()
    {
        Method();
    }
#endif
}",
                LanguageNames.CSharp);
        }
    }
}
