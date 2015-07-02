// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editing
{
    internal class GenerationOptions
    {
        public const string FeatureName = "Organizer";

        public static readonly PerLanguageOption<bool> PlaceSystemNamespaceFirst = new PerLanguageOption<bool>(FeatureName, "PlaceSystemNamespaceFirst", defaultValue: true);
    }
}
