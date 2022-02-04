// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal enum NameKind
    {
        GenericName,
        QualifiedName,
    }

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract class Name
    {
        internal abstract NameKind Kind { get; }

        internal abstract string GetDebuggerDisplay();
    }

    internal sealed class GenericName : Name
    {
        internal GenericName(QualifiedName qualifiedName, ImmutableArray<string> typeParameters)
        {
            Debug.Assert(qualifiedName != null);
            Debug.Assert(!typeParameters.IsDefault);
            QualifiedName = qualifiedName;
            TypeParameters = typeParameters;
        }

        internal override NameKind Kind => NameKind.GenericName;
        internal QualifiedName QualifiedName { get; }
        internal ImmutableArray<string> TypeParameters { get; }

        internal override string GetDebuggerDisplay()
        {
            var builder = new StringBuilder();
            builder.Append(QualifiedName.GetDebuggerDisplay());
            builder.Append('<');
            bool any = false;
            foreach (var typeParam in TypeParameters)
            {
                if (any)
                {
                    builder.Append(", ");
                }
                builder.Append(typeParam);
                any = true;
            }
            builder.Append('>');
            return builder.ToString();
        }
    }

    internal sealed class QualifiedName : Name
    {
        internal QualifiedName(Name qualifier, string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Qualifier = qualifier;
            Name = name;
        }

        internal override NameKind Kind => NameKind.QualifiedName;
        internal Name Qualifier { get; }
        internal string Name { get; }

        internal override string GetDebuggerDisplay()
        {
            return (Qualifier == null) ? Name : $"{Qualifier.GetDebuggerDisplay()}.{Name}";
        }
    }
}
