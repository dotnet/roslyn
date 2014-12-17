// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// An action that make changes to a declaration node within a <see cref="SyntaxEditor"/>.
    /// </summary>
    /// <param name="editor">The <see cref="SyntaxEditor"/> to apply edits to.</param>
    /// <param name="declaration">The declaration to edit.</param>
    /// <returns></returns>
    public delegate void DeclarationEditAction(SyntaxEditor editor, SyntaxNode declaration);
}
