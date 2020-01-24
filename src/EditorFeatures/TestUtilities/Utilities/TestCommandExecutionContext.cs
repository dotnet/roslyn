﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
{
    internal static class TestCommandExecutionContext
    {
        public static CommandExecutionContext Create()
        {
            return new CommandExecutionContext(new TestUIThreadOperationContext());
        }
    }
}
