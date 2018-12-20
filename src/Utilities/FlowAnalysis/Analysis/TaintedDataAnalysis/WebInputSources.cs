// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

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
            var sourceInfosBuilder = PooledHashSet<SourceInfo>.GetInstance();

            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebHttpCookie,
                isInterface: false,
                taintedProperties: new string[] {
                    "Domain",
                    "Name",
                    "Item",
                    "Path",
                    "Value",
                    "Values",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebHttpRequest,
                isInterface: false,
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
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebHttpRequestBase,
                isInterface: false,
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
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebHttpRequestWrapper,
                isInterface: false,
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
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIAdaptersPageAdapter,
                isInterface: false,
                taintedProperties: new string[] {
                    "QueryString",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIDataBoundLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIDesignerDataBoundLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIHtmlControlsHtmlInputControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIIndexedString,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value" },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUILiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIResourceBasedLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUISimplePropertyEntry,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIStateItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIStringPropertyBuilder,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUITemplateBuilder,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUITemplateParser,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsBaseValidator,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsBulletedList,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsButtonColumn,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsButtonField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsChangePassword,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsCheckBox,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsCheckBoxField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsCommandEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsCreateUserWizard,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsDataKey,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsDataList,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsDetailsView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsDetailsViewInsertEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsDetailsViewUpdateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsFormView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsFormViewInsertEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsFormViewUpdateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsGridView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsHiddenField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsHyperLink,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsHyperLinkColumn,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsHyperLinkField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsImageButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsLabel,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsLinkButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsListControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsListItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsLiteral,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsLogin,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsMenu,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsMenuItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsMenuItemBinding,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsPasswordRecovery,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsQueryStringParameter,
                isInterface: false,
                taintedProperties: new string[] {
                    "QueryStringField",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsRadioButtonList,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsServerValidateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsTableCell,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsTextBox,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsTreeNode,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsTreeNodeBinding,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsTreeView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsUnit,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsAppearanceEditorPart,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsPersonalizationEntry,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartDeleteVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorOKVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartExportVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHelpVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartMinimizeVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartRestoreVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypes.SystemWebUIITextControl,
                isInterface: true,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            SourceInfos = sourceInfosBuilder.ToImmutableAndFree();
        }
    }
}
