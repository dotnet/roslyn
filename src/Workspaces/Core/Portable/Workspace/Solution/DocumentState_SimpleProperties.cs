﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        public HostLanguageServices LanguageServices => _languageServices;

        public ParseOptions ParseOptions => _options;

        public SourceCodeKind SourceCodeKind
        {
            get
            {
                return this.ParseOptions == null ? SourceCodeKind.Regular : this.ParseOptions.Kind;
            }
        }

        public bool IsGenerated
        {
            get { return this.info.IsGenerated; }
        }
    }
}
