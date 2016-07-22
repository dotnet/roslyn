using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal static class FindAllReferencesUtilities
    {
        public static readonly SymbolDisplayFormat DefinitionDisplayFormat =
           new SymbolDisplayFormat(
               typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
               genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
               parameterOptions: SymbolDisplayParameterOptions.IncludeType,
               propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
               delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
               kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword | SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
               localOptions: SymbolDisplayLocalOptions.IncludeType,
               memberOptions:
                   SymbolDisplayMemberOptions.IncludeContainingType |
                   SymbolDisplayMemberOptions.IncludeExplicitInterface |
                   SymbolDisplayMemberOptions.IncludeModifiers |
                   SymbolDisplayMemberOptions.IncludeParameters |
                   SymbolDisplayMemberOptions.IncludeType,
               miscellaneousOptions:
                   SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                   SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    }
}