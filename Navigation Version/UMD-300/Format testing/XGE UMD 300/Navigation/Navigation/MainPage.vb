Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports Universal_Map_Data
Imports Newtonsoft.Json
Imports SharpDX.XInput

Public Class MainPage

    Private _udpClient As UdpClient
    Private _receiveThread As Thread
    Private _running As Boolean = False

    Private _data As TelemetryPacket = New TelemetryPacket()
    Private _lastReceived As DateTime = DateTime.MinValue

    Private _mapImage As Bitmap

    Private _tcpClient As TcpClient
    Private _tileA As Bitmap
    Private _tileB As Bitmap
    Private _activeTile As Integer = 0
    Private _tileIsPerspective3D As Boolean = True
    Private ReadOnly _tileLock As New Object()
    Private _lastTileX As Single = Single.MaxValue
    Private _lastTileZ As Single = Single.MaxValue

    Private _lastTileZoomUsed As Single = 1.0F
    Private _ownedDlcMask As Integer = -1

    Private Property Overlay2DWorldHeightUnits As Single = 45.0F
    Private Property Overlay2DMinDrawPx As Single = 20.0F
    Private Property Overlay2DMaxDrawPx As Single = 64.0F
    Private _fixedMap As Boolean = True
    Private _mapFast As Boolean = False
    Private _offlineMap As Boolean = False
    Private _offlineCompositor As Reader
    Private ReadOnly _compositorLock As New Object()
    Private _lastOfflineTileZ As Integer = -1
    Private _offlineRenderZoomStops() As Single = Array.Empty(Of Single)()
    Private _offlineZoomIdx As Integer
    Private _lastUMDRetryAttempt As DateTime = DateTime.MinValue
    Private Const UMD_RETRY_INTERVAL_MS As Double = 3000.0
    Private Const TILE_THRESHOLD_NORMAL As Single = 10.0F
    Private Const PC_IP As String = "192.168.178.23"

    Private _isDay As Boolean = True
    Private _dayNightAuto As Boolean = False
    Private _lastDayState As Boolean = True

    Private _displayHeading As Single = 0.0F
    Private _useSmoothing As Boolean = False
    Private _lastHeadingUpdate As DateTime = DateTime.MinValue

    Private Const SMOOTHING_FACTOR As Single = 0.3F

    Private _displayTruckX As Single = Single.NaN
    Private _displayTruckZ As Single = Single.NaN

    Private _zoomLevel As Single = 1.0F
    Private Const ZOOM_MIN As Single = 0.2F
    Private Const ZOOM_MAX As Single = 6.0F
    Private Const ZOOM_STEP As Single = 0.2F
    Private Const ETS2_SCALE As Double = 19.0

    Private Const SCALE_VAL_PATH As String = "C:\FIRMWARE\NAVIGATION.ESA\SCALE.VAL"

    Private Shared ReadOnly LIQUIDMAP_STA_PATH As String = IO.Path.Combine(IO.Path.GetDirectoryName(SCALE_VAL_PATH), "LIQUIDMAP.STA")

    Private Shared ReadOnly ScaleStepsM As Double() = {
        50, 100, 200, 500, 750,
        1000, 2000, 5000, 7500, 10000,
        20000, 50000, 75000, 100000, 200000, 500000, 1000000
    }

    Private Const MAP_OFFSET_X As Double = -94600
    Private Const MAP_OFFSET_Z As Double = -80000
    Private Const MAP_SCALE As Double = 0.0117

    Private Shared ReadOnly MapPaper As Color = Color.FromArgb(70, 86, 91)

    Private _blinkerState As Boolean = False

    Private _mapDirty As Boolean = True
    Private _compassDirty As Boolean = True
    Private _forceRedraw As Boolean = False

    ' GDI
    Private _scalePen As Pen
    Private _scaleFont As Font
    Private _speedLimitBg As SolidBrush
    Private _speedLimitRim As Pen
    Private _speedLimitFont As Font
    Private _speedLimitNumBrush As SolidBrush
    Private _speedLimitSf As StringFormat

    Private _compassImage As Image
    Private _vehicleImage As Image
    Private _idriveImage As Image = My.Resources.Resources.IDRIVE_ON

    Private _tcpBuffer() As Byte = New Byte(65535) {}

    Private WithEvents renderTimer As System.Windows.Forms.Timer

    Private Const PERSPECTIVE_ZOOM_DIVISOR_FAR As Single = 2.0F
    Private Const PERSPECTIVE_ZOOM_DIVISOR_NEAR As Single = 4.0F
    Private _perspective3D As Boolean = True
    Private ReadOnly _perspRenderer As New Perspective3DRenderer()

