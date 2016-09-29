// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// This interface is provided purely to enable some shared logic that handles multiple kinds of 
    /// metadata that share the Languages property. It should not be used to find exports via MEF,
    /// use LanguageMetadata instead.
    /// </summary>
    internal interface ILanguagesMetadata
    {
        IEnumerable<string> Languages { get; }
    }
}
