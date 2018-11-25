// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    internal static class MSBuildUtil
    {
        public static ITaskItem[] CreateTaskItems(params string[] fileNames)
        {
            return fileNames.Select(CreateTaskItem).ToArray();
        }

        public static ITaskItem CreateTaskItem(string fileName)
        {
            var taskItem = new Mock<ITaskItem>(MockBehavior.Strict);
            taskItem.Setup(x => x.ItemSpec).Returns(fileName);
            return taskItem.Object;
        }
    }
}
