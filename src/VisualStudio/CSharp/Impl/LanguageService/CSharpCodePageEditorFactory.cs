// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Implementation;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;

[Guid(Guids.CSharpCodePageEditorFactoryIdString)]
internal sealed class CSharpCodePageEditorFactory(AbstractEditorFactory editorFactory) : AbstractCodePageEditorFactory(editorFactory)
{
}
