// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
    public sealed class CSharpSerializableParseOptions : SerializableParseOptions
    {
        private readonly CSharpParseOptions _options;

        public CSharpSerializableParseOptions(CSharpParseOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            _options = options;
        }

        public new ParseOptions Options
        {
            get { return _options; }
        }

        protected override ParseOptions CommonOptions
        {
            get { return _options; }
        }

        private CSharpSerializableParseOptions(SerializationInfo info, StreamingContext context)
        {
            _options = new CSharpParseOptions(
                languageVersion: (LanguageVersion)info.GetValue("LanguageVersion", typeof(LanguageVersion)),
                documentationMode: (DocumentationMode)info.GetValue("DocumentationMode", typeof(DocumentationMode)),
                kind: (SourceCodeKind)info.GetValue("Kind", typeof(SourceCodeKind)),
                preprocessorSymbols: info.GetArray<string>("PreprocessorSymbols"));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            CommonGetObjectData(_options, info, context);

            info.AddValue("LanguageVersion", _options.LanguageVersion, typeof(LanguageVersion));
            info.AddArray("PreprocessorSymbols", _options.PreprocessorSymbols);
        }
    }
}
