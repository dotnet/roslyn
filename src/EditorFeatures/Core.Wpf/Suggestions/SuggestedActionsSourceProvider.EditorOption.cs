// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Utilities.BaseUtility;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    internal partial class SuggestedActionsSourceProvider
    {
        /// <summary>
        /// Method to enable quick actions from <see cref="SuggestedActionsSourceProvider"/>.
        /// </summary>
        /// <remarks>This method must be invoked on the UI thread.</remarks>
        public static void Enable(IEditorOptionsFactoryService editorOptionsFactory)
            => editorOptionsFactory.GlobalOptions.SetOptionValue(EditorOption.OptionName, true);

        /// <summary>
        /// Editor option to support lazy creation of <see cref="SuggestedActionsSourceProvider"/>
        /// for enabling quick actions for documents.
        /// See https://github.com/dotnet/roslyn/issues/62877#issuecomment-1271493105 for more details.
        /// </summary>
        [Export(typeof(EditorOptionDefinition))]
        [Name(OptionName)]
        [DefaultEditorOptionValue(false)]
        private sealed class EditorOption : EditorOptionDefinition<bool>
        {
            private static readonly EditorOptionKey<bool> s_optionKey = new(OptionName);
            public const string OptionName = $"{nameof(SuggestedActionsSourceProvider)}.{nameof(EditorOption)}";

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public EditorOption()
            {
            }

            public override bool Default => false;

            public override EditorOptionKey<bool> Key => s_optionKey;
        }
    }
}
