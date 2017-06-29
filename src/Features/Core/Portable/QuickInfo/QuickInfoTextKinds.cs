// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal static class QuickInfoTextKinds
    {
        public const string Description = nameof(Description);
        public const string DocumentationComments = nameof(DocumentationComments);
        public const string TypeParameters = nameof(TypeParameters);
        public const string AnonymousTypes = nameof(AnonymousTypes);
        public const string Usage = nameof(Usage);
        public const string Exception = nameof(Exception);
        public const string Text = nameof(Text);
    }
}