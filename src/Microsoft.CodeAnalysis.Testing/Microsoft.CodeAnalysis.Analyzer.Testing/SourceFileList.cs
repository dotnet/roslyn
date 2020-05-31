// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
