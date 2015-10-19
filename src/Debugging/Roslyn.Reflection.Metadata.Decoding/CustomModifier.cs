// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// NOTE: This is a temporary internal copy of code that will be cut from System.Reflection.Metadata v1.1 and
//       ship in System.Reflection.Metadata v1.2 (with breaking changes). Remove and use the public API when
//       a v1.2 prerelease is available and code flow is such that we can start to depend on it.

namespace Roslyn.Reflection.Metadata.Decoding
{
    internal struct CustomModifier<TType>
    {
        private readonly TType _type;
        private readonly bool _isRequired;

        public CustomModifier(TType type, bool isRequired)
        {
            _type = type;
            _isRequired = isRequired;
        }

        public TType Type
        {
            get { return _type; }
        }

        public bool IsRequired
        {
            get { return _isRequired; }
        }
    }
}
