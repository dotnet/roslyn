// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MetadataReferencePropertiesTests
    {
        [Fact]
        public void Constructor()
        {
            var m = new MetadataReferenceProperties();
            Assert.True(m.Aliases.IsDefault);
            Assert.False(m.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Assembly, m.Kind);

            m = new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases: ImmutableArray.Create("\\/[.'\":_)??\t\n*#$@^%*&)", "foo"), embedInteropTypes: true);
            AssertEx.Equal(new[] { "\\/[.'\":_)??\t\n*#$@^%*&)", "foo" }, m.Aliases);
            Assert.True(m.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Assembly, m.Kind);

            m = new MetadataReferenceProperties(MetadataImageKind.Module);
            Assert.True(m.Aliases.IsDefault);
            Assert.False(m.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Module, m.Kind);

            Assert.Equal(MetadataReferenceProperties.Module, new MetadataReferenceProperties(MetadataImageKind.Module, ImmutableArray<string>.Empty, false));
            Assert.Equal(MetadataReferenceProperties.Assembly, new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray<string>.Empty, false));
        }

        [Fact]
        public void Constructor_Errors()
        { 
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetadataReferenceProperties((MetadataImageKind)byte.MaxValue));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, ImmutableArray.Create("blah")));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, embedInteropTypes: true));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, ImmutableArray.Create("")));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, ImmutableArray.Create("x\0x")));

            Assert.Throws<ArgumentException>(() => MetadataReferenceProperties.Module.WithAliases(ImmutableArray.Create("blah")));
            Assert.Throws<ArgumentException>(() => MetadataReferenceProperties.Module.WithAliases(new[] { "blah" }));
            Assert.Throws<ArgumentException>(() => MetadataReferenceProperties.Module.WithEmbedInteropTypes(true));
        }
        
        [Fact]
        public void WithXxx()
        {
            var p = new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("a"), embedInteropTypes: false);

            Assert.Equal(p.WithAliases(null), new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray<string>.Empty, embedInteropTypes: false));
            Assert.Equal(p.WithAliases(default(ImmutableArray<string>)), new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray<string>.Empty, embedInteropTypes: false));
            Assert.Equal(p.WithAliases(ImmutableArray<string>.Empty), new MetadataReferenceProperties(MetadataImageKind.Assembly, default(ImmutableArray<string>), embedInteropTypes: false));
            Assert.Equal(p.WithAliases(new string[0]), new MetadataReferenceProperties(MetadataImageKind.Assembly, default(ImmutableArray<string>), embedInteropTypes: false));
            Assert.Equal(p.WithAliases(new[] { "foo", "aaa" }), new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("foo", "aaa"), embedInteropTypes: false));
            Assert.Equal(p.WithEmbedInteropTypes(true), new MetadataReferenceProperties(MetadataImageKind.Assembly, ImmutableArray.Create("a"), embedInteropTypes: true));

            var m = new MetadataReferenceProperties(MetadataImageKind.Module);
            Assert.Equal(m.WithAliases(default(ImmutableArray<string>)), new MetadataReferenceProperties(MetadataImageKind.Module, default(ImmutableArray<string>), embedInteropTypes: false));
            Assert.Equal(m.WithEmbedInteropTypes(false), new MetadataReferenceProperties(MetadataImageKind.Module, default(ImmutableArray<string>), embedInteropTypes: false));
        }
    }
}
