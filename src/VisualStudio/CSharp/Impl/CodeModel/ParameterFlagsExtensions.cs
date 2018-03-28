// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal static class ParameterFlagsExtensions
    {
        public static ParameterFlags GetParameterFlags(this ParameterSyntax parameter)
        {
            ParameterFlags result = 0;

            foreach (var modifier in parameter.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.RefKeyword:
                        result |= ParameterFlags.Ref;
                        break;
                    case SyntaxKind.OutKeyword:
                        result |= ParameterFlags.Out;
                        break;
                    case SyntaxKind.ParamsKeyword:
                        result |= ParameterFlags.Params;
                        break;
                }
            }

            return result;
        }
    }
}
