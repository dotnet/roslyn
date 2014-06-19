// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
    public sealed class CSharpSerializableParseOptions : SerializableParseOptions
    {
        private readonly CSharpParseOptions options;

        public CSharpSerializableParseOptions(CSharpParseOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            this.options = options;
        }

        public new ParseOptions Options
        {
            get { return options; }
        }

        protected override ParseOptions CommonOptions
        {
            get { return options; }
        }

        private CSharpSerializableParseOptions(SerializationInfo info, StreamingContext context)
        {
            this.options = new CSharpParseOptions(
                languageVersion: (LanguageVersion)info.GetValue("LanguageVersion", typeof(LanguageVersion)),
                documentationMode: (DocumentationMode)info.GetValue("DocumentationMode", typeof(DocumentationMode)),
                kind: (SourceCodeKind)info.GetValue("Kind", typeof(SourceCodeKind)),
                preprocessorSymbols: info.GetArray<string>("PreprocessorSymbols"));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            CommonGetObjectData(options, info, context);

            info.AddValue("LanguageVersion", options.LanguageVersion, typeof(LanguageVersion));
            info.AddArray("PreprocessorSymbols", options.PreprocessorSymbols);
        }
    }
}
