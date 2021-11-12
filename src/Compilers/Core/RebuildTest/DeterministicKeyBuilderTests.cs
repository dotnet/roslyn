// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;
using Newtonsoft;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.VisualBasic.UnitTests;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public abstract class DeterministicKeyBuilderTests
    {
        private static readonly char[] s_trimChars = { ' ', '\n', '\r' };

        public static SourceHashAlgorithm HashAlgorithm { get; } = SourceHashAlgorithm.Sha256;
        public static CSharpCompilationOptions CSharpOptions { get; } = new CSharpCompilationOptions(OutputKind.ConsoleApplication, deterministic: true);

        protected static void AssertJson(
            string expected,
            string actual,
            bool removeStandard = true) => AssertJson(expected, actual, "references", "extensions");

        protected static void AssertJson(
            string expected,
            string actual,
            params string[] ignoreSections)
        {
            var json = JObject.Parse(actual);
            if (ignoreSections.Length > 0)
            {
                json
                    .Descendants()
                    .OfType<JProperty>()
                    .Where(x => ignoreSections.Contains(x.Name))
                    .ToList()
                    .ForEach(x => x.Remove());
            }

            actual = json.ToString(Formatting.Indented);
            expected = JObject.Parse(expected).ToString(Formatting.Indented);
            AssertJsonCore(expected, actual);
        }

        protected static void AssertJsonSection(
            string expected,
            string actual,
            string sectionName)
        {
            var lastName = sectionName.Split('.').Last();
            AssertJsonCore(expected, getSection(actual));

            string getSection(string json) =>
                JObject.Parse(json)
                    .Descendants()
                    .OfType<JProperty>()
                    .Where(x => x.Name == lastName && getFullName(x) == sectionName)
                    .Single()
                    .ToString(Formatting.Indented);

            static string getFullName(JProperty property)
            {
                string name = property.Name;
                while (
                    property.Parent is JObject obj &&
                    obj.Parent is JProperty parent)
                {
                    name = $"{parent.Name}.{name}";
                    property = parent;
                }

                return name;
            }
        }

        protected static void AssertJsonCore(string expected, string actual)
        {
            expected = expected.Trim(s_trimChars);
            actual = actual.Trim(s_trimChars);
            Assert.Equal(expected, actual);
        }

        protected static string GetChecksum(SourceText text)
        {
            var checksum = text.GetChecksum();
            var builder = PooledStringBuilder.GetInstance();
            DeterministicKeyBuilder.EncodeByteArrayValue(checksum.AsSpan(), builder);
            return builder.ToStringAndFree();
        }
    }
}
