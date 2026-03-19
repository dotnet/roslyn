// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;

/// <summary>
/// An enum to identify what kind of completion a given Completion
/// represents.
/// </summary>

public enum XamlCompletionKind
{
    /// <summary>
    /// The completion represents a value.
    /// </summary>
    Element,

    /// <summary>
    /// The completion represents an attribute.
    /// </summary>
    Attribute,

    /// <summary>
    /// The completion represents an attribute value.
    /// </summary>
    Value,

    /// <summary>
    /// The completion represents a property element.
    /// </summary>
    PropertyElement,

    /// <summary>
    /// The completion represents a XML namespace prefix.
    /// </summary>
    Prefix,

    /// <summary>
    /// The completion represents a an event.
    /// </summary>
    Event,

    /// <summary>
    /// The completion represents a comment;
    /// </summary>
    Comment,

    /// <summary>
    /// This completion represents a CDATA
    /// </summary>
    CData,

    /// <summary>
    /// The completion represents a processing instruction.
    /// </summary>
    ProcessingInstruction,

    /// <summary>
    /// The completion represents an end tag.
    /// </summary>
    EndTag,

    /// <summary>
    /// The completion represents a type prefix for an attached property 
    /// or property elements (i.e. "Grid.").
    /// </summary>
    TypePrefix,

    /// <summary>
    /// The completion is returned for event handler values indicating
    /// that the language service expects the name of an event handler.
    /// The description of the event handler is found in 
    /// EventDescription property.
    /// </summary>
    EventHandlerDescription,

    /// <summary>
    /// The completion represents the type of a MarkupExtension.
    /// </summary>
    MarkupExtensionClass,

    /// <summary>
    /// The completion represents the name of a MarkupExtension parameter.
    /// </summary>
    MarkupExtensionParameter,

    /// <summary>
    /// The completion represents the value of a MarkupExtension parameter.
    /// </summary>
    MarkupExtensionValue,

    /// <summary>
    /// The completion represents a type for an attached property in the Property Completion.
    /// (i.e. "Grid.").
    /// </summary>
    Type,

    /// <summary>
    /// The completion represents a value for Property attribute in Styles. (These are direct DPs on TargetTypes).
    /// (i.e. "Grid.Background").
    /// </summary>
    PropertyValue,

    /// <summary>
    /// The completion represents a value for Property attribute in Styles. (These are APs on types).
    /// (i.e. "Grid.Row").
    /// </summary>
    AttachedPropertyValue,

    /// <summary>
    /// The completion represents a type  within for Property attribute in Styles. (These are the types that for APs).
    /// (i.e. "Grid.Row").
    /// </summary>
    AttachedPropertyTypePrefix,

    /// <summary>
    /// The completion represents a local resource.
    /// </summary>
    LocalResource,

    /// <summary>
    /// The completion represents a system resource.
    /// </summary>
    SystemResource,

    /// <summary>
    /// The completion represents a property from the schema generated from a data source.
    /// </summary>
    DataBoundProperty,

    /// <summary>
    /// The completion represents the name of an element in the current scope.
    /// </summary>
    ElementName,

    /// <summary>
    /// The completion represents a namespace value. For instance, xmlns:local="Completion"
    /// </summary>
    NamespaceValue,

    /// <summary>
    /// The completion represents a condition value in a namespace. For instance, xmlns:local="namespace?Completion"
    /// </summary>
    ConditionValue,

    /// <summary>
    /// The completion represents a conditional argument value in a namespace. For instance, xmlns:local="namespace?Condition(Completion)"
    /// </summary>
    ConditionalArgument,

    /// <summary>
    /// A completion that cannot legally be used, but is shown for sake of user-education or
    /// completeness. Example would be the phone Pivot control in a shared Mercury XAML file:
    /// this type cannot be legally used in a shared context, but we want to show it as it
    /// is a core phone type.
    /// </summary>
    Unusable,

    /// <summary>
    /// The completion represents #region for XAML
    /// </summary>
    RegionStart,

    /// <summary>
    /// The completion represents #endregion for XAML
    /// </summary>
    RegionEnd,

    /// <summary>
    /// The completion represents a snippet for XAML
    /// </summary>
    Snippet,

    /// <summary>
    /// The completion represents a method
    /// </summary>
    Method,
}
