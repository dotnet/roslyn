// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocationWithKey : IEditorConfigStorageLocation
    {
        string KeyName { get; }
        string GetEditorConfigStringForValue(object value, OptionSet optionSet);
    }
}
