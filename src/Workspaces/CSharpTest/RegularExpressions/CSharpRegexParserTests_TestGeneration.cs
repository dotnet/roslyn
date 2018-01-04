// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if false

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.RegularExpressions
{
    internal class LogicalStringComparer : IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        public static readonly IComparer<string> Instance = new LogicalStringComparer();

        private LogicalStringComparer()
        {
        }

        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }
    }

    public class Fixture : IDisposable
    {
        public void Dispose()
        {
            var tests = CSharpRegexParserTests.nameToTest;
            var other = new Dictionary<string, string>();

            var tree = SyntaxFactory.ParseSyntaxTree(
                File.ReadAllText(@"C:\GitHub\roslyn-internal\Open\src\Workspaces\CSharpTest\RegularExpressions\CSharpRegexParserTests.cs"));

            var methodNames = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Select(m => m.Identifier.ValueText);
            var nameToIndex = new Dictionary<string, int>();

            var index = 0;
            foreach (var name in methodNames)
            {
                nameToIndex[name] = index;
                index++;
            }

            var referenceTests =
                tests.Where(kvp => !kvp.Key.StartsWith("NegativeTest") && !kvp.Key.StartsWith("Reference"))
                     .OrderBy(kvp => nameToIndex[kvp.Key])
                     .Select(kvp => kvp.Value);

            var val = string.Join("\r\n", referenceTests);
        }
    }

    [CollectionDefinition(nameof(MyCollection))]
    public class MyCollection : ICollectionFixture<Fixture>
    {
    }

    [Collection(nameof(MyCollection))]
    public partial class CSharpRegexParserTests
    {
        private readonly Fixture _fixture;

        public CSharpRegexParserTests(Fixture fixture)
        {
            _fixture = fixture;
        }


        public static Dictionary<string, string> nameToTest = new Dictionary<string, string>();

        private void Test(string stringText, string expected, RegexOptions options, [CallerMemberName]string name = "")
        {
            var test = GenerateTests(stringText, options, name);
            nameToTest.Add(name, test);

#if false
            var tree = TryParseTree(stringText, options, conversionFailureOk: false);

            // TryParseSubTrees(stringText, options);

            var actual = TreeToText(tree).Replace("\"", "\"\"");
            Assert.Equal(expected.Replace("\"", "\"\""), actual);
#endif
        }

        public string GenerateTests(string val, RegexOptions options, string testName)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[Fact]");
            builder.AppendLine("public void " + testName + "()");
            builder.AppendLine("{");
            builder.Append(@"    Test(");

            var escaped = val.Replace("\"", "\"\"");
            var quoted = "" + '@' + '"' + escaped + '"';
            builder.Append(quoted);

            var token = GetStringToken(val);
            var allChars = _service.TryConvertToVirtualChars(token);
            var tree = RegexParser.TryParse(allChars, options);

            var actual = TreeToText(tree).Replace("\"", "\"\"");
            builder.Append(", " + '@' + '"');
            builder.Append(actual);

            builder.AppendLine("" + '"' + ", RegexOptions." + options.ToString() + ");");
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}

#endif
