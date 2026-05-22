Imports System.Windows
Imports System.Windows.Forms
Imports LibVLCSharp.Shared
Imports LibVLCSharp.WinForms

Public Class StartScreen

    Private _libVLC As LibVLC
    Private _mediaPlayer As MediaPlayer
    Private _videoView As VideoView
    Private _form2Shown As Boolean = False
    Private _timer As System.Windows.Forms.Timer

    Private Const SECONDS_BEFORE_END As Long = 1

    Private Sub StartScreen_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Core.Initialize()
        _libVLC = New LibVLC()
        _mediaPlayer = New MediaPlayer(_libVLC)

        _videoView = New VideoView()
        _videoView.Dock = DockStyle.Fill
        _videoView.MediaPlayer = _mediaPlayer
        Me.Controls.Add(_videoView)

        _timer = New System.Windows.Forms.Timer()
        _timer.Interval = 500
        AddHandler _timer.Tick, AddressOf Timer_Tick
        _timer.Start()

        Dim videoPath As String = "C:\SYS_START.mp4"

        If IO.File.Exists(videoPath) Then
            Dim media As New Media(_libVLC, New Uri(videoPath))
            _mediaPlayer.Play(media)
            media.Dispose()
        Else

        End If
    End Sub

    Private Sub Timer_Tick(sender As Object, e As EventArgs)
        If Me.InvokeRequired Then
            Me.Invoke(New Action(AddressOf CheckVideoPosition))
        Else
            CheckVideoPosition()
        End If
    End Sub

    Private Sub CheckVideoPosition()
        If _mediaPlayer Is Nothing Then Return
        If Not _mediaPlayer.IsPlaying Then Return

        Dim totalMs As Long = _mediaPlayer.Length
        Dim currentMs As Long = _mediaPlayer.Time

        If totalMs <= 0 Then Return

        Dim remainingMs As Long = totalMs - currentMs
        Dim remainingSeconds As Long = remainingMs \ 1000

        If remainingSeconds <= SECONDS_BEFORE_END AndAlso Not _form2Shown Then
            _form2Shown = True
            Form2.Show()
        End If
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        _timer?.Stop()
        _mediaPlayer?.Stop()
        _mediaPlayer?.Dispose()
        _libVLC?.Dispose()
    End Sub

End Class