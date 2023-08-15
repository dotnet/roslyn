// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class SmartIndentProvider(EditorOptionsService editorOptionsService) : ISmartIndentProvider
    {
        private readonly EditorOptionsService _editorOptionsService = editorOptionsService;

        public ISmartIndent? CreateSmartIndent(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!_editorOptionsService.GlobalOptions.GetOption(SmartIndenterOptionsStorage.SmartIndenter))
            {
                return null;
            }

            return new SmartIndent(textView, _editorOptionsService);
        }
    }
}
