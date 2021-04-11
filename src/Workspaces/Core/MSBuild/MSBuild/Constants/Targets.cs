// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public static class Targets
    {
        /// <summary>
        /// Default targets used to build a project
        /// </summary>
        public static ImmutableArray<string> Default => ImmutableArray.Create(TargetNames.Compile, TargetNames.CoreCompile);

        /// <summary>
        /// Targets used to restore and build a project
        /// </summary>
        public static ImmutableArray<string> Restore => ImmutableArray.Create(TargetNames.Restore, TargetNames.Compile, TargetNames.CoreCompile);
    }
}
