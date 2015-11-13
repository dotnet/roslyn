// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Input;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input.Commands
{
    [ProjectSystemTrait]
    public class OpenProjectDesignerOnDefaultActionCommandTests : OpenProjectDesignerCommandBaseTests
    {
        [Fact]
        public void Constructor_NullAsDesignerService_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("designerService", () => {

                new OpenProjectDesignerOnDefaultActionCommand((IProjectDesignerService)null);
            });
        }

        internal override long GetCommandId()
        {
            return UIHierarchyWindowCommandId.DoubleClick;
        }

        internal override OpenProjectDesignerCommandBase CreateInstance(IProjectDesignerService designerService = null)
        {
            designerService = designerService ?? IProjectDesignerServiceFactory.Create();

            return new OpenProjectDesignerOnDefaultActionCommand(designerService);
        }
    }
}
