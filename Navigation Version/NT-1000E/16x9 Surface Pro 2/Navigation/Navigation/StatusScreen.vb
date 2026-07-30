Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports Newtonsoft.Json

Public Class StatusScreen

    Private _data As TelemetryPacket = New TelemetryPacket()

    Private _cts As New Threading.CancellationTokenSource()
    Private _udpClient As UdpClient
    Private _listenTask As Task
    Private Const RECV_PORT As Integer = 11000
    Private Const DISC_PORT As Integer = 11001


    Private Async Sub StatusScreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LblStatus.Text = "Please wait..."

        SendHello()
        _listenTask = Task.Run(Sub() ListenLoop(_cts.Token))

        Await Task.Delay(500)

        While True
            Dim cmdPath As String = Await Task.Run(Function() FindCmdPath(True))

            If Not String.IsNullOrEmpty(cmdPath) Then
                LblStatus.Text = "Starting up Navigation..."
                Await Task.Delay(1500)

                MainPage.Show()
                Me.Hide()

                Exit While
            Else
                LblStatus.Text = "Please insert the DVD with the Navigation Map Data inside the DVD-Drive."
                Await Task.Delay(2000)
            End If
        End While
    End Sub
    Private Sub SendHello()
        Try
            Using disc As New UdpClient()
                disc.EnableBroadcast = True
                Dim ep As New IPEndPoint(IPAddress.Broadcast, DISC_PORT)
                Dim hello = Encoding.UTF8.GetBytes("HIQNAV_HELLO")
                disc.Send(hello, hello.Length, ep)
            End Using
        Catch

        End Try
    End Sub

    Private Sub ListenLoop(ct As Threading.CancellationToken)
        Try
            _udpClient = New UdpClient(RECV_PORT)
            _udpClient.Client.ReceiveTimeout = 5000

            Dim remoteEP As New IPEndPoint(IPAddress.Any, 0)

            While Not ct.IsCancellationRequested
                Try
                    Dim raw = _udpClient.Receive(remoteEP)
                    Dim json = Encoding.UTF8.GetString(raw)
                    Dim packet = JsonConvert.DeserializeObject(Of TelemetryPacket)(json)

                    If packet IsNot Nothing Then
                        _data = packet
                    End If

                Catch ex As SocketException
                    If Not ct.IsCancellationRequested Then SendHello()
                Catch

                End Try
            End While
        Catch

        Finally
            _udpClient?.Close()
        End Try
    End Sub

    Private Sub DateTime_Tick(sender As Object, e As EventArgs) Handles DateTime.Tick
        If _data IsNot Nothing AndAlso _data.GameTime IsNot Nothing Then
            LblTime.Text = _data.GameTime
        Else
            LblTime.Text = "--:--"
        End If
    End Sub

    Private Sub StatusScreen_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        _cts.Cancel()
        Try : _udpClient?.Close() : Catch : End Try
    End Sub

    Private Shared Function FindCmdPath(isDay As Boolean) As String
        Dim mapFolder = If(isDay, "DAY.MAP", "NIGHT.MAP")
        Dim rel = IO.Path.Combine(mapFolder, "EUROPE.CMD")
        For Each d In IO.DriveInfo.GetDrives()
            Try
                If Not d.IsReady Then Continue For
                If d.DriveType <> IO.DriveType.Fixed AndAlso
                   d.DriveType <> IO.DriveType.Removable AndAlso
                   d.DriveType <> IO.DriveType.Network AndAlso
                   d.DriveType <> IO.DriveType.CDRom Then Continue For

                Dim p = IO.Path.Combine(d.RootDirectory.FullName, rel)
                If IO.File.Exists(p) Then Return p
            Catch
                Continue For
            End Try
        Next
        Return Nothing
    End Function

End Class