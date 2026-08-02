# DouyiDownloadUI 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 构建一个 Windows 桌面应用：从抖音分享文字中提取链接、确认文件名（序号/类型/标题）、下载 MP4 或 MP3，并走完测试、CI/CD、打包发布的完整流程。

**Architecture:** WPF（MVVM）界面层 + 应用服务层（纯 C#，全部可单元测试）+ 可替换下载引擎层（yt-dlp 子进程封装、ffmpeg 提取 MP3）。GitHub Actions 负责测试门禁与 Release 构建，Inno Setup 生成安装包。

**Tech Stack:** .NET 8 / C# / WPF / CommunityToolkit.Mvvm / xUnit / yt-dlp / ffmpeg / GitHub Actions / Inno Setup

## Global Constraints

- 目标平台 Windows 10 x64，WPF 项目 TargetFramework 为 `net8.0-windows`。
- yt-dlp 与 ffmpeg 随安装包内置；版本固定在 `tools/engine-version.json`，更新是主动操作。
- 文件名规则：`序号 类型 标题.ext`；标题最长 30 字符（超出追加"…"）；非法字符替换为空格；重名自动追加"（2）"递增，绝不覆盖。
- 编号默认值优先级：目标文件夹最大编号 + 1 → 上次记忆编号 + 1 → 1。
- UI 全中文；文案面向有一定电脑水平的操作者；全软件任何地方不得出现"家人"。
- 错误提示文案按规格第 6 节表格，逐字一致。
- 提交规范：Conventional Commits（`feat:` `fix:` `test:` `docs:` `ci:`）；每次提交前 `dotnet test` 全绿。
- 设计规格：`docs/superpowers/specs/2026-08-02-douyi-download-ui-design.md`；项目记忆：`AGENTS.md`。

## File Structure

```
DouyiDownloadUI.sln
Directory.Build.props                     # 全局编译属性 + 版本号
src/DouyiDownloadUI/DouyiDownloadUI.csproj
src/DouyiDownloadUI/App.xaml / App.xaml.cs # 组合根、全局异常处理
src/DouyiDownloadUI/AppInfo.cs             # 版本、GitHub 仓库常量（Task 16）
src/DouyiDownloadUI/MainWindow.xaml / .xaml.cs
src/DouyiDownloadUI/SettingsWindow.xaml / .xaml.cs
src/DouyiDownloadUI/Core/Models.cs         # 领域模型与记录
src/DouyiDownloadUI/Core/LinkParser.cs
src/DouyiDownloadUI/Core/FilenameBuilder.cs
src/DouyiDownloadUI/Core/NumberingService.cs
src/DouyiDownloadUI/Services/SettingsService.cs
src/DouyiDownloadUI/Services/ProgressParser.cs
src/DouyiDownloadUI/Services/YtDlpCommandBuilder.cs
src/DouyiDownloadUI/Services/ProcessRunner.cs    # IProcessRunner + ProcessRunner
src/DouyiDownloadUI/Services/YtDlpEngine.cs      # IDownloadEngine + YtDlpEngine
src/DouyiDownloadUI/Services/ClipboardService.cs # IClipboardService + ClipboardService
src/DouyiDownloadUI/Services/LogService.cs
src/DouyiDownloadUI/Services/FontManager.cs
src/DouyiDownloadUI/Services/UpdateChecker.cs
src/DouyiDownloadUI/ViewModels/MainViewModel.cs
src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs
tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj
tests/DouyiDownloadUI.Tests/*Tests.cs           # 每模块一个测试文件
tests/DouyiDownloadUI.Tests/Fakes.cs            # FakeEngine/FakeProcessRunner/FakeClipboard
scripts/download-engine.ps1
tools/engine-version.json                        # 引擎版本锁定（运行时生成）
installer/installer.iss
LICENSES/YT-DLP-LICENSE.txt
LICENSES/FFMPEG-LICENSE.txt
.github/workflows/ci.yml
.github/workflows/release.yml
README.md
CHANGELOG.md
docs/superpowers/plans/2026-08-02-douyi-download-ui.md
```

## 任务总览

| 任务 | 内容 | 可测试交付物 |
| --- | --- | --- |
| 1 | 解决方案骨架 | 可构建、可跑测试的 sln |
| 2 | 领域模型 Models.cs | 编译通过 + 模型测试 |
| 3 | LinkParser 链接解析 | 单元测试全绿 |
| 4 | FilenameBuilder 文件名 | 单元测试全绿 |
| 5 | NumberingService 编号规则 | 单元测试全绿 |
| 6 | SettingsService 设置 | 单元测试全绿 |
| 7 | ProgressParser 进度解析 | 单元测试全绿 |
| 8 | YtDlpCommandBuilder 命令构造 | 单元测试全绿 |
| 9 | ProcessRunner + YtDlpEngine | 假进程集成测试全绿 |
| 10 | MainViewModel 状态机 | 单元测试全绿 |
| 11 | MainWindow 界面 | 构建通过 + 手动运行 |
| 12 | SettingsWindow + FontManager | 构建通过 + 手动运行 |
| 13 | LogService + 全局异常处理 | 构建通过 + 手动验证 |
| 14 | UpdateChecker + 关于/更新页 | 单元测试全绿 |
| 15 | CI 工作流 | GitHub Actions 全绿 |
| 16 | 打包：引擎脚本、Inno Setup、Release 工作流 | 本地安装包可安装 |
| 17 | README、CHANGELOG、手动清单、v1.0.0 | Release 发布成功 |

---

### Task 1: 解决方案骨架

**Files:**
- Create: `DouyiDownloadUI.sln`
- Create: `Directory.Build.props`
- Create: `src/DouyiDownloadUI/DouyiDownloadUI.csproj`
- Create: `src/DouyiDownloadUI/App.xaml`、`App.xaml.cs`、`MainWindow.xaml`、`MainWindow.xaml.cs`（dotnet new 模板）
- Create: `tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj`
- Create: `tests/DouyiDownloadUI.Tests/UnitTest1.cs`

**Interfaces:**
- Consumes: 无。
- Produces: 可构建的解决方案与测试项目；后续任务都依赖此结构。

- [ ] **Step 1: 生成项目骨架**

```powershell
dotnet new sln -n DouyiDownloadUI
dotnet new wpf -n DouyiDownloadUI -o src/DouyiDownloadUI
dotnet new xunit -n DouyiDownloadUI.Tests -o tests/DouyiDownloadUI.Tests
dotnet sln DouyiDownloadUI.sln add src/DouyiDownloadUI/DouyiDownloadUI.csproj tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj
dotnet add tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj reference src/DouyiDownloadUI/DouyiDownloadUI.csproj
dotnet add src/DouyiDownloadUI/DouyiDownloadUI.csproj package CommunityToolkit.Mvvm
```

- [ ] **Step 2: 固定编译属性与版本号**

Create `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```

修改 `src/DouyiDownloadUI/DouyiDownloadUI.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <AssemblyName>DouyiDownloadUI</AssemblyName>
    <RootNamespace>DouyiDownloadUI</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
  </ItemGroup>
</Project>
```

修改 `tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\DouyiDownloadUI\DouyiDownloadUI.csproj" />
  </ItemGroup>
</Project>
```

保留模板自带的 `UnitTest1.cs`（内容为 `Assert.True(true)`），供流水线验证。

- [ ] **Step 3: 构建并运行测试**

Run: `dotnet build DouyiDownloadUI.sln -c Debug`
Expected: Build succeeded。

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj`
Expected: 1 个测试通过。

- [ ] **Step 4: 提交**

```bash
git add DouyiDownloadUI.sln Directory.Build.props src tests
git commit -m "chore: 初始化 .NET 解决方案与测试项目"
```

---

### Task 2: 领域模型 Models.cs

**Files:**
- Create: `src/DouyiDownloadUI/Core/Models.cs`
- Test: `tests/DouyiDownloadUI.Tests/ModelsTests.cs`

**Interfaces:**
- Consumes: 无。
- Produces: 后续所有任务使用的类型（见代码）。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/ModelsTests.cs`：

```csharp
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class ModelsTests
{
    [Fact]
    public void DownloadResult_Default_Is_Failure()
    {
        var result = new DownloadResult(false, null, DownloadErrorKind.None, null);
        Assert.False(result.Success);
        Assert.Null(result.FilePath);
    }

    [Fact]
    public void AppSettings_Has_Defaults()
    {
        var settings = new AppSettings();
        Assert.Equal("Large", settings.FontSize);
        Assert.Null(settings.LastNumber);
        Assert.Empty(settings.RecentTypes);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~ModelsTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Core/Models.cs`：

```csharp
namespace DouyiDownloadUI.Core;

public enum DownloadMode { Video, Audio }

public enum DownloadErrorKind
{
    None,
    Network,
    VideoUnavailable,
    SavePathInvalid,
    EngineError,
    Canceled
}

public sealed record VideoMetadata(string Title);

public sealed record DownloadRequest(
    string ShareUrl,
    string OutputDirectory,
    string FileNameWithoutExtension,
    DownloadMode Mode);

public sealed record DownloadResult(
    bool Success,
    string? FilePath,
    DownloadErrorKind ErrorKind,
    string? ErrorDetail);

public sealed record DownloadProgress(double Percent, string? Speed, string? Eta);

public sealed record RecentDownloadEntry(
    string FileName,
    string FilePath,
    DateTime DownloadedAt,
    bool IsAudio);

public sealed class AppSettings
{
    public string SaveFolder { get; set; } = "";
    public string FontSize { get; set; } = "Large";
    public int? LastNumber { get; set; }
    public string? LastType { get; set; }
    public List<string> RecentTypes { get; set; } = new();
    public List<RecentDownloadEntry> RecentDownloads { get; set; } = new();
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~ModelsTests"`
Expected: PASS（2 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Core/Models.cs tests/DouyiDownloadUI.Tests/ModelsTests.cs
git commit -m "feat: 添加领域模型"
```

---

### Task 3: LinkParser 链接解析

**Files:**
- Create: `src/DouyiDownloadUI/Core/LinkParser.cs`
- Test: `tests/DouyiDownloadUI.Tests/LinkParserTests.cs`

**Interfaces:**
- Consumes: 无。
- Produces: `public static string? LinkParser.ExtractUrl(string? shareText)`——从分享文字提取第一个抖音 URL，无则返回 null。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/LinkParserTests.cs`：

```csharp
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class LinkParserTests
{
    private const string ShareText =
        "2.82 02/29 O@x.Sy :7pm aaN:/ 什么是Node.js https://v.douyin.com/h94R-IulXc8/ 复制此链接，打开Dou音搜索，直接观看视频！";

    [Fact]
    public void ExtractUrl_From_ShareText_Returns_ShortUrl()
    {
        var url = LinkParser.ExtractUrl(ShareText);
        Assert.Equal("https://v.douyin.com/h94R-IulXc8/", url);
    }

    [Fact]
    public void ExtractUrl_From_LongUrl_Returns_LongUrl()
    {
        const string text = "看看这个 https://www.douyin.com/video/6914948781100338440 怎么样";
        Assert.Equal("https://www.douyin.com/video/6914948781100338440", LinkParser.ExtractUrl(text));
    }

    [Theory]
    [InlineData("今天天气不错")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractUrl_Without_Url_Returns_Null(string? text)
    {
        Assert.Null(LinkParser.ExtractUrl(text));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~LinkParserTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Core/LinkParser.cs`：

