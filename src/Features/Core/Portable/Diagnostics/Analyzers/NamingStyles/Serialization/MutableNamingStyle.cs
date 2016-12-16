// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class MutableNamingStyle
    {
        private NamingStyle _namingStyle;

        public Guid ID => _namingStyle.ID;

        public string Name
        {
            get => _namingStyle.Name;
            set => _namingStyle = _namingStyle.With(name: value);
        }

        public string Prefix
        {
            get => _namingStyle.Prefix;
            set => _namingStyle = _namingStyle.With(prefix: value);
        }

        public string Suffix
        {
            get => _namingStyle.Suffix;
            set => _namingStyle = _namingStyle.With(suffix: value);
        }

        public string WordSeparator
        {
            get => _namingStyle.WordSeparator;
            set => _namingStyle = _namingStyle.With(wordSeparator: value);
        }

        public Capitalization CapitalizationScheme
        {
            get => _namingStyle.CapitalizationScheme;
            set => _namingStyle = _namingStyle.With(capitalizationScheme: value);
        }

        //public MutableNamingStyle()
        //{
        //    ID = Guid.NewGuid();
        //}

        public MutableNamingStyle()
            : this(new NamingStyle(Guid.NewGuid()))
        {
        }

        public MutableNamingStyle(NamingStyle namingStyle) 
            => _namingStyle = namingStyle;

        public string CreateName(IEnumerable<string> words)
            => _namingStyle.CreateName(words);

        public bool IsNameCompliant(string name, out string failureReason)
            => _namingStyle.IsNameCompliant(name, out failureReason);

        internal MutableNamingStyle Clone()
            => new MutableNamingStyle(_namingStyle);

        public IEnumerable<string> MakeCompliant(string name)
            => _namingStyle.MakeCompliant(name);

        internal XElement CreateXElement()
            => _namingStyle.CreateXElement();

        internal static MutableNamingStyle FromXElement(XElement namingStyleElement)
            => new MutableNamingStyle(NamingStyle.FromXElement(namingStyleElement));
    }
}