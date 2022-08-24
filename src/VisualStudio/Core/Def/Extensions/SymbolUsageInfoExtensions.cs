// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.CodeAnalysis
{
    internal static class SymbolUsageInfoExtensions
    {
        /// <summary>
        /// Converts our internal symbol usage info representation to platform layer's representation
        /// <see cref="Microsoft.VisualStudio.Shell.TableManager.SymbolReferenceKinds"/>
        /// </summary>
        public static SymbolReferenceKinds ToSymbolReferenceKinds(this SymbolUsageInfo symbolUsageInfo)
        {
            var kinds = SymbolReferenceKinds.None;

            if (symbolUsageInfo.ValueUsageInfoOpt.HasValue)
            {
                var usageInfo = symbolUsageInfo.ValueUsageInfoOpt.Value;
                if (usageInfo.IsReadFrom())
                {
                    kinds |= SymbolReferenceKinds.Read;
                }

                if (usageInfo.IsWrittenTo())
                {
                    kinds |= SymbolReferenceKinds.Write;
                }

                if (usageInfo.IsReference())
                {
                    kinds |= SymbolReferenceKinds.Reference;
                }

                if (usageInfo.IsNameOnly())
                {
                    kinds |= SymbolReferenceKinds.Name;
                }
            }

            if (symbolUsageInfo.TypeOrNamespaceUsageInfoOpt.HasValue)
            {
                var usageInfo = symbolUsageInfo.TypeOrNamespaceUsageInfoOpt.Value;
                if ((usageInfo & TypeOrNamespaceUsageInfo.Qualified) != 0)
                {
                    kinds |= SymbolReferenceKinds.Qualified;
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.TypeArgument) != 0)
                {
                    kinds |= SymbolReferenceKinds.TypeArgument;
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.TypeConstraint) != 0)
                {
                    kinds |= SymbolReferenceKinds.TypeConstraint;
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.Base) != 0)
                {
                    kinds |= SymbolReferenceKinds.BaseType;
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.ObjectCreation) != 0)
                {
                    kinds |= SymbolReferenceKinds.Construct;
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.Import) != 0)
                {
                    kinds |= SymbolReferenceKinds.Import;
                }

                if ((usageInfo & TypeOrNamespaceUsageInfo.NamespaceDeclaration) != 0)
                {
                    kinds |= SymbolReferenceKinds.Declare;
                }
            }

            return kinds;
        }
    }
}
