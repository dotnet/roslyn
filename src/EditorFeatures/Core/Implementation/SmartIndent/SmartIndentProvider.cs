// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class SmartIndentProvider : ISmartIndentProvider
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public SmartIndentProvider()
        {
        }

        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!textView.TextBuffer.GetFeatureOnOffOption(InternalFeatureOnOffOptions.SmartIndenter))
            {
                return null;
            }

            return new SmartIndent(textView);
        }
    }
}