#Region "Form Init"

    Public Sub New()
        InitializeComponent()
        _offlineCompositor = Nothing
    End Sub

    Private Sub MainPage_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Task.Run(Sub()
                     Try
                         Using udp As New UdpClient()
                             udp.EnableBroadcast = True
                             Dim msg = Encoding.UTF8.GetBytes("HIQNAV_HELLO")
                             Dim ep As New IPEndPoint(IPAddress.Broadcast, 11001)

                             For i = 1 To 5
                                 udp.Send(msg, msg.Length, ep)
                                 Thread.Sleep(500)
                             Next

                             While _running
                                 udp.Send(msg, msg.Length, ep)
                                 Thread.Sleep(5000)
                             End While
                         End Using
                     Catch
                     End Try
                 End Sub)

        Me.Text = "MainPage"
        'Me.MinimumSize = New Size(1440, 900)
        Me.TopMost = True

        _compassImage = My.Resources.COMPASS
        _vehicleImage = My.Resources.POINTER

        InitGdiCache()
        WireDesignerButtons()

        If VEHICLE_BOX IsNot Nothing Then
            If _perspective3D Then VEHICLE_BOX.Hide() Else VEHICLE_BOX.Show()
        End If

        StartUdpReceiver()
        LoadMap()
        _offlineMap = True
        RebuildOfflineZoomStops()

        _offlineZoomIdx = LoadLastScaleIdx()
        _useSmoothing = LoadLiquidMapSetting()
        _forceRedraw = True
        _mapDirty = True
        StartRenderTimer()

        UpdateTimeModeBtn()
        UpdateNetworkModeBtn()
        UpdateMapModeBtn()
        StatusScreen.Hide()
    End Sub

    Private Shared Function FindUMDPath(isDay As Boolean) As String
        Dim mapFolder = If(isDay, "DAY.MAP", "NIGHT.MAP")
        Dim candidateNames = {"EUROPE.UMD", "EUROPE.CMD"} ' CMD only for checking, not used.

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

    Public Function TryReacquireOfflineCompositor() As Reader.LoadResult
        SyncLock _compositorLock
            If _offlineCompositor IsNot Nothing AndAlso _offlineCompositor.IsAvailable Then
                Return New Reader.LoadResult With {.Status = Reader.LoadStatus.Ok}
            End If
            If (DateTime.Now - _lastUMDRetryAttempt).TotalMilliseconds < UMD_RETRY_INTERVAL_MS Then
                Return New Reader.LoadResult With {.Status = Reader.LoadStatus.NotFound}
            End If
            _lastUMDRetryAttempt = DateTime.Now

            Dim path = FindUMDPath(isDay:=_isDay)
            If String.IsNullOrEmpty(path) Then
                Return New Reader.LoadResult With {.Status = Reader.LoadStatus.NotFound}
            End If

            Dim result = Reader.TryLoadWithStatus(path)
            If result.Status = Reader.LoadStatus.Ok Then
                _offlineCompositor?.Dispose()
                _offlineCompositor = result.Reader
                _offlineCompositor.BackgroundColor = GetMapBackgroundColor(_isDay)
                RebuildOfflineZoomStops()
                _forceRedraw = True
                _lastTileX = Single.MaxValue
                Debug.WriteLine("[LOOP] Offline-Compositor reacquired — Drive available.")
            Else
                Debug.WriteLine($"[LOOP] Offline-Compositor not loadable: {result.Status} — {result.Message}")
            End If

            Return result
        End SyncLock
    End Function

    Private Sub InitGdiCache()
        _scalePen = New Pen(Color.White, 2)
        _scaleFont = New Font("Consolas", 7)
        _speedLimitBg = New SolidBrush(Color.White)
        _speedLimitRim = New Pen(Color.Red, 5)
        _speedLimitFont = New Font("Arial", 13, FontStyle.Bold)
        _speedLimitNumBrush = New SolidBrush(Color.Black)
        _speedLimitSf = New StringFormat With {
            .Alignment = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }
    End Sub

    Private Sub WireDesignerButtons()
        AddHandler ZOOMIN_BTN.Click, Sub(s, ev) ZoomBy(ZOOM_STEP)

        AddHandler ZOOMOUT_BTN.Click, Sub(s, ev) ZoomBy(-ZOOM_STEP)

        AddHandler COMPASSMODE_BTN.Click, Sub(s, ev)
                                              If _perspective3D Then Return

                                              Dim oldEff = GetEffectiveRenderZoom()
                                              _fixedMap = Not _fixedMap
                                              If _offlineMap AndAlso _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
                                                  RebuildOfflineZoomStops()
                                                  Dim bestIdx = 0, bestD = Single.MaxValue
                                                  For i = 0 To _offlineRenderZoomStops.Length - 1
                                                      Dim d = Math.Abs(_offlineRenderZoomStops(i) - oldEff)
                                                      If d < bestD Then bestD = d : bestIdx = i
                                                  Next
                                                  SetOfflineZoomIdx(bestIdx)
                                              Else
                                                  _zoomLevel = If(_fixedMap,
                                                      Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, oldEff / 0.65F)),
                                                      Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, oldEff)))
                                              End If
                                              _lastTileX = Single.MaxValue
                                              _mapDirty = True
                                          End Sub

        AddHandler FASTMODE_BTN.Click, Sub(s, ev)
                                           _mapFast = Not _mapFast
                                           _lastTileX = Single.MaxValue
                                       End Sub
        AddHandler TIMEMODE_BTN.Click, Sub(s, ev)
                                           If _dayNightAuto OrElse _isSwitchingDayNight Then Return
                                           SetDayNightModeAsync(Not _isDay)
                                       End Sub

        AddHandler NETWORKMODE_BTN.Click, Sub(s, ev)
                                              Dim oldEff = GetEffectiveRenderZoom()
                                              _offlineMap = Not _offlineMap
                                              _lastTileX = Single.MaxValue
                                              If Not _offlineMap Then
                                                  _lastOfflineTileZ = -1
                                                  SyncOnlineZoomFromOfflineStop()
                                                  _offlineRenderZoomStops = Array.Empty(Of Single)()
                                              Else
                                                  RebuildOfflineZoomStops()
                                                  If _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
                                                      Dim bestIdx = 0, bestD = Single.MaxValue
                                                      For i = 0 To _offlineRenderZoomStops.Length - 1
                                                          Dim d = Math.Abs(_offlineRenderZoomStops(i) - oldEff)
                                                          If d < bestD Then bestD = d : bestIdx = i
                                                      Next
                                                      SetOfflineZoomIdx(bestIdx)
                                                  End If
                                              End If
                                              Try : _tcpClient?.Close() : Catch : End Try
                                              UpdateNetworkModeBtn()
                                              _mapDirty = True
                                          End Sub

        AddHandler MAPMODE_BTN.Click, Sub(s, ev) Toggle3DPerspective()
    End Sub

#End Region

#Region "Button Image Updates"

    Private Sub UpdateTimeModeBtn()
        If TIMEMODE_BTN Is Nothing Then Return
        TIMEMODE_BTN.BackgroundImage = If(_isDay,
            CType(My.Resources.DAY, Image),
            CType(My.Resources.NIGHT, Image))
        TIMEMODE_BTN.Enabled = Not _dayNightAuto
    End Sub

    Private Sub UpdateNetworkModeBtn()
        If NETWORKMODE_BTN Is Nothing Then Return
        NETWORKMODE_BTN.BackgroundImage = If(_offlineMap,
            CType(My.Resources.OFFLINE, Image),
            CType(My.Resources.ONLINE, Image))
    End Sub

    Private Sub UpdateMapModeBtn()
        If MAPMODE_BTN Is Nothing Then Return
        MAPMODE_BTN.Text = If(_perspective3D, "3D", "2D")
    End Sub

#End Region

#Region "UDP Receiver"

    Private Sub StartUdpReceiver()
        _running = True
        Try
            _udpClient = New UdpClient()
            _udpClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress, True)
            _udpClient.Client.Bind(New IPEndPoint(IPAddress.Any, 11000))

            _receiveThread = New Thread(AddressOf ReceiveLoop) With {
                .IsBackground = True,
                .Name = "UDP-Receiver"
            }
            _receiveThread.Start()
        Catch ex As Exception
            Debug.WriteLine($"[UDP] BIND FAILED: {ex.Message} — Map-Rendering still continues, but without Telemetry...")
            UpdateStatus("UDP Error: " & ex.Message, Color.OrangeRed)
        End Try
    End Sub

    Private Sub ReceiveLoop()
        Dim ep As New IPEndPoint(IPAddress.Any, 0)
        While _running
            Try
                Dim bytes = _udpClient.Receive(ep)
                Dim json = Encoding.UTF8.GetString(bytes)
                Dim packet = JsonConvert.DeserializeObject(Of TelemetryPacket)(json)
                If packet IsNot Nothing Then
                    _data = packet
                    _lastReceived = DateTime.Now
                    _mapDirty = True
                    _compassDirty = True
                End If
            Catch ex As SocketException
                UpdateStatus("UDP Error: " & ex.Message, Color.OrangeRed)
                If _running Then Thread.Sleep(500)
            Catch
            End Try
        End While
    End Sub

#End Region

