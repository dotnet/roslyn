// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LookupSymbolsInfoTests : CSharpTestBase
    {
        class TemplateArgEnumerator : IEnumerator<string>
        {
            short current;

            public TemplateArgEnumerator()
            {
                this.current = 0;
            }

            public string Current
            {
                get { return string.Format("T{0}", current.ToString()); }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                if (current == short.MaxValue)
                {
                    return false;
                }

                current++;
                return true;
            }

            public void Reset()
            {
                this.current = 0;
            }
        }

        class TemplateArgEnumerable : IEnumerable<string>
        {
            public static IEnumerable<string> Instance = new TemplateArgEnumerable();

            public IEnumerator<string> GetEnumerator()
            {
                return new TemplateArgEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        static string GenerateTemplateArgs(int arity)
        {
            if (arity == 0) return string.Empty;
            return string.Format("<{0}>", string.Join(",", TemplateArgEnumerable.Instance.Take(arity)));
        }

        static void AppendEmptyClass(StringBuilder sb, string root, int arity)
        {
            sb.Append("class ");
            sb.Append(root);
            sb.Append(GenerateTemplateArgs(arity));
            sb.AppendLine(" {}");
        }

        static void CompileAndCheckSymbolCount(string source, string symbolName, int expectedSymbolCount)
        {
            var compilation = CreateCompilationWithMscorlib(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var symbols = model.LookupSymbols(0, null, symbolName);

            Assert.Equal(expectedSymbolCount, symbols.Length);
        }

        [Fact]
        public void UniqueSymbolOrAritiesTestBigArity()
        {
            // The LookupSymbols implementation is optimized for small arities (less than 32).
            // Since larger arity values are so rare, they are tested explicitly here.

            StringBuilder sb = new StringBuilder();

            AppendEmptyClass(sb, "Foo", 50);

            // Unique symbol test
            CompileAndCheckSymbolCount(sb.ToString(), "Foo", 1);

            AppendEmptyClass(sb, "Foo", 100);
            AppendEmptyClass(sb, "Foo", 150);
            AppendEmptyClass(sb, "Foo", 200);
            AppendEmptyClass(sb, "Foo", 250);

            // Multiple symbols test
            CompileAndCheckSymbolCount(sb.ToString(), "Foo", 5);
        }

        [Fact]
        public void UniqueSymbolOrAritiesTestSmallArity()
        {
            StringBuilder sb = new StringBuilder();

            AppendEmptyClass(sb, "Foo", 1);

            // Unique symbol test
            CompileAndCheckSymbolCount(sb.ToString(), "Foo", 1);

            AppendEmptyClass(sb, "Foo", 2);
            AppendEmptyClass(sb, "Foo", 3);

            // Multiple symbols test
            CompileAndCheckSymbolCount(sb.ToString(), "Foo", 3);
        }

        [Fact]
        public void UniqueSymbolOrAritiesTest()
        {
            for (int i = 0; i < 50; i++)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < i; j++)
                {
                    AppendEmptyClass(sb, "Foo", j);
                }

                CompileAndCheckSymbolCount(sb.ToString(), "Foo", i);
            }
        }
    }
}
