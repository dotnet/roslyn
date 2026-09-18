// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;

[Guid(Guids.CSharpCodePageEditorFactoryIdString)]
[ProvideView(LogicalView.Designer, "Form")]
internal sealed class CSharpCodePageEditorFactory(AbstractEditorFactory editorFactory) : AbstractCodePageEditorFactory(editorFactory)
{
}
