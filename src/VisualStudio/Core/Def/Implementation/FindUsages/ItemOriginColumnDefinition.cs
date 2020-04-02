// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;


namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    /// <summary>
    /// Custom column to display the item's origin for the Find All References window.
    /// </summary>
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal class ItemOriginColumnDefinition : TableColumnDefinitionBase
    {
        public const string ColumnName = StandardTableKeyNames.ItemOrigin;

        [ImportingConstructor]
        public ItemOriginColumnDefinition()
        {
        }

        public override bool IsFilterable => true;
        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Item_origin; // TODO: Localize

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            if (entry.TryGetValue(Name, out ItemOrigin origin))
            {
                content = origin switch
                {
                    ItemOrigin.Exact => ServicesVSResources.Origin_exact,
                    ItemOrigin.ExactMetadata => ServicesVSResources.Origin_exact_metadata,
                    ItemOrigin.IndexedInRepo => ServicesVSResources.Origin_indexed_repo,
                    ItemOrigin.IndexedInOrganization => ServicesVSResources.Origin_indexed_organization,
                    ItemOrigin.IndexedInThirdParty => ServicesVSResources.Origin_indexed_third_party,
                    _ => ServicesVSResources.Origin_other,
                };
                return true;
            }
            else
            {
                // Assume that items without ItemOrigin are "Exact" matches
                return ServicesVSResources.Exact_origin;
                return true;
            }
        }
    }
}
