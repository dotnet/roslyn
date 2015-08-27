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

        /// <summary>
        /// This option is currently unused by Roslyn, but we might want to implement it in the 
        /// future. Keeping the option while it's unimplemented allows all upgrade paths to 
        /// maintain any customized value for this setting, even through versions that have not
        /// implemented this feature yet.
        /// </summary>
        public static readonly PerLanguageOption<bool> WarnOnBuildErrors = new PerLanguageOption<bool>(FeatureName, "WarnOnBuildErrors", defaultValue: true);
    }
}