#Region "Load Offline-Map and TCP-Stream"

    Private Sub LoadMap()
        Dim mapPath = IO.Path.Combine(Application.StartupPath, "ets2map.png")
        _mapImage = If(IO.File.Exists(mapPath), New Bitmap(mapPath), CreatePlaceholderMap())
        Task.Run(Sub() MapProviderMain())
    End Sub

    Private Sub MapProviderMain()
        While _running
            If _offlineMap Then OfflineTileLoop() Else ConnectMapStream()
        End While
    End Sub

    Private Sub ConnectMapStream()
        While _running AndAlso Not _offlineMap
            Try
                Try : _tcpClient?.Close() : _tcpClient?.Dispose() : Catch : End Try
                _tcpClient = New TcpClient()
                _tcpClient.Connect(PC_IP, 11021)
                UpdateStatus("Online-Map: Connected", Color.LimeGreen)
                RequestTileLoop()
            Catch
                If Not _offlineMap Then UpdateStatus("Online-Map: Waiting for PC…", Color.Orange)
                Thread.Sleep(3000)
            End Try
        End While
    End Sub

    Private Sub OfflineTileLoop()
        Debug.WriteLine($"[LOOP] OfflineTileLoop ENTER (running={_running}, offlineMap={_offlineMap})")
        While _running AndAlso _offlineMap
            Try
                SyncLock _compositorLock
                    If _offlineCompositor Is Nothing OrElse Not _offlineCompositor.IsAvailable Then
                        Dim reacquireResult = TryReacquireOfflineCompositor()
                        If _offlineCompositor Is Nothing OrElse Not _offlineCompositor.IsAvailable Then
                            Select Case reacquireResult.Status
                                Case Reader.LoadStatus.Incompatible
                                    UpdateStatus("Offline-Map: " & reacquireResult.Message, Color.OrangeRed)
                                Case Reader.LoadStatus.NeedsAppUpdate
                                    UpdateStatus("Offline-Map: " & reacquireResult.Message, Color.Orange)
                                Case Else
                                    UpdateStatus("Offline-Map: EUROPE.UMD missing", Color.Orange)
                            End Select
                            ReturnToStatusScreen()
                            Thread.Sleep(1000)
                            Continue While
                        End If
                    End If

                    Dim threshold = If(_mapFast, 0.0F, TILE_THRESHOLD_NORMAL)
                    Dim moved = _forceRedraw OrElse
                    Math.Abs(_displayTruckX - _lastTileX) > threshold OrElse
                    Math.Abs(_displayTruckZ - _lastTileZ) > threshold

                    If moved Then
                        _forceRedraw = False
                        Dim renderZoom = GetEffectiveRenderZoom()
                        Dim cw = Map_Panel.ClientSize.Width
                        Dim ch = Map_Panel.ClientSize.Height
                        If cw < 9 OrElse ch < 9 Then Thread.Sleep(100) : Continue While

                        Dim requestedPerspective3D = _perspective3D

                        Dim tileZoom = If(requestedPerspective3D, renderZoom / GetPerspectiveZoomDivisor(), renderZoom)

                        Const MIN_TILE_ZOOM As Single = 0.0005F
                        If Single.IsNaN(tileZoom) OrElse Single.IsInfinity(tileZoom) OrElse tileZoom < MIN_TILE_ZOOM Then
                            Debug.WriteLine($"[LOOP] WARNING: tileZoom implausible ({tileZoom}), clamped to {MIN_TILE_ZOOM} (renderZoom={renderZoom}, isDay={_isDay})")
                            tileZoom = MIN_TILE_ZOOM
                        End If

                        Dim tileOversize = If(requestedPerspective3D, 1.5F, 1.0F)
                        Dim reqW = CInt(cw * tileOversize)
                        Dim reqH = CInt(ch * tileOversize)

                        Debug.WriteLine($"[LOOP] requesting tile: x={_displayTruckX:F1} z={_displayTruckZ:F1} renderZoom={renderZoom:F4} tileZoom={tileZoom:F4} cw={cw} ch={ch} reqW={reqW} reqH={reqH}")

                        Dim usedZ = 0
                        Dim renderedTile = _offlineCompositor.Render(_displayTruckX, _displayTruckZ, tileZoom, reqW, reqH, usedZ)

                        Dim newTile As Bitmap = Nothing
                        If renderedTile IsNot Nothing Then
                            newTile = New Bitmap(renderedTile)
                        End If

                        Debug.WriteLine($"[LOOP] Render returned: {(If(newTile IsNot Nothing, $"{newTile.Width}x{newTile.Height}", "Nothing"))}, usedZ={usedZ}")

                        If newTile IsNot Nothing Then
                            _lastOfflineTileZ = usedZ
                            SyncLock _tileLock
                                _tileIsPerspective3D = requestedPerspective3D
                                If _activeTile = 0 Then
                                    _tileB?.Dispose() : _tileB = newTile : _activeTile = 1
                                Else
                                    _tileA?.Dispose() : _tileA = newTile : _activeTile = 0
                                End If
                            End SyncLock
                            _lastTileX = _displayTruckX
                            _lastTileZ = _displayTruckZ
                            _lastTileZoomUsed = tileZoom
                            _mapDirty = True
                            UpdateStatus("Offline-Map: Ready", Color.LimeGreen)
                            StatusScreen.HideOverlay()
                        End If
                    End If
                End SyncLock
            Catch ex As ObjectDisposedException
                Debug.WriteLine($"[LOOP] Offline-Reader-Stream invalid (Disc removed/Standby?): {ex.Message}")
                SyncLock _compositorLock
                    _offlineCompositor?.Dispose()
                    _offlineCompositor = Nothing
                End SyncLock
                UpdateStatus("Offline-Map: EUROPE.UMD missing", Color.Orange)
                ReturnToStatusScreen()
            Catch ex As IOException
                Debug.WriteLine($"[LOOP] Offline-Reader Readerror (Disc damaged?): {ex.Message}")
                SyncLock _compositorLock
                    _offlineCompositor?.Dispose()
                    _offlineCompositor = Nothing
                End SyncLock
                UpdateStatus("Offline-Map: Readerror - Disc damaged?", Color.OrangeRed)
                ReturnToStatusScreen()
            Catch ex As Exception
                Debug.WriteLine($"[LOOP] EXCEPTION: {ex}")
                UpdateStatus("Offline-Map: Error - " & ex.Message, Color.OrangeRed)
            End Try
            If Not _mapFast Then Thread.Sleep(50)
        End While
        Debug.WriteLine("[LOOP] OfflineTileLoop EXIT")

        If _running Then StatusScreen.HideOverlay()
    End Sub

    Private Sub Draw3DPerspective(g As Graphics, tile As Bitmap,
                              panelW As Integer, panelH As Integer)
        Try
            _perspRenderer.IsDay = _isDay
            _perspRenderer.VehicleImage = _vehicleImage
            _perspRenderer.MapPaperColor = MapPaper
            _perspRenderer.Render(g, tile, panelW, panelH, _displayHeading)

            DrawPerspectiveOverlays(g, tile, panelW, panelH)
            DrawScaleBar(g, panelW, panelH)
        Catch ex As Exception
            Debug.WriteLine($"[3D] EXCEPTION: {ex}")
        End Try
    End Sub


    Private Const OVERLAYS_MAX_HALF_WORLD_2D As Single = 5000.0F
    Private Const OVERLAYS_MAX_HALF_WORLD_3D As Single = 25000.0F

    Private ReadOnly _overlayDebugLoggedPerMode As New HashSet(Of String)()

    Private Sub LogOverlayDebugOnce(is3D As Boolean, msg As String)
        Dim key = $"{_isDay}|{is3D}"
        If _overlayDebugLoggedPerMode.Add(key) Then
            Debug.WriteLine($"[Overlays-Debug] isDay={_isDay} 3D={is3D}: {msg}")
        End If
    End Sub

    Private Sub DrawPerspectiveOverlays(g As Graphics, tile As Bitmap, panelW As Integer, panelH As Integer)
        Dim comp As Reader = _offlineCompositor

        If comp Is Nothing Then
            LogOverlayDebugOnce(True, "compositor=Nothing (no Reader loaded)")
            Return
        End If
        If Not comp.HasOverlays Then
            LogOverlayDebugOnce(True, "comp.HasOverlays=False - loaded UMD does not contain Overlay section (with old packer without Overlay support packed?)")
            Return
        End If
        If _lastTileZoomUsed <= 0.0001F Then
            LogOverlayDebugOnce(True, $"_lastTileZoomUsed too small ({_lastTileZoomUsed})")
            Return
        End If

        Const MaxReasonableCoord As Single = 5000000.0F
        If Single.IsNaN(_lastTileX) OrElse Single.IsInfinity(_lastTileX) OrElse Math.Abs(_lastTileX) > MaxReasonableCoord Then
            LogOverlayDebugOnce(True, $"_lastTileX implausible ({_lastTileX})")
            Return
        End If
        If Single.IsNaN(_lastTileZ) OrElse Single.IsInfinity(_lastTileZ) OrElse Math.Abs(_lastTileZ) > MaxReasonableCoord Then
            LogOverlayDebugOnce(True, $"_lastTileZ implausible ({_lastTileZ})")
            Return
        End If

        Dim halfWorld = (Math.Max(tile.Width, tile.Height) / 2.0F) / _lastTileZoomUsed
        LogOverlayDebugOnce(True, $"tile={tile.Width}x{tile.Height}  zoomUsedForTile={_lastTileZoomUsed:N6}  halfWorld={halfWorld:N1}")

        If Single.IsNaN(halfWorld) OrElse Single.IsInfinity(halfWorld) OrElse halfWorld <= 0.0F Then Return
        If halfWorld > OVERLAYS_MAX_HALF_WORLD_3D Then Return

        Dim rawOverlays = comp.GetOverlaysNear(_lastTileX, _lastTileZ, halfWorld * 1.15F, ownedDlcMask:=_ownedDlcMask, includeSecret:=False)

        LogOverlayDebugOnce(True, $"found Overlays in radius: {rawOverlays.Count}")

        If rawOverlays.Count = 0 Then Return

        Dim instances As New List(Of Perspective3DRenderer.OverlayInstance)()
        For Each o In rawOverlays
            Dim icon = comp.GetOverlayIcon(o.Name)
            If icon Is Nothing Then Continue For
            instances.Add(New Perspective3DRenderer.OverlayInstance With {
                .WorldX = o.X, .WorldZ = o.Y, .Icon = icon,
                .Width = o.Width, .Height = o.Height
            })
        Next
        If instances.Count = 0 Then Return

        _perspRenderer.RenderOverlays(g, instances, tile.Width, tile.Height, panelW, panelH, _displayHeading, _lastTileX, _lastTileZ, _lastTileZoomUsed)
    End Sub

    Public Sub Toggle3DPerspective()
        _perspective3D = Not _perspective3D
        If VEHICLE_BOX IsNot Nothing Then
            If _perspective3D Then VEHICLE_BOX.Hide() Else VEHICLE_BOX.Show()
        End If
        _lastTileX = Single.MaxValue
        _forceRedraw = True
        _mapDirty = True
        UpdateMapModeBtn()
    End Sub

    Private Sub RequestTileLoop()
        Dim ns = _tcpClient.GetStream()
        Dim bw = New BinaryWriter(ns)
        Dim br = New BinaryReader(ns)

        While _running AndAlso _tcpClient.Connected AndAlso Not _offlineMap
            Try
                Dim threshold = If(_mapFast, 0.0F, TILE_THRESHOLD_NORMAL)
                Dim moved = _forceRedraw OrElse
            Math.Abs(_displayTruckX - _lastTileX) > threshold OrElse
            Math.Abs(_displayTruckZ - _lastTileZ) > threshold

                If moved Then
                    _forceRedraw = False
                    bw.Write(_displayTruckX)
                    bw.Write(_displayTruckZ)
                    bw.Write(GetEffectiveRenderZoom())
                    bw.Write(CSng(Map_Panel.ClientSize.Width))
                    bw.Write(CSng(Map_Panel.ClientSize.Height))
                    bw.Write(If(_isDay, CSng(1.0F), CSng(0.0F)))
                    bw.Flush()

                    Dim len = br.ReadInt32()
                    If _tcpBuffer.Length < len Then ReDim _tcpBuffer(len - 1)
                    Dim offset = 0
                    While offset < len
                        Dim read = ns.Read(_tcpBuffer, offset, len - offset)
                        If read <= 0 Then Exit While
                        offset += read
                    End While

                    Dim newTile As Bitmap
                    Using ms As New MemoryStream(_tcpBuffer, 0, len)
                        Using tmpBmp = New Bitmap(ms)
                            newTile = CloneBitmap32PArgb(tmpBmp)
                        End Using
                    End Using

                    SyncLock _tileLock
                        _tileIsPerspective3D = _perspective3D
                        If _activeTile = 0 Then
                            _tileB?.Dispose() : _tileB = newTile : _activeTile = 1
                        Else
                            _tileA?.Dispose() : _tileA = newTile : _activeTile = 0
                        End If
                    End SyncLock
                    _lastTileX = _displayTruckX
                    _lastTileZ = _displayTruckZ
                    _mapDirty = True
                End If
            Catch
                Exit While
            End Try
            If Not _mapFast Then Thread.Sleep(50)
        End While
    End Sub

    Private Shared Function CloneBitmap32PArgb(src As Bitmap) As Bitmap
        Dim clone As New Bitmap(src.Width, src.Height, Imaging.PixelFormat.Format32bppPArgb)
        Using gfx = Graphics.FromImage(clone)
            gfx.DrawImageUnscaled(src, 0, 0)
        End Using
        Return clone
    End Function

    Private Function CreatePlaceholderMap() As Bitmap
        Dim bmp As New Bitmap(512, 512)
        Using g = Graphics.FromImage(bmp)
            g.Clear(Color.FromArgb(28, 40, 28))
            Using gp As New Pen(Color.FromArgb(45, 255, 255, 255), 1)
                For x = 0 To 512 Step 40 : g.DrawLine(gp, x, 0, x, 512) : Next
                For y = 0 To 512 Step 40 : g.DrawLine(gp, 0, y, 512, y) : Next
            End Using
            Using fnt As New Font("Consolas", 9, FontStyle.Bold)
                Dim sf As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center
                }
                g.DrawString("EUROPE.UMD" & vbCrLf & "or Server missing", fnt, Brushes.Gray, New RectangleF(0, 0, 512, 512), sf)
            End Using
        End Using
        Return bmp
    End Function

