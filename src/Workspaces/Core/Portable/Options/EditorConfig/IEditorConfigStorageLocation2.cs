// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigStorageLocation2 : IEditorConfigStorageLocation
    {
        /// <summary>
        /// Gets the editorconfig string representation "key = value" for this storage location.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="optionSet"></param>
        /// <returns></returns>
        string GetEditorConfigString(object value, OptionSet optionSet);
    }
}
