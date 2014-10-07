// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class OptionChangedEventArgs : EventArgs
    {
        private readonly OptionKey optionKey;
        private readonly object value;

        internal OptionChangedEventArgs(OptionKey optionKey, object value)
        {
            this.optionKey = optionKey;
            this.value = value;
        }

        public IOption Option { get { return optionKey.Option; } }
        public string Language { get { return optionKey.Language; } }
        public object Value { get { return value; } }
    }
}