#End Region

#Region "Renderer"

    Private Sub DrawTileUniformCover(g As Graphics, tile As Bitmap, w As Integer, h As Integer, rotateNorthUp As Boolean)
        Dim tw = CSng(tile.Width)
        Dim th = CSng(tile.Height)
        If tw < 1.0F OrElse th < 1.0F Then Return

        If Not rotateNorthUp Then
            Dim st = g.Save()
            g.SmoothingMode = SmoothingMode.None
            g.PixelOffsetMode = PixelOffsetMode.Half
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.CompositingMode = CompositingMode.SourceOver
            g.Clear(MapPaper)
            Dim twi = CInt(tw), thi = CInt(th)
            Dim scale = CSng(Math.Max(w / tw, h / th))
            Dim srcWi = Math.Min(CInt(Math.Max(1, Math.Round(w / scale))), twi)
            Dim srcHi = Math.Min(CInt(Math.Max(1, Math.Round(h / scale))), thi)
            Dim srcXi = (twi - srcWi) \ 2
            Dim srcYi = (thi - srcHi) \ 2
            Const pad As Integer = 2
            g.SetClip(New Rectangle(0, 0, w, h))
            g.DrawImage(tile, New Rectangle(-pad, -pad, w + 2 * pad, h + 2 * pad), New Rectangle(srcXi, srcYi, srcWi, srcHi), GraphicsUnit.Pixel)
            g.Restore(st)
            Return
        End If

        Dim s = g.Save()
        g.SetClip(New Rectangle(0, 0, w, h))
        g.InterpolationMode = InterpolationMode.Bilinear
        Dim scaleN = CSng(Math.Max(w / tw, h / th) * 1.25F)
        Dim dw = tw * scaleN, dh = th * scaleN
        g.TranslateTransform(w / 2.0F, h / 2.0F)
        g.RotateTransform(-_displayHeading)
        g.DrawImage(tile, -dw / 2.0F, -dh / 2.0F, dw, dh)
        g.Restore(s)
    End Sub

    Private Sub Map_Panel_Paint(sender As Object, e As PaintEventArgs) Handles Map_Panel.Paint
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.Bilinear

        Dim w = Map_Panel.ClientSize.Width
        Dim h = Map_Panel.ClientSize.Height

        Dim tileWasNothing As Boolean
        SyncLock _tileLock
            Dim tile = If(_activeTile = 0, _tileA, _tileB)
            Dim tileIsPerspective3D = _tileIsPerspective3D
            tileWasNothing = (tile Is Nothing)

            If tile IsNot Nothing Then
                If tileIsPerspective3D Then
                    Draw3DPerspective(g, tile, w, h)
                    DrawIdriveBadge(g, w, h)
                    Return
                End If
                DrawTileUniformCover(g, tile, w, h, rotateNorthUp:=_fixedMap)
                Draw2DOverlays(g, w, h)
            End If
        End SyncLock

        If tileWasNothing Then
            Debug.WriteLine($"[PAINT] tile is Nothing (activeTile={_activeTile}, tileA={(_tileA IsNot Nothing)}, tileB={(_tileB IsNot Nothing)}), offlineMap={_offlineMap}, running={_running}")

            If _mapImage Is Nothing Then Return
            Dim truckPxX = (_data.TruckX - MAP_OFFSET_X) * MAP_SCALE
            Dim truckPxZ = (_data.TruckZ - MAP_OFFSET_Z) * MAP_SCALE
            Dim destW = CInt(_mapImage.Width * _zoomLevel)
            Dim destH = CInt(_mapImage.Height * _zoomLevel)
            Dim offX = CInt(w / 2 - truckPxX * _zoomLevel)
            Dim offZ = CInt(h / 2 - truckPxZ * _zoomLevel)

            If _fixedMap Then
                Dim s = g.Save()
                g.TranslateTransform(w / 2.0F, h / 2.0F)
                g.RotateTransform(-_displayHeading)
                g.TranslateTransform(-w / 2.0F, -h / 2.0F)
                g.DrawImage(_mapImage, offX, offZ, destW, destH)
                g.Restore(s)
            Else
                g.DrawImage(_mapImage, offX, offZ, destW, destH)
            End If
        End If

        DrawScaleBar(g, w, h)
        DrawIdriveBadge(g, w, h)
    End Sub

    Private Sub VEHICLE_BOX_Paint(sender As Object, e As PaintEventArgs) Handles VEHICLE_BOX.Paint
        If _vehicleImage Is Nothing Then Return

        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.Bilinear

        Dim cx = VEHICLE_BOX.Width / 2.0F
        Dim cy = VEHICLE_BOX.Height / 2.0F
        Dim rotDeg = If(_fixedMap, 0.0F, _displayHeading)

        g.TranslateTransform(cx, cy)
        g.RotateTransform(rotDeg)
        g.DrawImage(_vehicleImage, -cx, -cy, VEHICLE_BOX.Width, VEHICLE_BOX.Height)
    End Sub

    Private Sub DrawIdriveBadge(g As Graphics, panelW As Integer, panelH As Integer)
        If _idriveImage Is Nothing Then Return

        Dim state = g.Save()
        g.ResetTransform()

        Const leftMargin = 46
        Const bottomMargin = 12
        Const boxW = 504
        Const boxH = 159

        Dim x = leftMargin
        Dim y = panelH - bottomMargin - boxH

        Dim imgW = _idriveImage.Width
        Dim imgH = _idriveImage.Height
        Dim scale = Math.Min(boxW / CSng(imgW), boxH / CSng(imgH))
        Dim drawW = imgW * scale
        Dim drawH = imgH * scale
        Dim drawX = x + (boxW - drawW) / 2.0F
        Dim drawY = y + (boxH - drawH) / 2.0F

        g.DrawImage(_idriveImage, drawX, drawY, drawW, drawH)
        g.Restore(state)
    End Sub

    Private Sub DrawScaleBar(g As Graphics, w As Integer, h As Integer)
        Const refBarPx = 80
        Dim tile As Bitmap = Nothing
        SyncLock _tileLock
            tile = If(_activeTile = 0, _tileA, _tileB)
        End SyncLock

        Dim rawMeters As Double
        Dim renderZoom As Single
        If tile IsNot Nothing Then
            renderZoom = GetEffectiveRenderZoom()
            If renderZoom <= 0.0001F Then renderZoom = 0.0001F
            rawMeters = (refBarPx / renderZoom) * ETS2_SCALE
        Else
            renderZoom = _zoomLevel
            rawMeters = refBarPx / (_zoomLevel * MAP_SCALE * 1000)
        End If

        Dim snapped = ScaleStepsM.OrderBy(Function(s) Math.Abs(s - rawMeters)).First()

        If rawMeters > ScaleStepsM.Last() * 1.5 Then
            Return
        End If

        Dim scaleTxt As String
        If snapped >= 1000 Then
            Dim km = snapped / 1000.0
            scaleTxt = If(km = Math.Floor(km), $"{CInt(km)} km", $"{km:F1} km")
        Else
            scaleTxt = $"{CInt(snapped)} m"
        End If

        If ZOOM_SCALE IsNot Nothing AndAlso ZOOM_SCALE.Text <> scaleTxt Then
            ZOOM_SCALE.Text = scaleTxt
        End If
    End Sub

    Private Shared Function NiceMetersLength(raw As Double) As Double
        If raw <= 0 OrElse Double.IsNaN(raw) OrElse Double.IsInfinity(raw) Then Return 100
        Dim exp = Math.Floor(Math.Log10(raw))
        Dim basePow = Math.Pow(10, exp)
        Dim frac = raw / basePow
        Dim nf As Double = If(frac < 1.5, 1, If(frac < 3.5, 2, If(frac < 7.5, 5, 10)))
        Return nf * basePow
    End Function

