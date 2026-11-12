Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography

''' <summary>
''' 百宝箱：自定义下载、垃圾清理、内存优化、今日人品与随机文本。
''' </summary>
Public Class PageOtherTest

#Region "页面"

    Private Sub PageOtherTest_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If TextDownloadFolder.Text.Length = 0 Then
            Dim Saved As String = Settings.Get(Of String)("ToolDownloadFolder")
            TextDownloadFolder.Text = If(String.IsNullOrWhiteSpace(Saved), DefaultDownloadFolder(), Saved)
        End If
        If CurrentDownload IsNot Nothing AndAlso CurrentDownload.State = LoadState.Loading Then AttachDownload(CurrentDownload)
    End Sub

    Private Sub BtnRubbish_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnRubbish.Click
        BtnRubbish.IsEnabled = False
        SetToolState("正在清理……")
        RunInThread(
        Sub()
            Try
                RubbishClear()
            Finally
                RunInUi(Sub() BtnRubbish.IsEnabled = True)
            End Try
        End Sub)
    End Sub

    Private Sub BtnMemory_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnMemory.Click
        BtnMemory.IsEnabled = False
        SetToolState("正在优化内存……")
        RunInThread(
        Sub()
            Try
                MemoryOptimize(True)
            Finally
                RunInUi(Sub() BtnMemory.IsEnabled = True)
            End Try
        End Sub)
    End Sub

    Private Sub BtnJrrp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnJrrp.Click
        Jrrp()
    End Sub

    Private Sub BtnHint_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnHint.Click
        MyMsgBox(GetRandomHint(), "随机提示", "好耶")
    End Sub

    Private Sub BtnSelectFolder_Click(sender As Object, e As EventArgs) Handles BtnSelectFolder.Click
        Dim Folder As String = Dialogs.SelectFolder("选择下载保存位置", False).FirstOrDefault
        If String.IsNullOrWhiteSpace(Folder) Then Return
        TextDownloadFolder.Text = Folder
        Settings.Set("ToolDownloadFolder", Folder)
    End Sub

    Private Sub BtnDownloadStart_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDownloadStart.Click
        Dim Url As String = TextDownloadUrl.Text.Trim()
        If Not (Url.StartsWithF("http://", True) OrElse Url.StartsWithF("https://", True)) Then
            Hint("请输入以 http:// 或 https:// 开头的网址", HintType.Red)
            Return
        End If
        Dim Folder As String = TextDownloadFolder.Text.Trim()
        If Folder.Length = 0 Then Folder = DefaultDownloadFolder()
        Settings.Set("ToolDownloadFolder", Folder)
        Dim Name As String = TextDownloadName.Text.Trim()
        If Name.Length = 0 Then Name = IO.Path.GetFileName(Url.Split("?"c)(0))
        If Name.Length = 0 Then Name = "未命名文件"
        StartCustomDownload(Url, Name, Folder)
    End Sub

    Private Sub BtnDownloadOpen_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnDownloadOpen.Click
        Dim Folder As String = TextDownloadFolder.Text.Trim()
        If Folder.Length = 0 Then Folder = DefaultDownloadFolder()
        Try
            If Not Directory.Exists(Folder) Then Directory.CreateDirectory(Folder)
            OpenExplorer(Folder)
        Catch ex As Exception
            Logger.Warn(ex, "打开下载文件夹失败")
            Hint("打开文件夹失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    ''' <summary>
    ''' 把正在进行的下载挂到页面进度环上。
    ''' </summary>
    Friend Sub AttachDownload(Loader As LoaderBase)
        If LoadDownload Is Nothing Then Return
        LoadDownload.Visibility = Visibility.Visible
        LabDownloadState.Text = "正在下载……"
        '未加载时挂状态会绑不上，等 Loaded 时再补
        If IsLoaded Then LoadDownload.State = Loader
    End Sub

    ''' <summary>
    ''' 下载结束后的页面收尾。
    ''' </summary>
    Friend Sub FinishDownload(Message As String, IsError As Boolean)
        If LoadDownload Is Nothing Then Return
        LoadDownload.Visibility = Visibility.Collapsed
        LabDownloadState.Text = Message
        LabDownloadState.Opacity = If(IsError, 1.0, 0.7)
    End Sub

    Private Shared Sub SetToolState(Text As String)
        If FrmOtherTest Is Nothing Then Return
        FrmOtherTest.LabToolState.Text = Text
    End Sub

    Private Shared Sub PrepareDownloadPage(Url As String, Folder As String, FileName As String, Loader As LoaderBase)
        '从自定义事件进来时顺带切到百宝箱，方便看到进度
        Try
            If FrmMain IsNot Nothing Then FrmMain.PageChange(FormMain.PageType.Other, FormMain.PageSubType.OtherTest)
        Catch ex As Exception
            Logger.Warn(ex, "切换到百宝箱页面失败")
        End Try
        If FrmOtherTest Is Nothing Then Return
        FrmOtherTest.TextDownloadUrl.Text = Url
        FrmOtherTest.TextDownloadFolder.Text = Folder
        FrmOtherTest.TextDownloadName.Text = FileName
        FrmOtherTest.AttachDownload(Loader)
    End Sub

    Private Shared Sub FinishDownloadOnPage(Message As String, IsError As Boolean)
        If FrmOtherTest Is Nothing Then Return
        FrmOtherTest.FinishDownload(Message, IsError)
    End Sub

#End Region

#Region "自定义下载"

    Private Shared CurrentDownload As LoaderTask(Of Integer, Integer)

    Friend Shared Function DefaultDownloadFolder() As String
        Return PathTemp & "MyDownload\"
    End Function

    ''' <summary>
    ''' 从指定网址下载文件。Folder 为空时下载到百宝箱的默认目录。
    ''' 该方法立即返回，实际下载在后台线程中进行。
    ''' </summary>
    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
        Url = If(Url, "").Trim()
        If Url.Length = 0 Then Throw New ArgumentException("下载地址不能为空")
        If String.IsNullOrWhiteSpace(Folder) Then Folder = DefaultDownloadFolder()
        Dim TargetFolder As String = PathUtils.AddSlashSuffix(Folder.Trim())
        FileName = SanitizeFileName(If(FileName, "").Trim())
        If FileName.Length = 0 Then FileName = "未命名文件"
        Dim LocalPath As String = TargetFolder & FileName
        RunInThread(
        Sub()
            Dim DownloadTask As LoaderTask(Of Integer, Integer) = Nothing
            Try
                Directory.CreateDirectory(TargetFolder)
                DownloadTask = New LoaderTask(Of Integer, Integer)("自定义下载",
                    Sub(Loader)
                        NetDownloadByLoader(Url, LocalPath, Loader, New FileChecker With {.MinSize = 1})
                    End Sub)
                CurrentDownload = DownloadTask
                RunInUi(Sub() PrepareDownloadPage(Url, TargetFolder, FileName, DownloadTask))
                DownloadTask.WaitForExit()
                If DownloadTask.State <> LoadState.Finished Then Throw New Exception("下载未完成，可能是网络中断或文件校验失败")
                RunInUi(
                Sub()
                    FinishDownloadOnPage($"下载完成：{LocalPath}", False)
                    Hint($"下载完成：{FileName}", HintType.Green)
                End Sub)
            Catch ex As Exception
                Logger.Error(ex, "自定义下载失败")
                RunInUi(
                Sub()
                    FinishDownloadOnPage($"下载失败：{ex.Message}", True)
                    Hint("下载失败：" & ex.Message, HintType.Red)
                End Sub)
            Finally
                CurrentDownload = Nothing
            End Try
        End Sub)
    End Sub

    Private Shared Function SanitizeFileName(Name As String) As String
        If String.IsNullOrEmpty(Name) Then Return ""
        For Each InvalidChar As Char In IO.Path.GetInvalidFileNameChars()
            Name = Name.Replace(InvalidChar, "_"c)
        Next
        Return Name.Trim()
    End Function

#End Region

#Region "垃圾清理"

    ''' <summary>
    ''' 清理 PCL 的缓存、临时文件与旧日志。仅处理 PathTemp 内部，不触碰游戏文件。
    ''' </summary>
    Public Shared Sub RubbishClear()
        Dim Total As Long = 0
        Dim DeletedFolder As Integer = 0

        '缓存与临时目录：整目录删除，下次启动会自动重建
        For Each FolderName As String In {"Cache", "Temp"}
            Dim Folder As String = PathTemp & FolderName
            If Not Directory.Exists(Folder) Then Continue For
            Total += GetFolderSize(Folder)
            Try
                Directory.Delete(Folder, True)
                DeletedFolder += 1
            Catch ex As Exception
                Logger.Warn(ex, $"清理目录失败：{Folder}")
            End Try
        Next

        '临时文件与旧日志
        For Each Pattern As String In {"*.tmp", "*.log", "*.old", "*.bak"}
            Try
                For Each FilePath As String In Directory.GetFiles(PathTemp, Pattern)
                    Try
                        Total += New FileInfo(FilePath).Length
                        IO.File.Delete(FilePath)
                    Catch ex As Exception
                        Logger.Warn(ex, $"清理文件失败：{FilePath}")
                    End Try
                Next
            Catch ex As Exception
                Logger.Warn(ex, $"枚举临时文件失败：{Pattern}")
            End Try
        Next

        Logger.Info($"垃圾清理完成：{DeletedFolder} 个目录，释放 {Total} 字节")
        Dim Message As String = $"清理完成，释放 {FormatSize(Total)}"
        RunInUi(
        Sub()
            SetToolState(Message)
            Hint(Message, HintType.Green)
        End Sub)
    End Sub

    Private Shared Function GetFolderSize(Folder As String) As Long
        Dim Size As Long = 0
        Try
            For Each FilePath As String In Directory.GetFiles(Folder, "*", SearchOption.AllDirectories)
                Try
                    Size += New FileInfo(FilePath).Length
                Catch
                    '统计失败的文件直接忽略
                End Try
            Next
        Catch ex As Exception
            Logger.Warn(ex, $"统计目录大小失败：{Folder}")
        End Try
        Return Size
    End Function

