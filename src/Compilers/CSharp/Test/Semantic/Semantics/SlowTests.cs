// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class SlowTests : CSharpTestBase
    {
        [Fact, WorkItem(35949, "https://github.com/dotnet/roslyn/issues/35949")]
        public void NotNull_Complexity()
        {
            var source = @"
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    C f = null!;

    void M(C c)
    {
        c.f = c;
        c.NotNull(
            x => x.f.NotNull(
                y => y.f.NotNull(
                    z => z.f.NotNull(
                        q => q.f.NotNull(
                            w => w.f.NotNull(
                                e => e.f.NotNull(
                                    r => r.f.NotNull(
                                        _ =>
                                        {
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);

                                            return """";
                                        }))))))));
    }
}

static class Ext
{
    public static V NotNull<T, V>([NotNull] this T t, Func<T, V> f) => throw null!;
}
";
            var comp = CreateCompilation(new[] { NotNullAttributeDefinition, source });
            comp.VerifyDiagnostics();
        }
    }
}