#End Region

#Region "Coordinates"

    Private Sub Draw2DOverlays(g As Graphics, w As Integer, h As Integer)
        Dim comp As Reader = _offlineCompositor
        If comp Is Nothing OrElse Not comp.HasOverlays Then Return

        Dim zoom = GetEffectiveScreenZoom(w, h)
        If Single.IsNaN(zoom) OrElse Single.IsInfinity(zoom) OrElse zoom <= 0.0001F Then Return

        Dim truckX = _lastTileX
        Dim truckZ = _lastTileZ
        Const MaxReasonableCoord As Single = 5000000.0F
        If Single.IsNaN(truckX) OrElse Single.IsInfinity(truckX) OrElse Math.Abs(truckX) > MaxReasonableCoord Then Return
        If Single.IsNaN(truckZ) OrElse Single.IsInfinity(truckZ) OrElse Math.Abs(truckZ) > MaxReasonableCoord Then Return

        Dim diagPx = CSng(Math.Sqrt(CDbl(w) * w + CDbl(h) * h))
        Dim halfWorld = (diagPx / 2.0F) / zoom
        If Single.IsNaN(halfWorld) OrElse Single.IsInfinity(halfWorld) OrElse halfWorld <= 0.0F Then Return
        LogOverlayDebugOnce(False, $"zoom={zoom:N6}  halfWorld={halfWorld:N1}")
        If halfWorld > OVERLAYS_MAX_HALF_WORLD_2D Then Return

        Dim rawOverlays = comp.GetOverlaysNear(truckX, truckZ, halfWorld * 1.15F, ownedDlcMask:=_ownedDlcMask, includeSecret:=False)
        LogOverlayDebugOnce(False, $"found Overlays in radius: {rawOverlays.Count}")
        If rawOverlays.Count = 0 Then Return

        Dim worldHPx = Overlay2DWorldHeightUnits * zoom
        Dim drawH = Math.Max(Overlay2DMinDrawPx, Math.Min(worldHPx, Overlay2DMaxDrawPx))

        Dim savedState = g.Save()
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        Try
            For Each o In rawOverlays
                Dim icon = comp.GetOverlayIcon(o.Name)
                If icon Is Nothing Then Continue For

                Dim pt = WorldToScreen(o.X, o.Y, w, h)
                If pt.X < -drawH OrElse pt.X > w + drawH OrElse
                   pt.Y < -drawH OrElse pt.Y > h + drawH Then Continue For

                Dim aspect = If(o.Height > 0, o.Width / o.Height, 1.0F)
                Dim dw = drawH * aspect
                g.DrawImage(icon, pt.X - dw / 2.0F, pt.Y - drawH / 2.0F, dw, drawH)
            Next
        Finally
            g.Restore(savedState)
        End Try
    End Sub

    Private Function GetEffectiveScreenZoom(w As Integer, h As Integer) As Single
        Dim zoom = _lastTileZoomUsed
        Dim tileWidth As Integer = 0
        SyncLock _tileLock
            Dim tile = If(_activeTile = 0, _tileA, _tileB)
            If tile IsNot Nothing Then tileWidth = tile.Width
        End SyncLock
        If tileWidth > 0 Then
            Dim tileScale = CSng(Math.Max(w, h)) / tileWidth
            zoom *= If(_fixedMap, tileScale * 1.25F, tileScale)
        End If
        Return zoom
    End Function

    Private Function WorldToScreen(wx As Single, wz As Single, w As Integer, h As Integer) As PointF
        Dim zoom = GetEffectiveScreenZoom(w, h)

        Dim dx = (wx - _lastTileX) * zoom
        Dim dz = (wz - _lastTileZ) * zoom

        If _fixedMap Then
            Dim rad = -_displayHeading * CSng(Math.PI) / 180.0F
            Dim cosR = CSng(Math.Cos(rad))
            Dim sinR = CSng(Math.Sin(rad))
            Return New PointF(w / 2.0F + dx * cosR - dz * sinR, h / 2.0F + dx * sinR + dz * cosR)
        End If
        Return New PointF(w / 2.0F + dx, h / 2.0F + dz)
    End Function

    Private Function ScreenToWorld(sx As Integer, sy As Integer, w As Integer, h As Integer) As (X As Single, Z As Single)
        Dim zoom = GetEffectiveScreenZoom(w, h)
        If zoom <= 0.00001F Then Return (_lastTileX, _lastTileZ)

        Dim dx = (sx - w / 2.0F) / zoom
        Dim dz = (sy - h / 2.0F) / zoom

        If _fixedMap Then
            Dim rad = _displayHeading * CSng(Math.PI) / 180.0F
            Dim cosR = CSng(Math.Cos(rad))
            Dim sinR = CSng(Math.Sin(rad))
            Return (_lastTileX + dx * cosR - dz * sinR, _lastTileZ + dx * sinR + dz * cosR)
        End If
        Return (_lastTileX + dx, _lastTileZ + dz)
    End Function

