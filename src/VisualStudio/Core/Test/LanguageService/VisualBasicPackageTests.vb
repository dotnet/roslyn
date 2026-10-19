' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Sdk.TestFramework
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports Moq
Imports Roslyn.Test.Utilities
Imports Roslyn.Test.Utilities.Desktop
Imports WrapperPolicy = Microsoft.VisualStudio.LanguageServices.Implementation.Interop.WrapperPolicy

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.LanguageService
    Public Class VisualBasicPackageTests
        <WpfFact>
        Public Sub ConstructorDoesNotCreateComAggregate()
            RunIsolated(testCompilerService:=False)
        End Sub

        <WpfFact>
        Public Sub CompilerServiceCreatesOneAggregateOnMainThread()
            RunIsolated(testCompilerService:=True)
        End Sub

        Private Shared Sub RunIsolated(testCompilerService As Boolean)
            ' SDK statics can retain the mocked UI thread after the fixture is disposed.
            Dim domain = AppDomainUtils.Create(NameOf(VisualBasicPackageTests))
            Try
                Dim runnerType = GetType(RemoteTestRunner)
                Dim runner = DirectCast(domain.CreateInstanceAndUnwrap(runnerType.Assembly.FullName, runnerType.FullName), RemoteTestRunner)
                runner.Run(testCompilerService)
            Finally
                AppDomain.Unload(domain)
            End Try
        End Sub

        Public NotInheritable Class RemoteTestRunner
            Inherits MarshalByRefObject

            Public Sub Run(testCompilerService As Boolean)
                Using serviceProvider As New GlobalServiceProvider()
                    Dim previousContext = SynchronizationContext.Current
                    Dim exportProviderScope As New UseExportProviderAttribute()
                    SynchronizationContext.SetSynchronizationContext(New DispatcherSynchronizationContext())
                    exportProviderScope.Before(Nothing)
                    Try
                        If testCompilerService Then
                            Using environment As New TestEnvironment(GetType(MiscellaneousFilesWorkspace))
                                TestCompilerServiceAsync(environment).JoinUsingDispatcher(CancellationToken.None)
                            End Using
                        Else
                            TestConstructorAsync().JoinUsingDispatcher(CancellationToken.None)
                        End If
                    Finally
                        exportProviderScope.After(Nothing)
                        SynchronizationContext.SetSynchronizationContext(previousContext)
                    End Try
                End Using
            End Sub
        End Class

        Private Shared Async Function TestConstructorAsync() As Task
            Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None)

            Using factory As New TrackingComWrapperFactory()
                Dim package = Await Task.Run(Function() New VisualBasicPackage())
                Try
                    Assert.Equal(0, factory.CreationCount)
                    Assert.Null(ComAggregate.TryGetWrapper(package))
                Finally
                    DirectCast(package, IVsPackage).Close()
                End Try
            End Using
        End Function

        Private Shared Async Function TestCompilerServiceAsync(environment As TestEnvironment) As Task
            Await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None)

            Dim componentModel = DirectCast(environment.ServiceProvider.GetService(GetType(SComponentModel)), IComponentModel)

            Using factory As New TrackingComWrapperFactory()
                Dim package = Await Task.Run(Function() New VisualBasicPackage())
                Try
                    package.AddService(GetType(SComponentModel), Function(_1, _2, _3) Task.FromResult(Of Object)(componentModel), promote:=False)
                    package.AddService(GetType(RoslynPackageLoadService), Function(_1, _2, _3) Task.FromResult(Of Object)(New RoslynPackageLoadService()), promote:=False)
                    package.AddService(GetType(SVsRegisterEditors), Function(_1, _2, _3) Task.FromResult(Of Object)(New Mock(Of IVsRegisterEditors)(MockBehavior.Loose).Object), promote:=False)

                    Await Task.Run(Function() InitializePackageAsync(package))
                    Assert.Equal(0, factory.CreationCount)

                    Await Assert.ThrowsAnyAsync(Of OperationCanceledException)(
                        Function() package.GetServiceAsync(Of IVbCompilerService, Object)(
                            throwOnFailure:=True, New CancellationToken(canceled:=True)))
                    Assert.Equal(0, factory.CreationCount)

                    Dim services = Await Task.Run(
                        Async Function()
                            Return Await Task.WhenAll(
                                package.GetServiceAsync(Of IVbCompilerService, Object)(throwOnFailure:=True, CancellationToken.None),
                                package.GetServiceAsync(Of IVbCompilerService, Object)(throwOnFailure:=True, CancellationToken.None),
                                package.GetServiceAsync(Of IVbCompilerService, Object)(throwOnFailure:=True, CancellationToken.None))
                        End Function)

                    Assert.Equal(1, factory.CreationCount)
                    Assert.True(factory.CreatedOnMainThread)
                    Assert.Same(services(0), services(1))
                    Assert.Same(services(0), services(2))
                    Assert.Same(package, ComAggregate.GetManagedObject(Of VisualBasicPackage)(services(0)))
                    Dim compiler = DirectCast(services(0), IVbCompiler)
                    Assert.True(compiler.IsValidIdentifier("ValidName"))
                    Assert.False(compiler.IsValidIdentifier("1NotValid"))

                    Dim serviceGuid = GetType(IVbCompilerService).GUID
                    Dim compilerGuid = GetType(IVbCompiler).GUID
                    Dim compilerPointer As IntPtr
                    Assert.Equal(VSConstants.S_OK, DirectCast(package, OLE.Interop.IServiceProvider).QueryService(serviceGuid, compilerGuid, compilerPointer))
                    Try
                        Assert.Same(services(0), Marshal.GetObjectForIUnknown(compilerPointer))
                    Finally
                        Marshal.Release(compilerPointer)
                    End Try

                    ' WinForms defines its own managed interface with this IID, so the cast must use COM QI.
                    Dim entryPointProvider = DirectCast(services(0), IPropertyPageEntryPointProvider)
                    Assert.Equal(VSConstants.S_OK, entryPointProvider.GetFormEntryPointsList(Nothing, 0, Nothing, IntPtr.Zero))

                    Dim packageUnknown = Marshal.GetIUnknownForObject(package)
                    Dim serviceUnknown = Marshal.GetIUnknownForObject(services(0))
                    Try
                        Assert.Equal(packageUnknown, serviceUnknown)
                    Finally
                        Marshal.Release(serviceUnknown)
                        Marshal.Release(packageUnknown)
                    End Try
                Finally
                    DirectCast(package, IVsPackage).Close()
                End Try
            End Using
        End Function

        Private Shared Function InitializePackageAsync(package As VisualBasicPackage) As Task
            ' Exercise the real initialization work without asking the shell to load unrelated packages.
            Dim initialize = GetType(AbstractPackage).GetMethod("InitializeAsync", BindingFlags.Instance Or BindingFlags.NonPublic)
            Return DirectCast(initialize.Invoke(package, {CancellationToken.None, New Progress(Of ServiceProgressData)()}), Task)
        End Function

        <ComImport, Guid("3EB048DA-F881-4a7f-A9D4-0258E19978AA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
        Private Interface IPropertyPageEntryPointProvider
            Function GetFormEntryPointsList(
                <MarshalAs(UnmanagedType.IUnknown), [In]> hierarchy As Object,
                itemCount As Integer,
                <Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=1)> items As String(),
                <Out> actualItemCount As IntPtr) As Integer
        End Interface

        Private NotInheritable Class TrackingComWrapperFactory
            Implements IComWrapperFactory, IDisposable

            Private ReadOnly _previousFactory As IComWrapperFactory
            Private _creationCount As Integer

            Public Sub New()
                _previousFactory = WrapperPolicy.s_ComWrapperFactory
                WrapperPolicy.s_ComWrapperFactory = Me
            End Sub

            Public ReadOnly Property CreationCount As Integer
                Get
                    Return Volatile.Read(_creationCount)
                End Get
            End Property

            Public Property CreatedOnMainThread As Boolean

            Public Function CreateAggregatedObject(managedObject As Object) As Object Implements IComWrapperFactory.CreateAggregatedObject
                Interlocked.Increment(_creationCount)
                CreatedOnMainThread = ThreadHelper.JoinableTaskContext.IsOnMainThread
                Return CodeModelTestHelpers.MockComWrapperFactory.Instance.CreateAggregatedObject(managedObject)
            End Function

            Public Sub Dispose() Implements IDisposable.Dispose
                WrapperPolicy.s_ComWrapperFactory = _previousFactory
            End Sub
        End Class
    End Class
End Namespace
