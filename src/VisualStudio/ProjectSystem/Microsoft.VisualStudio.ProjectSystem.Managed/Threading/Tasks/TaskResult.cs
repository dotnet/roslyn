// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Threading.Tasks
{
    internal class TaskResult
    {
        public static readonly Task<bool> False = Task.FromResult(false);
        public static readonly Task<bool> True = Task.FromResult(true);
    }
}
