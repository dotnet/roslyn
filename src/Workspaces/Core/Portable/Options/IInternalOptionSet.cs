// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IInternalOptionSet
    {
        IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet);
    }
}
