// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.LinkedFileDiffMerging;

[Trait(Traits.Feature, Traits.Features.LinkedFileDiffMerging)]
public sealed partial class LinkedFileDiffMergingTests
{
    [Fact]
    public void TestChangeSignature()
        => TestLinkedFileSet(
            """
            public class Class1
            {
                void M(int x, string y, int z)
                {
                }
            #if LIB1
                void N()
                {
                    M(2, "A", 1);
                }
            #elif LIB2
                void N()
                {
                    M(4, "B", 3);
                }
            #endif
            }
            """,
            [
                """
                public class Class1
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
                        M(4, "B", 3);
                    }
                #endif
                }
                """,
                """
                public class Class1
                {
                    void M(int z, int x)
                    {
                    }
                #if LIB1
                    void N()
                    {
                        M(2, "A", 1);
                    }
                #elif LIB2
                    void N()
                    {
                        M(3, 4);
                    }
                #endif
                }
                """
            ],
            """
            public class Class1
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
            }
            """,
            LanguageNames.CSharp);

    [Fact]
    public void TestRename()
        => TestLinkedFileSet(
            """
            public class Class1
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
            }
            """,
            [
                """
                public class Class1
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
                }
                """,
                """
                public class Class1
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
                }
                """
            ],
            """
            public class Class1
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
            }
            """,
            LanguageNames.CSharp);
}
