Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports Newtonsoft.Json
Imports Compressed_Map_Data

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
    Private ReadOnly _tileLock As New Object()
    Private _lastTileX As Single = Single.MaxValue
    Private _lastTileZ As Single = Single.MaxValue
    Private _fixedMap As Boolean = True
    Private _mapFast As Boolean = False
    Private _offlineMap As Boolean = False
    Private _offlineCompositor As Reader
    Private _lastOfflineTileZ As Integer = -1
    Private _offlineRenderZoomStops() As Single = Array.Empty(Of Single)()
    Private _offlineZoomIdx As Integer
    Private Const TILE_THRESHOLD_NORMAL As Single = 10.0F
    Private Const PC_IP As String = "192.168.178.23"

    Private _isDay As Boolean = True
    Private _dayNightAuto As Boolean = True
    Private _lastDayState As Boolean = True

    Private _displayHeading As Single = 0.0F
    Private _useSmoothing As Boolean = False
    Private _lastHeadingUpdate As DateTime = DateTime.MinValue

    Private _zoomLevel As Single = 1.0F
    Private Const ZOOM_MIN As Single = 0.2F
    Private Const ZOOM_MAX As Single = 6.0F
    Private Const ZOOM_STEP As Single = 0.2F
    Private Const ETS2_SCALE As Double = 19.0

    Private Const MAP_OFFSET_X As Double = -94600
    Private Const MAP_OFFSET_Z As Double = -80000
    Private Const MAP_SCALE As Double = 0.0117

    Private Shared ReadOnly MapPaper As Color = Color.FromArgb(224, 218, 200)

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

    Private _tcpBuffer() As Byte = New Byte(65535) {}

    Private WithEvents renderTimer As System.Windows.Forms.Timer

#Region "Form Init"

    Public Sub New()
        InitializeComponent()
        _offlineCompositor = Reader.TryCreate(FindCmdPath(isDay:=True))
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
                         End Using
                     Catch
                     End Try
                 End Sub)

        Me.Text = "HiQ-Nav"
        Me.MinimumSize = New Size(1440, 900)
        Me.TopMost = True

        _compassImage = My.Resources.COMPASS
        _vehicleImage = My.Resources.POINTER

        InitGdiCache()
        WireDesignerButtons()
        StartUdpReceiver()
        LoadMap()
        _offlineMap = True
        RebuildOfflineZoomStops()
        StartRenderTimer()

        UpdateTimeModeBtn()
        UpdateNetworkModeBtn()
    End Sub

    Private Shared Function FindCmdPath(isDay As Boolean) As String
        Dim mapFolder = If(isDay, "DAY.MAP", "NIGHT.MAP")
        Dim rel = IO.Path.Combine("NAVIGATION_MAP.DATA", mapFolder, "EUROPE.CMD")

        For Each d In IO.DriveInfo.GetDrives()
            If Not d.IsReady Then Continue For
            If d.DriveType <> IO.DriveType.Fixed AndAlso
               d.DriveType <> IO.DriveType.Removable AndAlso
               d.DriveType <> IO.DriveType.Network AndAlso
               d.DriveType <> IO.DriveType.CDRom Then Continue For
            Dim p = IO.Path.Combine(d.RootDirectory.FullName, rel)
            If IO.File.Exists(p) Then Return p
        Next
        Return Nothing
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
                                              Dim oldEff = GetEffectiveRenderZoom()
                                              _fixedMap = Not _fixedMap
                                              If _offlineMap AndAlso _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
                                                  RebuildOfflineZoomStops()
                                                  Dim bestIdx = 0, bestD = Single.MaxValue
                                                  For i = 0 To _offlineRenderZoomStops.Length - 1
                                                      Dim d = Math.Abs(_offlineRenderZoomStops(i) - oldEff)
                                                      If d < bestD Then bestD = d : bestIdx = i
                                                  Next
                                                  _offlineZoomIdx = bestIdx
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
                                           If _dayNightAuto Then Return
                                           _isDay = Not _isDay
                                           _lastTileX = Single.MaxValue
                                           UpdateTimeModeBtn()
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
                                                      _offlineZoomIdx = bestIdx
                                                  End If
                                              End If
                                              Try : _tcpClient?.Close() : Catch : End Try
                                              UpdateNetworkModeBtn()
                                              _mapDirty = True
                                          End Sub
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

