Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Namespace Microsoft.VisualStudio.Editors.XmlIntellisense

    '--------------------------------------------------------------------------
    ' IXmlIntellisenseService:
    '     Interface that defines the contract for the XmlIntellisense service.
    '     Must be kept in sync with its unmanaged version in vbidl.idl
    '--------------------------------------------------------------------------
    <GuidAttribute("94B71D3D-628F-4036-BF89-7FE1508E78AE")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <ComImport()> _
    Friend Interface IXmlIntellisenseService

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function CreateSchemas( _
            <InAttribute()> ByVal ProjectGuid As Guid _
            ) _
            As IXmlIntellisenseSchemas

    End Interface

    '--------------------------------------------------------------------------
    ' IXmlIntellisenseSchemas:
    '     Interface that defines the contract for the Xml intellisense schemas
    '     manager.
    '     Must be kept in sync with its unmanaged version in vbidl.idl
    '--------------------------------------------------------------------------
    <GuidAttribute("1E9E02D2-0532-4BD2-8114-A24262CC9770")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <ComImport()> _
    Friend Interface IXmlIntellisenseSchemas

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Sub AsyncCompile()

        ReadOnly Property CompiledEvent() As IntPtr

        ReadOnly Property TargetNamespaces() As String()

        ReadOnly Property MemberList() As IXmlIntellisenseMemberList

        ReadOnly Property FirstErrorSource() As String

        ReadOnly Property IsEmpty() As <MarshalAs(UnmanagedType.Bool)> Boolean

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Sub ShowInXmlSchemaExplorer( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String, _
            <MarshalAs(UnmanagedType.Bool)> ByRef ElementFound As Boolean, _
            <MarshalAs(UnmanagedType.Bool)> ByRef NamespaceFound As Boolean)

    End Interface

    '--------------------------------------------------------------------------
    ' IXmlIntellisenseMemberList:
    '     Interface that defines the contract for lists of element and attribute
    '     declarations used as the basis of intellisense member dropdowns.
    '     Must be kept in sync with its unmanaged version in vbidl.idl
    '--------------------------------------------------------------------------
    <GuidAttribute("E90363FC-4246-4df8-869E-7BA42D29F526")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <ComImport()> _
    Friend Interface IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function Document() As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function AllElements() As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function GlobalElements() As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function All() As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function ElementsByNamespace( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String _
            ) As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function ElementsByName( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String _
            ) As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function AttributesByNamespace( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String _
            ) As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function AttributesByName( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String _
            ) As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function DescendantsByNamespace( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String _
            ) As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function DescendantsByName( _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal NamespaceName As String, _
            <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal LocalName As String _
            ) As IXmlIntellisenseMemberList

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function GetEnumerator() As IXmlIntellisenseMemberEnumerator

        ReadOnly Property MatchesNamedType() As Boolean

    End Interface

    '--------------------------------------------------------------------------
    ' IXmlIntellisenseMemberEnumerator:
    '     This is a list enumerator interface that is very to use from native
    '     code.  The enumerator begins just before the first item in the list.
    '--------------------------------------------------------------------------
    <GuidAttribute("C5E8D87C-674B-4966-9245-AA32914B05F7")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <ComImport()> _
    Friend Interface IXmlIntellisenseMemberEnumerator

        <MethodImpl(MethodImplOptions.InternalCall)> _
        Function GetNext() As IXmlIntellisenseMember

    End Interface

    '--------------------------------------------------------------------------
    ' IXmlIntellisenseMember:
    '     Represents an Xml schema element or attribute declaration, which
    '     is what is displayed in intellisense dropdowns.
    '--------------------------------------------------------------------------
    <GuidAttribute("AB892676-9227-4c8e-AD84-DE887646D416")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <ComImport()> _
    Friend Interface IXmlIntellisenseMember

        ReadOnly Property IsElement() As Boolean

        ReadOnly Property NamespaceName() As String

        ReadOnly Property LocalName() As String

    End Interface

End Namespace