#End Region

#Region "内存优化"

    <DllImport("psapi.dll", SetLastError:=True)>
    Private Shared Function EmptyWorkingSet(hProcess As IntPtr) As Boolean
    End Function

    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
        MemoryOptimizeInternal(ShowHint)
    End Sub

    ''' <summary>
    ''' 把各进程暂时不用的内存页换出到页面文件。
    ''' 与 --memory 启动参数共用同一实现，因此不能有任何依赖主窗体已加载的操作。
    ''' </summary>
    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
        Dim Before As ULong = My.Computer.Info.AvailablePhysicalMemory
        Dim SelfId As Integer
        Using SelfProcess As Process = Process.GetCurrentProcess()
            SelfId = SelfProcess.Id
        End Using
        Dim Handled As Integer = 0
        For Each Proc As Process In Process.GetProcesses()
            Try
                If Proc.Id <> SelfId AndAlso Proc.Handle <> IntPtr.Zero AndAlso EmptyWorkingSet(Proc.Handle) Then Handled += 1
            Catch
                '权限不足的进程直接跳过
            Finally
                Proc.Dispose()
            End Try
        Next
        Dim After As ULong = My.Computer.Info.AvailablePhysicalMemory
        Dim Freed As Long = If(After > Before, CLng((After - Before) / 1024), 0)
        Logger.Info($"内存优化完成：处理 {Handled} 个进程，可用内存增加 {Freed} KB")
        If Not ShowHint Then Return
        Dim Message As String = $"内存优化完成，释放约 {FormatSize(Freed * 1024)}"
        RunInUi(
        Sub()
            SetToolState(Message)
            Hint(Message, HintType.Green)
        End Sub)
    End Sub

