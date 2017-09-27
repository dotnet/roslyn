﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal class ClassifiableDeferredContent : IDeferredQuickInfoContent
    {
        internal readonly IList<TaggedText> ClassifiableContent;

        public ClassifiableDeferredContent(
            IList<TaggedText> content)
        {
            this.ClassifiableContent = content;
        }
    }
}
