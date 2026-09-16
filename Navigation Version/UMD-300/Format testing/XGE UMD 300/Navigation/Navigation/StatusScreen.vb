Imports System.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Text
Imports Newtonsoft.Json
Imports Universal_Map_Data

Public Class StatusScreen

    Private _data As TelemetryPacket = New TelemetryPacket()
    Private _spacer As String = "    "
    Private ReadOnly _supportsDVD As Boolean = True
    Private ReadOnly _supportsMedium As Boolean = True
    Private ReadOnly _supportsInternal As Boolean = False
    Private _isCheckingDVD As Boolean = False

    Private Const GENERIC_READ As UInteger = &H80000000UI
    Private Const FILE_SHARE_READ As UInteger = &H1UI
    Private Const FILE_SHARE_WRITE As UInteger = &H2UI
    Private Const OPEN_EXISTING As UInteger = 3UI
    Private Const IOCTL_STORAGE_CHECK_VERIFY2 As UInteger = &H2D0800UI
    Private Shared ReadOnly INVALID_HANDLE_VALUE As IntPtr = New IntPtr(-1)

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode, EntryPoint:="CreateFileW")>
    Private Shared Function CreateFileW(lpFileName As String, dwDesiredAccess As UInteger, dwShareMode As UInteger,
                                         lpSecurityAttributes As IntPtr, dwCreationDisposition As UInteger,
                                         dwFlagsAndAttributes As UInteger, hTemplateFile As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function DeviceIoControl(hDevice As IntPtr, dwIoControlCode As UInteger,
                                             lpInBuffer As IntPtr, nInBufferSize As UInteger,
                                             lpOutBuffer As IntPtr, nOutBufferSize As UInteger,
                                             ByRef lpBytesReturned As UInteger, lpOverlapped As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function CloseHandle(hObject As IntPtr) As Boolean
    End Function


    Private Shared Function IsDiscPresent(driveLetter As Char) As Boolean
        Dim handle = CreateFileW("\\.\" & driveLetter & ":", GENERIC_READ, FILE_SHARE_READ Or FILE_SHARE_WRITE,
                                  IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero)
        If handle = INVALID_HANDLE_VALUE Then Return False

        Try
            Dim bytesReturned As UInteger = 0
            Return DeviceIoControl(handle, IOCTL_STORAGE_CHECK_VERIFY2, IntPtr.Zero, 0, IntPtr.Zero, 0, bytesReturned, IntPtr.Zero)
        Finally
            CloseHandle(handle)
        End Try
    End Function

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
            Dim opticalDrives = IO.DriveInfo.GetDrives().Where(Function(d) d.DriveType = IO.DriveType.CDRom).ToList()
            Dim anyReady = opticalDrives.Any(Function(d) TryIsReady(d))
            Dim anyDiscPresentButNotReady = Not anyReady AndAlso
                opticalDrives.Any(Function(d) IsDiscPresent(CChar(d.Name.Substring(0, 1))))

            If anyReady Then
                LOAD_GIF.Visible = True
                LblStatus.Text = _spacer & "Reading disc..."
            ElseIf anyDiscPresentButNotReady Then
                LOAD_GIF.Visible = True
                LblStatus.Text = _spacer & "Starting disc drive..."
            Else
                LOAD_GIF.Visible = False
                LblStatus.Text = GetInsertOrLoadingText()
            End If

            Dim UMDPath As String = Await Task.Run(Function() FindUMDPath(True))

            If Not String.IsNullOrEmpty(UMDPath) Then

                LOAD_GIF.Visible = True
                LblStatus.Text = _spacer & "Starting Navigation System..."

                Dim result As Reader.LoadResult = Await Task.Run(Function() MainPage.TryReacquireOfflineCompositor())

                Select Case result.Status
                    Case Reader.LoadStatus.Ok
                        MainPage.Show()
                        Me.Hide()
                        Exit While

                    Case Reader.LoadStatus.NeedsAppUpdate
                        ShowNeedsUpdateMessage(result.Message)
                        Await Task.Delay(3000)

                    Case Reader.LoadStatus.Incompatible
                        ShowIncompatibleMessage(result.Message)
                        Await Task.Delay(3000)

                    Case Else ' NotFound / Corrupt
                        ShowReadErrorMessage()
                        Await Task.Delay(2000)
                End Select
            Else
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

    Private Shared Function FindUMDPath(isDay As Boolean) As String
        Dim mapFolder = If(isDay, "DAY.MAP", "NIGHT.MAP")
        Dim candidateNames = {"EUROPE.UMD", "EUROPE.CMD"}

        For Each d In IO.DriveInfo.GetDrives()
            Try
                If Not d.IsReady Then Continue For
                If d.DriveType <> IO.DriveType.Fixed AndAlso
               d.DriveType <> IO.DriveType.Removable AndAlso
               d.DriveType <> IO.DriveType.Network AndAlso
               d.DriveType <> IO.DriveType.CDRom Then Continue For

                For Each _Name In candidateNames
                    Dim rel = IO.Path.Combine(mapFolder, _Name)
                    Dim p = IO.Path.Combine(d.RootDirectory.FullName, rel)
                    Dim a = IO.Path.Combine(d.RootDirectory.FullName, "NAVIGATION", rel)
                    If IO.File.Exists(p) Then Return p
                    If IO.File.Exists(a) Then Return a
                Next
            Catch
                Continue For
            End Try
        Next
        Return Nothing
    End Function

    Private Shared Function TryIsReady(d As IO.DriveInfo) As Boolean
        Try
            Return d.IsReady
        Catch
            Return False
        End Try
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
            Return "Please insert the Disk with the Navigation Map Data inside the Blu-ray Drive."
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
        LOAD_GIF.Visible = False
        LblStatus.Text = "The System has difficulties reading the DVD. Please clean and reinsert the Navigation DVD."
        ShowOverlay()
    End Sub

    Public Sub ShowIncompatibleMessage(message As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() ShowIncompatibleMessage(message))
            Return
        End If
        LOAD_GIF.Visible = False
        LblStatus.Text = message
        ShowOverlay()
    End Sub

    Public Sub ShowNeedsUpdateMessage(message As String)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() ShowNeedsUpdateMessage(message))
            Return
        End If
        LOAD_GIF.Visible = False
        LblStatus.Text = message
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