// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Roslyn.VisualStudio.IntegrationTests.Fixtures
{
    public class VisualBasicClassLibraryProjectFixture : ClassLibraryProjectFixture
    {
        public VisualBasicClassLibraryProjectFixture()
            : base(LanguageNames.VisualBasic)
        {
        }
    }
}
