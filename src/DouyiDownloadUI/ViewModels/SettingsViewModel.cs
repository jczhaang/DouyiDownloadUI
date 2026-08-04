using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly UpdateChecker _updateChecker;

    [ObservableProperty]
    private string _saveFolder = "";

    [ObservableProperty]
    private string _fontSize = "Large";

    [ObservableProperty]
    private string _engineVersion = "未知";

    [ObservableProperty]
    private string _updateStatus = "";

    [ObservableProperty]
    private string _newType = "";

    public ObservableCollection<string> TypeOptions { get; } = new();

    public SettingsViewModel(SettingsService settings, UpdateChecker updateChecker)
    {
        _settings = settings;
        _updateChecker = updateChecker;
        EngineVersion = AppInfo.EngineVersion;
        Refresh();
    }

    public void Refresh()
    {
        var config = _settings.Load();
        SaveFolder = config.SaveFolder;
        FontSize = config.FontSize;
        TypeOptions.Clear();
        foreach (var t in config.TypeOptions) TypeOptions.Add(t);
    }

    public void ApplySaveFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var config = _settings.Load();
        config.SaveFolder = path;
        _settings.Save(config);
        SaveFolder = path;
    }

    [RelayCommand]
    private void SetFontSize(string size)
    {
        var config = _settings.Load();
        config.FontSize = size;
        _settings.Save(config);
        FontSize = size;
    }

    [RelayCommand]
    private void AddType()
    {
        var trimmed = NewType.Trim();
        if (trimmed.Length == 0) return;
        if (TypeOptions.Contains(trimmed)) return;
        var config = _settings.Load();
        config.TypeOptions.Add(trimmed);
        _settings.Save(config);
        TypeOptions.Add(trimmed);
        NewType = "";
    }

    [RelayCommand]
    private void RemoveType(string type)
    {
        var config = _settings.Load();
        config.TypeOptions.Remove(type);
        _settings.Save(config);
        TypeOptions.Remove(type);
    }

    [RelayCommand]
    private void CopyDiagnostics()
    {
        var text = $"DouyiDownloadUI v{typeof(App).Assembly.GetName().Version}\n" +
                   $"保存位置：{SaveFolder}\n" +
                   $"引擎版本：{EngineVersion}\n" +
                   $"日志目录：{LogService.LogDirectory}";
        System.Windows.Clipboard.SetText(text);
        UpdateStatus = "诊断信息已复制";
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        UpdateStatus = "正在检查…";
        var latest = await _updateChecker.GetLatestVersionAsync(CancellationToken.None);
        UpdateStatus = latest is null
            ? "检查失败（可能未联网）"
            : latest > _updateChecker.CurrentVersion
                ? $"发现新版本 {latest}，请到 GitHub Releases 下载新安装包"
                : "已是最新版本";
    }
}
