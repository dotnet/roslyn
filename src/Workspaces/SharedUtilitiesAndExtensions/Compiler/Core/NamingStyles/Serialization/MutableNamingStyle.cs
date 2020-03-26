// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