#End Region

#Region "Compass"

    Private Sub COMPASS_BOX_Paint(sender As Object, e As PaintEventArgs) Handles COMPASS_BOX.Paint
        If _compassImage Is Nothing Then Return

        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.Bilinear

        Dim cx = COMPASS_BOX.Width / 2.0F
        Dim cy = COMPASS_BOX.Height / 2.0F

        Dim size = CSng(Math.Min(COMPASS_BOX.Width, COMPASS_BOX.Height) / Math.Sqrt(2)) * 0.85F

        g.TranslateTransform(cx, cy)

        Dim compassRotation = If(Not _fixedMap, 0.0F, -_displayHeading)
        g.RotateTransform(compassRotation)
        g.DrawImage(_compassImage, -size / 2.0F, -size / 2.0F, size, size)
    End Sub

#End Region

#Region "Day/Night"

    Private Function ParseGameHour() As Integer
        Try
            If String.IsNullOrEmpty(_data.GameTime) OrElse Not _data.GameTime.Contains(":") Then Return -1
            Return CInt(_data.GameTime.Split(":"c)(0))
        Catch
            Return -1
        End Try
    End Function

    Private Sub UpdateDayNight()
        Dim udpActive = (DateTime.Now - _lastReceived).TotalSeconds < 5

        If udpActive Then
            _dayNightAuto = True
            Dim hour = ParseGameHour()
            If hour >= 0 Then
                Dim newIsDay = (hour >= 6 AndAlso hour < 20)
                If newIsDay <> _lastDayState Then
                    SetDayNightModeAsync(newIsDay)
                End If
            End If
        Else
            _dayNightAuto = False
        End If

        UpdateTimeModeBtn()
    End Sub

    Private _isSwitchingDayNight As Boolean = False

    Private Async Sub SetDayNightModeAsync(isDay As Boolean)
        If _isSwitchingDayNight Then Return
        _isSwitchingDayNight = True

        _isDay = isDay
        _lastDayState = _isDay

        Await Task.Run(Sub()
                           Dim path = FindUMDPath(isDay:=_isDay)
                           Dim candidate = Reader.TryCreate(path)
                           If candidate IsNot Nothing Then
                               SyncLock _compositorLock
                                   _offlineCompositor?.Dispose()
                                   _offlineCompositor = candidate
                                   _offlineCompositor.BackgroundColor = GetMapBackgroundColor(_isDay)
                                   RebuildOfflineZoomStops()
                               End SyncLock
                           End If
                       End Sub)

        _lastTileX = Single.MaxValue
        _lastTileZ = Single.MaxValue
        _forceRedraw = True
        _mapDirty = True
        UpdateTimeModeBtn()

        _isSwitchingDayNight = False
    End Sub

#End Region

#Region "Render-Timer"

    Private Sub StartRenderTimer()
        renderTimer = New System.Windows.Forms.Timer With {.Interval = 100}
        renderTimer.Start()
    End Sub

    Private Sub renderTimer_Tick(sender As Object, e As EventArgs) Handles renderTimer.Tick
        _blinkerState = Not _blinkerState

        Dim udpConnected = (DateTime.Now - _lastReceived).TotalSeconds < 3

        If _useSmoothing Then
            Dim diff = CSng(_data.Heading) - _displayHeading
            If diff > 180 Then diff -= 360
            If diff < -180 Then diff += 360
            If Math.Abs(diff) > 0.1F Then
                _displayHeading += diff * SMOOTHING_FACTOR
                _mapDirty = True
                _compassDirty = True
            End If
        Else
            If (DateTime.Now - _lastHeadingUpdate).TotalMilliseconds >= 500 Then
                Dim newHeading = CSng(_data.Heading)
                If Math.Abs(newHeading - _displayHeading) > 0.5F Then
                    _displayHeading = newHeading
                    _mapDirty = True
                    _compassDirty = True
                End If
                _lastHeadingUpdate = DateTime.Now
            End If
        End If

        If Single.IsNaN(_displayTruckX) OrElse Single.IsNaN(_displayTruckZ) Then
            _displayTruckX = CSng(_data.TruckX)
            _displayTruckZ = CSng(_data.TruckZ)
        ElseIf _useSmoothing Then
            Dim dx = CSng(_data.TruckX) - _displayTruckX
            Dim dz = CSng(_data.TruckZ) - _displayTruckZ
            If Math.Abs(dx) > 0.01F OrElse Math.Abs(dz) > 0.01F Then
                _displayTruckX += dx * SMOOTHING_FACTOR
                _displayTruckZ += dz * SMOOTHING_FACTOR
                _mapDirty = True
            End If
        Else
            _displayTruckX = CSng(_data.TruckX)
            _displayTruckZ = CSng(_data.TruckZ)
        End If

        UpdateDayNight()

        If _mapDirty Then
            Map_Panel.Invalidate()
            _mapDirty = False
        End If
        If _compassDirty Then
            COMPASS_BOX.Invalidate()
            VEHICLE_BOX.Invalidate()
            _compassDirty = False
        End If
    End Sub

