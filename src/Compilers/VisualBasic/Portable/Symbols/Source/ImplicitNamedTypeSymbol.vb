' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary> 
    ''' Represents implicit, script and submission classes. 
    ''' </summary>    
    Friend NotInheritable Class ImplicitNamedTypeSymbol
        Inherits SourceMemberContainerTypeSymbol

        Friend Sub New(declaration As MergedTypeDeclaration, containingSymbol As NamespaceOrTypeSymbol, containingModule As SourceModuleSymbol)
            MyBase.New(declaration, containingSymbol, containingModule)
            Debug.Assert(declaration.Kind = DeclarationKind.ImplicitClass OrElse declaration.Kind = DeclarationKind.Submission OrElse declaration.Kind = DeclarationKind.Script)
        End Sub

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return IsImplicitClass OrElse IsScriptClass
            End Get
        End Property

        Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Return AttributeUsageInfo.Null
        End Function

        Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Return Me.GetDeclaredBase(Nothing)
        End Function

        Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function MakeDeclaredBase(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
            Dim baseType = DeclaringCompilation.GetSpecialType(SpecialType.System_Object)

            ' check that System.Object is available. 
            ' Although submission semantically doesn't have a base class we need to emit one.
            Dim info = baseType.GetUseSiteInfo()
            diagnostics.Add(info, GetFirstLocation())

            Return If(Me.TypeKind = TypeKind.Submission, Nothing, baseType)
        End Function

        Friend Overrides Function MakeDeclaredInterfaces(basesBeingResolved As BasesBeingResolved, diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property IsComImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property Layout As TypeLayout
            Get
                Return Nothing
            End Get
        End Property

        Friend ReadOnly Property HasStructLayoutAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
            Get
                Return DefaultMarshallingCharSet
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Return Nothing
        End Function

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCodeAnalysisEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetGuidString(ByRef guidString As String) As Boolean
            guidString = Nothing
            Return False
        End Function

        Friend Overrides ReadOnly Property HasVisualBasicEmbeddedAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasCompilerLoweringPreserveAttribute As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExtensibleInterfaceNoUseSiteDiagnostics As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Function GetInheritsOrImplementsLocation(base As NamedTypeSymbol, getInherits As Boolean) As Location
            ' A script class may implement interfaces in hosted scenarios.
            ' The interface definitions are specified via API, not in compilation source.
            Return NoLocation.Singleton
        End Function

        Protected Overrides Sub AddDeclaredNonTypeMembers(membersBuilder As MembersAndInitializersBuilder, diagnostics As BindingDiagnosticBag)
            For Each syntaxRef In SyntaxReferences
                Dim node = syntaxRef.GetVisualBasicSyntax()

                ' Set up a binder for this part of the type.
                Dim binder As Binder = BinderBuilder.CreateBinderForType(ContainingSourceModule, syntaxRef.SyntaxTree, Me)

                Dim staticInitializers As ArrayBuilder(Of FieldOrPropertyInitializer) = Nothing
                Dim instanceInitializers As ArrayBuilder(Of FieldOrPropertyInitializer) = Nothing

                Debug.Assert(Me.IsScriptClass OrElse Me.IsImplicitClass)

                Dim globalCodeNotAllowed As Boolean = Me.IsImplicitClass
                Dim nodeMembers = If(node.Kind = SyntaxKind.CompilationUnit, DirectCast(node, CompilationUnitSyntax).Members, DirectCast(node, NamespaceBlockSyntax).Members)

                ' We don't need to do any checking, just declare the members.
                For Each memberSyntax In nodeMembers

                    ' Do not report semantic error if a parse error has already been reported.
                    ' Let the user fix the parse errors and then tell them to move the code to a method or a script.
                    Dim reportAsMisplacedGlobalCode = globalCodeNotAllowed AndAlso Not memberSyntax.HasErrors

                    AddMember(memberSyntax, binder, diagnostics, membersBuilder, staticInitializers, instanceInitializers, reportAsMisplacedGlobalCode)
                Next

                ' add the collected initializers for this (partial) type to the collections
                ' and free the array builders
                AddInitializers(membersBuilder.StaticInitializers, staticInitializers)
                AddInitializers(membersBuilder.InstanceInitializers, instanceInitializers)
            Next
        End Sub

        Friend Overrides Function GetSynthesizedWithEventsOverrides() As IEnumerable(Of PropertySymbol)
            ' All infrastructure for proper WithEvents handling is in SourceNamedTypeSymbol, 
            ' but this type derives directly from SourceMemberContainerTypeSymbol, which is a base class of 
            ' SourceNamedTypeSymbol.
            ' Tracked by https://github.com/dotnet/roslyn/issues/14073.
            Return SpecializedCollections.EmptyEnumerable(Of PropertySymbol)()
        End Function

    End Class
End Namespace
