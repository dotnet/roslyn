// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input.Commands
{
    public class OpenProjectDesignerCommandTests
    {
        [Fact]
        public void Constructor_NullAsProjectVsServices_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("projectVsServices", () => {

                new OpenProjectDesignerCommand((IUnconfiguredProjectVsServices)null);
            });
        }
    }
}