```csharp
using System.Text.RegularExpressions;

namespace DouyiDownloadUI.Core;

public static partial class LinkParser
{
    public static string? ExtractUrl(string? shareText)
    {
        if (string.IsNullOrWhiteSpace(shareText)) return null;
        var match = DouyinUrlPattern().Match(shareText);
        if (!match.Success) return null;
        return match.Value.TrimEnd('。', '，', ',', '.', '；', ';', '）', ')', '】', ']', '》', '>');
    }

    [GeneratedRegex(
        @"https?://(?:v\.douyin\.com|www\.douyin\.com)/[^\s\u4e00-\u9fff]+",
        RegexOptions.IgnoreCase)]
    private static partial Regex DouyinUrlPattern();
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~LinkParserTests"`
Expected: PASS（5 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Core/LinkParser.cs tests/DouyiDownloadUI.Tests/LinkParserTests.cs
git commit -m "feat: 实现抖音分享文字链接解析"
```

---

### Task 4: FilenameBuilder 文件名

**Files:**
- Create: `src/DouyiDownloadUI/Core/FilenameBuilder.cs`
- Test: `tests/DouyiDownloadUI.Tests/FilenameBuilderTests.cs`

**Interfaces:**
- Consumes: 无。
- Produces:
  - `public static string FilenameBuilder.Sanitize(string input)`——非法字符替换为空格、连续空格折叠、去首尾空白。
  - `public static string FilenameBuilder.Truncate(string text)`——超 `MaxTitleLength`(30) 截断并追加"…"。
  - `public static string FilenameBuilder.BuildFileName(string number, string type, string title, string extension)`——生成 `序号 类型 标题.ext`；空字段跳过；全空则"未命名"。
  - `public static string FilenameBuilder.MakeUnique(string directory, string fileNameWithoutExtension, string extension)`——存在则依次追加"（2）""（3）"。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/FilenameBuilderTests.cs`：

```csharp
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class FilenameBuilderTests
{
    [Fact]
    public void BuildFileName_With_All_Fields()
    {
        var name = FilenameBuilder.BuildFileName("007", "中三", "广场舞教学", "mp4");
        Assert.Equal("007 中三 广场舞教学.mp4", name);
    }

    [Fact]
    public void BuildFileName_Skips_Empty_Type()
    {
        var name = FilenameBuilder.BuildFileName("007", "", "广场舞教学", "MP4");
        Assert.Equal("007 广场舞教学.mp4", name);
    }

    [Fact]
    public void BuildFileName_Empty_Everything_Falls_Back()
    {
        Assert.Equal("未命名.mp3", FilenameBuilder.BuildFileName("", "", "", "mp3"));
    }

    [Fact]
    public void Sanitize_Replaces_Illegal_Chars()
    {
        Assert.Equal("a b c", FilenameBuilder.Sanitize("a/b\\c"));
        Assert.Equal("正常 标题", FilenameBuilder.Sanitize("  正常  标题  "));
    }

    [Fact]
    public void Truncate_Long_Title_Adds_Ellipsis()
    {
        var title = new string('长', 40);
        var result = FilenameBuilder.Truncate(title);
        Assert.Equal(31, result.Length);
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void MakeUnique_Adds_Number_When_Exists()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dyui-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var first = FilenameBuilder.MakeUnique(dir, "007 中三 舞", "mp4");
            Assert.Equal("007 中三 舞.mp4", first);
            File.WriteAllText(Path.Combine(dir, first), "x");
            var second = FilenameBuilder.MakeUnique(dir, "007 中三 舞", "mp4");
            Assert.Equal("007 中三 舞（2）.mp4", second);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~FilenameBuilderTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Core/FilenameBuilder.cs`：

```csharp
using System.Text.RegularExpressions;

namespace DouyiDownloadUI.Core;

public static partial class FilenameBuilder
{
    public const int MaxTitleLength = 30;
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var cleaned = new string(
            input.Select(ch => InvalidChars.Contains(ch) || char.IsControl(ch) ? ' ' : ch).ToArray());
        return WhitespacePattern().Replace(cleaned, " ").Trim();
    }

    public static string Truncate(string text)
    {
        if (text.Length <= MaxTitleLength) return text;
        return text[..MaxTitleLength] + "…";
    }

    public static string BuildFileName(string number, string type, string title, string extension)
    {
        var parts = new[] { Sanitize(number), Sanitize(type), Truncate(Sanitize(title)) }
            .Where(p => p.Length > 0);
        var name = string.Join(" ", parts);
        if (name.Length == 0) name = "未命名";
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        return name + ext.ToLowerInvariant();
    }

    public static string MakeUnique(string directory, string fileNameWithoutExtension, string extension)
    {
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        var candidate = fileNameWithoutExtension + ext;
        if (!File.Exists(Path.Combine(directory, candidate))) return candidate;
        for (var i = 2; ; i++)
        {
            candidate = $"{fileNameWithoutExtension}（{i}）{ext}";
            if (!File.Exists(Path.Combine(directory, candidate))) return candidate;
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~FilenameBuilderTests"`
Expected: PASS（6 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Core/FilenameBuilder.cs tests/DouyiDownloadUI.Tests/FilenameBuilderTests.cs
git commit -m "feat: 实现文件名生成与去重"
```

---

### Task 5: NumberingService 编号规则

**Files:**
- Create: `src/DouyiDownloadUI/Core/NumberingService.cs`
- Test: `tests/DouyiDownloadUI.Tests/NumberingServiceTests.cs`

**Interfaces:**
- Consumes: 无。
- Produces:
  - `public static int NumberingService.GetMaxNumberInFolder(string folderPath)`——扫描文件名开头数字（1-5 位），无则 0。
  - `public static int NumberingService.GetDefaultNumber(string folderPath, int? lastUsedNumber)`——三级默认值。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/NumberingServiceTests.cs`：

```csharp
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class NumberingServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dyui-num-" + Guid.NewGuid().ToString("N"));

    public NumberingServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void GetDefaultNumber_FolderMax_Plus_One()
    {
        File.WriteAllText(Path.Combine(_dir, "007 中三 舞.mp4"), "x");
        File.WriteAllText(Path.Combine(_dir, "003 平四 舞.mp4"), "x");
        File.WriteAllText(Path.Combine(_dir, "随意文件.txt"), "x");
        Assert.Equal(8, NumberingService.GetDefaultNumber(_dir, null));
    }

    [Fact]
    public void GetDefaultNumber_EmptyFolder_Uses_LastUsed_Plus_One()
    {
        Assert.Equal(11, NumberingService.GetDefaultNumber(_dir, 10));
    }

    [Fact]
    public void GetDefaultNumber_EmptyFolder_NoMemory_Returns_One()
    {
        Assert.Equal(1, NumberingService.GetDefaultNumber(_dir, null));
    }

    [Fact]
    public void GetDefaultNumber_Ignores_Long_Digit_Runs()
    {
        File.WriteAllText(Path.Combine(_dir, "123456 标题.mp4"), "x");
        Assert.Equal(1, NumberingService.GetDefaultNumber(_dir, null));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~NumberingServiceTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Core/NumberingService.cs`：

```csharp
namespace DouyiDownloadUI.Core;

public static class NumberingService
{
    public static int GetMaxNumberInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return 0;
        var max = 0;
        foreach (var file in Directory.EnumerateFiles(folderPath))
        {
            var name = Path.GetFileName(file);
            var i = 0;
            while (i < name.Length && char.IsDigit(name[i])) i++;
            if (i is 0 or > 5) continue;
            if (int.TryParse(name[..i], out var n) && n > max) max = n;
        }
        return max;
    }

    public static int GetDefaultNumber(string folderPath, int? lastUsedNumber)
    {
        var folderMax = GetMaxNumberInFolder(folderPath);
        if (folderMax > 0) return folderMax + 1;
        if (lastUsedNumber is int last && last > 0) return last + 1;
        return 1;
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~NumberingServiceTests"`
Expected: PASS（4 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Core/NumberingService.cs tests/DouyiDownloadUI.Tests/NumberingServiceTests.cs
git commit -m "feat: 实现编号默认值规则"
```

---

### Task 6: SettingsService 设置服务

**Files:**
- Create: `src/DouyiDownloadUI/Services/SettingsService.cs`
- Test: `tests/DouyiDownloadUI.Tests/SettingsServiceTests.cs`

**Interfaces:**
- Consumes: `AppSettings`（Task 2）。
- Produces:
  - `public sealed class SettingsService`，ctor `SettingsService(string settingsFilePath)`。
  - `public AppSettings Load()`——文件缺失/损坏时返回默认值（SaveFolder 默认 `文档\抖音下载`、FontSize "Large"）。
  - `public void Save(AppSettings settings)`。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/SettingsServiceTests.cs`：

```csharp
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dyui-set-" + Guid.NewGuid().ToString("N"));

    public SettingsServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string FilePath() => Path.Combine(_dir, "settings.json");

    [Fact]
    public void Load_Missing_File_Returns_Defaults()
    {
        var service = new SettingsService(FilePath());
        var settings = service.Load();
        Assert.Equal("Large", settings.FontSize);
        Assert.True(settings.SaveFolder.Contains("抖音下载"));
    }

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
            RecentTypes = new List<string> { "中三", "平四" },
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
        Assert.Equal(2, loaded.RecentTypes.Count);
        Assert.Single(loaded.RecentDownloads);
    }

    [Fact]
    public void Load_Corrupt_File_Returns_Defaults()
    {
        File.WriteAllText(FilePath(), "{ 这不是合法 JSON");
        var settings = new SettingsService(FilePath()).Load();
        Assert.Equal("Large", settings.FontSize);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~SettingsServiceTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Services/SettingsService.cs`：

```csharp
using System.Text.Json;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public sealed class SettingsService
{
    private readonly string _filePath;

    public SettingsService(string settingsFilePath) => _filePath = settingsFilePath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return CreateDefault();
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings is null) return CreateDefault();
            settings.RecentTypes ??= new List<string>();
            settings.RecentDownloads ??= new List<RecentDownloadEntry>();
            if (string.IsNullOrWhiteSpace(settings.SaveFolder)) settings.SaveFolder = DefaultSaveFolder();
            return settings;
        }
        catch (Exception)
        {
            return CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private static AppSettings CreateDefault() => new()
    {
        SaveFolder = DefaultSaveFolder(),
        FontSize = "Large",
        RecentTypes = new List<string>(),
        RecentDownloads = new List<RecentDownloadEntry>()
    };

    private static string DefaultSaveFolder() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "抖音下载");
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~SettingsServiceTests"`
Expected: PASS（3 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Services/SettingsService.cs tests/DouyiDownloadUI.Tests/SettingsServiceTests.cs
git commit -m "feat: 实现设置读写与损坏回退"
```

---

### Task 7: ProgressParser 进度解析

**Files:**
- Create: `src/DouyiDownloadUI/Services/ProgressParser.cs`
- Test: `tests/DouyiDownloadUI.Tests/ProgressParserTests.cs`

**Interfaces:**
- Consumes: `DownloadProgress`（Task 2）。
- Produces: `public static DownloadProgress? ProgressParser.ParseLine(string? line)`——解析 `download:45.6% 1.2MiB/s 00:05` 格式。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/ProgressParserTests.cs`：

