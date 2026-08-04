# 类型字段选择式改造 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将命名步的类型字段从手动输入 TextBox 改为 ComboBox 选择式，选项在设置页管理，默认五个：中三、中四、平四、三步、其他。

**Architecture:** `AppSettings` 新增 `TypeOptions` 字段替换 `RecentTypes`；`MainViewModel` 用 `TypeOptions` 集合绑定 ComboBox，`Type` 绑定 `SelectedItem`；`SettingsViewModel` 新增增删类型选项的命令；设置页新增类型管理分区。

**Tech Stack:** C# / .NET 8 / WPF / CommunityToolkit.Mvvm / xUnit

## Global Constraints

- 目标用户为老年人（极简、大字、全中文、无"家人"文案）
- 文件名规则 `序号 类型 标题` 不变
- 强制 TDD——先写失败测试并确认失败，再写最小实现至转绿
- 每次提交前全量 `dotnet test` 必须全绿
- Conventional Commits，不夹带无关改动

---

### Task 1: AppSettings 新增 TypeOptions，移除 RecentTypes

**Files:**
- Modify: `src/DouyiDownloadUI/Core/Models.cs:37-45`
- Test: `tests/DouyiDownloadUI.Tests/ModelsTests.cs`

**Interfaces:**
- Produces: `AppSettings.TypeOptions`（`List<string>`），默认 `["中三", "中四", "平四", "三步", "其他"]`

- [ ] **Step 1: 写失败测试**

在 `ModelsTests.cs` 中添加：

```csharp
[Fact]
public void AppSettings_Default_TypeOptions_Has_Five_Presets()
{
    var settings = new AppSettings();
    Assert.Equal(5, settings.TypeOptions.Count);
    Assert.Equal("中三", settings.TypeOptions[0]);
    Assert.Equal("中四", settings.TypeOptions[1]);
    Assert.Equal("平四", settings.TypeOptions[2]);
    Assert.Equal("三步", settings.TypeOptions[3]);
    Assert.Equal("其他", settings.TypeOptions[4]);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "ModelsTests"`
Expected: 编译失败（`TypeOptions` 不存在）

- [ ] **Step 3: 实现——更新 AppSettings**

修改 `src/DouyiDownloadUI/Core/Models.cs` 中的 `AppSettings` 类：

```csharp
public sealed class AppSettings
{
    public static readonly List<string> DefaultTypeOptions =
        new() { "中三", "中四", "平四", "三步", "其他" };

    public string SaveFolder { get; set; } = "";
    public string FontSize { get; set; } = "Large";
    public int? LastNumber { get; set; }
    public string? LastType { get; set; }
    public List<string> TypeOptions { get; set; } = new(DefaultTypeOptions);
    public List<RecentDownloadEntry> RecentDownloads { get; set; } = new();
}
```

移除 `RecentTypes` 字段。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "ModelsTests"`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Core/Models.cs tests/DouyiDownloadUI.Tests/ModelsTests.cs
git commit -m "refactor(models): replace RecentTypes with TypeOptions in AppSettings"
```

---

### Task 2: SettingsService 适配 TypeOptions

**Files:**
- Modify: `src/DouyiDownloadUI/Services/SettingsService.cs`
- Test: `tests/DouyiDownloadUI.Tests/SettingsServiceTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `AppSettings.TypeOptions` 和 `AppSettings.DefaultTypeOptions`
- Produces: `Load()` 对老配置文件（无 `TypeOptions`）自动初始化默认值；`CreateDefault()` 设置默认值

- [ ] **Step 1: 写失败测试**

在 `SettingsServiceTests.cs` 中添加：

```csharp
[Fact]
public void Load_Missing_File_Returns_Default_TypeOptions()
{
    var service = new SettingsService(FilePath());
    var settings = service.Load();
    Assert.Equal(5, settings.TypeOptions.Count);
    Assert.Contains("中三", settings.TypeOptions);
    Assert.Contains("其他", settings.TypeOptions);
}

[Fact]
public void Load_Old_Config_Without_TypeOptions_Initializes_Defaults()
{
    File.WriteAllText(FilePath(),
        """{"SaveFolder":"D:\\v","FontSize":"Large","RecentTypes":["中三"]}""");
    var settings = new SettingsService(FilePath()).Load();
    Assert.Equal(5, settings.TypeOptions.Count);
    Assert.Equal("中三", settings.TypeOptions[0]);
}