#End Region

#Region "Status"

    Private Sub UpdateStatus(msg As String, col As Color)
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() UpdateStatus(msg, col))
            Return
        End If
        Debug.WriteLine($"[STATUS] {msg}")
    End Sub

    Private Sub ReturnToStatusScreen()
        If Me.InvokeRequired Then
            Me.BeginInvoke(Sub() ReturnToStatusScreen())
            Return
        End If

        If Me.Visible Then
            Me.Hide()
            StatusScreen.StartCheckingForDVD()
        End If
    End Sub

#End Region

#Region "Zoom"

    Private Function GetMapBackgroundColor(isDay As Boolean) As Color
        Return If(isDay, Color.FromArgb(70, 86, 91), Color.FromArgb(18, 18, 24))
    End Function

    Private Function GetEffectiveRenderZoom() As Single
        If _offlineMap AndAlso _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
            Dim i = Math.Max(0, Math.Min(_offlineRenderZoomStops.Length - 1, _offlineZoomIdx))
            Return _offlineRenderZoomStops(i)
        End If
        Return If(_fixedMap, _zoomLevel * 0.65F, _zoomLevel)
    End Function

    Private Function GetPerspectiveZoomDivisor() As Single
        If Not _offlineMap OrElse _offlineRenderZoomStops Is Nothing OrElse _offlineRenderZoomStops.Length <= 1 Then
            Return PERSPECTIVE_ZOOM_DIVISOR_FAR
        End If
        Dim idx = Math.Max(0, Math.Min(_offlineRenderZoomStops.Length - 1, _offlineZoomIdx))
        Dim frac = CSng(idx) / CSng(_offlineRenderZoomStops.Length - 1)
        Return PERSPECTIVE_ZOOM_DIVISOR_FAR + (PERSPECTIVE_ZOOM_DIVISOR_NEAR - PERSPECTIVE_ZOOM_DIVISOR_FAR) * frac
    End Function

    Private Sub RebuildOfflineZoomStops()
        If _offlineCompositor Is Nothing OrElse Not _offlineCompositor.IsAvailable Then
            _offlineRenderZoomStops = Array.Empty(Of Single)()
            Return
        End If

        Const refBarPx As Double = 80.0

        Dim stops As New List(Of Single)()
        For Each m In ScaleStepsM.Reverse()
            Dim r = CSng(refBarPx * ETS2_SCALE / m)
            stops.Add(r)
        Next

        Dim savedIdx = _offlineZoomIdx
        _offlineRenderZoomStops = stops.ToArray()

        If _offlineRenderZoomStops.Length > 0 Then
            _offlineZoomIdx = Math.Max(0, Math.Min(_offlineRenderZoomStops.Length - 1, savedIdx))
        End If
    End Sub

    Private Sub SyncOnlineZoomFromOfflineStop()
        If _offlineRenderZoomStops Is Nothing OrElse _offlineRenderZoomStops.Length = 0 Then Return
        Dim i = Math.Max(0, Math.Min(_offlineRenderZoomStops.Length - 1, _offlineZoomIdx))
        Dim rz = _offlineRenderZoomStops(i)
        _zoomLevel = If(_fixedMap, rz / 0.65F, rz)
        _zoomLevel = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, _zoomLevel))
    End Sub

    Private Sub Map_Panel_MouseWheel(sender As Object, e As MouseEventArgs) Handles Map_Panel.MouseWheel
        ZoomBy(If(e.Delta > 0, ZOOM_STEP, -ZOOM_STEP))
    End Sub

    Private Sub SetOfflineZoomIdx(newIdx As Integer)
        Dim maxIdx = If(_offlineRenderZoomStops IsNot Nothing, _offlineRenderZoomStops.Length - 1, 0)
        Dim clamped = Math.Max(0, Math.Min(maxIdx, newIdx))
        _offlineZoomIdx = clamped
        SaveLastScale(clamped)
    End Sub

    Private Sub SaveLastScale(idx As Integer)
        Try
            Dim msIdx = Math.Max(0, Math.Min(ScaleStepsM.Length - 1, ScaleStepsM.Length - 1 - idx))
            Dim meters = ScaleStepsM(msIdx)
            Dim dir = IO.Path.GetDirectoryName(SCALE_VAL_PATH)
            If Not String.IsNullOrEmpty(dir) AndAlso Not IO.Directory.Exists(dir) Then
                IO.Directory.CreateDirectory(dir)
            End If
            IO.File.WriteAllText(SCALE_VAL_PATH, meters.ToString(System.Globalization.CultureInfo.InvariantCulture))
        Catch ex As Exception
            Debug.WriteLine($"[SCALE] Saving of SCALE.VAL failed: {ex.Message}")
        End Try
    End Sub

    Private Function LoadLastScaleIdx() As Integer
        Const fallback As Integer = 3
        Try
            If Not IO.File.Exists(SCALE_VAL_PATH) Then Return fallback
            Dim text = IO.File.ReadAllText(SCALE_VAL_PATH).Trim()
            Dim meters As Double
            If Not Double.TryParse(text, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, meters) Then
                Return fallback
            End If

            Dim bestMsIdx = 0, bestD = Double.MaxValue
            For i = 0 To ScaleStepsM.Length - 1
                Dim d = Math.Abs(ScaleStepsM(i) - meters)
                If d < bestD Then bestD = d : bestMsIdx = i
            Next
            Return ScaleStepsM.Length - 1 - bestMsIdx
        Catch ex As Exception
            Debug.WriteLine($"[SCALE] Loading of SCALE.VAL failed: {ex.Message}")
            Return fallback
        End Try
    End Function

    Private Function LoadLiquidMapSetting() As Boolean
        Const fallback As Boolean = False
        Try
            If Not IO.File.Exists(LIQUIDMAP_STA_PATH) Then Return fallback
            Dim text = IO.File.ReadAllText(LIQUIDMAP_STA_PATH).Trim()
            Dim result As Boolean
            If Boolean.TryParse(text, result) Then Return result
            Return fallback
        Catch ex As Exception
            Debug.WriteLine($"[LIQUIDMAP] Reading of LIQUIDMAP.STA failed: {ex.Message}")
            Return fallback
        End Try
    End Function

    Private Sub ZoomBy(delta As Single)
        Dim stops = _offlineRenderZoomStops
        If _offlineMap AndAlso stops IsNot Nothing AndAlso stops.Length > 0 Then
            Dim d = If(delta > 0.0001F, 1, If(delta < -0.0001F, -1, 0))
            Dim newIdx = Math.Max(0, Math.Min(stops.Length - 1, _offlineZoomIdx + d))
            If newIdx = _offlineZoomIdx Then Return
            SetOfflineZoomIdx(newIdx)

            Dim allStops = String.Join(", ", stops.Select(Function(s) s.ToString("F4")))
            Debug.WriteLine($"ZoomStops: {allStops}")
            Debug.WriteLine($"NewIdx: {newIdx}, RenderZoom: {stops(newIdx)}")
        Else
            _zoomLevel = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, _zoomLevel + delta))
        End If
        _forceRedraw = True
        _mapDirty = True
    End Sub

