// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the type of work that the workspace should automatically do in the background.
    /// The workspace can automatically, asynchronously, parse documents in its solution, and can
    /// also automatically, asynchronously, produce compilations from those syntax trees, including
    /// any necessary metadata or compilation references.
    /// </summary>
    [Flags]
    internal enum WorkspaceBackgroundWork
    {
        None,
        Parse = 1,
        Compile = 2,
        ParseAndCompile = Parse | Compile
    }

    internal static class WorkspaceBackgroundWorkExtensions
    {
        public static bool IsValid(this WorkspaceBackgroundWork work)
        {
            return work >= WorkspaceBackgroundWork.None && work <= WorkspaceBackgroundWork.ParseAndCompile;
        }
    }
}
