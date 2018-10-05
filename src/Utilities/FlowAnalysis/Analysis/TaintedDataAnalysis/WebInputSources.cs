// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class WebInputSources
    {
        /// <summary>
        /// Metadata for tainted data sources.
        /// </summary>
        /// <remarks>Keys are full type names (namespace + type name), values are the metadatas.</remarks>
        public static ImmutableDictionary<string, SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static WebInputSources()
        {
            ImmutableDictionary<string, SourceInfo>.Builder sourceInfosBuilder =
                ImmutableDictionary.CreateBuilder<string, SourceInfo>(StringComparer.Ordinal);

            AddSource(
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
            AddSource(
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
            AddSource(
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
            AddSource(
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
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIAdaptersPageAdapter,
                taintedProperties: new string[] {
                    "QueryString",    // TODO paulming: This doesn't exist in .NET Framework 4.7.2, what do we actually care about?
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIDataBoundLiteralControl,
                taintedProperties: new string[] {
                    "Text",   // TODO paulming: Test this works for both the interface and type method.
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIDesignerDataBoundLiteralControl,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIHtmlControlsHtmlInputControl,
                taintedProperties: new string[] {
                    "Value",   // TODO paulming: Test that this covers HtmlInputButton.Value and other derived classes.
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIIndexedString,
                taintedProperties: new string[] {
                    "Value" },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUILiteralControl,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIResourceBasedLiteralControl,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUISimplePropertyEntry,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIStateItem,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIStringPropertyBuilder,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUITemplateBuilder,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUITemplateParser,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsBaseValidator,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsBulletedList,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsButton,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsButtonColumn,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsButtonField,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsChangePassword,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCheckBox,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCheckBoxField,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCommandEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsCreateUserWizard,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDataKey,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDataList,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDetailsView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDetailsViewInsertEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsDetailsViewUpdateEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsFormView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsFormViewInsertEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsFormViewUpdateEventArgs,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsGridView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHiddenField,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHyperLink,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHyperLinkColumn,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsHyperLinkField,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsImageButton,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLabel,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLinkButton,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsListControl,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsListItem,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLiteral,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsLogin,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsMenu,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsMenuItem,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsMenuItemBinding,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsPasswordRecovery,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsQueryStringParameter,
                taintedProperties: new string[] {
                    "QueryStringField",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsRadioButtonList,
                taintedProperties: new string[] {
                    "TextAlign",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsServerValidateEventArgs,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTableCell,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTextBox,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTreeNode,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTreeNodeBinding,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsTreeView,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsUnit,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsAppearanceEditorPart,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsPersonalizationEntry,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartConnectVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartDeleteVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditorOKVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartEditVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartExportVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartHelpVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartMinimizeVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartRestoreVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            AddSource(
                sourceInfosBuilder,
                WellKnownTypes.SystemWebUIWebControlsWebPartsWebPartVerb,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);

            SourceInfos = sourceInfosBuilder.ToImmutable();
        }

        private static void AddSource(
            ImmutableDictionary<string, SourceInfo>.Builder builder, 
            string fullTypeName, 
            string[] taintedProperties,
            string[] taintedMethods)
        {
            SourceInfo metadata = new SourceInfo(
                fullTypeName,
                taintedProperties != null 
                    ? ImmutableHashSet.Create<string>(StringComparer.Ordinal, taintedProperties)
                    : ImmutableHashSet<string>.Empty,
                taintedMethods != null 
                    ? ImmutableHashSet.Create<string>(StringComparer.Ordinal, taintedMethods)
                    : ImmutableHashSet<string>.Empty);
            builder.Add(metadata.FullTypeName, metadata);
        }

        /// <summary>
        /// Determines if the instance property reference generates tainted data.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known types cache.</param>
        /// <param name="propertyReferenceOperation">IOperation representing the property reference.</param>
        /// <returns>True if the property returns tainted data, false otherwise.</returns>
        public static bool IsTaintedProperty(WellKnownTypeProvider wellKnownTypeProvider, IPropertyReferenceOperation propertyReferenceOperation)
        {
            return propertyReferenceOperation != null
                && propertyReferenceOperation.Instance != null
                && propertyReferenceOperation.Member != null
                && wellKnownTypeProvider.TryGetFullTypeName(propertyReferenceOperation.Instance.Type, out string instanceType)
                && SourceInfos.TryGetValue(instanceType, out SourceInfo sourceMetadata)
                && sourceMetadata.TaintedProperties.Contains(propertyReferenceOperation.Member.MetadataName);
        }

        /// <summary>
        /// Determines if the instance method call returns tainted data.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known types cache.</param>
        /// <param name="instance">IOperation representing the instance.</param>
        /// <param name="method">Instance method being called.</param>
        /// <returns>True if the method returns tainted data, false otherwise.</returns>
        public static bool IsTaintedMethod(WellKnownTypeProvider wellKnownTypeProvider, IOperation instance, IMethodSymbol method)
        {
            return instance != null
                && instance.Type != null
                && method != null
                && wellKnownTypeProvider.TryGetFullTypeName(instance.Type, out string instanceType)
                && SourceInfos.TryGetValue(instanceType, out SourceInfo sourceMetadata)
                && sourceMetadata.TaintedMethods.Contains(method.MetadataName);
        }

        /// <summary>
        /// Determines if the compilation (via its <see cref="WellKnownTypeProvider"/>) references a tainted data source type.
        /// </summary>
        /// <param name="wellKnownTypeProvider">Well known type provider to check.</param>
        /// <returns>True if the compilation references at least one tainted data source type.</returns>
        public static bool DoesCompilationIncludeSources(WellKnownTypeProvider wellKnownTypeProvider)
        {
            foreach (string metadataTypeName in SourceInfos.Keys)
            {
                if (wellKnownTypeProvider.TryGetType(metadataTypeName, out INamedTypeSymbol unused))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
