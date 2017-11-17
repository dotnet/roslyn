// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    [Serializable]
    public class ErrorListItem : IEquatable<ErrorListItem>
    {
        public string Severity { get; set; }
        public string Description { get; set; }
        public string Project { get; set; }
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public ErrorListItem(string severity, string description, string project, string fileName, int line, int column)
        {
            Severity = severity;
            Description = description;
            Project = project;
            FileName = fileName;
            Line = line;
            Column = column;
        }

        public ErrorListItem(EnvDTE80.ErrorItem errorItem)
        {
            Severity = errorItem.ErrorLevel.AsString();
            Description = errorItem.Description;
            Project = Path.GetFileName(errorItem.Project);
            FileName = Path.GetFileName(errorItem.FileName);
            Line = errorItem.Line;
            Column = errorItem.Column;
        }

        public bool Equals(ErrorListItem other)
            => other != null
            && Comparison.AreStringValuesEqual(Severity, other.Severity)
            && Comparison.AreStringValuesEqual(Description, other.Description)
            && Comparison.AreStringValuesEqual(Project, other.Project)
            && Comparison.AreStringValuesEqual(FileName, other.FileName)
            && Line == other.Line
            && Column == other.Column;

        public override bool Equals(object obj)
            => Equals(obj as ErrorListItem);

        public override int GetHashCode()
            => Hash.Combine(Severity, Hash.Combine(Description, Hash.Combine(Project, Hash.Combine(FileName, Hash.Combine(Line, Hash.Combine(Column, 0))))));

        public override string ToString()
            => $"Severity:{Severity} Description:{Description} Project:{Project} File:{FileName} Line:{Line} Column:{Column}";
    }
}
