// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    internal abstract partial class AbstractVisualStudioErrorTaskList
    {
        private class VisualStudioTaskItem : VisualStudioTaskItemBase<IErrorTaskItem>, IComparable<VisualStudioTaskItem>
        {
            public VisualStudioTaskItem(IErrorTaskItem item) :
                base(VSTASKCATEGORY.CAT_BUILDCOMPILE, item)
            {
            }

            public int CompareTo(VisualStudioTaskItem other)
            {
                Contract.Requires(this.ItemCategory == other.ItemCategory);
                Contract.Requires(this.Info.Workspace == other.Info.Workspace);
                Contract.Requires(this.Info.Workspace is VisualStudioWorkspace);

                return ProjectCompareTo(this.Info.Workspace.CurrentSolution, this.Info, other.Info);
            }

            private int ProjectCompareTo(Solution solution, IErrorTaskItem left, IErrorTaskItem right)
            {
                var project1 = solution.GetProject(left.ProjectId);
                var project2 = solution.GetProject(right.ProjectId);

                // existing project goes first
                var result = NullCompareTo(project1, project2);
                if (result != 0)
                {
                    return result;
                }

                // project doesn't exist, document won't exist as well.
                if (project1 == null && project2 == null)
                {
                    // compare just information it has
                    return InfoCompareTo(left, right);
                }

                // project name
                var name = string.Compare(project1.Name, project2.Name, StringComparison.OrdinalIgnoreCase);
                if (name != 0)
                {
                    return name;
                }

                // document
                return DocumentCompareTo(solution, left, right);
            }

            private int DocumentCompareTo(Solution solution, IErrorTaskItem left, IErrorTaskItem right)
            {
                var document1 = solution.GetDocument(left.DocumentId);
                var document2 = solution.GetDocument(right.DocumentId);

                // existing document goes first
                var result = NullCompareTo(document1, document2);
                if (result != 0)
                {
                    return result;
                }

                // document doesn't exist
                if (document1 == null && document2 == null)
                {
                    return InfoCompareTo(left, right);
                }

                // document filepath or name
                int nameResult = 0;
                if (document1.FilePath != null && document1.FilePath != null)
                {
                    nameResult = string.Compare(document1.FilePath, document2.FilePath, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    nameResult = string.Compare(document1.Name, document2.Name, StringComparison.OrdinalIgnoreCase);
                }

                if (nameResult != 0)
                {
                    return nameResult;
                }

                return InfoCompareTo(left, right);
            }

            private int InfoCompareTo(IErrorTaskItem left, IErrorTaskItem right)
            {
                if (left.DocumentId == null && right.DocumentId == null)
                {
                    return string.Compare(left.Message, right.Message);
                }

                Contract.Requires(left.DocumentId != null && right.DocumentId != null);
                var line = left.OriginalLine - right.OriginalLine;
                if (line != 0)
                {
                    return line;
                }

                var column = left.OriginalColumn - right.OriginalColumn;
                if (column != 0)
                {
                    return column;
                }

                return string.Compare(left.Message, right.Message);
            }

            private int NullCompareTo(object left, object right)
            {
                if (left != null && right == null)
                {
                    return -1;
                }
                else if (left == null && right != null)
                {
                    return 1;
                }

                return 0;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as VisualStudioTaskItem);
            }

            public bool Equals(VisualStudioTaskItem other)
            {
                if (this == other)
                {
                    return true;
                }

                return base.Equals(other) &&
                       this.Info.ProjectId == other.Info.ProjectId &&
                       this.Info.Severity == other.Info.Severity &&
                       this.Info.Id == other.Info.Id;
            }

            public override int GetHashCode()
            {
                return
                    Hash.Combine(base.GetHashCode(),
                    Hash.Combine(this.Info.ProjectId,
                    Hash.Combine((int)this.Info.Severity,
                    Hash.Combine(this.Info.Id, 0))));
            }

            public override int GetCategory(out uint pCategory)
            {
                pCategory = (uint)GetErrorCategory(this.Info.Severity);
                return VSConstants.S_OK;
            }

            public override int GetHierarchy(out IVsHierarchy hierarchy)
            {
                var workspace = this.Info.Workspace as VisualStudioWorkspace;
                if (workspace == null)
                {
                    hierarchy = null;
                    return VSConstants.E_NOTIMPL;
                }

                hierarchy = workspace.GetHierarchy(this.Info.ProjectId);
                return VSConstants.S_OK;
            }

            protected override int GetPriority(VSTASKPRIORITY[] ptpPriority)
            {
                if (ptpPriority != null)
                {
                    switch (GetErrorCategory(this.Info.Severity))
                    {
                        case TaskErrorCategory.Error:
                            ptpPriority[0] = VSTASKPRIORITY.TP_HIGH;
                            break;
                        case TaskErrorCategory.Warning:
                            ptpPriority[0] = VSTASKPRIORITY.TP_NORMAL;
                            break;
                        case TaskErrorCategory.Message:
                            ptpPriority[0] = VSTASKPRIORITY.TP_LOW;
                            break;
                        default:
                            return VSConstants.E_FAIL;
                    }

                    return VSConstants.S_OK;
                }

                return VSConstants.E_FAIL;
            }

            private TaskErrorCategory GetErrorCategory(DiagnosticSeverity severity)
            {
                switch (severity)
                {
                    default:
                    case DiagnosticSeverity.Error:
                        return TaskErrorCategory.Error;
                    case DiagnosticSeverity.Warning:
                        return TaskErrorCategory.Warning;
                    case DiagnosticSeverity.Info:
                        return TaskErrorCategory.Message;
                }
            }
        }
    }
}
