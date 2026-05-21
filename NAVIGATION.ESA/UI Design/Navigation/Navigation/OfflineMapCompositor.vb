Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms
Imports Newtonsoft.Json

Public NotInheritable Class OfflineMapCompositor
    Implements IMapCompositor

    Public ReadOnly Property IsAvailable As Boolean Implements IMapCompositor.IsAvailable
        Get
            Return _info IsNot Nothing AndAlso Not String.IsNullOrEmpty(_tilesRoot)
        End Get
    End Property

    Private _info As TileMapInfo
    Private _tilesRoot As String
    Private _tilePixelSize As Integer
    Private _detectedMaxZoom As Integer
    Private _availableZooms() As Integer = Array.Empty(Of Integer)

    Private Class TileMapInfo
        Public Property x1 As Double
        Public Property x2 As Double
        Public Property y1 As Double
        Public Property y2 As Double
        Public Property minZoom As Integer
        Public Property maxZoom As Integer
    End Class

    Private Sub New(info As TileMapInfo, tilesRoot As String, tilePixelSize As Integer, availableZooms() As Integer)
        _info = info
        _tilesRoot = tilesRoot
        _tilePixelSize = tilePixelSize
        _availableZooms = If(availableZooms, Array.Empty(Of Integer)())
    End Sub

    Public Shared Function TryCreate(Optional isDay As Boolean = True) As OfflineMapCompositor
        Dim jsonPath = FindTileMapInfoPath(isDay)
        If jsonPath Is Nothing Then Return Nothing

        Try
            Dim json = File.ReadAllText(jsonPath)
            Dim info = JsonConvert.DeserializeObject(Of TileMapInfo)(json)
            If info Is Nothing Then Return Nothing

            Dim tilesRoot = Path.Combine(Path.GetDirectoryName(jsonPath), "Tiles")
            If Not Directory.Exists(tilesRoot) Then Return Nothing

            Dim zoomDirs = Directory.GetDirectories(tilesRoot).Select(Function(d)
                                                                          Dim n = Path.GetFileName(d)
                                                                          Dim zi As Integer
                                                                          If Integer.TryParse(n, zi) Then Return zi
                                                                          Return -1
                                                                      End Function).Where(Function(z) z >= 0).OrderBy(Function(z) z).ToArray()
            If zoomDirs.Length = 0 Then Return Nothing

            Dim detectedMax = zoomDirs.Max()
            Dim usedZLocal As Integer = -1
            Dim tilePx = DetectTilePixelSize(tilesRoot, detectedMax, usedZLocal)
            If tilePx <= 0 Then Return Nothing

            Return New OfflineMapCompositor(info, tilesRoot, tilePx, zoomDirs) With {._detectedMaxZoom = usedZLocal}
        Catch
            Return Nothing
        End Try
    End Function

    Private Shared Function FindTileMapInfoPath(isDay As Boolean) As String
        Dim mapFolder = If(isDay, "DAY.MAP", "NIGHT.MAP")
        Dim rel = Path.Combine("NAVIGATION_MAP.DATA", mapFolder, "TileMapInfo.json")

        Dim drives = IO.DriveInfo.GetDrives().
                    Where(Function(d) d.IsReady AndAlso
                          (d.DriveType = IO.DriveType.Fixed OrElse
                           d.DriveType = IO.DriveType.Removable OrElse
                           d.DriveType = IO.DriveType.Network)).
                    Select(Function(d) d.RootDirectory.FullName)

        For Each root In drives
            Dim p = Path.Combine(root, rel)
            If File.Exists(p) Then Return p
        Next

        Dim dir As New DirectoryInfo(Application.StartupPath)
        For i = 0 To 9
            Dim p = Path.Combine(dir.FullName, rel)
            If File.Exists(p) Then Return p
            If dir.Parent Is Nothing Then Exit For
            dir = dir.Parent
        Next

        Return Nothing
    End Function

    Private Shared Function DetectTilePixelSize(tilesRoot As String, maxZ As Integer, ByRef usedZoom As Integer) As Integer
        usedZoom = -1
        Dim tries = Math.Max(0, maxZ)
        For z = tries To 0 Step -1
            Dim zDir = Path.Combine(tilesRoot, z.ToString())
            If Not Directory.Exists(zDir) Then Continue For
            Dim firstCol = Directory.GetDirectories(zDir).OrderBy(Function(d) d).FirstOrDefault()
            If firstCol Is Nothing Then Continue For
            Dim sample = Directory.GetFiles(firstCol, "*.png").FirstOrDefault()
            If sample Is Nothing OrElse Not File.Exists(sample) Then Continue For
            Try
                Using bmp As New Bitmap(sample)
                    usedZoom = z
                    Return Math.Max(bmp.Width, bmp.Height)
                End Using
            Catch
                Continue For
            End Try
        Next
        Return 0
    End Function

    Public Shared Function ComputeRenderSize(clientW As Integer, clientH As Integer) As Integer
        Dim wf = CSng(clientW)
        Dim hf = CSng(clientH)
        Dim diag = CInt(Math.Ceiling(Math.Sqrt(wf * wf + hf * hf)))
        Return Math.Max(diag, 512)
    End Function

    Public ReadOnly Property ExportMinZoom As Integer Implements IMapCompositor.ExportMinZoom
        Get
            If _availableZooms IsNot Nothing AndAlso _availableZooms.Length > 0 Then
                Return _availableZooms.Min()
            End If
            Return 0
        End Get
    End Property

    Public ReadOnly Property ExportMaxZoom As Integer Implements IMapCompositor.ExportMaxZoom
        Get
            If _info Is Nothing Then Return 9
            Dim cfgMax = Math.Min(14, _info.maxZoom)
            If _availableZooms IsNot Nothing AndAlso _availableZooms.Length > 0 Then
                cfgMax = Math.Min(cfgMax, _availableZooms.Max())
            ElseIf _detectedMaxZoom >= 0 Then
                cfgMax = Math.Min(cfgMax, _detectedMaxZoom)
            End If
            Return Math.Max(ExportMinZoom, Math.Min(14, cfgMax))
        End Get
    End Property

    Public Function Render(truckX As Single, truckZ As Single, zoom As Single,
                           clientW As Integer, clientH As Integer,
                           ByRef usedTileZoom As Integer) As Bitmap Implements IMapCompositor.Render
        usedTileZoom = -1
        If Not IsAvailable OrElse zoom <= 0.0001F Then Return Nothing

        Dim outSize = ComputeRenderSize(clientW, clientH)
        Dim halfWorld = (outSize / 2.0F) / zoom

        Dim wx0 = truckX - halfWorld
        Dim wx1 = truckX + halfWorld
        Dim wz0 = truckZ - halfWorld
        Dim wz1 = truckZ + halfWorld

        Dim xSpan = _info.x2 - _info.x1
        Dim ySpan = _info.y2 - _info.y1
        If xSpan <= 0 OrElse ySpan <= 0 Then Return Nothing

        Dim mppTarget = (2.0F * halfWorld) / outSize
        Dim zUse = PickZoom(mppTarget)
        usedTileZoom = zUse

        Dim n = 1 << zUse
        Dim tileWorldW = CSng(xSpan / n)
        Dim tileWorldH = CSng(ySpan / n)

        Dim bmp As New Bitmap(outSize, outSize, Imaging.PixelFormat.Format32bppPArgb)
        Using g = Graphics.FromImage(bmp)
            g.Clear(Color.FromArgb(224, 218, 200))
            g.InterpolationMode = InterpolationMode.NearestNeighbor
            g.PixelOffsetMode = PixelOffsetMode.Half
            g.CompositingMode = CompositingMode.SourceCopy

            Dim tx0 = CInt(Math.Floor((wx0 - _info.x1) / tileWorldW))
            Dim tx1 = CInt(Math.Floor((wx1 - _info.x1) / tileWorldW))
            Dim ty0 = CInt(Math.Floor((wz0 - _info.y1) / tileWorldH))
            Dim ty1 = CInt(Math.Floor((wz1 - _info.y1) / tileWorldH))

            tx0 = Math.Max(0, Math.Min(n - 1, tx0))
            tx1 = Math.Max(0, Math.Min(n - 1, tx1))
            ty0 = Math.Max(0, Math.Min(n - 1, ty0))
            ty1 = Math.Max(0, Math.Min(n - 1, ty1))

            For tx = tx0 To tx1
                For ty = ty0 To ty1
                    Dim tilePath = IO.Path.Combine(_tilesRoot, zUse.ToString(), tx.ToString(), ty.ToString() & ".png")
                    If Not File.Exists(tilePath) Then Continue For

                    Dim twx0 = CSng(_info.x1 + tx * tileWorldW)
                    Dim twx1 = twx0 + tileWorldW
                    Dim twz0 = CSng(_info.y1 + ty * tileWorldH)
                    Dim twz1 = twz0 + tileWorldH

                    Dim px0 = (twx0 - wx0) / (wx1 - wx0) * outSize
                    Dim px1 = (twx1 - wx0) / (wx1 - wx0) * outSize
                    Dim py0 = (twz0 - wz0) / (wz1 - wz0) * outSize
                    Dim py1 = (twz1 - wz0) / (wz1 - wz0) * outSize

                    Dim ix0 = CInt(Math.Floor(CDbl(px0)))
                    Dim iy0 = CInt(Math.Floor(CDbl(py0)))
                    Dim ix1 = CInt(Math.Ceiling(CDbl(px1)))
                    Dim iy1 = CInt(Math.Ceiling(CDbl(py1)))

                    ix1 = Math.Min(outSize, ix1 + 1)
                    iy1 = Math.Min(outSize, iy1 + 1)

                    Using tileBmp As New Bitmap(tilePath)
                        Dim dw = Math.Max(1, ix1 - ix0)
                        Dim dh = Math.Max(1, iy1 - iy0)
                        g.DrawImage(tileBmp,
                                    New Rectangle(ix0, iy0, dw, dh),
                                    New Rectangle(0, 0, tileBmp.Width, tileBmp.Height),
                                    GraphicsUnit.Pixel)
                    End Using
                Next
            Next
        End Using

        Return bmp
    End Function

    Private Function PickZoom(mppTarget As Single) As Integer
        Return PickZoomFromMpp(mppTarget)
    End Function

    Private Function PickZoomFromMpp(mppTarget As Single) As Integer
        Dim lo = Math.Max(_info.minZoom, 0)
        Dim hi = Math.Max(lo, Math.Min(ExportMaxZoom, 14))
        Dim best = hi
        For zt = lo To hi
            Dim n = 1 << zt
            Dim tileWorldW = CSng((_info.x2 - _info.x1) / n)
            Dim mppTile = tileWorldW / 256.0F
            If mppTile <= mppTarget * 1.35F Then
                best = zt
                Exit For
            End If
        Next
        Return best
    End Function

    Private Function PickAtRenderZoom(renderZoom As Single) As Integer
        If renderZoom <= 0.000001F Then Return ExportMinZoom
        Return PickZoomFromMpp(1.0F / renderZoom)
    End Function

    Public Function BuildOfflineRenderZoomStops() As Single() Implements IMapCompositor.BuildOfflineRenderZoomStops
        If Not IsAvailable Then Return Array.Empty(Of Single)()
        Dim xSpan = CSng(_info.x2 - _info.x1)
        Dim r0 = CSng(1.35 * 256.0 / xSpan * 0.5)
        r0 = Math.Max(0.0002F, Math.Min(0.05F, r0))
        Const r1 As Single = 18.0F
        Const steps = 480

        Dim bands As New List(Of (pick As Integer, rStart As Single, rEnd As Single))()
        Dim bandStartR = r0
        Dim lastPick = PickAtRenderZoom(r0)

        For i = 1 To steps
            Dim t = i / CSng(steps)
            Dim r = r0 + (r1 - r0) * t
            Dim p = PickAtRenderZoom(r)
            If p <> lastPick OrElse i = steps Then
                Dim rEnd = r
                If lastPick >= ExportMinZoom AndAlso lastPick <= ExportMaxZoom Then
                    bands.Add((lastPick, bandStartR, rEnd))
                End If
                bandStartR = r
                lastPick = p
            End If
        Next

        If bands.Count = 0 Then Return Array.Empty(Of Single)()

        Dim stepsPerBand = 6
        Dim candidates As New List(Of Single)()
        For Each b In bands
            Dim span = b.rEnd - b.rStart
            If span <= 0 Then
                candidates.Add((b.rStart + b.rEnd) * 0.5F)
            Else
                For j = 0 To stepsPerBand - 1
                    Dim v As Single
                    If stepsPerBand = 1 Then
                        v = (b.rStart + b.rEnd) * 0.5F
                    Else
                        v = b.rStart + span * (j / CSng(stepsPerBand - 1))
                    End If
                    candidates.Add(v)
                Next
            End If
        Next

        If candidates.Count = 0 Then Return Array.Empty(Of Single)()

        candidates = candidates.OrderBy(Function(x) x).ToList()

        Dim dedup As New List(Of Single)()
        Dim lastAdded As Single = -1.0F
        For Each v In candidates
            If dedup.Count = 0 OrElse Math.Abs(v - lastAdded) > 0.02F Then
                dedup.Add(v)
                lastAdded = v
            End If
        Next

        Return dedup.ToArray()
    End Function

End Class