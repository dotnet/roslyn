// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
