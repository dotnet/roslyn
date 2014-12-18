' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class ImplicitClassTests
        Inherits BasicTestBase

        <Fact>
        Public Sub ImplicitClassSymbol()
            Dim c = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C">
    <file>
Namespace N
    Sub Foo
    End Sub
End Namespace
    </file>
</compilation>)

            Dim n = DirectCast(c.Assembly.GlobalNamespace.GetMembers("N").Single(), NamespaceSymbol)
            Dim implicitClass = DirectCast(n.GetMembers().Single(), NamedTypeSymbol)

            Assert.Equal(0, implicitClass.GetAttributes().Length)
            Assert.Equal(0, implicitClass.Interfaces.Length)
            Assert.Equal(c.ObjectType, implicitClass.BaseType)
            Assert.Equal(0, implicitClass.Arity)
            Assert.True(implicitClass.IsImplicitlyDeclared)
            Assert.Equal(SyntaxKind.NamespaceStatement, implicitClass.DeclaringSyntaxReferences.Single().GetSyntax().VBKind)
            Assert.False(implicitClass.IsSubmissionClass)
            Assert.False(implicitClass.IsScriptClass)
        End Sub

        <Fact>
        Public Sub ScriptClassSymbol()
            Dim c = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="C">
    <file>
Sub Foo
End Sub
    </file>
</compilation>, parseOptions:=TestOptions.Script)

            Dim scriptClass = DirectCast(c.Assembly.GlobalNamespace.GetMembers().Single(), NamedTypeSymbol)

            Assert.Equal(0, scriptClass.GetAttributes().Length)
            Assert.Equal(0, scriptClass.Interfaces.Length)
            Assert.Equal(c.ObjectType, scriptClass.BaseType)
            Assert.Equal(0, scriptClass.Arity)
            Assert.True(scriptClass.IsImplicitlyDeclared)
            Assert.Equal(SyntaxKind.CompilationUnit, scriptClass.DeclaringSyntaxReferences.Single().GetSyntax().VBKind)
            Assert.False(scriptClass.IsSubmissionClass)
            Assert.True(scriptClass.IsScriptClass)
        End Sub
    End Class
End Namespace

