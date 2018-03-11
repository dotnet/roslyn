// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class MutableNamingStyle
    {
        public NamingStyle NamingStyle { get; private set; }

        public Guid ID => NamingStyle.ID;

        public string Name
        {
            get => NamingStyle.Name;
            set => NamingStyle = NamingStyle.With(name: value);
        }

        public string Prefix
        {
            get => NamingStyle.Prefix;
            set => NamingStyle = NamingStyle.With(prefix: value);
        }

        public string Suffix
        {
            get => NamingStyle.Suffix;
            set => NamingStyle = NamingStyle.With(suffix: value);
        }

        public string WordSeparator
        {
            get => NamingStyle.WordSeparator;
            set => NamingStyle = NamingStyle.With(wordSeparator: value);
        }

        public Capitalization CapitalizationScheme
        {
            get => NamingStyle.CapitalizationScheme;
            set => NamingStyle = NamingStyle.With(capitalizationScheme: value);
        }

        public MutableNamingStyle()
            : this(new NamingStyle(Guid.NewGuid()))
        {
        }

        public MutableNamingStyle(NamingStyle namingStyle)
            => NamingStyle = namingStyle;

        internal MutableNamingStyle Clone()
            => new MutableNamingStyle(NamingStyle);
    }
}
