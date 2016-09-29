' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Options that can be used to modify the symbol lookup mechanism. Multiple options can be combined together.
    ''' </summary>
    <Flags()>
    Friend Enum LookupOptions
        ''' <summary>
        ''' Consider all symbols, using normal accessibility rules.
        ''' </summary>
        [Default] = 0

        ''' <summary>
        ''' Consider only namespaces and types.
        ''' </summary>
        NamespacesOrTypesOnly = 1 << 1

        ''' <summary>
        ''' Consider only labels. If this is not set, labels are not considered at all.
        ''' </summary>
        LabelsOnly = 1 << 2

        ''' <summary>
        ''' Do not consider symbols that are shared members.
        ''' </summary>
        MustBeInstance = 1 << 3

        ''' <summary>
        ''' Do not consider symbols that are instance members.
        ''' </summary>
        MustNotBeInstance = 1 << 4

        ''' <summary>
        ''' Do not consider the return value local variable.
        ''' This is similar to the C# LookupOption.MustBeInvocableMember.
        ''' 
        ''' The only non-invocable member in Visual Basic is the function return variable 
        ''' (see language specification 10.1.1). If this flag is set, lookup will not 
        ''' return the function return variable, but instead return the containing function or property,
        ''' and any overloads thereof.
        ''' </summary>
        MustNotBeReturnValueVariable = 1 << 5

        ''' <summary>
        ''' Do not do lookup in base classes (similar to how types in Imports are bound).
        ''' </summary>
        ''' <remarks></remarks>
        NoBaseClassLookup = 1 << 6

        ''' <summary>
        ''' Ignore accessibility checking when determining if a symbol is a viable match.
        ''' </summary>
        IgnoreAccessibility = 1 << 7

        ''' <summary>
        ''' Consider methods of any arity (but still consider arity for other types of symbols).
        ''' </summary>
        AllMethodsOfAnyArity = 1 << 8

        ''' <summary>
        ''' Do not look for extension methods.
        ''' </summary>
        IgnoreExtensionMethods = 1 << 9

        ''' <summary>
        ''' Ensures that lookup eagerly looks for extension methods and merges them with 
        ''' instance methods, if any. 
        ''' 
        ''' If this flag is not set and lookup found viable instance method, it will not look 
        ''' for extension methods that might be in scope. 
        ''' 
        ''' It is not an error to combine this flag with LookupOptions.IgnoreExtensionMethods, 
        ''' the LookupOptions.IgnoreExtensionMethods takes precedence. 
        ''' </summary>
        EagerlyLookupExtensionMethods = 1 << 10

        ''' <summary>
        ''' Consider only methods. Used by Query Expressions.
        ''' 
        ''' 11.21.2 Queryable Types
        ''' When binding well-known method names, non-methods are ignored for the purpose of 
        ''' multiple inheritance in interfaces and extension method binding, although shadowing 
        ''' semantics still apply.
        ''' </summary>
        MethodsOnly = 1 << 11

        ''' <summary>
        ''' Ignore 'throughType' in accessibility checking. Used in checking accessibility of symbols accessed via 'MyBase'.
        ''' </summary>
        UseBaseReferenceAccessibility = 1 << 12

        ' <summary>
        ' Consider only attribute types.
        ' </summary>
        AttributeTypeOnly = (1 << 13) Or NamespacesOrTypesOnly

        ''' <summary>
        ''' Do not consider locals or parameters during lookup.
        ''' </summary>
        MustNotBeLocalOrParameter = 1 << 14

        ''' <summary>
        ''' Consider only events. Used to indicate that lookup searches for events only. Is used
        ''' to change lookup semantic for searching inside interfaces having CoClass attribute defined. 
        ''' 
        ''' Essentially this is a special casing for searching events (and non-event symbols) in 
        ''' COM interfaces, see the following example from Dev11 code:
        '''
        ''' Performing a lookup in a CoClass interface affects how we treat ambiguities between events and other members.
        ''' In COM, events are separated into their own binding space, thus it is possible for an event and member to have
        ''' the same name.  This is not possible in the .NET world, but for backwards compatibility, especially with Office,
        ''' the compiler will ignore ambiguities when performing a lookup in a CoClass interface.  Example:
        '''
        '''     Interface _Foo
        '''        Sub Quit
        '''
        '''     Interface FooSource
        '''        Event Quit
        '''
        '''     &lt; System.Runtime.InteropServices.CoClass(GetType(FooClass)) &gt;
        '''     Interface Foo : Inherits _Foo, FooSource
        '''
        '''     Class FooClass : Implements Foo
        '''         Event Quit Implements Foo.Quit
        '''         Sub Quit Implements Foo.Quit
        '''
        ''' </summary>
        EventsOnly = 1 << 15

        ''' <summary>
        ''' When performing a lookup in interface do NOT lookup in System.Object 
        ''' </summary>
        NoSystemObjectLookupForInterfaces = 1 << 16

        ''' <summary>
        ''' Ignore duplicate types from the cor library.
        ''' </summary>
        IgnoreCorLibraryDuplicatedTypes = 1 << 17

        ''' <summary>
        ''' Handle a case of being able to refer to System.Int32 through System.Integer.
        ''' Same for other intrinsic types with intrinsic name different from emitted name.
        ''' </summary>
        AllowIntrinsicAliases = 1 << 18
    End Enum

    Friend Module LookupOptionExtensions

        Friend Const QueryOperatorLookupOptions As LookupOptions = LookupOptions.MethodsOnly Or LookupOptions.MustBeInstance Or LookupOptions.AllMethodsOfAnyArity
        Friend Const ConsiderationMask As LookupOptions = LookupOptions.NamespacesOrTypesOnly Or LookupOptions.LabelsOnly

        <Extension()>
        Friend Function IsAttributeTypeLookup(options As LookupOptions) As Boolean
            Return (options And LookupOptions.AttributeTypeOnly) = LookupOptions.AttributeTypeOnly
        End Function

        <Extension()>
        Friend Function IsValid(options As LookupOptions) As Boolean
            ' These are exclusive; both must not be present.
            Dim mustBeAndNotBeInstance As LookupOptions = LookupOptions.MustBeInstance Or LookupOptions.MustNotBeInstance
            If ((options And mustBeAndNotBeInstance) = mustBeAndNotBeInstance) Then
                Return False
            End If

            Return True
        End Function

        <Extension()>
        Friend Sub ThrowIfInvalid(options As LookupOptions)
            If Not options.IsValid Then
                Throw New ArgumentException("LookupOptions has an invalid combination of options")
            End If
        End Sub

        <Extension>
        Friend Function ShouldLookupExtensionMethods(options As LookupOptions) As Boolean
            Const invalidOptions As LookupOptions = LookupOptions.IgnoreExtensionMethods Or
                                                    LookupOptions.MustNotBeInstance Or
                                                    LookupOptions.NamespacesOrTypesOnly Or
                                                    LookupOptions.AttributeTypeOnly

            Return (options And invalidOptions) = 0
        End Function
    End Module
End Namespace
