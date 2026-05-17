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
    Private Const PC_IP As String = "192.168.178.23"  ' TCP IP still manual

    Private _isDay As Boolean = True
    Private _dayNightAuto As Boolean = True
    Private _lastDayState As Boolean = True
    Private _btnDayNight As Button


    Private _displayHeading As Single = 0.0F
    Private _useSmoothing As Boolean = False
    Private _lastHeadingUpdate As DateTime = DateTime.MinValue

    Private _zoomLevel As Single = 1.0F
    Private Const ZOOM_MIN As Single = 0.2F
    Private Const ZOOM_MAX As Single = 6.0F
    Private Const ZOOM_STEP As Single = 0.2F

    Private Const MAP_OFFSET_X As Double = -94600
    Private Const MAP_OFFSET_Z As Double = -80000
    Private Const MAP_SCALE As Double = 0.0117

    Private Shared ReadOnly MapPaper As Color = Color.FromArgb(224, 218, 200)

    Private _blinkerState As Boolean = False

    Private _mapDirty As Boolean = True
    Private _compassDirty As Boolean = True
    Private _lastSpeed As Integer = -1
    Private _lastGear As String = ""
    Private _lastGameTime As String = ""
    Private _lastFuelPct As Integer = -1

    Private _truckFill As SolidBrush
    Private _truckOutline As Pen
    Private _scalePen As Pen
    Private _scaleFont As Font
    Private _speedLimitBg As SolidBrush
    Private _speedLimitRim As Pen
    Private _speedLimitFont As Font
    Private _speedLimitNumBrush As SolidBrush
    Private _speedLimitSf As StringFormat
    Private _compassBgBrush As SolidBrush
    Private _compassRimPen As Pen
    Private _compassTickPen As Pen
    Private _compassNorthBrush As SolidBrush
    Private _compassWhiteBrush As SolidBrush
    Private _compassCenterBrush As SolidBrush
    Private _compassDegFont As Font
    Private _compassDirFont As Font
    Private _compassDirSf As StringFormat
    Private _compassDegSf As StringFormat

    Private _truckPts() As PointF = {
        New PointF(0, -16), New PointF(10, 11),
        New PointF(0, 5), New PointF(-10, 11)
    }

    Private _tcpBuffer() As Byte = New Byte(65535) {}

    Private WithEvents picMap As PictureBox
    Private WithEvents picCompass As PictureBox
    Private lblGameTime As Label
    Private lblTemp As Label
    Private lblSpeed As Label
    Private lblSpeedLimit As Label
    Private lblGear As Label
    Private lblStatus As Label
    Private lblFuel As Label
    Private WithEvents btnZoomIn As Button
    Private WithEvents btnZoomOut As Button
    Private panelInfo As Panel
    Private panelControls As Panel
    Private WithEvents renderTimer As System.Windows.Forms.Timer