#End Region

#Region "今日人品"

    Public Shared Sub Jrrp()
        Dim Score As Integer = GetJrrpScore()
        Dim Level As String
        Dim Comment As String
        Select Case Score
            Case >= 90
                Level = "大吉"
                Comment = "今天的运气好得有点过分，抽卡开箱请随意。"
            Case >= 70
                Level = "中吉"
                Comment = "状态不错，适合去挑战点平时不敢碰的东西。"
            Case >= 50
                Level = "小吉"
                Comment = "平稳的一天，按计划做事就好。"
            Case >= 30
                Level = "末吉"
                Comment = "有点小坎坷，别急着下决定。"
            Case >= 10
                Level = "凶"
                Comment = "今天适合摸鱼，重要操作建议明天再说。"
            Case Else
                Level = "大凶"
                Comment = "建议原地躺平，等明天重新开局。"
        End Select
        Dim Message As String = $"今日人品：{Score} / 100{vbCrLf}等级：{Level}{vbCrLf}{vbCrLf}{Comment}"
        RunInUi(Sub() MyMsgBox(Message, "今日人品", "好耶"))
    End Sub

    ''' <summary>
    ''' 用日期与设备标识生成固定的分数，同一天内结果不变。
    ''' </summary>
    Private Shared Function GetJrrpScore() As Integer
        Dim Seed As String = $"{Now:yyyyMMdd}|{Settings.Get(Of String)("Identify")}|{Environment.UserName}"
        Using Md5 As MD5 = MD5.Create()
            Dim Hash As Byte() = Md5.ComputeHash(Encoding.UTF8.GetBytes(Seed))
            Return CInt(BitConverter.ToUInt32(Hash, 0) Mod 101UI)
        End Using
    End Function

#End Region

#Region "随机文本"

    Private Shared ReadOnly RandomLock As New Object()
    Private Shared ReadOnly RandomSource As New Random()

    Private Shared ReadOnly RandomHints As String() = {
        "下载源抽风时，可以在 设置 → 下载 里换成镜像源试试。",
        "整合包文件可以直接拖进启动器窗口，会自动开始安装。",
        "每个版本都能单独设置内存，互不影响。",
        "启动失败时先看日志，比起反复重启有用得多。",
        "缓存堆得太大占地方的话，来 更多 → 百宝箱 点一下清理。",
        "启动器支持用 --memory 参数单独执行一次内存优化。",
        "把鼠标放到版本图标上，可以快速看到它的加载器与版本号。"
    }

    Private Shared ReadOnly RandomCaves As String() = {
        "你走进了一个洞穴。洞穴深处传来回声：『这里什么也没有。』",
        "洞穴中立着一块告示牌，上面写着：『前方施工，请绕行。』",
        "洞穴里堆满了箱子，打开一看，全是空的。",
        "洞穴的尽头是一扇门，门上贴着：『下次一定。』",
        "洞壁上刻着一行小字：『挖三填一，方得始终。』"
    }

    Private Shared ReadOnly RandomPresetHints As String() = {
        "试试在版本设置里把内存上限调高一点，卡顿会改善不少。",
        "给常用的版本加个桌面快捷方式，能省掉每次找它的时间。",
        "整合包安装完成后可以在版本设置里单独调整它的 Mod 与配置。"
    }

    Public Shared Function GetRandomHint() As String
        Return PickRandom(RandomHints)
    End Function

    Public Shared Function GetRandomCave() As String
        Return PickRandom(RandomCaves)
    End Function

    Public Shared Function GetRandomPresetHint() As String
        Return PickRandom(RandomPresetHints)
    End Function

    Private Shared Function PickRandom(Source As String()) As String
        If Source Is Nothing OrElse Source.Length = 0 Then Return ""
        SyncLock RandomLock
            Return Source(RandomSource.Next(Source.Length))
        End SyncLock
    End Function

#End Region

#Region "工具"

    Private Shared Function FormatSize(Bytes As Long) As String
        If Bytes < 1024 Then Return $"{Bytes} B"
        If Bytes < 1024L * 1024 Then Return $"{Bytes / 1024.0:F1} KB"
        If Bytes < 1024L * 1024 * 1024 Then Return $"{Bytes / 1024.0 / 1024.0:F1} MB"
        Return $"{Bytes / 1024.0 / 1024.0 / 1024.0:F2} GB"
    End Function

#End Region

End Class
