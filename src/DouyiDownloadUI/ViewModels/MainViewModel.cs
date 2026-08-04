using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public enum Step { Paste, Name, Done }

    private const int MaxRecentDownloads = 20;
    private readonly IDownloadEngine _engine;
    private readonly SettingsService _settings;
    private readonly IClipboardService _clipboard;
    private AppSettings _config = new();
    private string? _extractedUrl;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private Step _currentStep = Step.Paste;

    [ObservableProperty]
    private string _shareText = "";

    [ObservableProperty]
    private bool _linkRecognized;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private string _number = "";

    [ObservableProperty]
    private string _type = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _resultFileName = "";

    [ObservableProperty]
    private bool _isImagePost;

    public ObservableCollection<string> TypeOptions { get; } = new();
    public ObservableCollection<RecentDownloadEntry> RecentDownloads { get; } = new();
    public SettingsService Settings { get; }

    public bool CanNext => LinkRecognized && !IsBusy;
    public bool CanDownloadVideo => !IsImagePost;
    public bool StepIsPaste => CurrentStep == Step.Paste;
    public bool StepIsName => CurrentStep == Step.Name;
    public bool StepIsDone => CurrentStep == Step.Done;
    public bool StepIsNameOrDone => CurrentStep is Step.Name or Step.Done;

    public MainViewModel(IDownloadEngine engine, SettingsService settings, IClipboardService clipboard)
    {
        _engine = engine;
        _settings = settings;
        _clipboard = clipboard;
        Settings = settings;
        _config = settings.Load();
        RefreshTypeOptions();
        RefreshRecentDownloads();
    }

    partial void OnShareTextChanged(string value) => LinkRecognized = LinkParser.ExtractUrl(value) is not null;
    partial void OnLinkRecognizedChanged(bool value) => OnPropertyChanged(nameof(CanNext));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanNext));
    partial void OnIsImagePostChanged(bool value) => OnPropertyChanged(nameof(CanDownloadVideo));

    partial void OnCurrentStepChanged(Step value)
    {
        OnPropertyChanged(nameof(StepIsPaste));
        OnPropertyChanged(nameof(StepIsName));
        OnPropertyChanged(nameof(StepIsDone));
        OnPropertyChanged(nameof(StepIsNameOrDone));
    }

    public void OnWindowActivated()
    {
        if (CurrentStep != Step.Paste || IsBusy) return;
        var text = _clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text)) return;
        if (LinkParser.ExtractUrl(text) is null) return;
        ShareText = text;
    }

    public void SetFileNameFromSelection(string? selectedText)
    {
        if (!string.IsNullOrWhiteSpace(selectedText))
        {
            FileName = selectedText.Trim();
        }
    }

    public void RefreshFromSettings()
    {
        _config = _settings.Load();
        RefreshTypeOptions();
        RefreshRecentDownloads();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        ErrorMessage = "";
        StatusMessage = "";
        _extractedUrl = LinkParser.ExtractUrl(ShareText);
        if (_extractedUrl is null)
        {
            ErrorMessage = "没有找到抖音视频，请重新复制";
            return;
        }
        IsBusy = true;
        StatusMessage = "正在读取视频信息…";
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var meta = await _engine.GetMetadataAsync(_extractedUrl, timeout.Token);
            if (meta is null)
            {
                LogService.Error($"读取视频信息失败：{_extractedUrl}");
                ErrorMessage = "读取视频信息失败，请检查网络或链接";
                return;
            }
            Title = meta.Title;
            IsImagePost = meta.IsImagePost;
            Number = NumberingService.GetDefaultNumber(_config.SaveFolder, _config.LastNumber)
                .ToString("D3");
            Type = !string.IsNullOrEmpty(_config.LastType) && _config.TypeOptions.Contains(_config.LastType)
                ? _config.LastType
                : (_config.TypeOptions.Count > 0 ? _config.TypeOptions[0] : "");
            FileName = FilenameBuilder.Truncate(FilenameBuilder.Sanitize(meta.Title));
            CurrentStep = Step.Name;
        }
        catch (Exception ex)
        {
            LogService.Error($"读取视频信息异常：{_extractedUrl}", ex);
            ErrorMessage = "读取视频信息失败，请检查网络或链接";
        }
        finally
        {
            IsBusy = false;
            StatusMessage = "";
        }
    }

    [RelayCommand]
    private Task DownloadVideoAsync() => StartDownloadAsync(DownloadMode.Video);

    [RelayCommand]
    private Task DownloadAudioAsync() => StartDownloadAsync(DownloadMode.Audio);

    private async Task StartDownloadAsync(DownloadMode mode)
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            ErrorMessage = "标题不能为空";
            return;
        }
        if (!int.TryParse(Number, out var number) || number <= 0)
        {
            ErrorMessage = "编号必须是数字";
            return;
        }
        if (_extractedUrl is null) return;
        if (IsImagePost && mode == DownloadMode.Video)
        {
            ErrorMessage = "这是图文作品，没有视频可下载，请点「下载音频」";
            return;
        }

        ErrorMessage = "";
        IsBusy = true;
        ProgressPercent = 0;
        ProgressText = "准备中…";
        _cts = new CancellationTokenSource();
        var ext = mode == DownloadMode.Audio ? "mp3" : "mp4";
        var fullName = FilenameBuilder.BuildFileName(Number, Type, FileName, ext);
        var request = new DownloadRequest(
            _extractedUrl,
            _config.SaveFolder,
            Path.GetFileNameWithoutExtension(fullName),
            mode);
        var progress = new Progress<DownloadProgress>(p =>
        {
            ProgressPercent = p.Percent;
            ProgressText = $"{p.Percent:0.#}%";
        });
        try
        {
            LogService.Info($"开始下载：{request.FileNameWithoutExtension} ({mode})");
            var result = await _engine.DownloadAsync(request, progress, _cts.Token);
            if (!result.Success)
            {
                LogService.Error($"下载失败：{result.ErrorKind} {result.ErrorDetail}");
                ErrorMessage = FriendlyError(result);
                return;
            }
            var finalName = Path.GetFileName(result.FilePath!);
            _config.LastNumber = number;
            if (!string.IsNullOrWhiteSpace(Type)) _config.LastType = Type.Trim();
            _config.RecentDownloads.Insert(
                0,
                new RecentDownloadEntry(
                    finalName, result.FilePath!, DateTime.Now, mode == DownloadMode.Audio));
            if (_config.RecentDownloads.Count > MaxRecentDownloads)
            {
                _config.RecentDownloads.RemoveRange(
                    MaxRecentDownloads, _config.RecentDownloads.Count - MaxRecentDownloads);
            }
            _settings.Save(_config);
            RefreshRecentDownloads();
            ResultFileName = finalName;
            CurrentStep = Step.Done;
        }
        catch (Exception)
        {
            ErrorMessage = "下载出错，请重试";
        }
        finally
        {
            IsBusy = false;
            ProgressText = "";
            _cts.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        if (_cts is null) return;
        _cts.Cancel();
        ErrorMessage = "";
        StatusMessage = "正在取消…";
    }

    [RelayCommand]
    private void DownloadAnother()
    {
        ShareText = "";
        LinkRecognized = false;
        ErrorMessage = "";
        ProgressPercent = 0;
        ResultFileName = "";
        IsImagePost = false;
        CurrentStep = Step.Paste;
    }

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(_config.SaveFolder);
            Process.Start(new ProcessStartInfo("explorer.exe", _config.SaveFolder)
            {
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            ErrorMessage = "打开文件夹失败";
        }
    }

    public void OpenRecent(RecentDownloadEntry? entry)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.FilePath)) return;
        try
        {
            if (File.Exists(entry.FilePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.FilePath}\"")
                {
                    UseShellExecute = true
                });
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "文件已被移动或删除，可在下载文件夹中查找",
                    "提示",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception)
        {
            ErrorMessage = "打开文件位置失败";
        }
    }

    private void RefreshTypeOptions()
    {
        TypeOptions.Clear();
        foreach (var type in _config.TypeOptions) TypeOptions.Add(type);
    }

    private void RefreshRecentDownloads()
    {
        RecentDownloads.Clear();
        foreach (var entry in _config.RecentDownloads) RecentDownloads.Add(entry);
    }

    private static string FriendlyError(DownloadResult result) => result.ErrorKind switch
    {
        DownloadErrorKind.Network => "网络好像不太通，检查一下网络再试",
        DownloadErrorKind.VideoUnavailable => "这个视频下载不了（可能已删除或设置了私密）",
        DownloadErrorKind.SavePathInvalid => "保存的位置打不开，请检查文件夹是否存在",
        DownloadErrorKind.Canceled => "已取消下载",
        _ => "下载引擎异常（yt-dlp 出错），可到设置页的疑难解答查看日志"
    };
}