```csharp
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class ProgressParserTests
{
    [Fact]
    public void ParseLine_Full_Line()
    {
        var p = ProgressParser.ParseLine("download:45.6% 1.2MiB/s 00:05");
        Assert.NotNull(p);
        Assert.Equal(45.6, p!.Percent);
        Assert.Equal("1.2MiB/s", p.Speed);
        Assert.Equal("00:05", p.Eta);
    }

    [Fact]
    public void ParseLine_No_Speed_Or_Eta()
    {
        var p = ProgressParser.ParseLine("download:100.0%");
        Assert.NotNull(p);
        Assert.Equal(100.0, p!.Percent);
        Assert.Null(p.Speed);
        Assert.Null(p.Eta);
    }

    [Theory]
    [InlineData("[download] Destination: C:\\x\\y.mp4")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseLine_NonProgress_Returns_Null(string? line)
    {
        Assert.Null(ProgressParser.ParseLine(line));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~ProgressParserTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Services/ProgressParser.cs`：

```csharp
using System.Globalization;
using System.Text.RegularExpressions;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public static partial class ProgressParser
{
    public static DownloadProgress? ParseLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        var match = ProgressLinePattern().Match(line);
        if (!match.Success) return null;
        var percent = double.Parse(match.Groups["percent"].Value, CultureInfo.InvariantCulture);
        var speed = match.Groups["speed"].Success ? match.Groups["speed"].Value : null;
        var eta = match.Groups["eta"].Success ? match.Groups["eta"].Value : null;
        return new DownloadProgress(percent, speed, eta);
    }

    [GeneratedRegex(@"^download:(\d+(?:\.\d+)?)%(?: ([^ ]+))?(?: (\S+))?$")]
    private static partial Regex ProgressLinePattern();
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~ProgressParserTests"`
Expected: PASS（5 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Services/ProgressParser.cs tests/DouyiDownloadUI.Tests/ProgressParserTests.cs
git commit -m "feat: 实现 yt-dlp 进度行解析"
```

---

### Task 8: YtDlpCommandBuilder 命令构造

**Files:**
- Create: `src/DouyiDownloadUI/Services/YtDlpCommandBuilder.cs`
- Test: `tests/DouyiDownloadUI.Tests/YtDlpCommandBuilderTests.cs`

**Interfaces:**
- Consumes: `DownloadRequest`、`DownloadMode`（Task 2）。
- Produces: `public static string[] YtDlpCommandBuilder.BuildArguments(DownloadRequest request, string ffmpegLocation)`。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/YtDlpCommandBuilderTests.cs`：

```csharp
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class YtDlpCommandBuilderTests
{
    private static DownloadRequest Request(DownloadMode mode) => new(
        "https://v.douyin.com/h94R-IulXc8/",
        @"D:\videos",
        "007 中三 舞",
        mode);

    [Fact]
    public void Video_Mode_Contains_Common_Args()
    {
        var args = YtDlpCommandBuilder.BuildArguments(Request(DownloadMode.Video), @"D:\tools");
        Assert.Contains("--no-playlist", args);
        Assert.Contains("--no-overwrites", args);
        Assert.Contains("--newline", args);
        Assert.Contains("--progress-template", args);
        Assert.Contains(@"D:\tools", args);
        Assert.Contains(@"D:\videos\007 中三 舞.%(ext)s", args);
        Assert.Equal("https://v.douyin.com/h94R-IulXc8/", args[^1]);
        Assert.DoesNotContain("--extract-audio", args);
    }

    [Fact]
    public void Audio_Mode_Contains_Extract_Audio()
    {
        var args = YtDlpCommandBuilder.BuildArguments(Request(DownloadMode.Audio), @"D:\tools");
        Assert.Contains("--extract-audio", args);
        Assert.Contains("--audio-format", args);
        Assert.Contains("mp3", args);
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~YtDlpCommandBuilderTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Services/YtDlpCommandBuilder.cs`：

```csharp
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public static class YtDlpCommandBuilder
{
    public static string[] BuildArguments(DownloadRequest request, string ffmpegLocation)
    {
        var args = new List<string>
        {
            "--no-playlist",
            "--no-overwrites",
            "--newline",
            "--progress-template",
            "download:%(progress._percent_str)s %(progress.speed)s %(progress.eta)s",
            "--output",
            Path.Combine(request.OutputDirectory, request.FileNameWithoutExtension + ".%(ext)s"),
            "--ffmpeg-location",
            ffmpegLocation
        };
        if (request.Mode == DownloadMode.Audio)
        {
            args.Add("--extract-audio");
            args.Add("--audio-format");
            args.Add("mp3");
        }
        args.Add(request.ShareUrl);
        return args.ToArray();
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~YtDlpCommandBuilderTests"`
Expected: PASS（2 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Services/YtDlpCommandBuilder.cs tests/DouyiDownloadUI.Tests/YtDlpCommandBuilderTests.cs
git commit -m "feat: 实现 yt-dlp 命令构造"
```

---

### Task 9: ProcessRunner + YtDlpEngine 下载引擎

**Files:**
- Create: `src/DouyiDownloadUI/Services/ProcessRunner.cs`
- Create: `src/DouyiDownloadUI/Services/YtDlpEngine.cs`
- Test: `tests/DouyiDownloadUI.Tests/Fakes.cs`
- Test: `tests/DouyiDownloadUI.Tests/YtDlpEngineTests.cs`

**Interfaces:**
- Consumes: `DownloadRequest/DownloadResult/DownloadProgress/VideoMetadata/DownloadErrorKind/DownloadMode`（Task 2）、`FilenameBuilder`（Task 4）、`ProgressParser`（Task 7）、`YtDlpCommandBuilder`（Task 8）。
- Produces:
  - `public interface IProcessRunner { Task<int> RunAsync(ProcessStartInfo startInfo, Action<string>? onStdoutLine, Action<string>? onStderrLine, CancellationToken ct); }`
  - `public sealed class ProcessRunner : IProcessRunner`（生产实现）。
  - `public interface IDownloadEngine { Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct); Task<DownloadResult> DownloadAsync(DownloadRequest request, IProgress<DownloadProgress>? progress, CancellationToken ct); }`
  - `public sealed class YtDlpEngine : IDownloadEngine`，ctor `YtDlpEngine(string ytDlpPath, string ffmpegLocation, IProcessRunner? processRunner = null)`。

- [ ] **Step 1: 写失败测试（先写 Fakes）**

Create `tests/DouyiDownloadUI.Tests/Fakes.cs`：

```csharp
using System.Diagnostics;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

internal sealed class FakeProcessRunner : IProcessRunner
{
    public int ExitCode { get; set; }
    public string? StderrLine { get; set; }
    public bool CancelOnRun { get; set; }
    public List<string> StdoutLines { get; } = new();
    public ProcessStartInfo? LastStartInfo { get; private set; }

    public async Task<int> RunAsync(
        ProcessStartInfo startInfo,
        Action<string>? onStdoutLine,
        Action<string>? onStderrLine,
        CancellationToken ct)
    {
        LastStartInfo = startInfo;
        if (CancelOnRun)
        {
            await Task.Delay(50, ct);
            throw new OperationCanceledException(ct);
        }
        if (startInfo.ArgumentList.Contains("--print"))
        {
            onStdoutLine?.Invoke("测试视频标题");
        }
        else
        {
            var args = startInfo.ArgumentList.ToList();
            var outputIndex = args.IndexOf("--output");
            if (outputIndex >= 0 && outputIndex + 1 < args.Count)
            {
                var template = args[outputIndex + 1].Replace("%(ext)s", "mp4");
                Directory.CreateDirectory(Path.GetDirectoryName(template)!);
                File.WriteAllText(template, "fake");
            }
            foreach (var line in StdoutLines) onStdoutLine?.Invoke(line);
        }
        onStderrLine?.Invoke(StderrLine!);
        return ExitCode;
    }
}

internal sealed class FakeClipboard : IClipboardService
{
    public string? Text { get; set; }
    public string? GetText() => Text;
}

internal sealed class FakeEngine : IDownloadEngine
{
    public VideoMetadata? Metadata { get; set; }
    public DownloadResult DownloadResult { get; set; } =
        new(true, null, DownloadErrorKind.None, null);
    public DownloadRequest? LastRequest { get; private set; }

    public Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct)
        => Task.FromResult(Metadata);

    public Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult(DownloadResult);
    }
}
```

- [ ] **Step 2: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/YtDlpEngineTests.cs`：

```csharp
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class YtDlpEngineTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dyui-engine-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessRunner _runner = new();

    public YtDlpEngineTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private YtDlpEngine NewEngine() => new("yt-dlp.exe", Path.Combine(_dir, "ffmpeg"), _runner);

    private DownloadRequest Request(string name = "001 中三 舞") =>
        new("https://v.douyin.com/abc/", _dir, name, DownloadMode.Video);

    [Fact]
    public async Task GetMetadataAsync_Returns_Title()
    {
        var engine = NewEngine();
        var meta = await engine.GetMetadataAsync("https://v.douyin.com/abc/", CancellationToken.None);
        Assert.Equal("测试视频标题", meta!.Title);
    }

    [Fact]
    public async Task GetMetadataAsync_NonZero_Exit_Returns_Null()
    {
        _runner.ExitCode = 1;
        var engine = NewEngine();
        Assert.Null(await engine.GetMetadataAsync("https://v.douyin.com/abc/", CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_Reports_Progress_And_Succeeds()
    {
        _runner.StdoutLines.Add("download:45.6% 1.2MiB/s 00:05");
        var reported = new List<DownloadProgress>();
        var engine = NewEngine();
        var result = await engine.DownloadAsync(
            Request(), new Progress<DownloadProgress>(reported.Add), CancellationToken.None);
        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.Equal("001 中三 舞.mp4", Path.GetFileName(result.FilePath!));
        Assert.Single(reported);
        Assert.Equal(45.6, reported[0].Percent);
    }

    [Fact]
    public async Task DownloadAsync_Missing_Directory_Returns_SavePathInvalid()
    {
        var engine = NewEngine();
        var result = await engine.DownloadAsync(
            new DownloadRequest("u", Path.Combine(_dir, "no-such"), "001 舞", DownloadMode.Video),
            null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(DownloadErrorKind.SavePathInvalid, result.ErrorKind);
    }

    [Fact]
    public async Task DownloadAsync_Stderr_Unavailable_Maps_To_VideoUnavailable()
    {
        _runner.ExitCode = 1;
        _runner.StderrLine = "ERROR: Video unavailable";
        var result = await NewEngine().DownloadAsync(Request(), null, CancellationToken.None);
        Assert.Equal(DownloadErrorKind.VideoUnavailable, result.ErrorKind);
    }

    [Fact]
    public async Task DownloadAsync_Existing_File_Gets_Unique_Name()
    {
        File.WriteAllText(Path.Combine(_dir, "001 中三 舞.mp4"), "old");
        var engine = NewEngine();
        var result = await engine.DownloadAsync(Request(), null, CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("001 中三 舞（2）.mp4", Path.GetFileName(result.FilePath!));
    }

    [Fact]
    public async Task DownloadAsync_Cancel_Cleans_Partial_And_Returns_Canceled()
    {
        _runner.CancelOnRun = true;
        File.WriteAllText(Path.Combine(_dir, "001 中三 舞.mp4.part"), "partial");
        var result = await NewEngine().DownloadAsync(Request(), null, CancellationToken.None);
        Assert.Equal(DownloadErrorKind.Canceled, result.ErrorKind);
        Assert.False(File.Exists(Path.Combine(_dir, "001 中三 舞.mp4.part")));
    }
}
```

