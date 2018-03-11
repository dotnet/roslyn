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
