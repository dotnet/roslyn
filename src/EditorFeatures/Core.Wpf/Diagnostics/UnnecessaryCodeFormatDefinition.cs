﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Diagnostics
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ClassificationTypeDefinitions.UnnecessaryCode)]
    [Name(ClassificationTypeDefinitions.UnnecessaryCode)]
    [Order(After = Priority.High)]
    [UserVisible(false)]
    internal sealed class UnnecessaryCodeFormatDefinition : ClassificationFormatDefinition
    {
        private UnnecessaryCodeFormatDefinition()
        {
            this.DisplayName = EditorFeaturesResources.Unnecessary_Code;
            this.ForegroundOpacity = 0.6;
        }
    }
}
