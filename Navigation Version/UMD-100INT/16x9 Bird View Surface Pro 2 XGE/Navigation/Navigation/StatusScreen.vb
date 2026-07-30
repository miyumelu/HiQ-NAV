Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports Newtonsoft.Json

Public Class StatusScreen

    Private _data As TelemetryPacket = New TelemetryPacket()
    Private _spacer As String = "    "
    Private ReadOnly _supportsDVD As Boolean = False
    Private ReadOnly _supportsMedium As Boolean = True
    Private ReadOnly _supportsInternal As Boolean = True
    Private _isCheckingDVD As Boolean = False

    Private Sub StatusScreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StartCheckingForDVD()
    End Sub

    Public Async Sub StartCheckingForDVD()
        If _isCheckingDVD Then Return
        _isCheckingDVD = True

        LOAD_GIF.Visible = False
        LblStatus.Text = GetInsertOrLoadingText()
        ShowOverlay()

        While True
            Dim cmdPath As String = Await Task.Run(Function() FindCmdPath(True))

            If Not String.IsNullOrEmpty(cmdPath) Then
                LOAD_GIF.Visible = True
                LblStatus.Text = _spacer & "Starting Navigation System..."
                Await Task.Delay(1500)

                Await Task.Run(Sub() MainPage.TryReacquireOfflineCompositor())

                MainPage.Show()
                Me.Hide()
                Exit While
            Else
                LblStatus.Text = GetInsertOrLoadingText()
                Await Task.Delay(2000)
            End If
        End While

        _isCheckingDVD = False
    End Sub
    Private Sub DateTime_Tick(sender As Object, e As EventArgs) Handles DateTime.Tick
        If _data IsNot Nothing AndAlso _data.GameTime IsNot Nothing Then
            LblTime.Text = _data.GameTime
        Else
            LblTime.Text = "--:--"
        End If
    End Sub

    Private Shared Function FindCmdPath(isDay As Boolean) As String
        Dim mapFolder = If(isDay, "DAY.MAP", "NIGHT.MAP")
        Dim rel = IO.Path.Combine(mapFolder, "EUROPE.CMD")
        For Each d In IO.DriveInfo.GetDrives()
            Try
                If Not d.IsReady Then Continue For
                If d.DriveType <> IO.DriveType.Fixed AndAlso
                   d.DriveType <> IO.DriveType.Removable AndAlso
                   d.DriveType <> IO.DriveType.CDRom Then Continue For

                Dim p = IO.Path.Combine(d.RootDirectory.FullName, rel)
                Dim a = IO.Path.Combine(d.RootDirectory.FullName, "NAVIGATION", rel)
                If IO.File.Exists(p) Then Return p
                If IO.File.Exists(a) Then Return a
            Catch
                Continue For
            End Try
        Next
        Return Nothing
    End Function

    Private Shared Function HasRemovableOrOpticalDrive() As Boolean
        For Each d In IO.DriveInfo.GetDrives()
            Try
                If d.DriveType = IO.DriveType.Removable OrElse d.DriveType = IO.DriveType.CDRom Then
                    Return True
                End If
            Catch
                Continue For
            End Try
        Next
        Return False
    End Function

    Private Function GetInsertOrLoadingText() As String
        If _supportsDVD Then
            Return "Please insert the DVD with the Navigation Map Data inside the DVD-Drive."
        ElseIf _supportsMedium Then
            Return "Please insert the Medium with the Navigation Map Data."
        ElseIf _supportsInternal Then
            Return "Please install the Map Data on the internal storage."
        Else
            LOAD_GIF.Visible = True
            Return _spacer & "Loading Navigation Data..."
        End If
    End Function

#Region "Overlay"

    Public Sub ShowInsertOrLoadingMessage()
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() ShowInsertOrLoadingMessage())
            Return
        End If
        LblStatus.Text = GetInsertOrLoadingText()
        ShowOverlay()
    End Sub

    Public Sub ShowReadErrorMessage()
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() ShowReadErrorMessage())
            Return
        End If
        LblStatus.Text = "The System has difficulties reading the DVD. Please clean and reinsert the Navigation DVD."
        ShowOverlay()
    End Sub

    Private Sub ShowOverlay()
        If Not Me.Visible Then Me.Show()
        Me.BringToFront()
    End Sub

    Public Sub HideOverlay()
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() HideOverlay())
            Return
        End If
        If Me.Visible Then
            Me.Hide()
            MainPage.BringToFront()
        End If
    End Sub

#End Region

End Class