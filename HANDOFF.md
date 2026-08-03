# 交接文档（2026-08-02）

> 给下一个 Codex 会话：先读 `AGENTS.md`（项目记忆，Codex 会自动加载），再读本文件。

## 当前状态

- 项目：DouyiDownloadUI（Windows 桌面工具：抖音分享文字 → 确认文件名 → 下载 MP4/MP3）
- 完整流程已走完：需求 → 设计 → 计划 → 实现（TDD，44 测试全绿）→ 打包 → v1.0.0 发布
- 仓库：公开 https://github.com/jczhaang/DouyiDownloadUI
- 分支：`main`（文档基线）+ `feat/douyi-download-ui`（全部实现，已推送）
- Release：v1.0.0（安装包 DouyiDownloadUI-1.0.0-setup.exe + sha256）
- 关键提交：`b1dd502` 设计规格 → `7520067` 实施计划 → `f1515bd` 最新 HEAD（feat 分支）

## 待办（新会话从这里继续）

1. **手动测试清单**（在真实 Win10 机器上，计划 Task 17 Step 3）：
   - 复制真实抖音分享文字 → 自动识别 → 下一步 → 圈字/改文件名 → 下载视频 → 打开文件夹
   - 同一编号再下载 → 文件名带"（2）"且不覆盖
   - 下载音频 → MP3 可播放；下载中取消 → 无 .part 残留
   - 断网 → 显示"网络好像不太通，检查一下网络再试"
   - 粘贴无链接文字 → "没有找到抖音视频，请重新复制"
   - 设置页改保存位置/字体 → 生效并记住；安装包安装/卸载正常
2. **分支集成**：用户尚未选择（本地合并回 main / 创建 PR / 保留分支）。按 finishing-a-development-branch 技能呈现三选项等用户决定。
3. **最终代码评审**（可选但推荐）：用 requesting-code-review 技能做全分支评审后再合并。
4. 评审/合并完成后：更新 `AGENTS.md` 状态，清理 `.superpowers` 下的 SDD 台账可留作记录。

## 环境注意事项（本机实测）

- 沙箱把 `.git` 设为只读：`git add/commit/push` 都要 `require_escalated`。
- dotnet 首次运行哨兵已建好；建议每条命令前设置：
  `$env:DOTNET_CLI_HOME="$PWD\.superpowers\dotnet-cli-home"; $env:NUGET_PACKAGES="$PWD\.superpowers\nuget-packages"; $env:NUGET_HTTP_CACHE_PATH="$PWD\.superpowers\nuget-http-cache"; $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'`
- `dotnet restore/publish` 需要联网 → 升级权限；包缓存在 `.superpowers\nuget-packages`。
- GitHub 推送间歇性 TLS 失败：用 `git -c http.version=HTTP/1.1 push ...` 并自动重试。
- gh CLI 已登录（keyring，用户 jczhaang）。
- 子代理通道不可用：deepseek-v4-pro 要 2026-08 初之后才开放；flash 子代理会"只问用户不干活"→ 用内联执行（executing-plans）。

## 关键路径

- 工作区（实现分支）：`C:\Users\Zhang\workspace\DouyiDownloadUI\.worktrees\douyi-download-ui`
- 主工作区：`C:\Users\Zhang\workspace\DouyiDownloadUI`（main）
- 设计规格：`docs/superpowers/specs/2026-08-02-douyi-download-ui-design.md`
- 实施计划：`docs/superpowers/plans/2026-08-02-douyi-download-ui.md`
- SDD 台账：`.superpowers\sdd\2026-08-02-douyi-download-ui\progress.md`
- 发布目录：`publish\`（含 tools）；安装包脚本：`installer\installer.iss`
- 引擎版本锁定：`tools\engine-version.json`（yt-dlp 2026.07.04）
