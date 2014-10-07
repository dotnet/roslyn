// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal class CodeFixProviderMetadata : OrderableMetadata, ILanguagesMetadata
    {
        public object Languages { get; private set; }

        internal IEnumerable<string> LanguagesTyped { get; set; }

        IEnumerable<string> ILanguagesMetadata.Languages
        {
            get
            {
                return LanguagesTyped;
            }
        }

        public CodeFixProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.LanguagesTyped = GetEnumerableMetadata<string>(data, "Languages");
        }

        public CodeFixProviderMetadata(string name, IEnumerable<string> after = null, IEnumerable<string> before = null, params string[] languages)
            : base(name, after, before)
        {
            this.LanguagesTyped = languages;
        }
    }
}