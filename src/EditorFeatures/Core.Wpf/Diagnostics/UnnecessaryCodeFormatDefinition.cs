// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnnecessaryCodeFormatDefinition()
        {
            this.DisplayName = EditorFeaturesResources.Unnecessary_Code;
            this.ForegroundOpacity = 0.6;
        }
    }
}
