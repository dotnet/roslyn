// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.using System;

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    [Export(typeof(EditorOptionDefinition))]
    internal class UseAsyncCompletionOptionDefinition : EditorOptionDefinition
    {
        public const string OptionName = "UseAsyncCompletion";

        /// <summary>
        /// The meaning of this option definition's values:
        /// -1 - user disabled async completion
        ///  0 - no changes from the user; check the experimentation service for whether to use async completion
        ///  1 - user enabled async completion
        /// </summary>
        public override object DefaultValue => 0;

        public override Type ValueType => typeof(int);

        public override string Name => OptionName;
    }
}
