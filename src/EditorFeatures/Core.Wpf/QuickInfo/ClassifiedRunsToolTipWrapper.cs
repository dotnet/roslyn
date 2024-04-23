// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    /// <summary>
    /// Tooltip wrapper for classified runs.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ClassifiedRunsToolTipWrapper"/> class.
    /// </remarks>
    /// <param name="target">Target <see cref="IList{ClassifiedTextRun}"/> instance.</param>
    /// <param name="lineIndentSize">Set the indent size of each line on the target.</param>
    internal class ClassifiedRunsToolTipWrapper(IList<ClassifiedTextRun> target, uint? lineIndentSize = 0) : ToolTipWrapper<IList<ClassifiedTextRun>>(target, lineIndentSize)
    {

        /// <inheritdoc/>
        protected override bool IsLastLineEmpty
        {
            get
            {
                return Target[Target.Count - 1].Text == Environment.NewLine;
            }
        }

        /// <inheritdoc/>
        protected override void AddEllipsis()
        {
            Target.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Other, "…", ClassifiedTextRunStyle.Plain));
        }

        /// <inheritdoc/>
        protected override void AddLine(string line)
        {
            Target.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.Other, line.ToString()));
        }

        /// <inheritdoc/>
        protected override void AddNewline()
        {
            Target.Add(new ClassifiedTextRun(PredefinedClassificationTypeNames.WhiteSpace, Environment.NewLine));
        }
    }
}
