' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

#If False Then ' not supported for now
' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Internal.Log
Imports Microsoft.Internal.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Telemetry

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Log
    Public Class VSTelemetryLoggerTest

        <Fact>
        Public Sub TestNoLog()
            Dim service = New Service()
            Dim logger = New VSTelemetryLogger(service)

            logger.Log(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, LogMessage.Create("test"))
            Assert.Equal(0, service.DefaultSession.PostedEvents.Cast(Of Service.TelemetryEvent)().Count())
        End Sub

        <Fact>
        Public Sub TestLog()
            Dim service = New Service()
            Dim logger = New VSTelemetryLogger(service)

            logger.Log(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(
                       Sub(m)
                           m.Add("test", "1")
                       End Sub))

            Dim postedEvent = service.DefaultSession.PostedEvents.Cast(Of Service.TelemetryEvent)().Single()
            Assert.Equal("vs/ide/vbcs/debugging/encsession/editsession/emitdeltaerrorid", postedEvent.EventName)

            Assert.Equal(1, postedEvent.Map.Count)

            Dim kv = postedEvent.Map.Single()
            Assert.Equal("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.test", kv.Key)
            Assert.Equal("1", kv.Value)
        End Sub

        <Fact>
        Public Sub TestNoLogBlockStart()
            Dim service = New Service()
            Dim logger = New VSTelemetryLogger(service)

            logger.LogBlockStart(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, LogMessage.Create("test"), 1, CancellationToken.None)
            Assert.Equal(0, service.DefaultSession.PostedEvents.Cast(Of Service.TelemetryEvent)().Count())
        End Sub

        <Fact>
        Public Sub TestLogBlockStart()
            Dim service = New Service()
            Dim logger = New VSTelemetryLogger(service)

            logger.LogBlockStart(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(
                       Sub(m)
                           m.Add("test", "2")
                       End Sub), 1, CancellationToken.None)

            Dim postedEvent = service.DefaultSession.PostedEvents.Cast(Of Service.TelemetryEvent)().Single()
            Assert.Equal("vs/ide/vbcs/debugging/encsession/editsession/emitdeltaerrorid/start", postedEvent.EventName)

            Assert.Equal(2, postedEvent.Map.Count)

            Assert.Equal("1", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.blockid"))
            Assert.Equal("2", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.test"))
        End Sub

        <Fact>
        Public Sub TestNoLogBlockEnd()
            Dim service = New Service()
            Dim logger = New VSTelemetryLogger(service)

            logger.LogBlockEnd(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, LogMessage.Create("test"), 1, 123, CancellationToken.None)
            Assert.Equal(0, service.DefaultSession.PostedEvents.Cast(Of Service.TelemetryEvent)().Count())
        End Sub

        <Fact>
        Public Sub TestLogBlockEnd()
            Dim service = New Service()
            Dim logger = New VSTelemetryLogger(service)

            logger.LogBlockEnd(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(
                       Sub(m)
                           m.Add("test", "2")
                       End Sub), 1, 123, CancellationToken.None)

            Dim postedEvent = service.DefaultSession.PostedEvents.Cast(Of Service.TelemetryEvent)().Single()
            Assert.Equal("vs/ide/vbcs/debugging/encsession/editsession/emitdeltaerrorid/end", postedEvent.EventName)

            Assert.Equal(3, postedEvent.Map.Count)

            Assert.Equal("1", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.blockid"))
            Assert.Equal("False", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.cancellationrequested"))
            Assert.Equal("123", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.duration"))
        End Sub

        <Fact>
        Public Sub TestLogBlockEndCacellation()
            Dim service = New Service()
            Dim logger = New VSTelemetryLogger(service)

            Dim cancellation = New CancellationTokenSource()
            cancellation.Cancel()

            logger.LogBlockEnd(FunctionId.Debugging_EncSession_EditSession_EmitDeltaErrorId, KeyValueLogMessage.Create(
                       Sub(m)
                           m.Add("test", "2")
                       End Sub), 1, 123, cancellation.Token)

            Dim postedEvent = service.DefaultSession.PostedEvents.Cast(Of Service.TelemetryEvent)().Single()
            Assert.Equal("vs/ide/vbcs/debugging/encsession/editsession/emitdeltaerrorid/end", postedEvent.EventName)

            Assert.Equal(3, postedEvent.Map.Count)

            Assert.Equal("1", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.blockid"))
            Assert.Equal("True", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.cancellationrequested"))
            Assert.Equal("123", postedEvent.Map("vs.ide.vbcs.debugging.encsession.editsession.emitdeltaerrorid.duration"))
        End Sub

        Public Class Service
            Implements IVsTelemetryService

            Public ReadOnly DefaultSession As Session = New Session()

            Public Function CreateEvent(szEventName As String) As IVsTelemetryEvent Implements IVsTelemetryService.CreateEvent
                Return New TelemetryEvent(szEventName)
            End Function

            Public Function GetDefaultSession() As IVsTelemetrySession Implements IVsTelemetryService.GetDefaultSession
                Return DefaultSession
            End Function

            Public Function CreateActivity(szActivityName As String) As IVsTelemetryActivity Implements IVsTelemetryService.CreateActivity
                Throw New NotImplementedException()
            End Function

            Public Function CreateActivityWithParentCorrelationId(szActivityName As String, ByRef parentCorrelationId As Guid) As IVsTelemetryActivity Implements IVsTelemetryService.CreateActivityWithParentCorrelationId
                Throw New NotImplementedException()
            End Function

            Public Function CreatePropertyBag() As IVsTelemetryPropertyBag Implements IVsTelemetryService.CreatePropertyBag
                Throw New NotImplementedException()
            End Function

            Public Class Session
                Implements IVsTelemetrySession

                Public ReadOnly PostedEvents As List(Of IVsTelemetryEvent) = New List(Of IVsTelemetryEvent)()

                Public Sub PostBoolProperty(szPropertyName As String, value As Boolean) Implements IVsTelemetrySession.PostBoolProperty
                End Sub

                Public Sub PostDoubleProperty(szPropertyName As String, value As Double) Implements IVsTelemetrySession.PostDoubleProperty
                End Sub

                Public Sub PostEvent(eventObject As IVsTelemetryEvent) Implements IVsTelemetrySession.PostEvent
                    PostedEvents.Add(eventObject)
                End Sub

                Public Sub PostIntProperty(szPropertyName As String, value As Integer) Implements IVsTelemetrySession.PostIntProperty
                End Sub

                Public Sub PostLongProperty(szPropertyName As String, value As Long) Implements IVsTelemetrySession.PostLongProperty
                End Sub

                Public Sub PostProperty(szPropertyName As String, varValue As Object) Implements IVsTelemetrySession.PostProperty
                End Sub

                Public Sub PostShortProperty(szPropertyName As String, value As Short) Implements IVsTelemetrySession.PostShortProperty
                End Sub

                Public Sub PostSimpleEvent(szEventName As String) Implements IVsTelemetrySession.PostSimpleEvent
                    PostedEvents.Add(New TelemetryEvent(szEventName))
                End Sub

                Public Sub PostStringProperty(szPropertyName As String, szValue As String) Implements IVsTelemetrySession.PostStringProperty
                End Sub

                Public Sub RemoveSharedProperty(szPropertyName As String) Implements IVsTelemetrySession.RemoveSharedProperty
                End Sub

                Public Sub SetSharedBoolProperty(szPropertyName As String, value As Boolean) Implements IVsTelemetrySession.SetSharedBoolProperty
                End Sub

                Public Sub SetSharedDoubleProperty(szPropertyName As String, value As Double) Implements IVsTelemetrySession.SetSharedDoubleProperty
                End Sub

                Public Sub SetSharedIntProperty(szPropertyName As String, value As Integer) Implements IVsTelemetrySession.SetSharedIntProperty
                End Sub

                Public Sub SetSharedLongProperty(szPropertyName As String, value As Long) Implements IVsTelemetrySession.SetSharedLongProperty
                End Sub

                Public Sub SetSharedProperty(szPropertyName As String, varValue As Object) Implements IVsTelemetrySession.SetSharedProperty
                End Sub

                Public Sub SetSharedShortProperty(szPropertyName As String, value As Short) Implements IVsTelemetrySession.SetSharedShortProperty
                End Sub

                Public Sub SetSharedStringProperty(szPropertyName As String, szValue As String) Implements IVsTelemetrySession.SetSharedStringProperty
                End Sub

                Public Function CreateContext(szContextName As String) As IVsTelemetryContext Implements IVsTelemetrySession.CreateContext
                    Throw New NotImplementedException()
                End Function

                Public Function GetSessionId() As String Implements IVsTelemetrySession.GetSessionId
                    Return "1"
                End Function

                Public Function CanCollectPrivateInformation() As Boolean Implements IVsTelemetrySession.CanCollectPrivateInformation
                    Return True
                End Function

                Public Function IsUserMicrosoftInternal() As Boolean Implements IVsTelemetrySession.IsUserMicrosoftInternal
                    Return True
                End Function

                Public Function IsUserOptedIn() As Boolean Implements IVsTelemetrySession.IsUserOptedIn
                    Return True
                End Function

                Public Function SerializeSettings() As String Implements IVsTelemetrySession.SerializeSettings
                    Return "SerializedSettings"
                End Function

                Public Sub SetUserOptedIn(IsUserOptedIn As Boolean) Implements IVsTelemetrySession.SetUserOptedIn
                    Throw New NotImplementedException()
                End Sub

                Public Sub RegisterPropertyBag(szPropertyBagName As String, pPropertyBag As IVsTelemetryPropertyBag) Implements IVsTelemetrySession.RegisterPropertyBag
                    Throw New NotImplementedException()
                End Sub

                Public Function GetPropertyBag(szPropertyBagName As String) As IVsTelemetryPropertyBag Implements IVsTelemetrySession.GetPropertyBag
                    Throw New NotImplementedException()
                End Function

                Public Sub UnregisterPropertyBag(szPropertyBagName As String) Implements IVsTelemetrySession.UnregisterPropertyBag
                    Throw New NotImplementedException()
                End Sub

                Public Sub PostPiiProperty(szPropertyName As String, varValue As Object) Implements IVsTelemetrySession.PostPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub PostIntPiiProperty(szPropertyName As String, varValue As Integer) Implements IVsTelemetrySession.PostIntPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub PostLongPiiProperty(szPropertyName As String, varValue As Long) Implements IVsTelemetrySession.PostLongPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub PostDoublePiiProperty(szPropertyName As String, varValue As Double) Implements IVsTelemetrySession.PostDoublePiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub PostStringPiiProperty(szPropertyName As String, varValue As String) Implements IVsTelemetrySession.PostStringPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetSharedPiiProperty(szPropertyName As String, varValue As Object) Implements IVsTelemetrySession.SetSharedPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetSharedIntPiiProperty(szPropertyName As String, varValue As Integer) Implements IVsTelemetrySession.SetSharedIntPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetSharedLongPiiProperty(szPropertyName As String, varValue As Long) Implements IVsTelemetrySession.SetSharedLongPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetSharedDoublePiiProperty(szPropertyName As String, varValue As Double) Implements IVsTelemetrySession.SetSharedDoublePiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetSharedStringPiiProperty(szPropertyName As String, varValue As String) Implements IVsTelemetrySession.SetSharedStringPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Function GetSharedProperty(szPropertyName As String) As Object Implements IVsTelemetrySession.GetSharedProperty
                    Throw New NotImplementedException()
                End Function

                Public Function GetContext(szContextName As String) As IVsTelemetryContext Implements IVsTelemetrySession.GetContext
                    Throw New NotImplementedException()
                End Function
            End Class

            Public Class TelemetryEvent
                Implements IVsTelemetryEvent

                Public ReadOnly EventName As String
                Public ReadOnly Map As Dictionary(Of String, String) = New Dictionary(Of String, String)()

                Public Sub New(eventName As String)
                    Me.EventName = eventName
                End Sub

                Public ReadOnly Property AllPropertyNames As Array Implements IVsTelemetryEvent.AllPropertyNames
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public Sub AddPropertyBag(pPropertyBag As IVsTelemetryPropertyBag) Implements IVsTelemetryEvent.AddPropertyBag
                    Throw New NotImplementedException()
                End Sub

                Public Sub RemovePropertyBag(pPropertyBag As IVsTelemetryPropertyBag) Implements IVsTelemetryEvent.RemovePropertyBag
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetBoolProperty(szPropertyName As String, value As Boolean) Implements IVsTelemetryEvent.SetBoolProperty
                    Map(szPropertyName) = value.ToString()
                End Sub

                Public Sub SetDoublePiiProperty(szPropertyName As String, varValue As Double) Implements IVsTelemetryEvent.SetDoublePiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetDoubleProperty(szPropertyName As String, value As Double) Implements IVsTelemetryEvent.SetDoubleProperty
                    Map(szPropertyName) = value.ToString()
                End Sub

                Public Sub SetIntPiiProperty(szPropertyName As String, varValue As Integer) Implements IVsTelemetryEvent.SetIntPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetIntProperty(szPropertyName As String, value As Integer) Implements IVsTelemetryEvent.SetIntProperty
                    Map(szPropertyName) = value.ToString()
                End Sub

                Public Sub SetLongPiiProperty(szPropertyName As String, varValue As Long) Implements IVsTelemetryEvent.SetLongPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetLongProperty(szPropertyName As String, value As Long) Implements IVsTelemetryEvent.SetLongProperty
                    Map(szPropertyName) = value.ToString()
                End Sub

                Public Sub SetOptOutFriendlyFlag(bOptOutFriendly As Boolean) Implements IVsTelemetryEvent.SetOptOutFriendlyFlag
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetPiiProperty(szPropertyName As String, varValue As Object) Implements IVsTelemetryEvent.SetPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetProperty(szPropertyName As String, varValue As Object) Implements IVsTelemetryEvent.SetProperty
                    Map(szPropertyName) = varValue.ToString()
                End Sub

                Public Sub SetShortProperty(szPropertyName As String, value As Short) Implements IVsTelemetryEvent.SetShortProperty
                    Map(szPropertyName) = value.ToString()
                End Sub

                Public Sub SetStringPiiProperty(szPropertyName As String, varValue As String) Implements IVsTelemetryEvent.SetStringPiiProperty
                    Throw New NotImplementedException()
                End Sub

                Public Sub SetStringProperty(szPropertyName As String, szValue As String) Implements IVsTelemetryEvent.SetStringProperty
                    Map(szPropertyName) = szValue
                End Sub

                Public Function ContainsProperty(szPropertyName As String) As Boolean Implements IVsTelemetryEvent.ContainsProperty
                    Return Map.ContainsKey(szPropertyName)
                End Function

                'Public Function GetAllPropertiesNames() As Array Implements IVsTelemetryEvent.GetAllPropertiesNames
                '    Return Map.ToArray()
                'End Function

                Public Function GetName() As String Implements IVsTelemetryEvent.GetName
                    Return Me.EventName
                End Function

                Public Function GetProperty(szPropertyName As String) As Object Implements IVsTelemetryEvent.GetProperty
                    Return Map(szPropertyName)
                End Function
            End Class
        End Class
    End Class
End Namespace
#End If
