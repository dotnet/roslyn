// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBoss
{
    internal struct ProjectReferenceEntry
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
