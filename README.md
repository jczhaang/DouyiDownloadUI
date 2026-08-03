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
