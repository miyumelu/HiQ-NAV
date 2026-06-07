Public Class StatusScreen

    Private _data As TelemetryPacket = New TelemetryPacket()

    Private Async Sub StatusScreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LblStatus.Text = "Please wait..."
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

    Private Shared Function FindCmdPath(isDay As Boolean) As String
        Dim mapFolder = If(isDay, "DAY.MAP", "NIGHT.MAP")
        ' Dim rel = IO.Path.Combine("NAVIGATION_MAP.DATA", mapFolder, "EUROPE.CMD")
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

    Private Sub DateTime_Tick(sender As Object, e As EventArgs) Handles DateTime.Tick
        If _data IsNot Nothing AndAlso _data.GameTime IsNot Nothing Then
            LblTime.Text = _data.GameTime
        Else
            LblTime.Text = "--:--"
        End If
    End Sub
End Class