// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        public DocumentId Id
        {
            get { return this.info.Id; }
        }

        public string FilePath
        {
            get { return this.info.FilePath; }
        }

        public HostLanguageServices LanguageServices
        {
            get { return this.languageServices; }
        }

        public DocumentInfo Info
        {
            get { return this.info; }
        }

        public IReadOnlyList<string> Folders
        {
            get { return this.info.Folders; }
        }

        public string Name
        {
            get { return this.info.Name; }
        }

        public ParseOptions ParseOptions
        {
            get { return this.options; }
        }

        public SourceCodeKind SourceCodeKind
        {
            get
            {
                return this.ParseOptions == null ? SourceCodeKind.Regular : this.ParseOptions.Kind;
            }
        }
    }
}