#Region "Form Init"

    Public Sub New()
        InitializeComponent()
        _offlineCompositor = Reader.TryCreate()
        BuildUI()
    End Sub

    Private Sub MainPage_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "HiQ-Nav"
        Me.BackColor = Color.FromArgb(18, 20, 26)
        Me.MinimumSize = New Size(800, 600)

        InitGdiCache()
        StartUdpReceiver()
        LoadMap()
        StartRenderTimer()
    End Sub

    Private Sub InitGdiCache()
        _truckFill = New SolidBrush(Color.FromArgb(240, 255, 180, 0))
        _truckOutline = New Pen(Color.White, 1.5F)
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
        _compassBgBrush = New SolidBrush(Color.FromArgb(215, 12, 14, 24))
        _compassRimPen = New Pen(Color.FromArgb(255, 180, 0), 2)
        _compassTickPen = New Pen(Color.FromArgb(90, 255, 255, 255), 1)
        _compassNorthBrush = New SolidBrush(Color.OrangeRed)
        _compassWhiteBrush = New SolidBrush(Color.White)
        _compassCenterBrush = New SolidBrush(Color.FromArgb(255, 180, 0))
        _compassDegFont = New Font("Consolas", 7)
        _compassDirFont = New Font("Consolas", 8, FontStyle.Bold)
        _compassDirSf = New StringFormat With {
            .Alignment = StringAlignment.Center,
            .LineAlignment = StringAlignment.Center
        }
        _compassDegSf = New StringFormat With {.Alignment = StringAlignment.Center}
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

            UpdateStatus("Waiting for PC…", Color.Orange)
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
        If IO.File.Exists(mapPath) Then
            _mapImage = New Bitmap(mapPath)
        Else
            _mapImage = CreatePlaceholderMap()
        End If

        Task.Run(Sub() MapProviderMain())
    End Sub

    Private Sub MapProviderMain()
        While _running
            If _offlineMap Then
                OfflineTileLoop()
            Else
                ConnectMapStream()
            End If
        End While
    End Sub

    Private Sub ConnectMapStream()
        While _running AndAlso Not _offlineMap
            Try
                Try : _tcpClient?.Close() : _tcpClient?.Dispose() : Catch : End Try

                _tcpClient = New TcpClient()
                _tcpClient.Connect(PC_IP, 11021)
                UpdateStatus("Offline-Map: Connected", Color.LimeGreen)
                RequestTileLoop()
            Catch
                If Not _offlineMap Then UpdateStatus("Offline-Map: Waiting for PC…", Color.Orange)
                Thread.Sleep(3000)
            End Try
        End While
    End Sub

    Private Sub OfflineTileLoop()
        While _running AndAlso _offlineMap
            Try
                If _offlineCompositor Is Nothing OrElse Not _offlineCompositor.IsAvailable Then
                    UpdateStatus("Offline-Map: NAVIGATION_MAP.DATA\DAY.MAP missing", Color.Orange)
                    Thread.Sleep(1000)
                    Continue While
                End If

                Dim threshold = If(_mapFast, 0.0F, TILE_THRESHOLD_NORMAL)
                Dim moved = Math.Abs(_data.TruckX - _lastTileX) > threshold OrElse
                            Math.Abs(_data.TruckZ - _lastTileZ) > threshold OrElse
                            _lastTileX = Single.MaxValue

                If moved Then
                    Dim renderZoom = GetEffectiveRenderZoom()
                    Dim cw = picMap.ClientSize.Width
                    Dim ch = picMap.ClientSize.Height
                    If cw < 9 OrElse ch < 9 Then
                        Thread.Sleep(100)
                        Continue While
                    End If

                    Dim usedZ = 0
                    Dim newTile = _offlineCompositor.Render(
                        CSng(_data.TruckX), CSng(_data.TruckZ), renderZoom, cw, ch, usedZ)
                    If newTile IsNot Nothing Then
                        _lastOfflineTileZ = usedZ
                        SyncLock _tileLock
                            If _activeTile = 0 Then
                                _tileB?.Dispose()
                                _tileB = newTile
                                _activeTile = 1
                            Else
                                _tileA?.Dispose()
                                _tileA = newTile
                                _activeTile = 0
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
                Dim moved = Math.Abs(_data.TruckX - _lastTileX) > threshold OrElse
                            Math.Abs(_data.TruckZ - _lastTileZ) > threshold OrElse
                            _lastTileX = Single.MaxValue

                If moved Then
                    bw.Write(CSng(_data.TruckX))
                    bw.Write(CSng(_data.TruckZ))
                    Dim renderZoom = GetEffectiveRenderZoom()
                    bw.Write(renderZoom)
                    bw.Write(CSng(picMap.ClientSize.Width))
                    bw.Write(CSng(picMap.ClientSize.Height))
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
                            _tileB?.Dispose()
                            _tileB = newTile
                            _activeTile = 1
                        Else
                            _tileA?.Dispose()
                            _tileA = newTile
                            _activeTile = 0
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
                             fnt, Brushes.Gray,
                             New RectangleF(0, 0, 512, 512), sf)
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

            Dim twi = CInt(tw)
            Dim thi = CInt(th)
            Dim scale = CSng(Math.Max(w / tw, h / th))
            Dim srcWi = CInt(Math.Max(1, Math.Round(w / scale)))
            Dim srcHi = CInt(Math.Max(1, Math.Round(h / scale)))
            srcWi = Math.Min(srcWi, twi)
            srcHi = Math.Min(srcHi, thi)
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
        Dim dw = tw * scaleN
        Dim dh = th * scaleN
        g.TranslateTransform(w / 2.0F, h / 2.0F)
        g.RotateTransform(-_displayHeading)
        g.DrawImage(tile, -dw / 2.0F, -dh / 2.0F, dw, dh)
        g.Restore(s)
    End Sub

    Private Sub picMap_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.Bilinear

        Dim w = picMap.ClientSize.Width
        Dim h = picMap.ClientSize.Height

        Dim tile As Bitmap = Nothing
        SyncLock _tileLock
            tile = If(_activeTile = 0, _tileA, _tileB)
        End SyncLock

        If tile IsNot Nothing Then
            DrawTileUniformCover(g, tile, w, h, rotateNorthUp:=_fixedMap)
        Else
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

        DrawTruck(g, w / 2.0, h / 2.0, If(_fixedMap, 0.0, CDbl(_displayHeading)))
        DrawScaleBar(g, w, h)

        If _data.SpeedLimitVisible Then
            DrawSpeedLimit(g, _data.SpeedLimit)
        End If
    End Sub

    Private Sub DrawTruck(g As Graphics, cx As Double, cy As Double, rotDeg As Double)
        Dim s = g.Save()
        g.TranslateTransform(CSng(cx), CSng(cy))
        g.RotateTransform(CSng(rotDeg))
        g.FillPolygon(_truckFill, _truckPts)
        g.DrawPolygon(_truckOutline, _truckPts)
        g.Restore(s)
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
            rawMeters = refBarPx / renderZoom
        Else
            renderZoom = _zoomLevel
            rawMeters = refBarPx / (_zoomLevel * MAP_SCALE * 1000)
        End If

        Dim niceM = NiceMetersLength(rawMeters)
        Dim drawPx As Integer
        If tile IsNot Nothing Then
            Dim ideal = niceM * renderZoom
            drawPx = CInt(Math.Max(50, Math.Min(110, Math.Round(ideal))))
        Else
            drawPx = refBarPx
        End If

        Dim lbl As String
        If niceM >= 1000 Then
            lbl = $"{niceM / 1000.0:F1} km"
        Else
            lbl = $"{CInt(Math.Round(niceM))} m"
        End If

        If _offlineMap AndAlso _lastOfflineTileZ >= 0 AndAlso _offlineCompositor IsNot Nothing Then
            lbl &= $"  (z{_lastOfflineTileZ}/{_offlineCompositor.ExportMinZoom}-{_offlineCompositor.ExportMaxZoom})"
        End If

        Dim x1 = 14, y1 = h - 26
        g.DrawLine(_scalePen, x1, y1, x1 + drawPx, y1)
        g.DrawLine(_scalePen, x1, y1 - 4, x1, y1 + 4)
        g.DrawLine(_scalePen, x1 + drawPx, y1 - 4, x1 + drawPx, y1 + 4)
        g.DrawString(lbl, _scaleFont, Brushes.White, x1, y1 - 16)
    End Sub

    Private Shared Function NiceMetersLength(raw As Double) As Double
        If raw <= 0 OrElse Double.IsNaN(raw) OrElse Double.IsInfinity(raw) Then Return 100
        Dim exp = Math.Floor(Math.Log10(raw))
        Dim basePow = Math.Pow(10, exp)
        Dim frac = raw / basePow
        Dim nf As Double = If(frac < 1.5, 1, If(frac < 3.5, 2, If(frac < 7.5, 5, 10)))
        Return nf * basePow
    End Function

    Private Sub DrawSpeedLimit(g As Graphics, limit As Integer)
        Dim cx = 50, cy = picMap.Height - 60, r = 28
        g.FillEllipse(_speedLimitBg, cx - r, cy - r, r * 2, r * 2)
        g.DrawEllipse(_speedLimitRim, cx - r, cy - r, r * 2, r * 2)
        g.DrawString(limit.ToString(), _speedLimitFont, _speedLimitNumBrush,
                     New RectangleF(cx - r, cy - r, r * 2, r * 2), _speedLimitSf)
    End Sub

