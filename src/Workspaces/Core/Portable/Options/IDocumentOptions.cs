// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Returned from a <see cref="IDocumentOptionsProvider"/>
    /// </summary>
    interface IDocumentOptions
    {
        bool TryGetDocumentOption(Document document, OptionKey option, OptionSet underlyingOptions, out object value);
    }
}
