' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BinderFactory
        Private Enum NodeUsage As Byte
            ' For the compilation unit 
            CompilationUnit

            ' For implicit class (top-level or within a namespace)
            ImplicitClass

            ' For top-level script code of a compilation unit
            ScriptCompilationUnit

            ' All executable statements in top-level script code share the same TopLevelCodeBinder.
            ' We use CompilationUnitSyntax as a key to the cache for all such statements.
            TopLevelExecutableStatement

            ' For an Imports statement
            ImportsStatement

            ' No special binder for the full namespace block. For interior of namespace block
            ' (namespace members are in scope)
            NamespaceBlockInterior

            ' For the full type block (type parameters and members in scope). In VB, no special
            ' binder needed for the interior of a type block; members are in scope even in the
            ' header)
            TypeBlockFull

            ' For the full enum block (members in scope)
            EnumBlockFull

            ' For delegate declaration
            DelegateDeclaration

            ' For an inherits statement in a type
            InheritsStatement

            ' For an implements statement in a type
            ImplementsStatement

            ' For the full part of a method (type parameters in scope, but not parameter or locals)
            MethodFull

            ' For the interior of a method (parameters and locals in scope also)
            MethodInterior

            ' For field or property initializer
            FieldOrPropertyInitializer

            ' For array bounds of a field
            FieldArrayBounds

            ' For binding an attribute
            Attribute

            ' For binding parameter default values
            ParameterDefaultValue

            ' For the full part of the property, similar to MethodFull
            PropertyFull

            ' Add more usages here if necessary. 
        End Enum
    End Class
End Namespace