#End Region

#Region "Compass"

    Private Sub picCompass_Paint(sender As Object, e As PaintEventArgs)
        Dim g = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        Dim cx = picCompass.Width \ 2
        Dim cy = picCompass.Height \ 2
        Dim r = CInt(Math.Min(cx, cy) * 0.84)
        Dim rot = _data.Heading

        g.FillEllipse(_compassBgBrush, cx - r, cy - r, r * 2, r * 2)
        g.DrawEllipse(_compassRimPen, cx - r, cy - r, r * 2, r * 2)

        Dim dirs() = {"N", "E", "S", "W"}
        Dim degrs() = {0, 90, 180, 270}
        For i = 0 To 3
            Dim a = (degrs(i) - rot) * Math.PI / 180
            Dim lx = cx + CSng(Math.Sin(a) * (r - 13))
            Dim ly = cy - CSng(Math.Cos(a) * (r - 13))
            Dim br = If(i = 0, _compassNorthBrush, _compassWhiteBrush)
            g.DrawString(dirs(i), _compassDirFont, br, lx, ly, _compassDirSf)
        Next

        For deg = 0 To 350 Step 10
            Dim a = (deg - rot) * Math.PI / 180
            Dim inner = r - If(deg Mod 90 = 0, 15, 9)
            g.DrawLine(_compassTickPen,
                cx + CSng(Math.Sin(a) * inner), cy - CSng(Math.Cos(a) * inner),
                cx + CSng(Math.Sin(a) * (r - 3)), cy - CSng(Math.Cos(a) * (r - 3)))
        Next

        Dim nLen = r - 20
        Dim aN = (0 - rot) * Math.PI / 180
        Dim aSi = (180 - rot) * Math.PI / 180
        g.FillPolygon(_compassNorthBrush, {
            New PointF(cx, cy),
            New PointF(cx + CSng(Math.Sin(aN) * nLen), cy - CSng(Math.Cos(aN) * nLen)),
            New PointF(cx + CSng(Math.Sin(aN + Math.PI / 2) * 4), cy - CSng(Math.Cos(aN + Math.PI / 2) * 4))
        })
        g.FillPolygon(_compassWhiteBrush, {
            New PointF(cx, cy),
            New PointF(cx + CSng(Math.Sin(aSi) * CInt(nLen * 0.65)), cy - CSng(Math.Cos(aSi) * CInt(nLen * 0.65))),
            New PointF(cx + CSng(Math.Sin(aSi + Math.PI / 2) * 4), cy - CSng(Math.Cos(aSi + Math.PI / 2) * 4))
        })
        g.FillEllipse(_compassCenterBrush, cx - 4, cy - 4, 8, 8)

        g.DrawString($"{CInt(rot) Mod 360}°", _compassDegFont, Brushes.LightGray,
                     cx, cy + r - 15, _compassDegSf)
    End Sub