说明：`CancelOnRun` 的 `Task.Delay(50, ct)` 模拟取消抛异常；真实 `ProcessRunner` 中取消表现为读取流抛 `OperationCanceledException`，引擎以相同方式处理。

- [ ] **Step 3: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~YtDlpEngineTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 4: 最小实现**

Create `src/DouyiDownloadUI/Services/ProcessRunner.cs`：

```csharp
using System.Diagnostics;

namespace DouyiDownloadUI.Services;

public interface IProcessRunner
{
    Task<int> RunAsync(
        ProcessStartInfo startInfo,
        Action<string>? onStdoutLine,
        Action<string>? onStderrLine,
        CancellationToken ct);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(
        ProcessStartInfo startInfo,
        Action<string>? onStdoutLine,
        Action<string>? onStderrLine,
        CancellationToken ct)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("进程启动失败");
        var stdoutTask = ReadLinesAsync(process.StandardOutput, onStdoutLine, ct);
        var stderrTask = ReadLinesAsync(process.StandardError, onStderrLine, ct);
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        Action<string>? onLine,
        CancellationToken ct)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            onLine?.Invoke(line);
        }
    }
}
```

Create `src/DouyiDownloadUI/Services/YtDlpEngine.cs`：

```csharp
using System.Diagnostics;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public interface IDownloadEngine
{
    Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct);
    Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct);
}

public sealed class YtDlpEngine : IDownloadEngine
{
    private readonly string _ytDlpPath;
    private readonly string _ffmpegLocation;
    private readonly IProcessRunner _processRunner;

    public YtDlpEngine(string ytDlpPath, string ffmpegLocation, IProcessRunner? processRunner = null)
    {
        _ytDlpPath = ytDlpPath;
        _ffmpegLocation = ffmpegLocation;
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct)
    {
        var start = CreateStartInfo(
            "--no-playlist", "--skip-download", "--print", "title", shareUrl);
        string? title = null;
        var exitCode = await _processRunner.RunAsync(
            start,
            line => title ??= string.IsNullOrWhiteSpace(line) ? null : line,
            null,
            ct);
        return exitCode == 0 && !string.IsNullOrWhiteSpace(title)
            ? new VideoMetadata(title)
            : null;
    }

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        if (!Directory.Exists(request.OutputDirectory))
        {
            return new DownloadResult(
                false, null, DownloadErrorKind.SavePathInvalid, request.OutputDirectory);
        }

        var ext = request.Mode == DownloadMode.Audio ? "mp3" : "mp4";
        var unique = FilenameBuilder.MakeUnique(
            request.OutputDirectory, request.FileNameWithoutExtension, ext);
        var safeRequest = request with
        {
            FileNameWithoutExtension = Path.GetFileNameWithoutExtension(unique)
        };

        var start = CreateStartInfo(
            YtDlpCommandBuilder.BuildArguments(safeRequest, _ffmpegLocation));
        string? stderrTail = null;
        int exitCode;
        try
        {
            exitCode = await _processRunner.RunAsync(
                start,
                line =>
                {
                    var p = ProgressParser.ParseLine(line);
                    if (p is not null) progress?.Report(p);
                },
                line => stderrTail = line,
                ct);
        }
        catch (OperationCanceledException)
        {
            CleanupPartial(safeRequest);
            return new DownloadResult(false, null, DownloadErrorKind.Canceled, "已取消");
        }

        if (exitCode != 0)
        {
            return MapError(stderrTail ?? $"退出码 {exitCode}");
        }

        var file = FindOutputFile(safeRequest);
        return file is null
            ? new DownloadResult(false, null, DownloadErrorKind.EngineError, "未找到输出文件")
            : new DownloadResult(true, file, DownloadErrorKind.None, null);
    }

    private ProcessStartInfo CreateStartInfo(params string[] arguments)
    {
        var start = new ProcessStartInfo { FileName = _ytDlpPath };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return start;
    }

    private static DownloadResult MapError(string stderr)
    {
        if (stderr.Contains("Video unavailable", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("Private video", StringComparison.OrdinalIgnoreCase))
        {
            return new DownloadResult(false, null, DownloadErrorKind.VideoUnavailable, stderr);
        }
        if (stderr.Contains("Unable to download webpage", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("Connection", StringComparison.OrdinalIgnoreCase))
        {
            return new DownloadResult(false, null, DownloadErrorKind.Network, stderr);
        }
        return new DownloadResult(false, null, DownloadErrorKind.EngineError, stderr);
    }

    private static void CleanupPartial(DownloadRequest request)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(
                         request.OutputDirectory, request.FileNameWithoutExtension + ".*"))
            {
                if (file.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception)
        {
            // 清理失败不阻塞主流程
        }
    }

    private static string? FindOutputFile(DownloadRequest request)
    {
        foreach (var file in Directory.EnumerateFiles(request.OutputDirectory))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith(request.FileNameWithoutExtension, StringComparison.Ordinal))
            {
                return file;
            }
        }
        return null;
    }
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~YtDlpEngineTests"`
Expected: PASS（7 个测试）。

- [ ] **Step 6: 提交**

```bash
git add src/DouyiDownloadUI/Services/ProcessRunner.cs src/DouyiDownloadUI/Services/YtDlpEngine.cs tests/DouyiDownloadUI.Tests/Fakes.cs tests/DouyiDownloadUI.Tests/YtDlpEngineTests.cs
git commit -m "feat: 实现 yt-dlp 引擎封装与进程运行器"
```

---

### Task 10: MainViewModel 状态机

**Files:**
- Create: `src/DouyiDownloadUI/Services/ClipboardService.cs`
- Create: `src/DouyiDownloadUI/ViewModels/MainViewModel.cs`
- Test: `tests/DouyiDownloadUI.Tests/MainViewModelTests.cs`

**Interfaces:**
- Consumes: `LinkParser`（Task 3）、`FilenameBuilder`（Task 4）、`NumberingService`（Task 5）、`SettingsService`（Task 6）、`IDownloadEngine`（Task 9）、`Fakes.FakeEngine/FakeClipboard`（Task 9）。
- Produces:
  - `public interface IClipboardService { string? GetText(); }` + `ClipboardService`（WPF 实现）。
  - `public sealed partial class MainViewModel : ObservableObject`，ctor `MainViewModel(IDownloadEngine engine, SettingsService settings, IClipboardService clipboard)`。
  - 公开成员：`CurrentStep`（enum Paste/Name/Done）、`ShareText`、`LinkRecognized`、`StatusMessage`、`ErrorMessage`、`Number`、`Type`、`Title`、`FileName`、`ProgressPercent`、`ProgressText`、`IsBusy`、`ResultFileName`、`RecentTypes`、`RecentDownloads`、`Settings`、`CanNext`、`StepIsPaste/StepIsName/StepIsDone`、`NextCommand`、`DownloadVideoCommand`、`DownloadAudioCommand`、`CancelDownloadCommand`、`OpenFolderCommand`、`DownloadAnotherCommand`、`OnWindowActivated()`、`SetFileNameFromSelection(string?)`、`RefreshFromSettings()`。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/MainViewModelTests.cs`：

```csharp
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
        var file = Path.Combine(_dir, "001 中三 广场舞教学.mp4");
        _engine.DownloadResult = new DownloadResult(true, file, DownloadErrorKind.None, null);
        var vm = NewVm();
        vm.ShareText = "https://v.douyin.com/h94R-IulXc8/";
        await vm.NextCommand.ExecuteAsync(null);
        vm.Number = "007";
        vm.Type = "中三";
        vm.FileName = "广场舞教学";
        await vm.DownloadVideoCommand.ExecuteAsync(null);
        Assert.Equal(MainViewModel.Step.Done, vm.CurrentStep);
        Assert.Equal("001 中三 广场舞教学.mp4", vm.ResultFileName);
        Assert.Single(vm.RecentDownloads);
        Assert.Equal(7, _settings.Load().LastNumber);
        Assert.Equal("中三", _settings.Load().LastType);
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
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~MainViewModelTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Services/ClipboardService.cs`：

```csharp
namespace DouyiDownloadUI.Services;

public interface IClipboardService
{
    string? GetText();
}

