// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    [UnitTestTrait]
    public class ProjectDesignerPageMetadataTests
    {
        [Fact]
        public void Constructor_GuidEmptyAsPageGuid_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("pageGuid", () => {

                new ProjectDesignerPageMetadata(Guid.Empty, 0, hasConfigurationCondition: false);
            });
        }

        [Fact]
        public void Constructor_ValueAsPageGuid_SetsPageGuidProperty()
        {
            var guid = Guid.NewGuid();
            var metadata = new ProjectDesignerPageMetadata(guid, 0, hasConfigurationCondition: false);

            Assert.Equal(guid, metadata.PageGuid);
        }

        [Fact]
        public void Constructor_ValueAsPageOrder_SetsPageOrderProperty()
        {
            for (int i = -10; i < 10; i++)
            {
                var metadata = new ProjectDesignerPageMetadata(Guid.NewGuid(), i, hasConfigurationCondition: false);

                Assert.Equal(i, metadata.PageOrder);
            }
        }

        [Fact]
        public void Constructor_ValueAsHasConfigurationCondition_SetsHasConfigurationConditionProperty()
        {
            foreach (var value in new[] { true, false })
            {
                var metadata = new ProjectDesignerPageMetadata(Guid.NewGuid(), 0, hasConfigurationCondition: value);

                Assert.Equal(value, metadata.HasConfigurationCondition);
            }
        }

        [Fact]
        public void Name_ReturnsNull()
        {
            var metadata = CreateInstance();

            Assert.Null(metadata.Name);
        }

        private static ProjectDesignerPageMetadata CreateInstance()
        {
            return new ProjectDesignerPageMetadata(Guid.NewGuid(), 0, false);
        }
    }
}
