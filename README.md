# 抖音下载（DouyiDownloadUI）

Windows 桌面工具：粘贴抖音分享文字 → 确认文件名 → 下载视频（MP4）或音频（MP3）。面向不熟悉电脑的用户设计：三步向导、大按钮、全中文，无需说明书。

## 下载与安装

- 从 [Releases 页面](https://github.com/jczhaang/DouyiDownloadUI/releases) 下载 `DouyiDownloadUI-1.0.1-setup.exe`
- 自包含安装包，目标电脑无需安装 .NET；安装后开始菜单出现"抖音下载"

## 功能

- 三步向导：粘贴链接 → 确认名字 → 完成
- 自动识别剪贴板中的抖音分享文字（整段口令即可，无需理解"链接"）
- 文件名规则：`序号 类型 标题`（编号自动接续、类型记忆、重名自动加"（2）"、绝不覆盖）
- 下载视频（MP4）或只下载音频（MP3）
- 最近下载列表（双击定位文件）、一键打开下载文件夹
- 设置页：保存位置、字体大小（标准/大/特大）、疑难解答（日志、引擎版本、检查更新）

## 使用方法

1. 在抖音里点"分享 → 复制链接"
2. 打开本软件，分享文字会自动填入（也可手动粘贴）
3. 下一步 → 圈字或修改标题 → 点"下载视频"或"下载音频"
4. 完成后点"打开文件夹"，把文件拷贝到 U 盘

> 说明：下载的视频为带抖音水印的 720p 文件（本项目不提供去水印功能）。

## 下载引擎

抖音网页接口会封锁第三方下载工具。本软件用手机浏览器标识读取分享页内嵌数据，直接获取视频标题与播放地址，无需登录或 Cookie；yt-dlp 作为兜底引擎，ffmpeg 负责 MP3 转换。

## 技术栈

.NET 8 / WPF / MVVM（CommunityToolkit.Mvvm）/ xUnit / GitHub Actions / Inno Setup

## 开发

```powershell
dotnet build DouyiDownloadUI.sln
dotnet test DouyiDownloadUI.sln
```

## 发布

打 `vX.Y.Z` 标签并推送，GitHub Actions 自动构建安装包并发布 Release（CI 以全量测试通过为门禁）。

## 许可证

本项目代码与 yt-dlp（Unlicense）、ffmpeg（GPL）的分发声明见安装目录 licenses 文件夹。
