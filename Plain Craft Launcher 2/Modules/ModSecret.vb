'由于包含加解密等安全信息，本文件中的部分代码已被删除

Friend Module ModSecret

    '标注 PCL 的不同分支，仅用于替换标记
    Public Const VersionBranchMain As String = "OpenSource"
    '在开源版的注册表与常规版的注册表隔离，以防数据冲突
    Public Const RegFolder As String = "PCLDebug"
    '用于微软登录的 ClientId
    Public OAuthClientId As String = If(Environment.GetEnvironmentVariable("PCL_MS_CLIENT_ID"), "")
    'CurseForge API Key
    Public CurseForgeAPIKey As String = If(Environment.GetEnvironmentVariable("PCL_CURSEFORGE_API_KEY"), "")
    '用于匿名数据收集的腾讯云日志服务上报 URL，形如 https://{region}.cls.tencentcs.com/track?topic_id={topic_id}
    Public Const ClsBaseUrl As String = ""

#Region "网络鉴权"

    '开源版原本为 True，会整体关闭 MC 下载（版本列表、整合包安装、修改页、资源页一键安装）
    '本分支关闭该限制以恢复核心下载功能，下载源仍为 Mojang 官方源、BMCLAPI 与 mcimirror
    Public Const RestrictMcDownload As Boolean = False
    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")  '#4951
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"PCL2/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.pcl2.server/")
        If Url.Contains("api.curseforge.com") OrElse Url.Contains("forgecdn.net") Then Req.Headers.Add("x-api-key", CurseForgeAPIKey)
    End Sub

#End Region

#Region "主题"

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210, ColorSat As Integer = 85, ColorLightAdjust As Integer = 0, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = 0
    Public ThemeDontClick As Integer = 0

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        Try
            If ThemeNow = NewTheme AndAlso NewTheme >= 0 Then Return
            If NewTheme >= 0 Then ThemeNow = NewTheme

            Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
            Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
            Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
            Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
            Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
            Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
            Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
            Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
            ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
            ColorBg1 = New MyColor(190, Color7)

            ColorSemiTransparent = New MyColor(1, Color8)
            Application.Current.Resources("ColorBrush1") = New SolidColorBrush(Color1)
            Application.Current.Resources("ColorBrush2") = New SolidColorBrush(Color2)
            Application.Current.Resources("ColorBrush3") = New SolidColorBrush(Color3)
            Application.Current.Resources("ColorBrush4") = New SolidColorBrush(Color4)
            Application.Current.Resources("ColorBrush5") = New SolidColorBrush(Color5)
            Application.Current.Resources("ColorBrush6") = New SolidColorBrush(Color6)
            Application.Current.Resources("ColorBrush7") = New SolidColorBrush(Color7)
            Application.Current.Resources("ColorBrush8") = New SolidColorBrush(Color8)
            Application.Current.Resources("ColorBrushBg0") = New SolidColorBrush(ColorBg0)
            Application.Current.Resources("ColorBrushBg1") = New SolidColorBrush(ColorBg1)
            Application.Current.Resources("ColorObject1") = CType(Color1, Color)
            Application.Current.Resources("ColorObject2") = CType(Color2, Color)
            Application.Current.Resources("ColorObject3") = CType(Color3, Color)
            Application.Current.Resources("ColorObject4") = CType(Color4, Color)
            Application.Current.Resources("ColorObject5") = CType(Color5, Color)
            Application.Current.Resources("ColorObject6") = CType(Color6, Color)
            Application.Current.Resources("ColorObject7") = CType(Color7, Color)
            Application.Current.Resources("ColorObject8") = CType(Color8, Color)
            Application.Current.Resources("ColorObjectBg0") = CType(ColorBg0, Color)
            Application.Current.Resources("ColorObjectBg1") = CType(ColorBg1, Color)
            ThemeRefreshMain()
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub
    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If Not FrmMain.IsLoaded Then Return
            '顶部条背景
            Dim Brush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
            Dim Deltas = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + Deltas(0), ColorSat, 48 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + Deltas(1), ColorSat, 54 + ColorLightAdjust)})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + Deltas(2), ColorSat, 48 + ColorLightAdjust)})
            FrmMain.PanTitle.Background = Brush
            FrmMain.PanTitle.Background.Freeze()
            '主页面背景
            If Settings.Get(Of Boolean)("UiBackgroundColorful") Then
                Brush = New LinearGradientBrush With {.EndPoint = New Point(0.1, 1), .StartPoint = New Point(0.9, 0)}
                Brush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 15, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, ColorSat * 0.8, 91)})
                Brush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 15, ColorSat * 0.8, 91)})
                FrmMain.PanForm.Background = Brush
            Else
                FrmMain.PanForm.Background = New MyColor(245, 245, 245)
            End If
            FrmMain.PanForm.Background.Freeze()
        End Sub)
    End Sub
    Friend Sub ThemeCheckAll(EffectSetup As Boolean)
    End Sub
    Friend Function ThemeCheckOne(Id As Integer) As Boolean
        Return True
    End Function
    Friend Function ThemeUnlock(Id As Integer, Optional ShowDoubleHint As Boolean = True, Optional UnlockHint As String = Nothing) As Boolean
        Return False
    End Function

