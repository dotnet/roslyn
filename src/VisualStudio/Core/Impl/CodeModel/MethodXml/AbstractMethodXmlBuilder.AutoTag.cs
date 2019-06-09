// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                _xmlBuilder.AppendCloseTag(_name);
            }
        }
    }
}
