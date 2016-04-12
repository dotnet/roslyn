// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.AddParameterXmlDocumentation;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddParameterXmlDocumentation
{
    public class AddParameterXmlDocumentationTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(null, new AddParameterXmlDocumentationCodeFixProvider());
        }

        // TODO: I don't think I completely understand how I'm supposed to use this hierarchy of base classes for unit tests
        private ParseOptions _regularParseOptions = Options.Regular.WithDocumentationMode(DocumentationMode.Diagnose);
        private ParseOptions _scriptParseOptions = Options.Script.WithDocumentationMode(DocumentationMode.Diagnose);
        private async Task MyTestAsync(string initialMarkup, string expectedMarkup)
        {
            await TestAsync(initialMarkup, expectedMarkup, _regularParseOptions);
            await TestAsync(initialMarkup, expectedMarkup, _scriptParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public Task TestClassesBefore()
        {
            return MyTestAsync(@"
/// <summary> should stay the same </summary>
/// <typeparam name=""U""></typeparam>
class C<[|T|], U, V> { }
", @"
/// <summary> should stay the same </summary>
/// <typeparam name=""T""></typeparam>
/// <typeparam name=""U""></typeparam>
class C<T, U, V> { }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public Task TestClassesAfter()
        {
            return MyTestAsync(@"
/// <summary> should stay the same </summary>
/// <typeparam name=""U""></typeparam>
class C<T, U, [|V|]> { }
", @"
/// <summary> should stay the same </summary>
/// <typeparam name=""U""></typeparam>
/// <typeparam name=""V""></typeparam>
class C<T, U, V> { }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public Task TestClassesNoOtherDocumentedParamter()
        {
            return MyTestAsync(@"
/// <summary> should stay the same </summary>
/// <typeparam name=""XXX""></typeparam>
class C<T, U, [|V|]> { }
", @"
/// <summary> should stay the same </summary>
/// <typeparam name=""V""></typeparam>
/// <typeparam name=""XXX""></typeparam>
class C<T, U, V> { }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public Task TestClassesInBetween()
        {
            return MyTestAsync(@"
/// <summary> should stay the same </summary>
/// <typeparam name=""T""></typeparam>
/// <typeparam name=""V""></typeparam>
class C<T, [|U|], V> { }
", @"
/// <summary> should stay the same </summary>
/// <typeparam name=""T""></typeparam>
/// <typeparam name=""U""></typeparam>
/// <typeparam name=""V""></typeparam>
class C<T, U, V> { }
");
        }

        // TODO remove: I don't know how to do a test like this
        //[Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public async Task TestOnDifferentItems()
        {
            var input = @"
namespace ConsoleApplication1
{
    class Program
    {
        /// <param name=""arg1""></param>
        /// <typeparam name=""U""></typeparam>
        static void M<[|T, U|]> ([|int arg1, int arg2|]) { }

        /// <param name=""arg1""></param>
        /// <typeparam name=""U""></typeparam>
        delegate void D<[|T, U|]> ([|int arg1, int arg2|]);

        /// <summary>
        /// </summary>
        /// <typeparam name=""U""></typeparam>
        interface I<[|T, U|]> { }

        /// <summary> should stay the same </summary>
        /// <typeparam name=""U""></typeparam>
        class C<[|T, U, V|]>
        {
            /// <summary></summary>
            /// <param name=""arg2""></param>
            public C([|C<V, V, V> arg1, int arg2|]) { }

            public static explicit operator int(C<T, U, V> arg1) { return 1; }

            /// <param name=""arg1""></param>
            public static C<T, U, V> operator *([|C<T, U, V> arg1, C<T, U, U> arg2|]) { return null; }
        }

        /// <summary>
        /// </summary>
        /// <typeparam name=""U""></typeparam>
        struct S<[|T, U, V|]> { }
    }
}";
            var expected = @"
namespace ConsoleApplication1
{
    class Program
    {
        /// <param name=""arg1""></param>
        /// <param name=""arg2""></param>
        /// <typeparam name=""T""></typeparam>
        /// <typeparam name=""U""></typeparam>
        static void M<T, U> (int arg1, int arg2) { }

        /// <param name=""arg1""></param>
        /// <param name=""arg2""></param>
        /// <typeparam name=""T""></typeparam>
        /// <typeparam name=""U""></typeparam>
        delegate void D<T, U> (int arg1, int arg2);

        /// <summary>
        /// </summary>
        /// <typeparam name=""T""></typeparam>
        /// <typeparam name=""U""></typeparam>
        interface I<T, U> { }

        /// <summary> should stay the same </summary>
        /// <typeparam name=""T""></typeparam>
        /// <typeparam name=""U""></typeparam>
        /// <typeparam name=""V""></typeparam>
        class C<T, U, V>
        {
            /// <summary></summary>
            /// <param name=""arg1""></param>
            /// <param name=""arg2""></param>
            public C(C<V, V, V> arg1, int arg2) { }

            public static explicit operator int(C<T, U, V> arg1) { return 1; }

            /// <param name=""arg1""></param>
            /// <param name=""arg2""></param>
            public static C<T, U, V> operator *(C<T, U, V> arg1, C<T, U, U> arg2) { return null; }
        }

        /// <summary>
        /// </summary>
        /// <typeparam name=""T""></typeparam>
        /// <typeparam name=""U""></typeparam>
        /// <typeparam name=""V""></typeparam>
        struct S<T, U, V> { }
    }
}";
            await TestAsync(input, expected, _regularParseOptions);
            await TestAsync(input, expected, _scriptParseOptions);
        }

        // TODO remove; this is only for testing the unit test helpers
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public async Task TestTest()
        {
            var input = @"
namespace ConsoleApplication1
{
    class Program
    {
        /// <param name=""arg2""></param>
        static void M[|(int arg1, int arg2)|] { }
    }
}";
            var expected = @"
namespace ConsoleApplication1
{
    class Program
    {
        /// <param name=""arg1""></param>
        /// <param name=""arg2""></param>
        static void M(int arg1, int arg2) { }
    }
}";
            await TestAsync(input, expected, _regularParseOptions);
            await TestAsync(input, expected, _scriptParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public async Task TestQuotesAndPlacement()
        {
            Func<string, string> inputTemplate = doc => $@"
namespace ConsoleApplication1
{{
    class Programm
    {{
        /// <summary>
        /// Should stay intact.
        /// </summary>
        /// {doc}
        /// <summary> Should stay intact. </summary>
        void m[|(int p1, int p2)|] {{ }}
    }}
}}
";

            Func<string, string> expectedTemplate = quote => $@"
namespace ConsoleApplication1
{{
    class Programm
    {{
        /// <summary>
        /// Should stay intact.
        /// </summary>
        /// <param name={quote}p1{quote}></param>
        /// <param name={quote}p2{quote}></param>
        /// <summary> Shoulp stay intact. </summary>
        void m(int p1, int p2) {{ }}
    }}
}}
";
            foreach (var givenParam in new[] { "p1", "p2" })
            {
                foreach (var quote in new[] { "'", "\"" })
                {
                    var input = inputTemplate($"<param name={quote}{givenParam}{quote}></param>");
                    var expected = expectedTemplate(quote);
                    await MyTestAsync(input, expected);
                }
            }
        }

        // TODO remove: I don't know how to do a test like this
        //[Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameterXmlDocumentation)]
        public async Task TestNoOtherParamAvailable()
        {
            var input = @"
namespace ConsoleApplication1
{
    class Program
    {
        /// <param name=""a""></param>
        /// <param name=""b""></param>
        /// <typeparam name=""A""></typeparam>
        /// <typeparam name=""B""></typeparam>
        static void M<[|T, U|]>([|int arg1, int arg2|]) { }
    }
}";
            var expected = @"
namespace ConsoleApplication1
{
    class Program
    {
        /// <param name=""arg1""></param>
        /// <param name=""arg2""></param>
        /// <param name=""a""></param>
        /// <param name=""b""></param>
        /// <typeparam name=""T""></typeparam>
        /// <typeparam name=""U""></typeparam>
        /// <typeparam name=""A""></typeparam>
        /// <typeparam name=""B""></typeparam>
        static void M<T, U>(int arg1, int arg2) { }
    }
}";
            await TestAsync(input, expected, _regularParseOptions);
            await TestAsync(input, expected, _scriptParseOptions);
        }
    }
}
