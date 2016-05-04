// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    // TODO: split these into VB and C# options
    internal partial class OrganizerOptions
    {
        public const string FeatureName = "Organizer";

        public static PerLanguageOption<bool> PlaceSystemNamespaceFirst
        {
            get { return Editing.GenerationOptions.PlaceSystemNamespaceFirst; }
        }
    }
}
