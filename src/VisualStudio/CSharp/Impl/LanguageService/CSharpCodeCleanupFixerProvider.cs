// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeCleanup
{
    [Export(typeof(ICodeCleanUpFixerProvider))]
    [AppliesToProject("CSharp")]
    [ContentType(ContentTypeNames.CSharpContentType)]
    internal class CSharpCodeCleanUpFixerProvider : AbstractCodeCleanUpFixerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeCleanUpFixerProvider(
            [ImportMany] IEnumerable<Lazy<AbstractCodeCleanUpFixer, ContentTypeMetadata>> codeCleanUpFixers)
            : base(codeCleanUpFixers)
        {
        }
    }
}
