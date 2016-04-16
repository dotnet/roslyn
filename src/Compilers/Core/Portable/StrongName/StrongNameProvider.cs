// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides strong name and signs source assemblies.
    /// </summary>
    public abstract class StrongNameProvider
    {
        protected StrongNameProvider()
        {
        }

        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();

        internal abstract Stream CreateInputStream();

        internal abstract StrongNameKeys CreateKeys(string keyFilePath, string keyContainerName, CommonMessageProvider messageProvider);

        internal abstract void SignAssembly(StrongNameKeys keys, Stream inputStream, Stream outputStream);
    }
}
