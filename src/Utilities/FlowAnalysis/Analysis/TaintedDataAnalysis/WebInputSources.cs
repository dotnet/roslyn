// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class WebInputSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for web input tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static WebInputSources()
        {
            ImmutableHashSet<SourceInfo>.Builder sourceInfosBuilder = ImmutableHashSet.CreateBuilder<SourceInfo>();

            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebHttpCookie,
                taintedProperties: new string[] {
                    "Domain",
                    "Name",
                    "Item",
                    "Path",
                    "Value",
                    "Values",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebHttpRequest,
                taintedProperties: new string[] {
                    "AcceptTypes",
                    "AnonymousID",
                    // Anything potentially bad in Browser?
                    "ContentType",
                    "Cookies",
                    "Files",
                    "Form",
                    "Headers",
                    "HttpMethod",
                    "InputStream",
                    "Item",
                    "Params",
                    "Path",
                    "PathInfo",
                    "QueryString",
                    "RawUrl",
                    "RequestType",
                    "Url",
                    "UrlReferrer",
                    "UserAgent",
                    "UserLanguages",
                },
                taintedMethods: new string[] {
                    "BinaryRead",
                    "GetBufferedInputStream",
                    "GetBufferlessInputStream",
                });
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebHttpRequestBase,
                taintedProperties: new string[] {
                    "AcceptTypes",
                    "AnonymousID",
                    // Anything potentially bad in Browser?
                    "ContentType",
                    "Cookies",
                    "Files",
                    "Form",
                    "Headers",
                    "HttpMethod",
                    "InputStream",
                    "Item",
                    "Params",
                    "Path",
                    "PathInfo",
                    "QueryString",
                    "RawUrl",
                    "RequestType",
                    "Url",
                    "UrlReferrer",
                    "UserAgent",
                    "UserLanguages",
                },
                taintedMethods: new string[] {
                    "BinaryRead",
                    "GetBufferedInputStream",
                    "GetBufferlessInputStream",
                });
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebHttpRequestWrapper,
                taintedProperties: new string[] {
                    "AcceptTypes",
                    "AnonymousID",
                    // Anything potentially bad in Browser?
                    "ContentType",
                    "Cookies",
                    "Files",
                    "Form",
                    "Headers",
                    "HttpMethod",
                    "InputStream",
                    "Item",
                    "Params",
                    "Path",
                    "PathInfo",
                    "QueryString",
                    "RawUrl",
                    "RequestType",
                    "Url",
                    "UrlReferrer",
                    "UserAgent",
                    "UserLanguages",
                },
                taintedMethods: new string[] {
                    "BinaryRead",
                    "GetBufferedInputStream",
                    "GetBufferlessInputStream",
                });
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIAdaptersPageAdapter,
                taintedProperties: new string[] {
                    "QueryString",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIDataBoundLiteralControl,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIDesignerDataBoundLiteralControl,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIHtmlControlsHtmlInputControl,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIIndexedString,
                taintedProperties: new string[] {
                    "Value" },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUILiteralControl,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIResourceBasedLiteralControl,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUISimplePropertyEntry,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIStateItem,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIStringPropertyBuilder,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUITemplateBuilder,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUITemplateParser,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsBaseValidator,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsBulletedList,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsButton,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsButtonColumn,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsButtonField,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsChangePassword,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCheckBox,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCheckBoxField,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCommandEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCreateUserWizard,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDataKey,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDataList,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDetailsView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDetailsViewInsertEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDetailsViewUpdateEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsFormView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsFormViewInsertEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsFormViewUpdateEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsGridView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHiddenField,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHyperLink,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHyperLinkColumn,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHyperLinkField,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsImageButton,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLabel,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLinkButton,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsListControl,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsListItem,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLiteral,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLogin,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsMenu,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsMenuItem,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsMenuItemBinding,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsPasswordRecovery,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsQueryStringParameter,
                taintedProperties: new string[] {
                    "QueryStringField",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsRadioButtonList,
                taintedProperties: new string[] {
                    "TextAlign",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsServerValidateEventArgs,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTableCell,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTextBox,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTreeNode,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTreeNodeBinding,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTreeView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsUnit,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsAppearanceEditorPart,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsPersonalizationEntry,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartDeleteVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorOKVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartExportVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHelpVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartMinimizeVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartRestoreVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddConcreteSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddInterfaceSource(
                sourceInfosBuilder,
                "System.Web.UI.ITextControl",
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            SourceInfos = sourceInfosBuilder.ToImmutable();
        }

        private static void AddConcreteSource(
            ImmutableHashSet<SourceInfo>.Builder builder, 
            string fullTypeName, 
            string[] taintedProperties,
            string[] taintedMethods)
        {
            AddSource(builder, fullTypeName, false, taintedProperties, taintedMethods);
        }

        private static void AddInterfaceSource(
            ImmutableHashSet<SourceInfo>.Builder builder,
            string fullTypeName,
            string[] taintedProperties,
            string[] taintedMethods)
        {
            AddSource(builder, fullTypeName, true, taintedProperties, taintedMethods);
        }

        private static void AddSource(
            ImmutableHashSet<SourceInfo>.Builder builder,
            string fullTypeName,
            bool isInterface,
            string[] taintedProperties,
            string[] taintedMethods)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                isInterface: isInterface,
                taintedProperties: taintedProperties != null
                    ? ImmutableHashSet.Create<string>(StringComparer.Ordinal, taintedProperties)
                    : ImmutableHashSet<string>.Empty,
                taintedMethods: taintedMethods != null
                    ? ImmutableHashSet.Create<string>(StringComparer.Ordinal, taintedMethods)
                    : ImmutableHashSet<string>.Empty);
            builder.Add(metadata);
        }
    }
}
