// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ItemOriginColumnDefinition()
        {
        }

        public override bool IsFilterable => true;
        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Item_origin; // TODO: Localize
        public override bool DefaultVisible => false;

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            if (entry.TryGetValue(Name, out ItemOrigin origin))
            {
                content = origin switch
                {
                    ItemOrigin.Exact => ServicesVSResources.Local,
                    ItemOrigin.ExactMetadata => ServicesVSResources.Local_metadata,
                    ItemOrigin.IndexedInRepo => ServicesVSResources.Indexed_in_repo,
                    ItemOrigin.IndexedInOrganization => ServicesVSResources.Indexed_in_organization,
                    ItemOrigin.IndexedInThirdParty => ServicesVSResources.Indexed_in_third_party,
                    _ => ServicesVSResources.Other,
                };
            }
            else
            {
                // Assume that items without ItemOrigin are "Exact" matches
                content = ServicesVSResources.Local;
            }

            return true;
        }
    }
}