#End Region

#Region "Dispose"

    Private Sub MainPage_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        _running = False
        renderTimer?.Stop()

        Try
            Using udp As New UdpClient()
                udp.EnableBroadcast = True
                Dim msg = Encoding.UTF8.GetBytes("HIQNAV_BYE:11000")
                Dim ep As New IPEndPoint(IPAddress.Broadcast, 11001)
                udp.Send(msg, msg.Length, ep)
            End Using
        Catch
        End Try

        Try : _udpClient?.Close() : Catch : End Try
        Try : _tcpClient?.Close() : _tcpClient?.Dispose() : Catch : End Try
        _mapImage?.Dispose()
        SyncLock _tileLock
            _tileA?.Dispose()
            _tileB?.Dispose()
        End SyncLock
        _scalePen?.Dispose()
        _scaleFont?.Dispose()
        _speedLimitBg?.Dispose()
        _speedLimitRim?.Dispose()
        _speedLimitFont?.Dispose()
        _speedLimitNumBrush?.Dispose()
        _speedLimitSf?.Dispose()
    End Sub

#End Region

#Region "Declarations"

    Private Sub HOME_BTN_Click(sender As Object, e As EventArgs) Handles HOME_BTN.Click
        Hide()
    End Sub

    Private Sub SysTimer_Tick(sender As Object, e As EventArgs) Handles SysTimer.Tick
        If _data IsNot Nothing AndAlso _data.GameTime IsNot Nothing Then
            LblTime.Text = _data.GameTime
        Else
            LblTime.Text = "--:--"
        End If
    End Sub

#End Region

#Region "QCM"

    Private PanelOpen As Boolean = False
    Private QCM As Boolean = False

    Private p_ind = 1

    Private Sub PanelSelector()
        Dim color_a As Color = Color.FromArgb(111, 255, 190)
        Dim color_b As Color = Color.FromArgb(127, 127, 127)

        Select Case p_ind
            Case 1
                POP1.ForeColor = color_a
                POP2.ForeColor = color_b
                POP3.ForeColor = color_b
                POP4.ForeColor = color_b
                POP5.ForeColor = color_b
                POP6.ForeColor = color_b
                POP7.ForeColor = color_b
                POP8.ForeColor = color_b
            Case 2
                POP1.ForeColor = color_b
                POP2.ForeColor = color_a
                POP3.ForeColor = color_b
                POP4.ForeColor = color_b
                POP5.ForeColor = color_b
                POP6.ForeColor = color_b
                POP7.ForeColor = color_b
                POP8.ForeColor = color_b
            Case 3
                POP1.ForeColor = color_b
                POP2.ForeColor = color_b
                POP3.ForeColor = color_a
                POP4.ForeColor = color_b
                POP5.ForeColor = color_b
                POP6.ForeColor = color_b
                POP7.ForeColor = color_b
                POP8.ForeColor = color_b
            Case 4
                POP1.ForeColor = color_b
                POP2.ForeColor = color_b
                POP3.ForeColor = color_b
                POP4.ForeColor = color_a
                POP5.ForeColor = color_b
                POP6.ForeColor = color_b
                POP7.ForeColor = color_b
                POP8.ForeColor = color_b
            Case 5
                POP1.ForeColor = color_b
                POP2.ForeColor = color_b
                POP3.ForeColor = color_b
                POP4.ForeColor = color_b
                POP5.ForeColor = color_a
                POP6.ForeColor = color_b
                POP7.ForeColor = color_b
                POP8.ForeColor = color_b
            Case 6
                POP1.ForeColor = color_b
                POP2.ForeColor = color_b
                POP3.ForeColor = color_b
                POP4.ForeColor = color_b
                POP5.ForeColor = color_b
                POP6.ForeColor = color_a
                POP7.ForeColor = color_b
                POP8.ForeColor = color_b
            Case 7
                POP1.ForeColor = color_b
                POP2.ForeColor = color_b
                POP3.ForeColor = color_b
                POP4.ForeColor = color_b
                POP5.ForeColor = color_b
                POP6.ForeColor = color_b
                POP7.ForeColor = color_a
                POP8.ForeColor = color_b
            Case 8
                POP1.ForeColor = color_b
                POP2.ForeColor = color_b
                POP3.ForeColor = color_b
                POP4.ForeColor = color_b
                POP5.ForeColor = color_b
                POP6.ForeColor = color_b
                POP7.ForeColor = color_b
                POP8.ForeColor = color_a
        End Select
    End Sub

    Private Sub MainPage_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Right Then
            If PanelOpen Then
                Side_Panel.Location = New Point(-305, 0)
                PanelOpen = False
            Else
                Side_Panel.Location = New Point(0, 0)
                QCM = False
                PanelOpen = True
            End If
        ElseIf e.KeyCode = Keys.Left Then
            If Not PanelOpen Then
                If QCM Then
                    QCM = False
                Else
                    QCM = True
                End If
            Else
                Select Case p_ind
                    Case 1
                        ZoomBy(ZOOM_STEP)
                    Case 2
                        ZoomBy(-ZOOM_STEP)
                    Case 3
                        If _perspective3D Then Return

                        Dim oldEff = GetEffectiveRenderZoom()
                        _fixedMap = Not _fixedMap
                        If _offlineMap AndAlso _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
                            RebuildOfflineZoomStops()
                            Dim bestIdx = 0, bestD = Single.MaxValue
                            For i = 0 To _offlineRenderZoomStops.Length - 1
                                Dim d = Math.Abs(_offlineRenderZoomStops(i) - oldEff)
                                If d < bestD Then bestD = d : bestIdx = i
                            Next
                            SetOfflineZoomIdx(bestIdx)
                        Else
                            _zoomLevel = If(_fixedMap,
                                Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, oldEff / 0.65F)),
                                Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, oldEff)))
                        End If
                        _lastTileX = Single.MaxValue
                        _mapDirty = True
                    Case 4
                        _mapFast = Not _mapFast
                        _lastTileX = Single.MaxValue
                    Case 5
                        If Not _dayNightAuto Then SetDayNightModeAsync(Not _isDay)
                    Case 6
                        Toggle3DPerspective()
                    Case 7
                        Side_Panel.Location = New Point(-305, 0)
                        PanelOpen = False
                        Me.Hide()
                End Select
            End If
        ElseIf e.KeyCode = Keys.Up Then
            If Not PanelOpen Then
                If QCM Then
                Else
                    ZoomBy(ZOOM_STEP)
                End If
            Else
                If p_ind > 1 Then
                    p_ind = p_ind - 1
                End If
                PanelSelector()
            End If
        ElseIf e.KeyCode = Keys.Down Then
            If Not PanelOpen Then
                If QCM Then
                    If Not _dayNightAuto Then SetDayNightModeAsync(Not _isDay)
                Else
                    ZoomBy(-ZOOM_STEP)
                End If
            Else
                If p_ind < 8 Then
                    p_ind = p_ind + 1
                End If
                PanelSelector()
            End If
        End If
    End Sub
#End Region

End Class