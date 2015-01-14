// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.Services.OptionService
{
    internal struct OptionKeyAndLanguage : IEquatable<OptionKeyAndLanguage>
    {
        public readonly OptionKey OptionKey;
        public readonly string Language;

        public OptionKeyAndLanguage(OptionKey optionKey, string language)
        {
            this.OptionKey = optionKey;
            this.Language = language;
        }

        public override bool Equals(object obj)
        {
            if (obj is OptionKeyAndLanguage)
            {
                return Equals((OptionKeyAndLanguage)obj);
            }

            return false;
        }

        public bool Equals(OptionKeyAndLanguage other)
        {
            return OptionKey == other.OptionKey && Language == other.Language;
        }

        public override int GetHashCode()
        {
            var hash = OptionKey.GetHashCode();

            if (Language != null)
            {
                hash = Hash.Combine(Language.GetHashCode(), hash);
            }

            return hash;
        }

        public static bool operator ==(OptionKeyAndLanguage x, OptionKeyAndLanguage y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(OptionKeyAndLanguage x, OptionKeyAndLanguage y)
        {
            return !x.Equals(y);
        }
    }
}
