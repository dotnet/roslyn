// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    internal partial class VisualStudioTodoTaskList
    {
        private class VisualStudioTaskItem : VisualStudioTaskItemBase<TodoTaskItem>
        {
            private bool _checked;

            public VisualStudioTaskItem(TodoTaskItem item)
                : base(VSTASKCATEGORY.CAT_COMMENTS, item)
            {
                _checked = false;
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

                return _checked == other._checked && base.Equals(other);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(base.GetHashCode(), _checked ? 1 : 0);
            }

            protected override int GetChecked(out int pfChecked)
            {
                pfChecked = _checked ? 1 : 0;
                return VSConstants.S_OK;
            }

            protected override int GetPriority(VSTASKPRIORITY[] ptpPriority)
            {
                if (ptpPriority != null)
                {
                    ptpPriority[0] = (VSTASKPRIORITY)this.Info.Priority;
                }

                return VSConstants.S_OK;
            }

            protected override int PutChecked(int fChecked)
            {
                _checked = fChecked == 1;
                return VSConstants.S_OK;
            }
        }
    }
}