[Fact]
public void Save_And_Load_TypeOptions_RoundTrip()
{
    var service = new SettingsService(FilePath());
    var settings = new AppSettings
    {
        SaveFolder = @"D:\videos",
        TypeOptions = new List<string> { "华尔兹", "探戈" }
    };
    service.Save(settings);
    var loaded = service.Load();
    Assert.Equal(2, loaded.TypeOptions.Count);
    Assert.Equal("华尔兹", loaded.TypeOptions[0]);
    Assert.Equal("探戈", loaded.TypeOptions[1]);
}
```

同时更新现有的 `Save_And_Load_RoundTrip` 测试，将 `RecentTypes` 替换为 `TypeOptions`：

```csharp
[Fact]
public void Save_And_Load_RoundTrip()
{
    var service = new SettingsService(FilePath());
    var settings = new AppSettings
    {
        SaveFolder = @"D:\videos",
        FontSize = "ExtraLarge",
        LastNumber = 42,
        LastType = "中三",
        TypeOptions = new List<string> { "中三", "平四" },
        RecentDownloads = new List<RecentDownloadEntry>
        {
            new("001 中三 舞.mp4", @"D:\videos\001 中三 舞.mp4", DateTime.Now, false)
        }
    };
    service.Save(settings);
    var loaded = service.Load();
    Assert.Equal(@"D:\videos", loaded.SaveFolder);
    Assert.Equal("ExtraLarge", loaded.FontSize);
    Assert.Equal(42, loaded.LastNumber);
    Assert.Equal("中三", loaded.LastType);
    Assert.Equal(2, loaded.TypeOptions.Count);
    Assert.Single(loaded.RecentDownloads);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "SettingsServiceTests"`
Expected: FAIL（`TypeOptions` 相关逻辑未实现）

- [ ] **Step 3: 实现——更新 SettingsService**

修改 `src/DouyiDownloadUI/Services/SettingsService.cs`：

`Load()` 方法中，将 `settings.RecentTypes ??= new List<string>();` 替换为：

```csharp
if (settings.TypeOptions is null || settings.TypeOptions.Count == 0)
{
    settings.TypeOptions = new List<string>(AppSettings.DefaultTypeOptions);
}
```

`CreateDefault()` 方法中，将 `RecentTypes = new List<string>()` 替换为：

```csharp
TypeOptions = new List<string>(AppSettings.DefaultTypeOptions)
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "SettingsServiceTests"`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Services/SettingsService.cs tests/DouyiDownloadUI.Tests/SettingsServiceTests.cs
git commit -m "refactor(settings): adapt SettingsService for TypeOptions"
```

---

### Task 3: MainViewModel 用 TypeOptions 替换 RecentTypes

**Files:**
- Modify: `src/DouyiDownloadUI/ViewModels/MainViewModel.cs`
- Test: `tests/DouyiDownloadUI.Tests/MainViewModelTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `AppSettings.TypeOptions`；Task 2 的 `SettingsService` 兼容性
- Produces: `MainViewModel.TypeOptions`（`ObservableCollection<string>`）；`NextAsync` 中默认选中 `LastType` 或第一项；移除 `RememberType`

- [ ] **Step 1: 写失败测试**

在 `MainViewModelTests.cs` 中添加：

```csharp
[Fact]
public async Task Next_With_Link_Populates_TypeOptions()
{
    _engine.Metadata = new VideoMetadata("广场舞教学");
    var vm = NewVm();
    vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
    await vm.NextCommand.ExecuteAsync(null);

    Assert.NotEmpty(vm.TypeOptions);
    Assert.Contains("中三", vm.TypeOptions);
    Assert.Contains("其他", vm.TypeOptions);
}

[Fact]
public async Task Next_Defaults_Type_To_LastType_When_Available()
{
    _engine.Metadata = new VideoMetadata("广场舞教学");
    var config = _settings.Load();
    config.LastType = "平四";
    _settings.Save(config);
    var vm = NewVm();
    vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
    await vm.NextCommand.ExecuteAsync(null);

    Assert.Equal("平四", vm.Type);
}

[Fact]
public async Task Next_Defaults_Type_To_First_When_LastType_Not_In_Options()
{
    _engine.Metadata = new VideoMetadata("广场舞教学");
    var config = _settings.Load();
    config.LastType = "已删除的类型";
    _settings.Save(config);
    var vm = NewVm();
    vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
    await vm.NextCommand.ExecuteAsync(null);

    Assert.Equal("中三", vm.Type);
}
```

同时更新现有测试 `Next_With_Link_Moves_To_Name_Step_With_Defaults`，将 `Assert.Equal("", vm.Type)` 改为 `Assert.Equal("中三", vm.Type)`（因为现在默认选第一项）。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "MainViewModelTests"`
Expected: 编译失败（`TypeOptions` 不存在）

- [ ] **Step 3: 实现——更新 MainViewModel**

修改 `src/DouyiDownloadUI/ViewModels/MainViewModel.cs`：

1. 将 `public ObservableCollection<string> RecentTypes { get; } = new();` 替换为：
```csharp
public ObservableCollection<string> TypeOptions { get; } = new();
```

2. 构造函数中 `RefreshRecentTypes();` 替换为 `RefreshTypeOptions();`

3. `RefreshFromSettings` 中 `RefreshRecentTypes();` 替换为 `RefreshTypeOptions();`

4. `NextAsync` 中，将 `Type = _config.LastType ?? "";` 替换为：
```csharp
Type = _config.TypeOptions.Contains(_config.LastType)
    ? _config.LastType!
    : (_config.TypeOptions.Count > 0 ? _config.TypeOptions[0] : "");
```

5. `StartDownloadAsync` 中，移除 `RememberType(Type);` 这一行（保留 `_config.LastType = Type.Trim()` 逻辑）

6. 将 `RememberType` 和 `RefreshRecentTypes` 方法替换为：
```csharp
private void RefreshTypeOptions()
{
    TypeOptions.Clear();
    foreach (var type in _config.TypeOptions) TypeOptions.Add(type);
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "MainViewModelTests"`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/ViewModels/MainViewModel.cs tests/DouyiDownloadUI.Tests/MainViewModelTests.cs
git commit -m "refactor(viewmodel): replace RecentTypes with TypeOptions in MainViewModel"
```

---

### Task 4: SettingsViewModel 新增类型管理功能

**Files:**
- Modify: `src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs`
- Test: `tests/DouyiDownloadUI.Tests/SettingsViewModelTests.cs`（新建）

**Interfaces:**
- Consumes: Task 1 的 `AppSettings.TypeOptions`
- Produces: `SettingsViewModel.TypeOptions`（`ObservableCollection<string>`）；`NewType` 属性；`AddTypeCommand`；`RemoveTypeCommand(string)`

- [ ] **Step 1: 写失败测试**

创建 `tests/DouyiDownloadUI.Tests/SettingsViewModelTests.cs`：

```csharp
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
            new System.Net.Http.HttpClient(),
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
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "SettingsViewModelTests"`
Expected: 编译失败（`TypeOptions`、`NewType`、`AddTypeCommand`、`RemoveTypeCommand` 不存在）

- [ ] **Step 3: 实现——更新 SettingsViewModel**

修改 `src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs`，在现有字段之后添加：

```csharp
[ObservableProperty]
private string _newType = "";

public ObservableCollection<string> TypeOptions { get; } = new();
```

在 `Refresh()` 方法末尾添加：
```csharp
TypeOptions.Clear();
foreach (var t in config.TypeOptions) TypeOptions.Add(t);
```

添加两个命令：
```csharp
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
```

需要在文件顶部添加 `using System.Collections.ObjectModel;`。

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "SettingsViewModelTests"`
Expected: PASS

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs tests/DouyiDownloadUI.Tests/SettingsViewModelTests.cs
git commit -m "feat(settings): add type options management to SettingsViewModel"
```

---

### Task 5: MainWindow.xaml 类型字段改为 ComboBox

**Files:**
- Modify: `src/DouyiDownloadUI/MainWindow.xaml:96-100`

- [ ] **Step 1: 修改类型字段控件**

将 `MainWindow.xaml` 中的类型 TextBox：

```xml
<TextBox Text="{Binding Type, UpdateSourceTrigger=PropertyChanged}"
         Padding="6"/>
```

替换为：

```xml
<ComboBox IsEditable="False"
          ItemsSource="{Binding TypeOptions}"
          SelectedItem="{Binding Type}"
          Padding="6"/>
```

- [ ] **Step 2: 构建确认编译通过**

Run: `dotnet build src/DouyiDownloadUI/DouyiDownloadUI.csproj`
Expected: PASS

- [ ] **Step 3: 运行全量测试**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj`
Expected: PASS

- [ ] **Step 4: 提交**

```bash
git add src/DouyiDownloadUI/MainWindow.xaml
git commit -m "feat(ui): replace type TextBox with ComboBox in main window"
```

---

### Task 6: SettingsWindow.xaml 新增类型管理分区

**Files:**
- Modify: `src/DouyiDownloadUI/SettingsWindow.xaml`
- Modify: `src/DouyiDownloadUI/SettingsWindow.xaml.cs`

- [ ] **Step 1: 在设置页 XAML 中新增类型选项分区**

在 `SettingsWindow.xaml` 中，在"字体大小"分区（第 28-41 行的 Border）之后、"疑难解答"分区之前插入：

```xml
<Border Background="#FFFFFF" BorderBrush="#EEEEEE" BorderThickness="1"
        CornerRadius="8" Padding="14" Margin="0,12,0,0">
    <StackPanel>
        <TextBlock Text="🏷 类型选项"/>
        <ItemsControl ItemsSource="{Binding TypeOptions}" Margin="0,8,0,0">
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Grid Margin="0,2,0,2">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/>
                            <ColumnDefinition Width="Auto"/>
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="{Binding}" VerticalAlignment="Center"/>
                        <Button Grid.Column="1" Content="删除" Padding="10,4"
                                Background="#C0392B" Foreground="White"
                                Command="{Binding DataContext.RemoveTypeCommand,
                                    RelativeSource={RelativeSource AncestorType=Window}}"
                                CommandParameter="{Binding}"/>
                    </Grid>
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
        <Grid Margin="0,10,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            <TextBox Grid.Column="0" Text="{Binding NewType, UpdateSourceTrigger=PropertyChanged}"
                     Padding="6" Margin="0,0,8,0"/>
            <Button Grid.Column="1" Content="添加" Padding="14,6"
                    Background="#4C8DFF" Foreground="White"
                    Command="{Binding AddTypeCommand}"/>
        </Grid>
    </StackPanel>
</Border>
```

- [ ] **Step 2: 构建确认编译通过**

Run: `dotnet build src/DouyiDownloadUI/DouyiDownloadUI.csproj`
Expected: PASS

- [ ] **Step 3: 运行全量测试**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj`
Expected: PASS

- [ ] **Step 4: 提交**

```bash
git add src/DouyiDownloadUI/SettingsWindow.xaml
git commit -m "feat(ui): add type options management section to settings page"
```

---

### Task 7: 更新设计规格与 CHANGELOG

**Files:**
- Modify: `docs/superpowers/specs/2026-08-02-douyi-download-ui-design.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: 更新设计规格第 5 节界面设计**

将命名步的描述从"两个并排大按钮（下载视频/下载音频）"之前的部分，更新类型字段描述：

```markdown
类型字段为下拉选择（ComboBox），选项在设置页管理，默认提供五个：中三、中四、平四、三步、其他。进入命名步时默认选中上次用过的类型。
```

- [ ] **Step 2: 更新设计规格第 10 节关键决策记录**

追加决策：

```markdown
- 类型字段改为下拉选择（ComboBox），选项在设置页增删管理；默认五个：中三、中四、平四、三步、其他
```

- [ ] **Step 3: 更新 CHANGELOG**

在 `CHANGELOG.md` 顶部添加：

```markdown
## [Unreleased]

### 变更

- 类型字段从手动输入改为下拉选择，选项可在设置页增删管理（默认：中三、中四、平四、三步、其他）
```

- [ ] **Step 4: 提交**

```bash
git add docs/superpowers/specs/2026-08-02-douyi-download-ui-design.md CHANGELOG.md
git commit -m "docs: update design spec and changelog for type selector"
```

---

### Task 8: 全量验证

- [ ] **Step 1: 全量测试**

Run: `dotnet test DouyiDownloadUI.sln`
Expected: 全部通过

- [ ] **Step 2: 手动冒烟测试（由用户执行）**

1. 打开设置页 → 检查"类型选项"分区显示 5 个默认选项
2. 添加一个新类型（如"华尔兹"）→ 检查列表更新
3. 删除一个类型 → 检查列表更新
4. 返回主窗口 → 粘贴链接 → 下一步 → 检查类型下拉框显示选项
5. 选择类型 → 下载 → 检查文件名包含选择的类型
6. 再下载一个 → 检查类型默认选中上次选择的
