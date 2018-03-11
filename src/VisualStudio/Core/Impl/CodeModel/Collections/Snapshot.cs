// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    internal abstract class Snapshot
    {
        public abstract int Count { get; }
        public abstract EnvDTE.CodeElement this[int index] { get; }
    }
}