public sealed class ClipboardService : IClipboardService
{
    public string? GetText()
    {
        try
        {
            return System.Windows.Clipboard.ContainsText()
                ? System.Windows.Clipboard.GetText()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
```

Create `src/DouyiDownloadUI/ViewModels/MainViewModel.cs`：

```csharp
using System.Collections.ObjectModel;
using System.Diagnostics;
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

    public ObservableCollection<string> RecentTypes { get; } = new();
    public ObservableCollection<RecentDownloadEntry> RecentDownloads { get; } = new();
    public SettingsService Settings { get; }

    public bool CanNext => LinkRecognized && !IsBusy;
    public bool StepIsPaste => CurrentStep == Step.Paste;
    public bool StepIsName => CurrentStep == Step.Name;
    public bool StepIsDone => CurrentStep == Step.Done;

    public MainViewModel(IDownloadEngine engine, SettingsService settings, IClipboardService clipboard)
    {
        _engine = engine;
        _settings = settings;
        _clipboard = clipboard;
        Settings = settings;
        _config = settings.Load();
        RefreshRecentTypes();
        RefreshRecentDownloads();
    }

    partial void OnShareTextChanged(string value) => LinkRecognized = LinkParser.ExtractUrl(value) is not null;
    partial void OnLinkRecognizedChanged(bool value) => OnPropertyChanged(nameof(CanNext));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(CanNext));

    partial void OnCurrentStepChanged(Step value)
    {
        OnPropertyChanged(nameof(StepIsPaste));
        OnPropertyChanged(nameof(StepIsName));
        OnPropertyChanged(nameof(StepIsDone));
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
        RefreshRecentTypes();
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
            var meta = await _engine.GetMetadataAsync(_extractedUrl, CancellationToken.None);
            if (meta is null)
            {
                ErrorMessage = "读取视频信息失败，请检查网络或链接";
                return;
            }
            Title = meta.Title;
            Number = NumberingService.GetDefaultNumber(_config.SaveFolder, _config.LastNumber)
                .ToString("D3");
            Type = _config.LastType ?? "";
            FileName = FilenameBuilder.Truncate(FilenameBuilder.Sanitize(meta.Title));
            CurrentStep = Step.Name;
        }
        catch (Exception)
        {
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
            ErrorMessage = "文件名不能为空";
            return;
        }
        if (!int.TryParse(Number, out var number) || number <= 0)
        {
            ErrorMessage = "编号必须是数字";
            return;
        }
        if (_extractedUrl is null) return;

        ErrorMessage = "";
        IsBusy = true;
        ProgressPercent = 0;
        ProgressText = "准备中…";
        _cts = new CancellationTokenSource();
        var request = new DownloadRequest(
            _extractedUrl,
            _config.SaveFolder,
            FilenameBuilder.Sanitize(FileName),
            mode);
        var progress = new Progress<DownloadProgress>(p =>
        {
            ProgressPercent = p.Percent;
            ProgressText = $"{p.Percent:0.#}%";
        });
        try
        {
            var result = await _engine.DownloadAsync(request, progress, _cts.Token);
            if (!result.Success)
            {
                ErrorMessage = FriendlyError(result);
                return;
            }
            var finalName = Path.GetFileName(result.FilePath!);
            _config.LastNumber = number;
            if (!string.IsNullOrWhiteSpace(Type)) _config.LastType = Type.Trim();
            RememberType(Type);
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

    private void RememberType(string type)
    {
        var trimmed = type.Trim();
        if (trimmed.Length == 0) return;
        if (!_config.RecentTypes.Contains(trimmed)) _config.RecentTypes.Add(trimmed);
        if (_config.RecentTypes.Count > 10) _config.RecentTypes.RemoveRange(10, _config.RecentTypes.Count - 10);
        RefreshRecentTypes();
    }

    private void RefreshRecentTypes()
    {
        RecentTypes.Clear();
        foreach (var type in _config.RecentTypes) RecentTypes.Add(type);
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
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~MainViewModelTests"`
Expected: PASS（7 个测试）。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Services/ClipboardService.cs src/DouyiDownloadUI/ViewModels/MainViewModel.cs tests/DouyiDownloadUI.Tests/MainViewModelTests.cs
git commit -m "feat: 实现主流程 ViewModel 状态机"
```

---

### Task 11: MainWindow 界面

**Files:**
- Modify: `src/DouyiDownloadUI/MainWindow.xaml`、`MainWindow.xaml.cs`
- Modify: `src/DouyiDownloadUI/App.xaml.cs`（组合根）
- Create: `src/DouyiDownloadUI/AppInfo.cs`

**Interfaces:**
- Consumes: `MainViewModel`（Task 10）、`SettingsService`、`YtDlpEngine`、`ClipboardService`。
- Produces: 可启动的主窗口（三步流程 + 最近下载列表 + 圈字交互）。

- [ ] **Step 1: 写 AppInfo 与组合根**

Create `src/DouyiDownloadUI/AppInfo.cs`：

```csharp
namespace DouyiDownloadUI;

public static class AppInfo
{
    public const string AppName = "抖音下载";
    public const string GitHubRepo = "your-username/DouyiDownloadUI"; // Task 16 创建仓库后替换
    public static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DouyiDownloadUI",
        "settings.json");

    public static string EnginePath(string fileName)
    {
        var tools = Path.Combine(AppContext.BaseDirectory, "tools", fileName);
        return File.Exists(tools) ? tools : Path.Combine(AppContext.BaseDirectory, fileName);
    }
}
```

替换 `App.xaml.cs`：

```csharp
using System.Windows;
using DouyiDownloadUI.Services;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI;

public partial class App : Application
{
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = new SettingsService(AppInfo.SettingsPath);
        var config = settings.Load();
        var engine = new YtDlpEngine(AppInfo.EnginePath("yt-dlp.exe"), AppInfo.EnginePath("ffmpeg.exe"));
        var viewModel = new MainViewModel(engine, settings, new ClipboardService());
        _window = new MainWindow(viewModel);
        FontManager.Apply(config.FontSize, _window);
        _window.Show();
    }
}
```

替换 `MainWindow.xaml.cs`：

```csharp
using System.Windows;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Window_Activated(object sender, EventArgs e) => _viewModel.OnWindowActivated();

    private void Title_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (TitleTextBox is { SelectionLength: > 0 })
        {
            _viewModel.SetFileNameFromSelection(TitleTextBox.SelectedText);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_viewModel.Settings);
        window.Owner = this;
        window.ShowDialog();
        _viewModel.RefreshFromSettings();
    }
}
```

- [ ] **Step 2: 写主窗口 XAML**

替换 `MainWindow.xaml`：

```xml
<Window x:Class="DouyiDownloadUI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="抖音下载" Height="680" Width="560"
        WindowStartupLocation="CenterScreen"
        Activated="Window_Activated">
    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
    </Window.Resources>
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <DockPanel Grid.Row="0">
            <TextBlock Text="抖音下载" FontSize="28" FontWeight="Bold"/>
            <Button Content="⚙ 设置" FontSize="16" Padding="12,6"
                    HorizontalAlignment="Right" Click="Settings_Click"/>
        </DockPanel>

        <StackPanel Grid.Row="1" Orientation="Horizontal"
                    HorizontalAlignment="Center" Margin="0,12,0,8">
            <Border Background="#4C8DFF" CornerRadius="12" Padding="10,4" Margin="0,0,4,0"
                    Visibility="{Binding StepIsPaste, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="① 粘贴链接" FontSize="13" Foreground="White"/>
            </Border>
            <Border Background="#E8ECF2" CornerRadius="12" Padding="10,4" Margin="0,0,4,0"
                    Visibility="{Binding StepIsName, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="① 粘贴链接 ✓" FontSize="13"/>
            </Border>
            <TextBlock Text="→" FontSize="14" VerticalAlignment="Center" Margin="2,0"/>
            <Border Background="#4C8DFF" CornerRadius="12" Padding="10,4" Margin="4,0"
                    Visibility="{Binding StepIsName, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="② 确认名字" FontSize="13" Foreground="White"/>
            </Border>
            <Border Background="#E8ECF2" CornerRadius="12" Padding="10,4" Margin="4,0"
                    Visibility="{Binding StepIsPaste, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="② 确认名字" FontSize="13"/>
            </Border>
            <TextBlock Text="→" FontSize="14" VerticalAlignment="Center" Margin="2,0"/>
            <Border Background="#34C759" CornerRadius="12" Padding="10,4" Margin="4,0,0,0"
                    Visibility="{Binding StepIsDone, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="③ 完成" FontSize="13" Foreground="White"/>
            </Border>
            <Border Background="#E8ECF2" CornerRadius="12" Padding="10,4" Margin="4,0,0,0"
                    Visibility="{Binding StepIsName, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="③ 完成" FontSize="13"/>
            </Border>
        </StackPanel>

        <Grid Grid.Row="2" Margin="0,4">
            <!-- 第①步 -->
            <StackPanel Visibility="{Binding StepIsPaste, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="从抖音复制后，直接粘贴到这里：" FontSize="15"/>
                <TextBox x:Name="ShareTextBox" Height="150" Margin="0,8,0,0"
                         AcceptsReturn="True" TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"
                         FontSize="14" Padding="8"
                         Text="{Binding ShareText, UpdateSourceTrigger=PropertyChanged}"/>
                <TextBlock Margin="0,8,0,0" FontSize="15" FontWeight="Bold"
                           Foreground="#34C759"
                           Text="✓ 已识别到抖音视频"
                           Visibility="{Binding LinkRecognized, Converter={StaticResource BoolToVis}}"/>
                <TextBlock Margin="0,8,0,0" FontSize="14" Foreground="#C0392B"
                           Text="{Binding ErrorMessage}"/>
                <Button Content="下一步" FontSize="22" FontWeight="Bold" Height="56"
                        Margin="0,14,0,0" Background="#4C8DFF" Foreground="White"
                        Command="{Binding NextCommand}"
                        IsEnabled="{Binding CanNext}"/>
                <TextBlock Margin="0,8,0,0" FontSize="13" Foreground="#666"
                           Text="{Binding StatusMessage}"/>
            </StackPanel>

            <!-- 第②步 -->
            <StackPanel Visibility="{Binding StepIsName, Converter={StaticResource BoolToVis}}">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="72"/>
                        <ColumnDefinition Width="100"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,6,0">
                        <TextBlock Text="编号" FontSize="14"/>
                        <TextBox Text="{Binding Number, UpdateSourceTrigger=PropertyChanged}"
                                 FontSize="16" Padding="6" TextAlignment="Center"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="0,0,6,0">
                        <TextBlock Text="类型" FontSize="14"/>
                        <TextBox Text="{Binding Type, UpdateSourceTrigger=PropertyChanged}"
                                 FontSize="15" Padding="6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="2">
                        <TextBlock Text="文件名（可直接打字修改）" FontSize="14"/>
                        <TextBox Text="{Binding FileName, UpdateSourceTrigger=PropertyChanged}"
                                 FontSize="14" Padding="6"/>
                    </StackPanel>
                </Grid>
                <TextBlock Text="原标题（用鼠标选中想要的字，会自动填到上面）"
                           FontSize="14" Margin="0,14,0,0"/>
                <TextBox x:Name="TitleTextBox" Height="110" Margin="0,6,0,0"
                         IsReadOnly="True" AcceptsReturn="True" TextWrapping="Wrap"
                         VerticalScrollBarVisibility="Auto" FontSize="12" Padding="6"
                         Text="{Binding Title, Mode=OneWay}"
                         SelectionChanged="Title_SelectionChanged"/>
                <TextBlock Text="选择要下载的内容：" FontSize="15" Margin="0,14,0,0"/>
                <Grid Margin="0,8,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Button Grid.Column="0" Content="下载视频" FontSize="20" FontWeight="Bold"
                            Height="54" Margin="0,0,6,0" Background="#4C8DFF" Foreground="White"
                            Command="{Binding DownloadVideoCommand}"/>
                    <Button Grid.Column="1" Content="下载音频" FontSize="20" FontWeight="Bold"
                            Height="54" Margin="6,0,0,0" Background="#FF9F43" Foreground="White"
                            Command="{Binding DownloadAudioCommand}"/>
                </Grid>
                <ProgressBar Height="18" Margin="0,14,0,0" Minimum="0" Maximum="100"
                             Value="{Binding ProgressPercent}"/>
                <TextBlock Margin="0,6,0,0" FontSize="13" Foreground="#666"
                           Text="{Binding ProgressText}"/>
                <TextBlock Margin="0,6,0,0" FontSize="14" Foreground="#C0392B"
                           Text="{Binding ErrorMessage}"/>
                <Button Content="取消" FontSize="13" HorizontalAlignment="Right"
                        Background="Transparent" BorderThickness="0" Foreground="#888"
                        Command="{Binding CancelDownloadCommand}"/>
            </StackPanel>

            <!-- 第③步 -->
            <StackPanel Visibility="{Binding StepIsDone, Converter={StaticResource BoolToVis}}">
                <TextBlock Text="✓" FontSize="56" Foreground="#34C759" HorizontalAlignment="Center"/>
                <TextBlock Text="下载完成！" FontSize="26" FontWeight="Bold"
                           HorizontalAlignment="Center" Margin="0,6,0,0"/>
                <TextBlock Text="{Binding ResultFileName}" FontSize="15" Foreground="#666"
                           HorizontalAlignment="Center" Margin="0,8,0,0" TextWrapping="Wrap"/>
                <Grid Margin="0,18,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Button Grid.Column="0" Content="📁 打开文件夹" FontSize="18" FontWeight="Bold"
                            Height="52" Margin="0,0,6,0" Background="#4C8DFF" Foreground="White"
                            Command="{Binding OpenFolderCommand}"/>
                    <Button Grid.Column="1" Content="再下载一个" FontSize="16"
                            Height="52" Margin="6,0,0,0" BorderBrush="#4C8DFF"
                            Foreground="#4C8DFF" Command="{Binding DownloadAnotherCommand}"/>
                </Grid>
            </StackPanel>
        </Grid>

        <StackPanel Grid.Row="3" Margin="0,10,0,0">
            <TextBlock Text="最近下载" FontSize="15" FontWeight="Bold"/>
            <ListView MaxHeight="150" Margin="0,6,0,0" ItemsSource="{Binding RecentDownloads}"
                      ScrollViewer.HorizontalScrollBarVisibility="Disabled">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <TextBlock Text="{Binding FileName}" FontSize="13" TextTrimming="CharacterEllipsis"/>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
            <Button Content="📁 打开下载文件夹" FontSize="15" Height="40" Margin="0,8,0,0"
                    HorizontalAlignment="Right" Command="{Binding OpenFolderCommand}"/>
        </StackPanel>
    </Grid>
