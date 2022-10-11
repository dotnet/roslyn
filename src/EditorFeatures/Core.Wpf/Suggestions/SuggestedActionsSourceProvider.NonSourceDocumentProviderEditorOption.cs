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
        /// Method to enable quick actions inside non-source documents, i.e. <see cref="AdditionalDocument"/> and
        /// <see cref="AnalyzerConfigDocument"/>.
        /// </summary>
        /// <remarks>This method must be invoked on the UI thread.</remarks>
        public static void EnableForNonSourceDocuments(IEditorOptionsFactoryService editorOptionsFactory)
            => editorOptionsFactory.GlobalOptions.SetOptionValue(NonSourceDocumentProviderEditorOption.OptionName, true);

        /// <summary>
        /// Editor option to support lazy creation of <see cref="NonSourceDocumentProvider"/>
        /// for enabling quick actions for non-source documents.
        /// See https://github.com/dotnet/roslyn/issues/62877#issuecomment-1271493105 for more details.
        /// </summary>
        [Export(typeof(EditorOptionDefinition))]
        [Name(OptionName)]
        [DefaultEditorOptionValue(false)]
        private sealed class NonSourceDocumentProviderEditorOption : EditorOptionDefinition<bool>
        {
            private static readonly EditorOptionKey<bool> s_optionKey = new(OptionName);
            public const string OptionName = nameof(NonSourceDocumentProviderEditorOption);

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public NonSourceDocumentProviderEditorOption()
            {
            }

            public override bool Default => false;

            public override EditorOptionKey<bool> Key => s_optionKey;
        }
    }
}
