// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class FunctionPointerUnmanagedCallingConventionCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType() => typeof(FunctionPointerUnmanagedCallingConventionCompletionProvider);

        [Fact]
        public async Task TypeFound()
        {
            var markup = """
                namespace System.Runtime.CompilerServices
                {
                    public class CallConvUnitTest { }
                }

                class C
                {
                    delegate* unmanaged[$$] <int, string> f;
                }
                """;
            await VerifyItemExistsAsync(markup, "UnitTest");
        }

        [Fact]
        public async Task TypeFoundSecondCallingConvention()
        {
            var markup = """
                namespace System.Runtime.CompilerServices
                {
                    public class CallConvUnitTest { }
                }

                class C
                {
                    delegate* unmanaged[Thiscall, $$] <int, string> f;
                }
                """;
            await VerifyItemExistsAsync(markup, "UnitTest");
        }

        [Theory]
        [InlineData("Cdecl")]
        [InlineData("Fastcall")]
        [InlineData("Thiscall")]
        [InlineData("Stdcall")]
        public async Task PredefinedCallingConventionFound(string callingConvention)
        {
            // We explicitly create a project with no references (not even common references) to ensure we
            // get the defaults
            var markup = """
                <Workspace>
                    <Project Language="C#">
                        <Document>
                    class C
                    {
                        delegate* unmanaged[$$] &lt;int, string&gt; f;
                    }
                        </Document>
                    </Project>
                </Workspace>
                """;
            await VerifyItemExistsAsync(markup, callingConvention, glyph: (int)Glyph.Keyword);
        }
    }
}
