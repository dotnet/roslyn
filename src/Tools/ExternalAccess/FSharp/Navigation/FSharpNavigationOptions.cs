// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation
{
    internal static class FSharpNavigationOptions
    {
        public static Option<bool> PreferProvisionalTab => NavigationOptions.PreferProvisionalTab;
    }
}
