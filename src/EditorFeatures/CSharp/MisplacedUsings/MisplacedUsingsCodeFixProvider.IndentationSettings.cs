// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    internal partial class MisplacedUsingsCodeFixProvider
    {
        internal class IndentationSettings
        {
            public int IndentationSize { get; }
            public int TabSize { get; }
            public bool UseTabs { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="IndentationSettings"/> class.
            /// </summary>
            protected internal IndentationSettings()
            {
                this.IndentationSize = 4;
                this.TabSize = 4;
                this.UseTabs = false;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="IndentationSettings"/> class.
            /// </summary>
            protected internal IndentationSettings(OptionSet options)
                : this()
            {
                this.TabSize = options.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp);
                this.IndentationSize = options.GetOption(FormattingOptions.IndentationSize, LanguageNames.CSharp);
                this.UseTabs = options.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp);
            }
        }
    }
}
