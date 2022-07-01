// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    internal sealed class SmartIndentProvider : ISmartIndentProvider
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly IEditorOptionsFactoryService _editorOptionsFactory;
        private readonly IIndentationManagerService _indentationManager;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SmartIndentProvider(IGlobalOptionService globalOptions, IEditorOptionsFactoryService editorOptionsFactory, IIndentationManagerService indentationManager)
        {
            _globalOptions = globalOptions;
            _editorOptionsFactory = editorOptionsFactory;
            _indentationManager = indentationManager;
        }

        public ISmartIndent? CreateSmartIndent(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!_globalOptions.GetOption(InternalFeatureOnOffOptions.SmartIndenter))
            {
                return null;
            }

            return new SmartIndent(textView, _globalOptions, _editorOptionsFactory, _indentationManager);
        }
    }
}