#End Region

#Region "更新"

    '本分支的发布渠道：本仓库自己的 GitHub Releases
    Private Const UpdateRepoApi As String = "https://api.github.com/repos/XRC111/PCL"
    Private Const UpdateReleasePage As String = "https://github.com/XRC111/PCL/releases/latest"

    ''' <summary>
    ''' 检查更新。发现新版本时展示更新内容，并引导前往下载页。
    ''' </summary>
    Friend Sub UpdateCheckByButton()
        Hint("正在检查更新……", HintType.Blue, Log:=False)
        RunInThread(
        Sub()
            Try
                Dim Release As JObject = UpdateGetLatestRelease()
                If Release Is Nothing Then
                    Hint("检查更新失败，请检查网络连接", HintType.Red)
                    Return
                End If
                Dim Latest As Version = Nothing
                If Not Version.TryParse(Release("version").ToString, Latest) Then
                    Hint("检查更新失败：无法解析版本号 " & Release("version").ToString, HintType.Red)
                    Return
                End If
                If Latest <= New Version(VersionBaseName) Then
                    Hint($"当前已是最新版本（{VersionBaseName}）", HintType.Green)
                    Return
                End If
                Dim Notes As String = Release("notes").ToString.Trim
                If Notes.Length > 600 Then Notes = Notes.Substring(0, 600) & "……"
                MyMsgBox($"发现新版本 {Latest}，当前版本 {VersionBaseName}。{vbCrLf}{vbCrLf}{Notes}",
                         "检查更新", "前往下载", "稍后", Button1Action:=Sub() OpenWebsite(UpdateReleasePage))
            Catch ex As Exception
                Logger.Error(ex, "检查更新失败")
                Hint("检查更新失败：" & ex.Message, HintType.Red)
            End Try
        End Sub)
    End Sub

    ''' <summary>
    ''' 拉取本仓库最新的 Release 信息，返回 version、notes、exe 三个字段。失败返回 Nothing。
    ''' </summary>
    Private Function UpdateGetLatestRelease() As JObject
        Try
            Dim Headers As New String(,) {{"User-Agent", "PCL2/" & VersionBaseName}, {"Accept", "application/vnd.github+json"}}
            Dim Raw As String = NetRequestByClientRetry(UpdateRepoApi & "/releases/latest", RequireJson:=True, Headers:=Headers)
            Dim Json As JObject = Raw.DeserializeJson()
            Dim Tag As String = If(Json("tag_name"), "").ToString.Trim
            If Tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) Then Tag = Tag.Substring(1)
            If String.IsNullOrEmpty(Tag) Then Return Nothing
            Dim ExeUrl As String = ""
            Dim Assets = TryCast(Json("assets"), JArray)
            If Assets IsNot Nothing Then
                For Each Asset As JObject In Assets
                    If If(Asset("name"), "").ToString.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) Then
                        ExeUrl = If(Asset("browser_download_url"), "").ToString
                        Exit For
                    End If
                Next
            End If
            Return New JObject(New JProperty("version", Tag), New JProperty("notes", If(Json("body"), "").ToString), New JProperty("exe", ExeUrl))
        Catch ex As Exception
            Logger.Warn(ex, "获取最新版本信息失败")
            Return Nothing
        End Try
    End Function

    Friend IsUpdateWaitingRestart As Boolean = False
    Public Sub UpdateRestart(TriggerRestartAndByEnd As Boolean)
    End Sub
    Public Sub UpdateReplace(ProcessId As Integer, OldFileName As String, NewFileName As String, TriggerRestart As Boolean)
    End Sub

    ''' <summary>
    ''' 确保 PathTemp/Latest.exe 是最新正式版的 PCL，它会被用于整合包打包。
    ''' 若不是最新，则下载一个。
    ''' </summary>
    Friend Sub DownloadLatestPCL(Optional LoaderToSyncProgress As LoaderBase = Nothing)
        Dim Release As JObject = UpdateGetLatestRelease()
        If Release Is Nothing Then Throw New Exception("获取最新版本信息失败，请检查网络连接")
        Dim ExeUrl As String = Release("exe").ToString
        If String.IsNullOrEmpty(ExeUrl) Then Throw New Exception($"未在 {Release("version")} 的发布中找到启动器文件")
        NetDownloadByLoader({ExeUrl}, PathTemp & "Latest.exe", LoaderToSyncProgress, New FileChecker With {.MinSize = 1024 * 1024})
    End Sub

