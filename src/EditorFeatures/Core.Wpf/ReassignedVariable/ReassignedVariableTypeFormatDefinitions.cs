// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReassignedVariable
{
    internal sealed class ReassignedVariableTypeFormatDefinitions
    {
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ReassignedVariableClassificationTypeDefinitions.ReassignedVariable)]
        [Name(ReassignedVariableClassificationTypeDefinitions.ReassignedVariable)]
        [Order(After = Priority.High)]
        [UserVisible(false)]
        [ExcludeFromCodeCoverage]
        private class ReassignedVariableFormatDefinition : ClassificationFormatDefinition
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public ReassignedVariableFormatDefinition()
            {
                this.DisplayName = EditorFeaturesResources.Reassigned_variable;
                this.TextDecorations = System.Windows.TextDecorations.Underline;
            }
        }
    }
}
