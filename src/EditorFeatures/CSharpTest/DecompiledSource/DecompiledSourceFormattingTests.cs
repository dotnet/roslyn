// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.DecompiledSource;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DecompiledSource
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.DecompiledSource)]
    public class DecompiledSourceFormattingTests
    {
        [Fact]
        public async Task TestIfFormatting1()
        {
            await TestAsync(
                """
                class C {
                  void M() {
                    if (true) {
                    }
                  }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestIfFormatting2()
        {
            await TestAsync(
                """
                class C {
                  void M() {
                    if (true) {
                    }
                    return;
                  }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }

                        return;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestIfFormatting3()
        {
            await TestAsync(
                """
                class C {
                  void M() {
                    if (true) {
                    } else {
                    return;
                }
                  }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestTryCatchFinally()
        {
            await TestAsync(
                """
                class C {
                  void M() {
                    try {
                    } catch {
                    } finally {
                    }
                  }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        try
                        {
                        }
                        catch
                        {
                        }
                        finally
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestDoWhile()
        {
            await TestAsync(
                """
                class C {
                  void M() {
                    do {
                    } while(true);
                  }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        do
                        {
                        } while (true);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNestedIf()
        {
            await TestAsync(
                """
                class C {
                  void M() {
                    if (true) {
                        if (true) {
                        }
                    }
                  }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                            if (true)
                            {
                            }
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestBraces()
        {
            await TestAsync(
                """
                class C {
                  void M() {
                    if (true) {
                    }
                    while (true) {
                    }
                    switch (true) {
                    }
                    try {
                    } finally {
                    }
                    using (null) {
                    }
                    foreach (var x in y) {
                    }
                  }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }

                        while (true)
                        {
                        }

                        switch (true)
                        {
                        }

                        try
                        {
                        }
                        finally
                        {
                        }

                        using (null)
                        {
                        }

                        foreach (var x in y)
                        {
                        }
                    }
                }
                """);
        }

        private static async Task TestAsync(string input, string expected)
        {
            using var workspace = TestWorkspace.CreateCSharp(input);
            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var formatted = await CSharpDecompiledSourceService.FormatDocumentAsync(document, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
            var test = await formatted.GetTextAsync();

            AssertEx.Equal(expected, test.ToString());
        }
    }
}