#End Region

#Region "联网配置"

    ''' <summary>
    ''' 联网获取的配置信息。
    ''' 若获取失败或仍在获取中，可能为 Nothing。
    ''' </summary>
    Public ServerConfig As JObject

    ''' <summary>
    ''' 联网通知源，可用环境变量 PCL_NOTICE_URL 覆盖，需指向一个 JSON 文件。
    ''' </summary>
    Private Function ServerNoticeUrl() As String
        Dim FromEnv As String = Environment.GetEnvironmentVariable("PCL_NOTICE_URL")
        If Not String.IsNullOrEmpty(FromEnv) Then Return FromEnv
        Return "https://raw.githubusercontent.com/XRC111/PCL/main/.pcl/notice.json"
    End Function

    ''' <summary>
    ''' 联网获取通知与配置，并把版本号写入更新检查缓存（Cache\Notice.cfg）。
    ''' 配置格式：{"notice": "公告文本", "snapshotCode": 414, "releaseCode": 413}
    ''' </summary>
    Private Sub ServerLoaderMain()
        Try
            Dim Raw As String = NetRequestByClientRetry(ServerNoticeUrl(), RequireJson:=True)
            If String.IsNullOrWhiteSpace(Raw) Then Return
            Dim Json As JObject = Raw.DeserializeJson()
            ServerConfig = Json
            '写入更新缓存，供 PageSetupSystem.IsLauncherNewest 使用
            Dim ReleaseCode As Integer = VersionCode
            Dim SnapshotCode As Integer = VersionCode
            If Json("releaseCode") IsNot Nothing Then ReleaseCode = Json("releaseCode").ToObject(Of Integer)()
            If Json("snapshotCode") IsNot Nothing Then SnapshotCode = Json("snapshotCode").ToObject(Of Integer)()
            Try
                IO.Directory.CreateDirectory(PathTemp & "Cache")
                FileUtils.Write(PathTemp & "Cache\Notice.cfg", $"PCL|{SnapshotCode}|{ReleaseCode}")
            Catch ex As Exception
                Logger.Warn(ex, "写入更新缓存失败")
            End Try
            '同一条公告只提示一次
            Dim Notice As String = If(Json("notice"), "").ToString.Trim
            If Notice.Length = 0 Then Return
            Dim ReadMark As String = PathTemp & "Cache\NoticeRead.txt"
            If FileUtils.TryReadAsString(ReadMark) = Notice Then Return
            FileUtils.Write(ReadMark, Notice)
            MyMsgBox(Notice, "公告")
        Catch ex As Exception
            Logger.Warn(ex, "联网获取通知失败")
        End Try
    End Sub

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 配置更新", AddressOf ServerLoaderMain, Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60} '超时 1 小时

#End Region

#Region "赞助等级"

    Public ReadOnly Property CurrentRank As DonationRank
        Get
            Return DonationRank.None
        End Get
    End Property

    Public Sub InputPotatoCode(IsUpdating As Boolean)
    End Sub
    Friend Sub GeneratePotatoCode()
    End Sub

    ''' <summary>
    ''' 获取设备识别码。
    ''' </summary>
    Friend Function GetIdentify() As String
        Return "0000-0000-0000-0000"
    End Function

#End Region

End Module
