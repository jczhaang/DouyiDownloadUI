using System.Net.Http;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI.Tests;

public class SettingsViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "dyui-svm-" + Guid.NewGuid().ToString("N"));
    private readonly SettingsService _settings;
    private readonly UpdateChecker _updateChecker;

    public SettingsViewModelTests()
    {
        Directory.CreateDirectory(_dir);
        _settings = new SettingsService(Path.Combine(_dir, "settings.json"));
        _updateChecker = new UpdateChecker(
            new HttpClient(),
            AppInfo.GitHubRepo,
            new Version(1, 0, 0));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private SettingsViewModel NewVm() => new(_settings, _updateChecker);

    [Fact]
    public void Refresh_Loads_TypeOptions_From_Settings()
    {
        var vm = NewVm();
        Assert.NotEmpty(vm.TypeOptions);
        Assert.Contains("中三", vm.TypeOptions);
    }

    [Fact]
    public void AddType_Appends_New_Type_And_Persists()
    {
        var vm = NewVm();
        vm.NewType = "华尔兹";
        vm.AddTypeCommand.Execute(null);

        Assert.Contains("华尔兹", vm.TypeOptions);
        Assert.Contains("华尔兹", _settings.Load().TypeOptions);
        Assert.Equal("", vm.NewType);
    }

    [Fact]
    public void AddType_Ignores_Empty_Input()
    {
        var vm = NewVm();
        var countBefore = vm.TypeOptions.Count;
        vm.NewType = "  ";
        vm.AddTypeCommand.Execute(null);

        Assert.Equal(countBefore, vm.TypeOptions.Count);
    }

    [Fact]
    public void AddType_Ignores_Duplicate()
    {
        var vm = NewVm();
        var countBefore = vm.TypeOptions.Count;
        vm.NewType = "中三";
        vm.AddTypeCommand.Execute(null);

        Assert.Equal(countBefore, vm.TypeOptions.Count);
    }

    [Fact]
    public void RemoveType_Removes_From_List_And_Persists()
    {
        var vm = NewVm();
        vm.RemoveTypeCommand.Execute("中三");

        Assert.DoesNotContain("中三", vm.TypeOptions);
        Assert.DoesNotContain("中三", _settings.Load().TypeOptions);
    }
}