</Window>
```

说明：`App.xaml.cs` 中引用的 `SettingsWindow` 与 `FontManager` 在 Task 12 实现；本任务结束时先临时注释这两行以保证编译，Task 12 解除注释。

- [ ] **Step 3: 构建**

Run: `dotnet build DouyiDownloadUI.sln -c Debug`
Expected: Build succeeded（若 `SettingsWindow`/`FontManager` 缺失，按 Step 2 说明临时注释 `App.xaml.cs` 与 `MainWindow.xaml.cs` 中相关行）。

- [ ] **Step 4: 手动运行验证**

Run: `dotnet run --project src/DouyiDownloadUI/DouyiDownloadUI.csproj`
手动检查：窗口打开、标题/按钮为大字号、步骤指示显示第①步。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/AppInfo.cs src/DouyiDownloadUI/App.xaml.cs src/DouyiDownloadUI/MainWindow.xaml src/DouyiDownloadUI/MainWindow.xaml.cs
git commit -m "feat: 实现主窗口三步流程界面"
```

---

### Task 12: SettingsWindow 设置页 + FontManager

**Files:**
- Create: `src/DouyiDownloadUI/Services/FontManager.cs`
- Create: `src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs`
- Create: `src/DouyiDownloadUI/SettingsWindow.xaml`、`SettingsWindow.xaml.cs`
- Modify: `src/DouyiDownloadUI/App.xaml.cs`、`MainWindow.xaml.cs`（解除 Task 11 的临时注释）

**Interfaces:**
- Consumes: `SettingsService`（Task 6）、`AppInfo`（Task 11）。
- Produces:
  - `public static class FontManager { public static void Apply(string fontSize, FrameworkElement root); }`——"Standard"→14、"Large"→18、"ExtraLarge"→22。
  - `public sealed partial class SettingsViewModel : ObservableObject`，ctor `SettingsViewModel(SettingsService settings)`；属性 `SaveFolder`、`FontSize`、`EngineVersion`、`UpdateStatus`；命令 `ChangeSaveFolderCommand`、`SetFontSizeCommand`、`CopyDiagnosticsCommand`；方法 `ApplySaveFolder(string)`、`Refresh()`。

- [ ] **Step 1: 写 FontManager 与 SettingsViewModel**

Create `src/DouyiDownloadUI/Services/FontManager.cs`：

```csharp
using System.Windows;

namespace DouyiDownloadUI.Services;

public static class FontManager
{
    public static void Apply(string fontSize, FrameworkElement root)
    {
        root.FontSize = fontSize switch
        {
            "Standard" => 14,
            "ExtraLarge" => 22,
            _ => 18
        };
    }
}
```

Create `src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs`：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;

    [ObservableProperty]
    private string _saveFolder = "";

    [ObservableProperty]
    private string _fontSize = "Large";

    [ObservableProperty]
    private string _engineVersion = "未知";

    [ObservableProperty]
    private string _updateStatus = "";

    public SettingsViewModel(SettingsService settings)
    {
        _settings = settings;
        Refresh();
    }

    public void Refresh()
    {
        var config = _settings.Load();
        SaveFolder = config.SaveFolder;
        FontSize = config.FontSize;
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
    private void CopyDiagnostics()
    {
        var text = $"DouyiDownloadUI v{typeof(App).Assembly.GetName().Version}\n" +
                   $"保存位置：{SaveFolder}\n" +
                   $"引擎版本：{EngineVersion}\n" +
                   $"日志目录：{LogService.LogDirectory}";
        System.Windows.Clipboard.SetText(text);
        UpdateStatus = "诊断信息已复制";
    }
}
```

说明：`LogService` 在 Task 13 实现；本任务构建时若缺 `LogService`，先临时把 `LogService.LogDirectory` 替换为字符串常量，Task 13 修正。

- [ ] **Step 2: 写设置窗口 XAML 与代码后置**

Create `src/DouyiDownloadUI/SettingsWindow.xaml`：

```xml
<Window x:Class="DouyiDownloadUI.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="设置" Width="480" Height="520" WindowStartupLocation="CenterOwner">
    <StackPanel Margin="20">
        <TextBlock Text="设置" FontSize="24" FontWeight="Bold"/>

        <Border Background="#FFFFFF" BorderBrush="#EEEEEE" BorderThickness="1"
                CornerRadius="8" Padding="14" Margin="0,16,0,0">
            <StackPanel>
                <TextBlock Text="📁 保存位置" FontSize="16"/>
                <Grid Margin="0,8,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="{Binding SaveFolder}" FontSize="13"
                               Foreground="#999" TextTrimming="CharacterEllipsis"
                               VerticalAlignment="Center"/>
                    <Button Grid.Column="1" Content="更改" FontSize="14" Padding="14,6"
                            Margin="10,0,0,0" Background="#4C8DFF" Foreground="White"
                            Click="ChangeFolder_Click"/>
                </Grid>
            </StackPanel>
        </Border>

        <Border Background="#FFFFFF" BorderBrush="#EEEEEE" BorderThickness="1"
                CornerRadius="8" Padding="14" Margin="0,12,0,0">
            <StackPanel>
                <TextBlock Text="🔠 字体大小" FontSize="16"/>
                <StackPanel Orientation="Horizontal" Margin="0,10,0,0">
                    <Button Content="标准" FontSize="12" Padding="18,8" Margin="0,0,10,0"
                            Command="{Binding SetFontSizeCommand}" CommandParameter="Standard"/>
                    <Button Content="大" FontSize="14" Padding="18,8" Margin="0,0,10,0"
                            Command="{Binding SetFontSizeCommand}" CommandParameter="Large"/>
                    <Button Content="特大" FontSize="16" Padding="18,8"
                            Command="{Binding SetFontSizeCommand}" CommandParameter="ExtraLarge"/>
                </StackPanel>
            </StackPanel>
        </Border>

        <Border Background="#FFFFFF" BorderBrush="#EEEEEE" BorderThickness="1"
                CornerRadius="8" Padding="14" Margin="0,12,0,0">
            <StackPanel>
                <TextBlock Text="🔧 疑难解答" FontSize="16"/>
                <TextBlock Margin="0,8,0,0" FontSize="13" Foreground="#666"
                           Text="{Binding EngineVersion, StringFormat=下载引擎版本：{0}}"/>
                <TextBlock Margin="0,4,0,0" FontSize="13" Foreground="#666"
                           Text="{Binding UpdateStatus}"/>
                <Button Content="复制诊断信息" FontSize="14" Padding="14,6"
                        Margin="0,10,0,0" HorizontalAlignment="Left"
                        Command="{Binding CopyDiagnosticsCommand}"/>
            </StackPanel>
        </Border>

        <Border Background="#FFFFFF" BorderBrush="#EEEEEE" BorderThickness="1"
                CornerRadius="8" Padding="14" Margin="0,12,0,0">
            <StackPanel>
                <TextBlock Text="ℹ️ 关于" FontSize="16"/>
                <TextBlock Margin="0,6,0,0" FontSize="12" Foreground="#999"
                           Text="版本 1.0.0 · 内置 yt-dlp（Unlicense）与 ffmpeg（GPL），许可声明见安装目录 licenses 文件夹"/>
            </StackPanel>
        </Border>
    </StackPanel>
</Window>
```

Create `src/DouyiDownloadUI/SettingsWindow.xaml.cs`：

```csharp
using System.Windows;
using Microsoft.Win32;
using DouyiDownloadUI.Services;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly SettingsService _settings;

    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _viewModel = new SettingsViewModel(settings);
        DataContext = _viewModel;
    }

    private void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择保存位置",
            InitialDirectory = _viewModel.SaveFolder
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ApplySaveFolder(dialog.FolderName);
        }
    }
}
```

注意：`OpenFolderDialog` 需要 .NET 8 的 WPF 内置类型（Microsoft.Win32.OpenFolderDialog 自 .NET 8 提供）；若 SDK 版本不支持，改用 `System.Windows.Forms.FolderBrowserDialog` 并添加 `UseWindowsForms`。

- [ ] **Step 3: 解除 Task 11 临时注释并构建**

在 `App.xaml.cs` 恢复 `FontManager.Apply(config.FontSize, _window);`，在 `MainWindow.xaml.cs` 恢复 `Settings_Click` 中的 `new SettingsWindow(_viewModel.Settings)`。

Run: `dotnet build DouyiDownloadUI.sln -c Debug`
Expected: Build succeeded。

- [ ] **Step 4: 手动验证**

Run: `dotnet run --project src/DouyiDownloadUI/DouyiDownloadUI.csproj`
手动检查：设置页可打开；改保存位置后主窗口"打开下载文件夹"指向新位置；字体三档切换立即生效。

- [ ] **Step 5: 提交**

```bash
git add src/DouyiDownloadUI/Services/FontManager.cs src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs src/DouyiDownloadUI/SettingsWindow.xaml src/DouyiDownloadUI/SettingsWindow.xaml.cs src/DouyiDownloadUI/App.xaml.cs src/DouyiDownloadUI/MainWindow.xaml.cs
git commit -m "feat: 实现设置页与字体大小切换"
```

---

### Task 13: LogService + 全局异常处理

**Files:**
- Create: `src/DouyiDownloadUI/Services/LogService.cs`
- Modify: `src/DouyiDownloadUI/App.xaml.cs`（全局异常处理）
- Modify: `src/DouyiDownloadUI/SettingsWindow.xaml.cs`（复制诊断信息恢复真实日志目录）

**Interfaces:**
- Consumes: 无。
- Produces:
  - `public static class LogService`：`LogDirectory`、`Info(string)`、`Error(string, Exception?)`、`GetLatestLogPath()`。

- [ ] **Step 1: 实现 LogService**

Create `src/DouyiDownloadUI/Services/LogService.cs`：

```csharp
namespace DouyiDownloadUI.Services;

