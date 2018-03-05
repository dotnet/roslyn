// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    /// <summary>
    /// A test-only implementation of ITextManagerAdapter used for testing CodeModel.
    /// </summary>
    internal partial class MockTextManagerAdapter : ITextManagerAdapter
    {
        public EnvDTE.TextPoint CreateTextPoint(FileCodeModel fileCodeModel, VirtualTreePoint point)
        {
            return new TextPoint(point);
        }
    }
}
