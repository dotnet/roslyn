// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    internal readonly struct ProjectReferenceEntry
    {
        internal string FileName { get; }
        internal Guid? Project { get; }

        internal ProjectKey ProjectKey => new ProjectKey(FileName);

        internal ProjectReferenceEntry(string fileName, Guid? project)
        {
            FileName = fileName;
            Project = project;
        }
    }
}
