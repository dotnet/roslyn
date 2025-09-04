// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class WebInputSources
    {
        /// <summary>
        /// <see cref="SourceInfo"/>s for web input tainted data sources.
        /// </summary>
        public static ImmutableHashSet<SourceInfo> SourceInfos { get; }

        /// <summary>
        /// Cached information if the specified symbol is a Asp.Net Core Controller: (compilation) -> ((class symbol) -> (is Controller))
        /// </summary>
        private static readonly BoundedCacheWithFactory<Compilation, ConcurrentDictionary<INamedTypeSymbol, bool>> s_classIsControllerByCompilation = new();

        /// <summary>
        /// Statically constructs.
        /// </summary>
        static WebInputSources()
        {
            var dependencyFullTypeNames = ImmutableArray.Create(WellKnownTypeNames.MicrosoftAspNetCoreMvcControllerBase,
                                                                WellKnownTypeNames.MicrosoftAspNetCoreMvcControllerAttribute,
                                                                WellKnownTypeNames.MicrosoftAspNetCoreMvcNonControllerAttribute,
                                                                WellKnownTypeNames.MicrosoftAspNetCoreMvcNonActionAttribute,
                                                                WellKnownTypeNames.MicrosoftAspNetCoreMvcFromServicesAttribute);

            var sourceInfosBuilder = PooledHashSet<SourceInfo>.GetInstance();

            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.MicrosoftAspNetCoreHttpHttpRequest,
                isInterface: false,
                taintedProperties: new string[] {
                    "Body",
                    "ContentType",
                    "Cookies",
                    "Form",
                    "Headers",
                    "Host",
                    "Method",
                    "Path",
                    "PathBase",
                    "Protocol",
                    "Query",
                    "QueryString",
                    "RouteValues",
                    "Scheme",
                },
                taintedMethods: new string[] {
                    "ReadFormAsync",
                });

            sourceInfosBuilder.AddSourceInfoSpecifyingTaintedTargets(
                WellKnownTypeNames.SystemWebHttpServerUtility,
                isInterface: false,
                taintedProperties: null,
                taintedMethodsNeedsPointsToAnalysis: null,
                taintedMethodsNeedsValueContentAnalysis: null,
                transferMethods: new (MethodMatcher, (string, string)[])[]{
                    (
                        (methodName, arguments) =>
                            methodName == "HtmlEncode" &&
                            arguments.Length == 2,
                        new (string, string)[]{
                            ("s", "output"),
                        }
                    )
                });

            sourceInfosBuilder.AddSourceInfo(
                // checking all System.Object derived types is expensive, so it first checks if MicrosoftAspNetCoreMvcControllerBase is resolvable
                dependencyFullTypeNames,
                WellKnownTypeNames.SystemObject,
                 new ParameterMatcher[]{
                    (parameter, wellKnownTypeProvider) => {
                        if (parameter.ContainingSymbol is not IMethodSymbol methodSymbol
                            || methodSymbol.ContainingSymbol is not INamedTypeSymbol typeSymbol)
                        {
                            return false;
                        }

                        var classCache = s_classIsControllerByCompilation.GetOrCreateValue(wellKnownTypeProvider.Compilation, (compilation) => new ConcurrentDictionary<INamedTypeSymbol, bool>());
                        if (!classCache.TryGetValue(typeSymbol, out bool isController))
                        {
                            if ((!typeSymbol.GetBaseTypesAndThis().Any(x => x.Name.EndsWith("Controller", System.StringComparison.Ordinal))
                                && (!typeSymbol.HasDerivedTypeAttribute(wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftAspNetCoreMvcControllerAttribute))))
                                || typeSymbol.HasDerivedTypeAttribute(wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftAspNetCoreMvcNonControllerAttribute)))
                            {
                                isController = false;
                            }
                            else
                            {
                                isController = true;
                            }

                            classCache.TryAdd(typeSymbol, isController);
                        }

                        if (!isController)
                        {
                            return false;
                        }

                        if (methodSymbol.DeclaredAccessibility != Accessibility.Public
                            || methodSymbol.IsConstructor()
                            || methodSymbol.IsStatic
                            || methodSymbol.MethodKind != MethodKind.Ordinary
                            || methodSymbol.HasDerivedMethodAttribute(wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftAspNetCoreMvcNonActionAttribute)))
                        {
                            return false;
                        }

                        if (parameter.HasAnyAttribute(wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftAspNetCoreMvcFromServicesAttribute)))
                        {
                            return false;
                        }

                        return true;
                    }
                 });
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebHttpCookie,
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
                WellKnownTypeNames.SystemWebHttpRequest,
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
                WellKnownTypeNames.SystemWebHttpRequestBase,
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
                WellKnownTypeNames.SystemWebHttpRequestWrapper,
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
                WellKnownTypeNames.SystemWebUIAdaptersPageAdapter,
                isInterface: false,
                taintedProperties: new string[] {
                    "QueryString",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIDataBoundLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIDesignerDataBoundLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIHtmlControlsHtmlInputControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIIndexedString,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value" },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUILiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIResourceBasedLiteralControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUISimplePropertyEntry,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIStateItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIStringPropertyBuilder,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUITemplateBuilder,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUITemplateParser,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsBaseValidator,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsBulletedList,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsButtonColumn,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsButtonField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsChangePassword,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsCheckBox,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsCheckBoxField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsCommandEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsCreateUserWizard,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsDataKey,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsDataList,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsDetailsView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsDetailsViewInsertEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsDetailsViewUpdateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsFormView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsFormViewInsertEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsFormViewUpdateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsGridView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsHiddenField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsHyperLink,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsHyperLinkColumn,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsHyperLinkField,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsImageButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsLabel,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsLinkButton,
                isInterface: false,
                taintedProperties: new string[] {
                    "CommandArgument",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsListControl,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsListItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsLiteral,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsLogin,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsMenu,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsMenuItem,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsMenuItemBinding,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsPasswordRecovery,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextBoxStyle",
                    "TextLayout",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsQueryStringParameter,
                isInterface: false,
                taintedProperties: new string[] {
                    "QueryStringField",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsRadioButtonList,
                isInterface: false,
                taintedProperties: new string[] {
                    "TextAlign",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsServerValidateEventArgs,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsTableCell,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsTextBox,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsTreeNode,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsTreeNodeBinding,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                    "TextField",
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsTreeView,
                isInterface: false,
                taintedProperties: new string[] {
                    "SelectedValue",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsUnit,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsAppearanceEditorPart,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsPersonalizationEntry,
                isInterface: false,
                taintedProperties: new string[] {
                    "Value",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartCatalogAddVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartCatalogCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartConnectionsCancelVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartConnectionsCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartConnectionsConfigureVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartConnectionsConnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartConnectionsDisconnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartConnectVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartDeleteVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartEditorApplyVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartEditorCancelVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartEditorOKVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartEditVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartExportVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartHeaderCloseVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartHelpVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartMinimizeVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartRestoreVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIWebControlsWebPartsWebPartVerb,
                isInterface: false,
                taintedProperties: new string[] {
                    "Text",
                },
                taintedMethods: null);
            sourceInfosBuilder.AddSourceInfo(
                WellKnownTypeNames.SystemWebUIITextControl,
                isInterface: true,
                taintedProperties: new string[] {
                    "Text"
                },
                taintedMethods: null);
            SourceInfos = sourceInfosBuilder.ToImmutableAndFree();
        }
    }
}
