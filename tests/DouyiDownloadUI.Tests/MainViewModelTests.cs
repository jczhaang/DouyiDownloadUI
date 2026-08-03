using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI.Tests;

public class MainViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dyui-vm-" + Guid.NewGuid().ToString("N"));
    private readonly FakeEngine _engine = new();
    private readonly FakeClipboard _clipboard = new();
    private readonly SettingsService _settings;

    public MainViewModelTests()
    {
        Directory.CreateDirectory(_dir);
        _settings = new SettingsService(Path.Combine(_dir, "settings.json"));
        var config = _settings.Load();
        config.SaveFolder = _dir;
        _settings.Save(config);
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private MainViewModel NewVm() => new(_engine, _settings, _clipboard);

    [Fact]
    public void OnWindowActivated_Fills_ShareText_When_Clipboard_Has_Link()
    {
        _clipboard.Text = "2.82 复制打开抖音 https://v.douyin.com/h94R-IulXc8/ 复制此链接";
        var vm = NewVm();
        vm.OnWindowActivated();
        Assert.True(vm.LinkRecognized);
        Assert.Equal(_clipboard.Text, vm.ShareText);
    }

    [Fact]
    public void OnWindowActivated_Ignores_Clipboard_Without_Link()
    {
        _clipboard.Text = "今天天气不错";
        var vm = NewVm();
        vm.OnWindowActivated();
        Assert.False(vm.LinkRecognized);
        Assert.Equal("", vm.ShareText);
    }

    [Fact]
    public async Task Next_Without_Link_Shows_Error()
    {
        var vm = NewVm();
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal("没有找到抖音视频，请重新复制", vm.ErrorMessage);
        Assert.Equal(MainViewModel.Step.Paste, vm.CurrentStep);
    }

    [Fact]
    public async Task Next_With_Link_Moves_To_Name_Step_With_Defaults()
    {
        _engine.Metadata = new VideoMetadata(new string('长', 40));
        var vm = NewVm();
        vm.ShareText = "看看 https://v.douyin.com/h94R-IulXc8/ 怎么样";
        await vm.NextCommand.ExecuteAsync(null);
        Assert.Equal(MainViewModel.Step.Name, vm.CurrentStep);
        Assert.Equal("001", vm.Number);
        Assert.Equal("", vm.Type);
        Assert.Equal(FilenameBuilder.Truncate(new string('长', 40)), vm.FileName);
    }

    [Fact]
    public async Task Download_Video_Goes_To_Done_And_Remembers_Number_And_Type()
    {
        _engine.Metadata = new VideoMetadata("广场舞教学");
        var expectedName = FilenameBuilder.BuildFileName("007", "中三", "广场舞教学", "mp4");
        _engine.DownloadResult = new DownloadResult(
            true, Path.Combine(_dir, expectedName), DownloadErrorKind.None, null);
        var vm = NewVm();
        vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
        await vm.NextCommand.ExecuteAsync(null);
        vm.Number = "007";
        vm.Type = "中三";
        vm.FileName = "广场舞教学";
        await vm.DownloadVideoCommand.ExecuteAsync(null);
        Assert.Equal(MainViewModel.Step.Done, vm.CurrentStep);
        Assert.Equal(expectedName, vm.ResultFileName);
        Assert.Equal(expectedName, _engine.LastRequest!.FileNameWithoutExtension + ".mp4");
        Assert.Single(vm.RecentDownloads);
        Assert.Equal(7, _settings.Load().LastNumber);
        Assert.Equal("中三", _settings.Load().LastType);
    }

    [Fact]
    public async Task Download_Audio_Uses_Mp3_In_Request_Name()
    {
        _engine.Metadata = new VideoMetadata("广场舞教学");
        var expectedName = FilenameBuilder.BuildFileName("007", "中三", "广场舞教学", "mp3");
        _engine.DownloadResult = new DownloadResult(
            true, Path.Combine(_dir, expectedName), DownloadErrorKind.None, null);
        var vm = NewVm();
        vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
        await vm.NextCommand.ExecuteAsync(null);
        vm.Number = "007";
        vm.Type = "中三";
        vm.FileName = "广场舞教学";
        await vm.DownloadAudioCommand.ExecuteAsync(null);
        Assert.Equal(MainViewModel.Step.Done, vm.CurrentStep);
        Assert.Equal(expectedName, vm.ResultFileName);
        Assert.Equal(expectedName, _engine.LastRequest!.FileNameWithoutExtension + ".mp3");
    }

    [Fact]
    public async Task Download_With_Empty_Title_Shows_Error()
    {
        _engine.Metadata = new VideoMetadata("广场舞教学");
        var vm = NewVm();
        vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
        await vm.NextCommand.ExecuteAsync(null);
        vm.FileName = "";
        await vm.DownloadVideoCommand.ExecuteAsync(null);
        Assert.Equal("标题不能为空", vm.ErrorMessage);
        Assert.Equal(MainViewModel.Step.Name, vm.CurrentStep);
    }

    [Fact]
    public async Task Download_Failure_Shows_Friendly_Message()
    {
        _engine.Metadata = new VideoMetadata("广场舞教学");
        _engine.DownloadResult = new DownloadResult(
            false, null, DownloadErrorKind.Network, "err");
        var vm = NewVm();
        vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
        await vm.NextCommand.ExecuteAsync(null);
        vm.FileName = "广场舞教学";
        await vm.DownloadVideoCommand.ExecuteAsync(null);
        Assert.Equal("网络好像不太通，检查一下网络再试", vm.ErrorMessage);
        Assert.Equal(MainViewModel.Step.Name, vm.CurrentStep);
    }

    [Fact]
    public void SetFileNameFromSelection_Updates_FileName()
    {
        var vm = NewVm();
        vm.SetFileNameFromSelection("什么是Node.js");
        Assert.Equal("什么是Node.js", vm.FileName);
    }
}
