// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    public class SourceFileList : SourceFileCollection
    {
        private readonly string _defaultPrefix;
        private readonly string _defaultExtension;

        public SourceFileList(string defaultPrefix, string defaultExtension)
        {
            _defaultPrefix = defaultPrefix;
            _defaultExtension = defaultExtension;
        }

        public void Add(string content)
        {
            Add(($"{_defaultPrefix}{Count}.{_defaultExtension}", content));
        }

        public void Add(SourceText content)
        {
            Add(($"{_defaultPrefix}{Count}.{_defaultExtension}", content));
        }
    }
}
