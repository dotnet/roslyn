// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EditorConfigTests
    {
        private static EditorConfig ParseConfigFile(string text) => EditorConfig.Parse(text, "");

        [Fact]
        public void SimpleCase()
        {
            var config = EditorConfig.Parse(@"
root = true

# Comment1
# Comment2
##################################

my_global_prop = my_global_val

[*.cs]
my_prop = my_val

", "Z:\\bogus");

            Assert.Equal("", config.GlobalSection.Name);
            var properties = config.GlobalSection.Properties;
            Assert.Equal(2, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_global_prop", "my_global_val"),
                        KeyValuePair.Create("root", "true") },
                properties);

            var namedSections = config.NamedSections;
            Assert.Equal(1, namedSections.Count);
            Assert.Equal("*.cs", namedSections[0].Name);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val")},
                namedSections[0].Properties);
            
            Assert.True(config.IsRoot);
            Assert.Equal("Z:\\bogus", config.Directory);
        }

        [Fact]
        public void MissingClosingBracket()
        {
            var config = ParseConfigFile(@"
[*.cs
my_prop = my_val");
            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val")},
                properties);

            Assert.Equal(0, config.NamedSections.Count);
        }

        [Fact]
        public void CaseInsensitivePropKey()
        {
            var config = ParseConfigFile(@"
my_PROP = my_VAL");
            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);

            Assert.True(properties.TryGetValue("my_prop", out var val));
            Assert.Equal(val, "my_VAL");
            Assert.Equal("my_PROP", properties.Keys.Single());
        }

        [Fact]
        public void DuplicateKeys()
        {
            var config = ParseConfigFile(@"
my_prop = my_val
my_prop = my_other_val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            Assert.Equal(new[] { KeyValuePair.Create("my_prop", "my_other_val") }, properties);
        }

        [Fact]
        public void DuplicateKeysCasing()
        {
            var config = ParseConfigFile(@"
my_prop = my_val
my_PROP = my_other_val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            Assert.Equal(new[] { KeyValuePair.Create("my_prop", "my_other_val") }, properties);
        }

        [Fact]
        public void MissingKey()
        {
            var config = ParseConfigFile(@"
= my_val1
my_prop = my_val2");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val2") },
                properties);
        }

        [Fact]
        public void MissingVal()
        {
            var config = ParseConfigFile(@"
my_prop1 =
my_prop2 = my_val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(2, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop1", ""),
                        KeyValuePair.Create("my_prop2", "my_val") },
                properties);
        }

        [Fact]
        public void SpacesInProperties()
        {
            var config = ParseConfigFile(@"
my prop1 = my_val1
my_prop2 = my val2");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop2", "my val2") },
                properties);
        }

        [Fact]
        public void EndOfLineComments()
        {
            var config = ParseConfigFile(@"
my_prop2 = my val2 # Comment");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop2", "my val2") },
                properties);
        }

        [Fact]
        public void SymbolsStartKeys()
        {
            var config = ParseConfigFile(@"
@!$abc = my_val1
@!$\# = my_val2");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(0, properties.Count);
        }

        [Fact]
        public void EqualsAndColon()
        {
            var config = ParseConfigFile(@"
my:key1 = my_val
my_key2 = my:val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(2, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my", "key1 = my_val"),
                        KeyValuePair.Create("my_key2", "my:val")},
                properties);
        }

        [Fact]
        public void SymbolsInProperties()
        {
            var config = ParseConfigFile(@"
my@key1 = my_val
my_key2 = my@val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_key2", "my@val")},
                properties);
        }

        [Fact]
        public void LongLines()
        {
            // PROTOTYPE(editorconfig): This example is described in the Python ConfigParser as
            // allowing line continuation via the RFC 822 specification, section 3.1.1 LONG
            // HEADER FIELDS. It's unclear whether it's intended for editorconfig to permit
            // this interpretation. The VS parser does not. We should probably check other
            // parsers for completeness.
            var config = ParseConfigFile(@"
long: this value continues
   in the next line");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(1, properties.Count);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("long", "this value continues")},
                properties);
        }
    }
}