public static class LogService
{
    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DouyiDownloadUI",
        "logs");

    private static readonly object Sync = new();

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}\n{ex}");

    public static string GetLatestLogPath()
    {
        Directory.CreateDirectory(LogDirectory);
        return Path.Combine(LogDirectory, $"douyi-{DateTime.Now:yyyy-MM-dd}.log");
    }

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = GetLatestLogPath();
            lock (Sync)
            {
                File.AppendAllText(
                    path,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
            // 日志失败不影响主流程
        }
    }
}
```

- [ ] **Step 2: 接入全局异常处理与日志清理**

修改 `App.xaml.cs`，`OnStartup` 开头加入：

```csharp
DispatcherUnhandledException += (_, args) =>
{
    LogService.Error("未处理异常", args.Exception);
    MessageBox.Show(
        "软件遇到问题，日志已保存",
        "提示",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
    args.Handled = true;
};
CleanupOldLogs();
```

在 `App` 类中加入：

```csharp
private static void CleanupOldLogs()
{
    try
    {
        Directory.CreateDirectory(LogService.LogDirectory);
        foreach (var file in Directory.EnumerateFiles(LogService.LogDirectory, "douyi-*.log"))
        {
            if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-30)) File.Delete(file);
        }
    }
    catch (Exception)
    {
    }
}
```

并在下载关键节点补日志：在 `MainViewModel.StartDownloadAsync` 的 `DownloadAsync` 调用前加 `LogService.Info($"开始下载：{request.FileNameWithoutExtension} ({mode})")`；失败分支加 `LogService.Error($"下载失败：{result.ErrorKind} {result.ErrorDetail}")`。

- [ ] **Step 3: 构建并手动验证**

Run: `dotnet build DouyiDownloadUI.sln -c Debug`
Expected: Build succeeded。

Run: `dotnet run --project src/DouyiDownloadUI/DouyiDownloadUI.csproj`
手动检查：`%LOCALAPPDATA%\DouyiDownloadUI\logs` 下生成当日日志文件，包含"开始下载"记录。

- [ ] **Step 4: 提交**

```bash
git add src/DouyiDownloadUI/Services/LogService.cs src/DouyiDownloadUI/App.xaml.cs src/DouyiDownloadUI/ViewModels/MainViewModel.cs
git commit -m "feat: 添加日志与全局异常处理"
```

---

### Task 14: UpdateChecker + 检查更新

**Files:**
- Create: `src/DouyiDownloadUI/Services/UpdateChecker.cs`
- Create: `tests/DouyiDownloadUI.Tests/UpdateCheckerTests.cs`
- Modify: `src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs`（检查更新按钮）
- Modify: `src/DouyiDownloadUI/SettingsWindow.xaml`（按钮）
- Modify: `src/DouyiDownloadUI/App.xaml.cs`（注入 UpdateChecker）

**Interfaces:**
- Consumes: `AppInfo.GitHubRepo`（Task 11）。
- Produces:
  - `public sealed class UpdateChecker`，ctor `UpdateChecker(HttpClient httpClient, string repo, Version currentVersion)`；`public async Task<Version?> GetLatestVersionAsync(CancellationToken ct)`。

- [ ] **Step 1: 写失败测试**

Create `tests/DouyiDownloadUI.Tests/UpdateCheckerTests.cs`：

```csharp
using System.Net;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class UpdateCheckerTests
{
    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        public string Json { get; set; } = "{\"tag_name\":\"v1.2.0\"}";
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(Status)
            {
                Content = new StringContent(Json)
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task GetLatestVersionAsync_Parses_Tag()
    {
        var handler = new FakeHttpHandler();
        var checker = new UpdateChecker(
            new HttpClient(handler), "user/repo", new Version(1, 0, 0));
        var version = await checker.GetLatestVersionAsync(CancellationToken.None);
        Assert.Equal(new Version(1, 2, 0), version);
    }

    [Fact]
    public async Task GetLatestVersionAsync_HttpError_Returns_Null()
    {
        var handler = new FakeHttpHandler { Status = HttpStatusCode.NotFound };
        var checker = new UpdateChecker(
            new HttpClient(handler), "user/repo", new Version(1, 0, 0));
        Assert.Null(await checker.GetLatestVersionAsync(CancellationToken.None));
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~UpdateCheckerTests"`
Expected: FAIL（类型不存在）。

- [ ] **Step 3: 最小实现**

Create `src/DouyiDownloadUI/Services/UpdateChecker.cs`：

```csharp
using System.Net.Http.Headers;
using System.Text.Json;

namespace DouyiDownloadUI.Services;

public sealed class UpdateChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _repo;
    private readonly Version _currentVersion;

    public UpdateChecker(HttpClient httpClient, string repo, Version currentVersion)
    {
        _httpClient = httpClient;
        _repo = repo;
        _currentVersion = currentVersion;
    }

    public async Task<Version?> GetLatestVersionAsync(CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.github.com/repos/{_repo}/releases/latest");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DouyiDownloadUI", "1.0"));
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            return Version.TryParse(tag?.TrimStart('v'), out var version)
                ? version
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj --filter "FullyQualifiedName~UpdateCheckerTests"`
Expected: PASS（2 个测试）。

- [ ] **Step 5: 设置页接入"检查更新"**

`SettingsViewModel` 增加 `UpdateChecker` 依赖与命令：

```csharp
private readonly UpdateChecker _updateChecker;
// ctor 增加参数 UpdateChecker updateChecker
[RelayCommand]
private async Task CheckUpdateAsync()
{
    UpdateStatus = "正在检查…";
    var latest = await _updateChecker.GetLatestVersionAsync(CancellationToken.None);
    var current = typeof(App).Assembly.GetName().Version;
    UpdateStatus = latest is null
        ? "检查失败（可能未联网）"
        : latest > current
            ? $"发现新版本 {latest}，请到 GitHub Releases 下载新安装包"
            : "已是最新版本";
}
```

`SettingsWindow.xaml` 的疑难解答区增加按钮：

```xml
<Button Content="检查更新" FontSize="14" Padding="14,6" Margin="0,10,0,0"
        HorizontalAlignment="Left" Command="{Binding CheckUpdateCommand}"/>
```

`App.xaml.cs` 创建 `SettingsViewModel` 处改为 `new SettingsViewModel(settings, new UpdateChecker(new HttpClient(), AppInfo.GitHubRepo, typeof(App).Assembly.GetName().Version))`，并在 `SettingsWindow` ctor 增加对应参数。

- [ ] **Step 6: 构建 + 测试 + 提交**

Run: `dotnet test DouyiDownloadUI.sln`
Expected: 全部通过。

```bash
git add src/DouyiDownloadUI/Services/UpdateChecker.cs tests/DouyiDownloadUI.Tests/UpdateCheckerTests.cs src/DouyiDownloadUI/ViewModels/SettingsViewModel.cs src/DouyiDownloadUI/SettingsWindow.xaml src/DouyiDownloadUI/SettingsWindow.xaml.cs src/DouyiDownloadUI/App.xaml.cs
git commit -m "feat: 实现版本检查与更新提示"
```

---

### Task 15: CI 工作流

**Files:**
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: Task 1-14 的解决方案与测试。
- Produces: 每次 push/PR 自动构建 + 测试。

- [ ] **Step 1: 创建工作流**

Create `.github/workflows/ci.yml`：

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:

jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore DouyiDownloadUI.sln
      - name: Build
        run: dotnet build DouyiDownloadUI.sln -c Release --no-restore
      - name: Test
        run: dotnet test tests/DouyiDownloadUI.Tests/DouyiDownloadUI.Tests.csproj -c Release --no-build
```

- [ ] **Step 2: 推送并验证**

推送到 GitHub（Task 17 创建仓库；若仓库已存在则直接推送）。

Expected: GitHub Actions 页面出现 CI 运行且全绿。

- [ ] **Step 3: 提交**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: 添加构建与测试工作流"
```

---

### Task 16: 打包发布：引擎脚本、Inno Setup、Release 工作流

**Files:**
- Create: `scripts/download-engine.ps1`
- Create: `tools/engine-version.json`（由脚本生成）
- Create: `LICENSES/YT-DLP-LICENSE.txt`
- Create: `LICENSES/FFMPEG-LICENSE.txt`
- Create: `installer/installer.iss`
- Create: `.github/workflows/release.yml`
- Modify: `src/DouyiDownloadUI/AppInfo.cs`（填入真实 GitHub 仓库名）

**Interfaces:**
- Consumes: Task 1-15 产物。
- Produces: 可双击安装的安装包；打 `v*` 标签自动发布 GitHub Release。

- [ ] **Step 1: 创建 GitHub 仓库并填入仓库名**

用户确认 GitHub 用户名与公开/私有后执行：

```bash
gh repo create DouyiDownloadUI --public --source . --push
```

然后把 `src/DouyiDownloadUI/AppInfo.cs` 的 `GitHubRepo` 改为实际值（如 `ZJC0123/DouyiDownloadUI`）。

- [ ] **Step 2: 写引擎下载脚本**

Create `scripts/download-engine.ps1`：

```powershell
param(
    [string]$ConfigFile = "tools/engine-version.json",
    [string]$OutDir = "tools",
    [switch]$UpdateLatest
)
$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$headers = @{ "User-Agent" = "DouyiDownloadUI-build" }

if ($UpdateLatest) {
    $ytRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest" -Headers $headers
    $tag = $ytRelease.tag_name
    $ffRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest" -Headers $headers
    $ffAsset = $ffRelease.assets | Where-Object { $_.name -like "ffmpeg-master-latest-win64-gpl.zip" } | Select-Object -First 1
    if (-not $ffAsset) { throw "未找到 ffmpeg 下载资产" }
    $config = @{
        version     = $tag
        ytDlpUrl    = "https://github.com/yt-dlp/yt-dlp/releases/download/$tag/yt-dlp.exe"
        ffmpegZipUrl = $ffAsset.browser_download_url
    } | ConvertTo-Json
    Set-Content -Path $ConfigFile -Value $config -Encoding utf8
    Write-Host "engine-version.json 已更新到 $tag"
}

$cfg = Get-Content -Raw $ConfigFile | ConvertFrom-Json
Invoke-WebRequest -Uri $cfg.ytDlpUrl -OutFile "$OutDir/yt-dlp.exe" -Headers $headers
Invoke-WebRequest -Uri $cfg.ffmpegZipUrl -OutFile "$OutDir/ffmpeg.zip" -Headers $headers
Expand-Archive -Path "$OutDir/ffmpeg.zip" -DestinationPath "$OutDir/ffmpeg-tmp" -Force
Get-ChildItem "$OutDir/ffmpeg-tmp" -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1 |
    Copy-Item -Destination "$OutDir/ffmpeg.exe" -Force
Remove-Item "$OutDir/ffmpeg.zip" -Force
Remove-Item "$OutDir/ffmpeg-tmp" -Recurse -Force
Write-Host "yt-dlp SHA256: $((Get-FileHash "$OutDir/yt-dlp.exe" -Algorithm SHA256).Hash)"
Write-Host "ffmpeg SHA256: $((Get-FileHash "$OutDir/ffmpeg.exe" -Algorithm SHA256).Hash)"
```

运行（需要联网与 GitHub API 访问）：

```powershell
./scripts/download-engine.ps1 -UpdateLatest
```

Expected: `tools/yt-dlp.exe`、`tools/ffmpeg.exe`、`tools/engine-version.json` 生成（`tools/` 已在 .gitignore）。

- [ ] **Step 3: 写许可证文件**

Create `LICENSES/YT-DLP-LICENSE.txt`：

```text
yt-dlp

This software is provided under the Unlicense:

This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or distribute
this software, either in source code form or as a compiled binary, for any
purpose, commercial or non-commercial, and by any means.

For more information, please refer to <https://unlicense.org/>
```

Create `LICENSES/FFMPEG-LICENSE.txt`：

```text
ffmpeg

本安装包内置的 ffmpeg 来自 BtbN/FFmpeg-Builds（GPL 构建），
依据 GNU General Public License（GPL）v3 分发。
完整许可证文本见 https://www.gnu.org/licenses/gpl-3.0.txt
源码获取方式：https://github.com/BtbN/FFmpeg-Builds
```

- [ ] **Step 4: 写 Inno Setup 脚本**

Create `installer/installer.iss`：

```iss
#define MyAppName "抖音下载"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ZJC0123"
#define MyAppExeName "DouyiDownloadUI.exe"

[Setup]
AppId={{6A4B9E2C-3F1D-4E8A-9C5B-7D2A0F1B3E44}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\DouyiDownloadUI
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=DouyiDownloadUI-{#MyAppVersion}-setup
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务："

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\tools\yt-dlp.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "..\tools\ffmpeg.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "..\LICENSES\*"; DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即运行"; Flags: nowait postinstall skipifsilent
```

- [ ] **Step 5: 写 Release 工作流**

Create `.github/workflows/release.yml`：

```yaml
name: Release

on:
  push:
    tags: ['v*']

jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Publish
        run: dotnet publish src/DouyiDownloadUI/DouyiDownloadUI.csproj -c Release -r win-x64 --self-contained false -o publish
      - name: Download engines
        shell: pwsh
        run: ./scripts/download-engine.ps1 -ConfigFile tools/engine-version.json -OutDir tools
      - name: Install Inno Setup
        run: choco install innosetup --no-progress -y
      - name: Compile installer
        shell: pwsh
        run: |
          $version = $env:GITHUB_REF_NAME.TrimStart('v')
          & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\installer.iss /DMyAppVersion=$version
      - name: Generate checksum
        shell: pwsh
        run: |
          $file = Get-ChildItem installer\Output\DouyiDownloadUI-*.exe | Select-Object -First 1
          Get-FileHash $file.FullName -Algorithm SHA256 | ForEach-Object {
            "$($_.Hash)  $($file.Name)" | Set-Content -Path "$($file.FullName).sha256"
          }
      - uses: softprops/action-gh-release@v2
        with:
          files: |
            installer/Output/DouyiDownloadUI-*.exe
            installer/Output/DouyiDownloadUI-*.sha256
          body_path: CHANGELOG.md
```

- [ ] **Step 6: 本地验证安装包**

本地先跑一次：

```powershell
dotnet publish src/DouyiDownloadUI/DouyiDownloadUI.csproj -c Release -r win-x64 --self-contained false -o publish
./scripts/download-engine.ps1
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\installer.iss /DMyAppVersion=1.0.0
```

Expected: `installer/Output/DouyiDownloadUI-1.0.0-setup.exe` 生成；在测试机安装后，双击应用可完成一次真实下载（手动冒烟）。

- [ ] **Step 7: 提交**

```bash
git add scripts/download-engine.ps1 tools/engine-version.json LICENSES installer .github/workflows/release.yml src/DouyiDownloadUI/AppInfo.cs
git commit -m "build: 添加引擎下载、安装包与 Release 工作流"
```

---

### Task 17: 文档、手动测试清单与 v1.0.0 发布

**Files:**
- Create: `README.md`
- Create: `CHANGELOG.md`
- Create: `docs/superpowers/specs/2026-08-02-douyi-download-ui-design.md`（引用，不改动）

**Interfaces:**
- Consumes: 全部任务产物。
- Produces: 可对外发布的 v1.0.0。

- [ ] **Step 1: 写 README**

Create `README.md`：

```markdown
# 抖音下载（DouyiDownloadUI）

Windows 桌面工具：粘贴抖音分享文字 → 确认文件名（序号/类型/标题）→ 下载视频（MP4）或音频（MP3）。

## 使用方法
1. 在抖音里点"分享 → 复制链接"
2. 打开本软件，分享文字会自动填入（也可手动粘贴）
3. 下一步 → 圈字或直接改文件名 → 点"下载视频"或"下载音频"
4. 完成后点"打开文件夹"，把文件拷贝到 U 盘

## 技术栈
.NET 8 / WPF / MVVM（CommunityToolkit.Mvvm）/ xUnit / yt-dlp / ffmpeg / GitHub Actions / Inno Setup

## 开发
```powershell
dotnet build DouyiDownloadUI.sln
dotnet test DouyiDownloadUI.sln
```

## 发布
打 `v1.0.0` 标签并推送，GitHub Actions 自动构建安装包并发布 Release。

## 许可证
本项目代码与 yt-dlp（Unlicense）、ffmpeg（GPL）的分发声明见安装目录 licenses 文件夹。
```

- [ ] **Step 2: 写 CHANGELOG**

Create `CHANGELOG.md`：

```markdown
# 更新日志

## [1.0.0] - 2026-08-02

### 新增
- 从抖音分享文字自动识别视频链接（支持手动粘贴）
- 三步向导：粘贴链接 → 确认名字 → 完成
- 文件名规则：`序号 类型 标题`，支持鼠标圈字、类型记忆、重名自动加"（2）"
- 下载视频（MP4）或只下载音频（MP3）
- 最近下载列表、打开下载文件夹、设置页（保存位置/字体大小/疑难解答）
- CI/CD：GitHub Actions 自动测试与 Release 构建
- Inno Setup 安装包，内置 yt-dlp 与 ffmpeg
```

- [ ] **Step 3: 手动测试清单执行**

在真实 Win10 机器上按清单逐项验收（本任务的人工步骤）：

1. 复制真实抖音分享文字 → 打开软件 → 自动填入 → 下一步
2. 圈字修改文件名 → 下载视频 → 完成页出现 → 打开文件夹 → 文件可播放
3. 同一编号再下载一次 → 文件名带"（2）"，原文件未被覆盖
4. 下载音频 → 生成 MP3 可播放
5. 下载中点"取消" → 无 `.part` 残留
6. 断网下载 → 显示"网络好像不太通，检查一下网络再试"
7. 粘贴无链接文字 → 显示"没有找到抖音视频，请重新复制"
8. 设置页改保存位置与字体 → 生效并记住
9. 安装包安装/卸载 → 桌面快捷方式、开始菜单、许可文件存在

每项通过后打勾；发现缺陷则回对应任务修复并补测试，重新跑 CI。

- [ ] **Step 4: 打标签发布 v1.0.0**

```bash
git add README.md CHANGELOG.md
git commit -m "docs: 添加 README 与更新日志"
git tag -a v1.0.0 -m "v1.0.0 首个正式版本"
git push origin main
git push origin v1.0.0
```

Expected: GitHub Actions Release 工作流运行成功，Releases 页出现 `v1.0.0` 与安装包。

- [ ] **Step 5: 学习复盘**

与学习者一起复盘：走一遍需求 → 设计 → 计划 → 实现（TDD）→ 评审 → 发布 → 打包全流程，由学习者用自己的话讲每个环节为什么存在；把复盘要点追加到 `AGENTS.md` 决策记录。

---

## Self-Review 记录

**规格覆盖检查：**
- FR1 链接获取 → Task 3、Task 10、Task 11
- FR2 确认文件名（圈字）→ Task 10、Task 11（Title_SelectionChanged）
- FR3 编号三级默认值 → Task 5、Task 10
- FR4 类型记忆 → Task 10（RememberType/RecentTypes）
- FR5/FR6 视频/音频 → Task 8、Task 9、Task 10
- FR7 保存位置记忆 → Task 6、Task 12
- FR8 进度/取消/清理 → Task 7、Task 9（Canceled + CleanupPartial）、Task 10
- FR9 打开文件夹/最近下载/再下载一个 → Task 10、Task 11
- FR10 设置页四分区 → Task 12、Task 13、Task 14
- FR11 重名不覆盖 → Task 4、Task 9（MakeUnique + --no-overwrites）
- 错误文案表 → Task 10（FriendlyError）与规格一致
- 日志 30 天滚动 → Task 13（CleanupOldLogs）
- CI/CD 双工作流 → Task 15、Task 16
- 语义化版本/CHANGELOG → Task 17
- Inno Setup 内置引擎与许可 → Task 16
- 学习检查点 → Task 17 Step 5

**类型一致性检查：**
- `IDownloadEngine` 在两个文件（Fakes.cs 与 YtDlpEngine.cs）签名一致。
- `DownloadErrorKind.Canceled` 在 Models、Engine、FriendlyError 三处一致。
- `FilenameBuilder.MakeUnique(directory, fileNameWithoutExtension, extension)` 在 Task 4 定义、Task 9 使用一致。
- `SettingsService(string settingsFilePath)` ctor 在 Task 6 定义、Task 10/12 使用一致。
- `MainViewModel(engine, settings, clipboard)` 与 `FakeEngine/FakeClipboard` 构造一致。
