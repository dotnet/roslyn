// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class MetadataReferencePropertiesTests
    {
        [Fact]
        public void Constructor()
        {
            var m = new MetadataReferenceProperties();
            Assert.Null(m.Alias);
            Assert.False(m.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Assembly, m.Kind);

            m = new MetadataReferenceProperties(MetadataImageKind.Assembly, alias: "\\/[.'\":_)??\t\n*#$@^%*&)", embedInteropTypes: true);
            Assert.Equal("\\/[.'\":_)??\t\n*#$@^%*&)", m.Alias);
            Assert.True(m.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Assembly, m.Kind);

            m = new MetadataReferenceProperties(MetadataImageKind.Module);
            Assert.Equal(null, m.Alias);
            Assert.False(m.EmbedInteropTypes);
            Assert.Equal(MetadataImageKind.Module, m.Kind);

            Assert.Equal(MetadataReferenceProperties.Module, new MetadataReferenceProperties(MetadataImageKind.Module, null, false));
            Assert.Equal(MetadataReferenceProperties.Assembly, new MetadataReferenceProperties(MetadataImageKind.Assembly, null, false));
        }

        [Fact]
        public void Constructor_Errors()
        { 
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetadataReferenceProperties((MetadataImageKind)Byte.MaxValue));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, alias: "blah"));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, embedInteropTypes: true));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, alias: ""));
            Assert.Throws<ArgumentException>(() => new MetadataReferenceProperties(MetadataImageKind.Module, alias: "x\0x"));

            Assert.Throws<ArgumentException>(() => MetadataReferenceProperties.Module.WithAlias("blah"));
            Assert.Throws<ArgumentException>(() => MetadataReferenceProperties.Module.WithEmbedInteropTypes(true));
        }
        
        [Fact]
        public void WithXxx()
        {
            var p = new MetadataReferenceProperties(MetadataImageKind.Assembly, "a", embedInteropTypes: false);

            Assert.Equal(p.WithAlias("foo"), new MetadataReferenceProperties(MetadataImageKind.Assembly, "foo", embedInteropTypes: false));
            Assert.Equal(p.WithEmbedInteropTypes(true), new MetadataReferenceProperties(MetadataImageKind.Assembly, "a", embedInteropTypes: true));

            var m = new MetadataReferenceProperties(MetadataImageKind.Module);
            Assert.Equal(m.WithAlias(null), new MetadataReferenceProperties(MetadataImageKind.Module, null, embedInteropTypes: false));
            Assert.Equal(m.WithEmbedInteropTypes(false), new MetadataReferenceProperties(MetadataImageKind.Module, null, embedInteropTypes: false));
        }
    }
}
