// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.MethodXml
{
    internal abstract partial class AbstractMethodXmlBuilder
    {
        private class AutoTag : IDisposable
        {
            private readonly AbstractMethodXmlBuilder _xmlBuilder;
            private readonly string _name;

            public AutoTag(AbstractMethodXmlBuilder xmlBuilder, string name, AttributeInfo[] attributes)
            {
                _xmlBuilder = xmlBuilder;
                _name = name;

                _xmlBuilder.AppendOpenTag(name, attributes);
            }

            public void Dispose()
                => _xmlBuilder.AppendCloseTag(_name);
        }
    }
}