#End Region

#Region "UDP Receiver"

    Private Sub StartUdpReceiver()
        Try
            _udpClient = New UdpClient()
            _udpClient.Client.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress, True)
            _udpClient.Client.Bind(New IPEndPoint(IPAddress.Any, 11000))

            _running = True
            _receiveThread = New Thread(AddressOf ReceiveLoop) With {
                .IsBackground = True,
                .Name = "UDP-Receiver"
            }
            _receiveThread.Start()
        Catch ex As Exception
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

#Region "Load Offline-Map & TCP-Stream"

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
        While _running AndAlso _offlineMap
            Try
                If _offlineCompositor Is Nothing OrElse Not _offlineCompositor.IsAvailable Then
                    UpdateStatus("Offline-Map: EUROPE.CMD missing", Color.Orange)
                    Thread.Sleep(1000)
                    Continue While
                End If

                Dim threshold = If(_mapFast, 0.0F, TILE_THRESHOLD_NORMAL)
                Dim moved = _forceRedraw OrElse
            Math.Abs(_data.TruckX - _lastTileX) > threshold OrElse
            Math.Abs(_data.TruckZ - _lastTileZ) > threshold

                If moved Then
                    _forceRedraw = False
                    Dim renderZoom = GetEffectiveRenderZoom()
                    Dim cw = Map_Panel.ClientSize.Width
                    Dim ch = Map_Panel.ClientSize.Height
                    If cw < 9 OrElse ch < 9 Then Thread.Sleep(100) : Continue While

                    Dim usedZ = 0
                    Dim newTile = _offlineCompositor.Render(
                        CSng(_data.TruckX), CSng(_data.TruckZ), renderZoom, cw, ch, usedZ)
                    If newTile IsNot Nothing Then
                        _lastOfflineTileZ = usedZ
                        SyncLock _tileLock
                            If _activeTile = 0 Then
                                _tileB?.Dispose() : _tileB = newTile : _activeTile = 1
                            Else
                                _tileA?.Dispose() : _tileA = newTile : _activeTile = 0
                            End If
                        End SyncLock
                        _lastTileX = CSng(_data.TruckX)
                        _lastTileZ = CSng(_data.TruckZ)
                        _mapDirty = True
                    End If
                    UpdateStatus("Offline-Map: Ready", Color.LimeGreen)
                End If
            Catch
                UpdateStatus("Offline-Map: Error", Color.OrangeRed)
            End Try
            If Not _mapFast Then Thread.Sleep(50)
        End While
    End Sub

    Private Sub RequestTileLoop()
        Dim ns = _tcpClient.GetStream()
        Dim bw = New BinaryWriter(ns)
        Dim br = New BinaryReader(ns)

        While _running AndAlso _tcpClient.Connected AndAlso Not _offlineMap
            Try
                Dim threshold = If(_mapFast, 0.0F, TILE_THRESHOLD_NORMAL)
                Dim moved = _forceRedraw OrElse
            Math.Abs(_data.TruckX - _lastTileX) > threshold OrElse
            Math.Abs(_data.TruckZ - _lastTileZ) > threshold

                If moved Then
                    _forceRedraw = False
                    bw.Write(CSng(_data.TruckX))
                    bw.Write(CSng(_data.TruckZ))
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
                        If _activeTile = 0 Then
                            _tileB?.Dispose() : _tileB = newTile : _activeTile = 1
                        Else
                            _tileA?.Dispose() : _tileA = newTile : _activeTile = 0
                        End If
                    End SyncLock
                    _lastTileX = CSng(_data.TruckX)
                    _lastTileZ = CSng(_data.TruckZ)
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
                Dim sf As New StringFormat With {
                    .Alignment = StringAlignment.Center,
                    .LineAlignment = StringAlignment.Center
                }
                g.DrawString("EUROPE.CMD" & vbCrLf & "or Server missing",
                             fnt, Brushes.Gray, New RectangleF(0, 0, 512, 512), sf)
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
            g.DrawImage(tile,
                        New Rectangle(-pad, -pad, w + 2 * pad, h + 2 * pad),
                        New Rectangle(srcXi, srcYi, srcWi, srcHi),
                        GraphicsUnit.Pixel)
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

        Dim tile As Bitmap = Nothing
        SyncLock _tileLock
            tile = If(_activeTile = 0, _tileA, _tileB)
        End SyncLock

        If tile IsNot Nothing Then
            DrawTileUniformCover(g, tile, w, h, rotateNorthUp:=_fixedMap)
        Else
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
        g.DrawImage(_vehicleImage,
                    -cx, -cy,
                    VEHICLE_BOX.Width, VEHICLE_BOX.Height)
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

        Dim scaleTxt As String
        If rawMeters >= 1000 Then
            scaleTxt = $"{rawMeters / 1000.0:F2} km"
        ElseIf rawMeters >= 100 Then
            scaleTxt = $"{CInt(Math.Round(rawMeters / 10.0) * 10)} m"
        Else
            scaleTxt = $"{CInt(Math.Round(rawMeters))} m"
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
        g.RotateTransform(-_displayHeading)
        g.DrawImage(_compassImage, -size / 2.0F, -size / 2.0F, size, size)
    End Sub

