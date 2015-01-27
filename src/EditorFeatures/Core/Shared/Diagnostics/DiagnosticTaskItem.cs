// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Diagnostics
{
    internal sealed class DiagnosticTaskItem : IErrorTaskItem
    {
        private readonly DiagnosticData _diagnostic;

        public DiagnosticTaskItem(DiagnosticData diagnostic)
        {
            _diagnostic = diagnostic;
        }

        public string Id
        {
            get { return _diagnostic.Id; }
        }

        public ProjectId ProjectId
        {
            get { return _diagnostic.ProjectId; }
        }

        public DiagnosticSeverity Severity
        {
            get { return _diagnostic.Severity; }
        }

        public string Message
        {
            get { return _diagnostic.Message; }
        }

        public Workspace Workspace
        {
            get { return _diagnostic.Workspace; }
        }

        public DocumentId DocumentId
        {
            get { return _diagnostic.DocumentId; }
        }

        public string MappedFilePath
        {
            get { return _diagnostic.MappedFilePath; }
        }

        public string OriginalFilePath
        {
            get { return _diagnostic.OriginalFilePath; }
        }

        public int MappedLine
        {
            get
            {
                if (this.DocumentId != null)
                {
                    return _diagnostic.MappedStartLine;
                }
                else
                {
                    throw new InvalidOperationException(EditorFeaturesResources.NotASourceError);
                }
            }
        }

        public int MappedColumn
        {
            get
            {
                if (this.DocumentId != null)
                {
                    return _diagnostic.MappedStartColumn;
                }
                else
                {
                    throw new InvalidOperationException(EditorFeaturesResources.NotASourceError);
                }
            }
        }

        public int OriginalLine
        {
            get
            {
                if (this.DocumentId != null)
                {
                    return _diagnostic.OriginalStartLine;
                }
                else
                {
                    throw new InvalidOperationException(EditorFeaturesResources.NotASourceError);
                }
            }
        }

        public int OriginalColumn
        {
            get
            {
                if (this.DocumentId != null)
                {
                    return _diagnostic.OriginalStartColumn;
                }
                else
                {
                    throw new InvalidOperationException(EditorFeaturesResources.NotASourceError);
                }
            }
        }

        public override bool Equals(object obj)
        {
            IErrorTaskItem other = obj as IErrorTaskItem;
            if (other == null)
            {
                return false;
            }

            return
                AbstractTaskItem.Equals(this, other) &&
                Id == other.Id &&
                ProjectId == other.ProjectId &&
                Severity == other.Severity;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(AbstractTaskItem.GetHashCode(this), Hash.Combine(Id.GetHashCode(), (int)Severity));
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4} {5} ({5}, {6}) [original: {7} ({8}, {9})]",
                Id,
                Message,
                Severity.ToString(),
                ProjectId,
                MappedFilePath ?? "",
                MappedLine.ToString(),
                MappedColumn.ToString(),
                OriginalFilePath ?? "",
                OriginalLine.ToString(),
                OriginalColumn.ToString());
        }
    }
}