#End Region

#Region "Day/Night Cycle"

    Private Function ParseGameHour() As Integer
        Try
            If String.IsNullOrEmpty(_data.GameTime) OrElse Not _data.GameTime.Contains(":") Then
                Return -1
            End If
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
                _isDay = (hour >= 6 AndAlso hour < 20)
            End If
        Else
            _dayNightAuto = False
        End If

        If _isDay <> _lastDayState Then
            _lastDayState = _isDay
            _lastTileX = Single.MaxValue
            _mapDirty = True

            If _btnDayNight IsNot Nothing Then
                _btnDayNight.BackColor = If(_isDay, Color.FromArgb(200, 160, 0), Color.FromArgb(30, 60, 120))
                _btnDayNight.Text = If(_isDay, "Day", "Night")
            End If
        End If

        If _btnDayNight IsNot Nothing Then
            _btnDayNight.Enabled = Not _dayNightAuto
            _btnDayNight.BackColor = If(_isDay, Color.FromArgb(200, 160, 0), Color.FromArgb(30, 60, 120))
            _btnDayNight.Text = If(_isDay, "Day", "Night")
        End If
    End Sub

#End Region

#Region "Render-Timer Label-Update"

    Private Sub StartRenderTimer()
        renderTimer = New System.Windows.Forms.Timer With {.Interval = 100}
        renderTimer.Start()
    End Sub

    Private Sub renderTimer_Tick(sender As Object, e As EventArgs) Handles renderTimer.Tick
        _blinkerState = Not _blinkerState

        Dim udpConnected = (DateTime.Now - _lastReceived).TotalSeconds < 3
        If Not udpConnected Then
            lblStatus.Text = "Waiting for telemetry..."
            lblStatus.ForeColor = Color.Orange
        End If

        Dim newSpeed = CInt(_data.Speed)
        If newSpeed <> _lastSpeed Then
            lblSpeed.Text = $"{newSpeed}"
            _lastSpeed = newSpeed
        End If

        If _data.Gear <> _lastGear Then
            lblGear.Text = $"Gear: {_data.Gear}"
            _lastGear = _data.Gear
        End If

        If _data.GameTime <> _lastGameTime Then
            lblGameTime.Text = $"Time: {_data.GameTime}"
            _lastGameTime = _data.GameTime
        End If

        Dim fuelPct = If(_data.FuelMax > 0, CInt(_data.FuelAmount / _data.FuelMax * 100), 0)
        If fuelPct <> _lastFuelPct Then
            lblFuel.Text = $"{fuelPct}%  ({CInt(_data.FuelAmount)} L)"
            _lastFuelPct = fuelPct
        End If

        lblTemp.Text = $"Temp: {_data.OutsideTemp} °C"

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
            picMap.Invalidate()
            _mapDirty = False
        End If
        If _compassDirty Then
            picCompass.Invalidate()
            _compassDirty = False
        End If
    End Sub

