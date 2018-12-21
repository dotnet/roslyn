// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if false

// This file is entirely disabled, but serves a useful purpose and is thus kept in the source tree.
// Specifically, it provides a test scaffolding that allows us to easily run and regenerate tests
// when appropriate.  For example, if/when we decide to add/change information in the baselines, we
// can easily just regenerate all the tests instead of having to manually fix up the baselines
// themselves.
//
// The main logic is in CSharpRegexParserTests.GenerateTests.  Basically, all tests will funnel
// through to that, passing along all the appropriate test arguments.  That helper will then
// generate the new baseline.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EmbeddedLanguages.RegularExpressions
{
    // used so we can keep tests sorted numerically.  i.e.  test9 then test10 (even though 1 is
    // before 9 in ascii).
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
            var other = new Dictionary<string, string>();

            var tree = SyntaxFactory.ParseSyntaxTree(
                File.ReadAllText(@"C:\GitHub\roslyn\src\Workspaces\CSharpTest\EmbeddedLanguages\RegularExpressions\CSharpRegexParserTests_BasicTests.cs"));

            var methodNames = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Select(m => m.Identifier.ValueText);
            var nameToIndex = new Dictionary<string, int>();

            var index = 0;
            foreach (var name in methodNames)
            {
                nameToIndex[name] = index;
                index++;
            }

#if false
            var tests =
                CSharpRegexParserTests.nameToTest.Where(kvp => !kvp.Key.StartsWith("NegativeTest") && !kvp.Key.StartsWith("Reference"))
                     .OrderBy(kvp => nameToIndex[kvp.Key])
                     .Select(kvp => kvp.Value);
#elif false
            var tests =
                CSharpRegexParserTests.nameToTest.Where(kvp => kvp.Key.StartsWith("NegativeTest"))
                     .OrderBy(kvp => kvp.Key, LogicalStringComparer.Instance)
                     .Select(kvp => kvp.Value);
#else
            var tests =
                CSharpRegexParserTests.nameToTest.Where(kvp => kvp.Key.StartsWith("Reference"))
                     .OrderBy(kvp => kvp.Key, LogicalStringComparer.Instance)
                     .Select(kvp => kvp.Value);
#endif

            var val = string.Join("\r\n", tests);
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

        private void Test(
            string stringText, string expected, RegexOptions options, 
            bool runSubTreeTests = true, [CallerMemberName]string name = "",
            bool allowIndexOutOfRange = false,
            bool allowNullReference = false,
            bool allowOutOfMemory = false)
        {
            var test = GenerateTests(stringText, options, runSubTreeTests, name,
                allowIndexOutOfRange, allowNullReference, allowOutOfMemory);
            nameToTest.Add(name, test);
        }

        public string GenerateTests(
            string val, RegexOptions options, bool runSubTreeTests, string testName,
            bool allowIndexOutOfRange, bool allowNullReference, bool allowOutOfMemory)
        {
            // Actually call into our regex APIs to produce figure out the tree produced.
            // Then spit out a test that does the same, encoding the tree as the expected
            
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

            var actual = TreeToText(token.SyntaxTree.GetText(), tree).Replace("\"", "\"\"");
            builder.Append(", " + '@' + '"');
            builder.Append(actual);

            builder.Append('"');
            builder.Append(", RegexOptions." + options.ToString());

            if (!runSubTreeTests)
            {
                builder.Append(", runSubTreeTests: false");
            }

            if (allowIndexOutOfRange)
            {
                builder.Append(", allowIndexOutOfRange: true");
            }

            if (allowNullReference)
            {
                builder.Append(", allowNullReference: true");
            }

            if (allowOutOfMemory)
            {
                builder.Append(", allowOutOfMemory: true");
            }

            builder.AppendLine(");");
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}

#endif
