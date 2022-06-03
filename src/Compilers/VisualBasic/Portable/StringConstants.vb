' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class StringConstants

        Private Sub New()
        End Sub

        ' non localizable strings
        Friend Const AnonymousTypeName As String = "<anonymous type>"
        Friend Const AnonymousMethodName As String = "<anonymous method>"
        Friend Const AsEnumerableMethod As String = "AsEnumerable"
        Friend Const AsQueryableMethod As String = "AsQueryable"
        Friend Const DistinctMethod As String = "Distinct"
        Friend Const CastMethod As String = "Cast"
        Friend Const DelegateConstructorInstanceParameterName As String = "TargetObject"
        Friend Const DelegateConstructorMethodParameterName As String = "TargetMethod"
        Friend Const DelegateMethodCallbackParameterName As String = "DelegateCallback"
        Friend Const DelegateMethodInstanceParameterName As String = "DelegateAsyncState"
        Friend Const DelegateMethodResultParameterName As String = "DelegateAsyncResult"
        Friend Const ElementAtMethod As String = "ElementAtOrDefault"
        Friend Const GroupByMethod As String = "GroupBy"
        Friend Const GroupJoinMethod As String = "GroupJoin"
        Friend Const JoinMethod As String = "Join"
        Friend Const Lambda As String = "Lambda"
        Friend Const NamedSymbolErrorName As String = "?"
        Friend Const OrderByDescendingMethod As String = "OrderByDescending"
        Friend Const OrderByMethod As String = "OrderBy"
        Friend Const SelectManyMethod As String = "SelectMany"
        Friend Const SelectMethod As String = "Select"
        Friend Const SkipMethod As String = "Skip"
        Friend Const SkipWhileMethod As String = "SkipWhile"
        Friend Const TakeMethod As String = "Take"
        Friend Const TakeWhileMethod As String = "TakeWhile"
        Friend Const ThenByDescendingMethod As String = "ThenByDescending"
        Friend Const ThenByMethod As String = "ThenBy"
        Friend Const UnnamedNamespaceErrName As String = "<Default>"
        Friend Const WhereMethod As String = "Where"

        Friend Const PropertyGetPrefix As String = "get_"
        Friend Const PropertySetPrefix As String = "set_"
        Friend Const WinMdPropertySetPrefix As String = "put_"
        Friend Const EventAddPrefix As String = "add_"
        Friend Const EventRemovePrefix As String = "remove_"
        Friend Const EventRaisePrefix As String = "raise_"

        Friend Const ValueParameterName As String = "Value"
        Friend Const WithEventsValueParameterName As String = "WithEventsValue"
        Friend Const AutoPropertyValueParameterName As String = "AutoPropertyValue"
        Friend Const EventDelegateSuffix As String = "EventHandler"
        Friend Const EventVariableSuffix As String = "Event"

        Friend Const DefaultXmlnsPrefix As String = ""
        Friend Const DefaultXmlNamespace As String = ""
        Friend Const XmlPrefix As String = "xml"
        Friend Const XmlNamespace As String = "http://www.w3.org/XML/1998/namespace"
        Friend Const XmlnsPrefix As String = "xmlns"
        Friend Const XmlnsNamespace As String = "http://www.w3.org/2000/xmlns/"

        Friend Const XmlAddMethodName As String = "Add"
        Friend Const XmlGetMethodName As String = "Get"
        Friend Const XmlElementsMethodName As String = "Elements"
        Friend Const XmlDescendantsMethodName As String = "Descendants"
        Friend Const XmlAttributeValueMethodName As String = "AttributeValue"
        Friend Const XmlCreateAttributeMethodName As String = "CreateAttribute"
        Friend Const XmlCreateNamespaceAttributeMethodName As String = "CreateNamespaceAttribute"
        Friend Const XmlRemoveNamespaceAttributesMethodName As String = "RemoveNamespaceAttributes"

        Friend Const ValueProperty As String = "Value"
    End Class
End Namespace