#End Region

#Region "Status"

    Private Sub UpdateStatus(msg As String, col As Color)
        If Me.InvokeRequired Then
            'Me.Invoke(Sub() UpdateStatus(msg, col))
            Return
        End If
        lblStatus.Text = msg
        lblStatus.ForeColor = col
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
        _offlineRenderZoomStops = _offlineCompositor.BuildOfflineRenderZoomStops()
        If _offlineRenderZoomStops.Length = 0 Then Return
        Dim cur = If(_fixedMap, _zoomLevel * 0.65F, _zoomLevel)
        Dim bestIdx = 0
        Dim bestD = Single.MaxValue
        For i = 0 To _offlineRenderZoomStops.Length - 1
            Dim d = Math.Abs(_offlineRenderZoomStops(i) - cur)
            If d < bestD Then
                bestD = d
                bestIdx = i
            End If
        Next
        _offlineZoomIdx = bestIdx
    End Sub

    Private Sub SyncOnlineZoomFromOfflineStop()
        If _offlineRenderZoomStops Is Nothing OrElse _offlineRenderZoomStops.Length = 0 Then Return
        Dim i = Math.Max(0, Math.Min(_offlineRenderZoomStops.Length - 1, _offlineZoomIdx))
        Dim rz = _offlineRenderZoomStops(i)
        _zoomLevel = If(_fixedMap, rz / 0.65F, rz)
        _zoomLevel = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, _zoomLevel))
    End Sub

    Private Sub btnZoomIn_Click(sender As Object, e As EventArgs) Handles btnZoomIn.Click
        ZoomBy(ZOOM_STEP)
    End Sub

    Private Sub btnZoomOut_Click(sender As Object, e As EventArgs) Handles btnZoomOut.Click
        ZoomBy(-ZOOM_STEP)
    End Sub

    Private Sub picMap_MouseWheel(sender As Object, e As MouseEventArgs)
        ZoomBy(If(e.Delta > 0, ZOOM_STEP, -ZOOM_STEP))
    End Sub

    Private Sub ZoomBy(delta As Single)
        If _offlineMap AndAlso _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
            Dim d = If(delta > 0.0001F, 1, If(delta < -0.0001F, -1, 0))
            _offlineZoomIdx = Math.Max(0, Math.Min(_offlineRenderZoomStops.Length - 1, _offlineZoomIdx + d))
        Else
            _zoomLevel = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, _zoomLevel + delta))
        End If
        _lastTileX = Single.MaxValue
        _mapDirty = True
    End Sub