#End Region

#Region "Day/Night Cycle"

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
            If hour >= 0 Then _isDay = (hour >= 6 AndAlso hour < 20)
        Else
            _dayNightAuto = False
        End If

        If _isDay <> _lastDayState Then
            _lastDayState = _isDay
            _lastTileX = Single.MaxValue
            _mapDirty = True
            _offlineCompositor?.Dispose()
            _offlineCompositor = Reader.TryCreate(FindCmdPath(isDay:=_isDay))
            If _offlineMap Then RebuildOfflineZoomStops()
        End If

        UpdateTimeModeBtn()
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
                _displayHeading += diff * 0.08F
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
    End Sub

#End Region

#Region "Zoom"

    Private Function GetEffectiveRenderZoom() As Single
        If _offlineMap AndAlso _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
            Dim i = Math.Max(0, Math.Min(_offlineRenderZoomStops.Length - 1, _offlineZoomIdx))
            Return _offlineRenderZoomStops(i)
        End If
        Return If(_fixedMap, _zoomLevel * 0.65F, _zoomLevel)
    End Function

    Private Sub RebuildOfflineZoomStops()
        If _offlineCompositor Is Nothing OrElse Not _offlineCompositor.IsAvailable Then
            _offlineRenderZoomStops = Array.Empty(Of Single)()
            Return
        End If

        Dim savedIdx = _offlineZoomIdx
        _offlineRenderZoomStops = _offlineCompositor.BuildOfflineRenderZoomStops()

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

    Private Sub ZoomBy(delta As Single)
        Dim stops = _offlineRenderZoomStops
        If _offlineMap AndAlso stops IsNot Nothing AndAlso stops.Length > 0 Then
            Dim d = If(delta > 0.0001F, 1, If(delta < -0.0001F, -1, 0))
            Dim newIdx = Math.Max(0, Math.Min(stops.Length - 1, _offlineZoomIdx + d))
            If newIdx = _offlineZoomIdx Then Return
            _offlineZoomIdx = newIdx

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
        Me.Close()
    End Sub

#End Region
End Class