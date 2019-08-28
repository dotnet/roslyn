// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.QuickInfo
{
    /// <summary>
    /// The set of well known kinds used for the <see cref="QuickInfoSection.Kind"/> property.
    /// These tags influence the presentation of quick info section.
    /// </summary>
    public static class QuickInfoSectionKinds
    {
        public const string Description = nameof(Description);
        public const string DocumentationComments = nameof(DocumentationComments);
        public const string TypeParameters = nameof(TypeParameters);
        public const string AnonymousTypes = nameof(AnonymousTypes);
        public const string Usage = nameof(Usage);
        public const string Exception = nameof(Exception);
        public const string Text = nameof(Text);
        public const string Captures = nameof(Captures);
        internal const string NullabilityAnalysis = nameof(NullabilityAnalysis);
    }
}