#End Region

#Region "UI - Demo"

    Private Sub BuildUI()
        picMap = New PictureBox With {.Dock = DockStyle.Fill, .BackColor = Color.FromArgb(22, 30, 22)}
        AddHandler picMap.Paint, AddressOf picMap_Paint
        AddHandler picMap.MouseWheel, AddressOf picMap_MouseWheel

        panelInfo = New Panel With {
            .Size = New Size(220, 170),
            .Location = New Point(10, 10),
            .BackColor = Color.FromArgb(210, 12, 14, 20)
        }
        RoundPanel(panelInfo, 8)

        Dim lblTitle As New Label With {
            .Text = "HiQ-Nav",
            .ForeColor = Color.FromArgb(255, 180, 0),
            .Font = New Font("Consolas", 10, FontStyle.Bold),
            .Location = New Point(10, 8), .AutoSize = True
        }
        lblStatus = MkLabel("Waiting for PC…", 30)
        lblStatus.ForeColor = Color.Orange
        lblGameTime = MkLabel("Time: --:--", 52)
        lblTemp = MkLabel("Temp: -- °C", 72) ' Faked
        lblGear = MkLabel("Gear: --", 92)
        lblFuel = MkLabel("Fuel: -- %", 112)

        lblSpeed = New Label With {
            .Text = "0",
            .ForeColor = Color.White,
            .Font = New Font("Consolas", 32, FontStyle.Bold),
            .Location = New Point(130, 100),
            .AutoSize = True
        }
        Dim lblKmh As New Label With {
            .Text = "km/h",
            .ForeColor = Color.FromArgb(160, 160, 160),
            .Font = New Font("Consolas", 8),
            .Location = New Point(135, 148),
            .AutoSize = True
        }

        panelInfo.Controls.AddRange({lblTitle, lblStatus, lblGameTime,
                                     lblTemp, lblGear, lblFuel, lblSpeed, lblKmh})

        picCompass = New PictureBox With {
            .Size = New Size(115, 115),
            .BackColor = Color.Transparent
        }
        AddHandler picCompass.Paint, AddressOf picCompass_Paint

        panelControls = New Panel With {
            .Size = New Size(130, 295),
            .BackColor = Color.FromArgb(210, 12, 14, 20)
        }
        RoundPanel(panelControls, 8)

        Dim lz As New Label With {
            .Text = "Zoom",
            .ForeColor = Color.FromArgb(255, 180, 0),
            .Font = New Font("Consolas", 8, FontStyle.Bold),
            .Location = New Point(8, 8), .AutoSize = True
        }
        btnZoomIn = MkButton("+", New Point(8, 28))
        btnZoomOut = MkButton("–", New Point(68, 28))

        Dim btnMapMode As New Button With {
            .Text = "Compass",
            .Size = New Size(110, 34),
            .Location = New Point(8, 64),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 12),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(60, 80, 60),
            .Cursor = Cursors.Hand
        }
        btnMapMode.FlatAppearance.BorderSize = 0
        AddHandler btnMapMode.Click, Sub(s, ev)
                                         Dim oldEff = GetEffectiveRenderZoom()
                                         _fixedMap = Not _fixedMap
                                         If _offlineMap AndAlso _offlineRenderZoomStops IsNot Nothing AndAlso _offlineRenderZoomStops.Length > 0 Then
                                             RebuildOfflineZoomStops()
                                             Dim bestIdx = 0
                                             Dim bestD = Single.MaxValue
                                             For i = 0 To _offlineRenderZoomStops.Length - 1
                                                 Dim d = Math.Abs(_offlineRenderZoomStops(i) - oldEff)
                                                 If d < bestD Then
                                                     bestD = d
                                                     bestIdx = i
                                                 End If
                                             Next
                                             _offlineZoomIdx = bestIdx
                                         Else
                                             If _fixedMap Then
                                                 _zoomLevel = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, oldEff / 0.65F))
                                             Else
                                                 _zoomLevel = Math.Max(ZOOM_MIN, Math.Min(ZOOM_MAX, oldEff))
                                             End If
                                         End If
                                         _lastTileX = Single.MaxValue
                                         btnMapMode.BackColor = If(_fixedMap, Color.FromArgb(60, 80, 60), Color.FromArgb(255, 140, 0))
                                     End Sub

        ' ~° Smooth Heading
        Dim btnSmooth As New Button With {
            .Text = "Smooth Comp",
            .Size = New Size(110, 34),
            .Location = New Point(8, 102),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 11, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(60, 80, 60),
            .Cursor = Cursors.Hand
        }
        btnSmooth.FlatAppearance.BorderSize = 0
        AddHandler btnSmooth.Click, Sub(s, ev)
                                        _useSmoothing = Not _useSmoothing
                                        btnSmooth.BackColor = If(_useSmoothing, Color.FromArgb(60, 80, 60), Color.FromArgb(255, 140, 0))
                                    End Sub

        _btnDayNight = New Button With {
            .Text = "Day/Night",
            .Size = New Size(110, 34),
            .Location = New Point(8, 140),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 12),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(200, 160, 0),
            .Cursor = Cursors.Hand,
            .Enabled = Not _dayNightAuto
        }
        _btnDayNight.FlatAppearance.BorderSize = 0
        AddHandler _btnDayNight.Click, Sub(s, ev)
                                           _isDay = Not _isDay
                                           _lastTileX = Single.MaxValue
                                           _btnDayNight.BackColor = If(_isDay, Color.FromArgb(200, 160, 0), Color.FromArgb(30, 60, 120))
                                           _btnDayNight.Text = If(_isDay, "Day", "Night")
                                       End Sub

        ' ⚡ MapFast Toggle
        Dim btnFast As New Button With {
            .Text = "Fast Mode",
            .Size = New Size(110, 34),
            .Location = New Point(8, 178),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 12),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(60, 80, 60),
            .Cursor = Cursors.Hand
        }
        btnFast.FlatAppearance.BorderSize = 0
        AddHandler btnFast.Click, Sub(s, ev)
                                      _mapFast = Not _mapFast
                                      _lastTileX = Single.MaxValue
                                      btnFast.BackColor = If(_mapFast, Color.FromArgb(200, 80, 0), Color.FromArgb(60, 80, 60))
                                  End Sub

        Dim btnOffline As New Button With {
            .Text = "Offline Mode",
            .Size = New Size(110, 34),
            .Location = New Point(8, 216),
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 12),
            .ForeColor = Color.White,
            .BackColor = If(_offlineCompositor IsNot Nothing AndAlso _offlineCompositor.IsAvailable,
                            Color.FromArgb(60, 80, 60), Color.FromArgb(80, 50, 50)),
            .Cursor = Cursors.Hand
        }
        btnOffline.FlatAppearance.BorderSize = 0
        AddHandler btnOffline.Click, Sub(s, ev)
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
                                                 Dim bestIdx = 0
                                                 Dim bestD = Single.MaxValue
                                                 For i = 0 To _offlineRenderZoomStops.Length - 1
                                                     Dim d = Math.Abs(_offlineRenderZoomStops(i) - oldEff)
                                                     If d < bestD Then
                                                         bestD = d
                                                         bestIdx = i
                                                     End If
                                                 Next
                                                 _offlineZoomIdx = bestIdx
                                             End If
                                         End If
                                         Try
                                             _tcpClient?.Close()
                                         Catch
                                         End Try
                                         btnOffline.BackColor = If(_offlineMap,
                                             Color.FromArgb(70, 130, 110),
                                             If(_offlineCompositor IsNot Nothing AndAlso _offlineCompositor.IsAvailable,
                                                Color.FromArgb(60, 80, 60), Color.FromArgb(80, 50, 50)))
                                         _mapDirty = True
                                     End Sub

        panelControls.Controls.AddRange({lz, btnZoomIn, btnZoomOut, btnMapMode,
                                         btnSmooth, _btnDayNight, btnFast, btnOffline})

        Me.Controls.Add(picMap)
        picMap.Controls.AddRange({panelInfo, picCompass, panelControls})

        RepositionOverlays()
        AddHandler Me.Resize, Sub(s, ev) RepositionOverlays()
    End Sub

    Private Sub RepositionOverlays()
        If picCompass Is Nothing Then Return
        picCompass.Location = New Point(picMap.Width - 125, 10)
        panelControls.Location = New Point(picMap.Width - 140, picMap.Height - 264)
    End Sub

    Private Function MkLabel(text As String, top As Integer) As Label
        Return New Label With {
            .Text = text,
            .ForeColor = Color.FromArgb(220, 230, 255),
            .Font = New Font("Consolas", 8),
            .Location = New Point(10, top),
            .AutoSize = True
        }
    End Function

    Private Function MkButton(text As String, loc As Point) As Button
        Dim btn As New Button With {
            .Text = text,
            .Size = New Size(50, 34),
            .Location = loc,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Consolas", 14, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(255, 180, 0),
            .Cursor = Cursors.Hand
        }
        btn.FlatAppearance.BorderSize = 0
        Return btn
    End Function

    Private Sub RoundPanel(ctrl As Control, radius As Integer)
        Dim path As New GraphicsPath()
        Dim r = ctrl.ClientRectangle
        path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90)
        path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90)
        path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90)
        path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90)
        path.CloseFigure()
        ctrl.Region = New Region(path)
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

        _truckFill?.Dispose()
        _truckOutline?.Dispose()
        _scalePen?.Dispose()
        _scaleFont?.Dispose()
        _speedLimitBg?.Dispose()
        _speedLimitRim?.Dispose()
        _speedLimitFont?.Dispose()
        _speedLimitNumBrush?.Dispose()
        _speedLimitSf?.Dispose()
        _compassBgBrush?.Dispose()
        _compassRimPen?.Dispose()
        _compassTickPen?.Dispose()
        _compassNorthBrush?.Dispose()
        _compassWhiteBrush?.Dispose()
        _compassCenterBrush?.Dispose()
        _compassDegFont?.Dispose()
        _compassDirFont?.Dispose()
        _compassDirSf?.Dispose()
        _compassDegSf?.Dispose()
    End Sub

#End Region

End Class