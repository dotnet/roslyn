// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class CSharpResultProvider : ResultProvider
    {
        public CSharpResultProvider()
            : this(new CSharpFormatter())
        {
        }

        private CSharpResultProvider(CSharpFormatter formatter)
            : this(formatter, formatter)
        {
        }

        internal CSharpResultProvider(IDkmClrFormatter2 formatter2, IDkmClrFullNameProvider fullNameProvider)
            : base(formatter2, fullNameProvider)
        {
        }

        internal override string StaticMembersString
        {
            get { return Resources.StaticMembers; }
        }

        internal override bool IsPrimitiveType(Type type)
        {
            return type.IsPredefinedType();
        }

#nullable enable
        internal override bool TryGetMemberDisplay(string metadataName, out bool isGenerated, out string? displayName)
        {
            isGenerated = GeneratedNameParser.TryParseGeneratedName(metadataName, out var kind, out var openBracketOffset, out var closeBracketOffset);
            if (!isGenerated)
            {
                displayName = metadataName;
                return true;
            }

            switch (kind)
            {
                case GeneratedNameKind.PrimaryConstructorParameter:
                    // display the member using the unmangled name:
                    displayName = metadataName.Substring(openBracketOffset + 1, closeBracketOffset - openBracketOffset - 1);
                    return true;

                default:
                    // do not display other generated members:
                    displayName = null;
                    return false;
            }
        }
    }
}
