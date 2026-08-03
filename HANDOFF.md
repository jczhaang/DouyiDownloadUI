# 交接文档（2026-08-03）

> 给下一个 Codex 会话：先读 `AGENTS.md`（项目记忆，Codex 会自动加载），再读本文件。

## 当前状态

- 项目：DouyiDownloadUI v1.0.1 已发布，完整流程走完（需求 → 设计 → 计划 → 实现 TDD → 评审 → 发布 → 打包）
- 仓库：公开 https://github.com/jczhaang/DouyiDownloadUI
- 分支：`main`（已含全部代码）；`feat/douyi-download-ui` 已通过 PR #1 合并并删除
- Release：v1.0.1（DouyiDownloadUI-1.0.1-setup.exe + sha256）
- 验证：68 个自动化测试全绿；手动测试清单真机通过（含安装/卸载）
- 引擎：抖音分享页解析引擎为主（无需登录/Cookie），yt-dlp 兜底；决策见 `AGENTS.md` 决策记录 2026-08-03

## 后续可能的维护方向（候选，非待办）

- 抖音改版导致分享页解析失败时：更新 `DouyinShareParser` 正则，或依赖 yt-dlp 兜底
- 引擎版本升级：改 `tools/engine-version.json`（yt-dlp 2026.07.04）并替换工具二进制
- v2 候选（设计文档第 11 节）：UI 自动化测试（FlaUI）、代码签名、自动静默更新
- 发新版本流程：改 `Directory.Build.props` 版本号 → 更新 `CHANGELOG.md` → 打标签推送（CI 自动出包）

## 环境注意事项（本机实测）

- 沙箱把 `.git` 设为只读：`git add/commit/push` 都要 `require_escalated`。
- dotnet 包缓存此前在 worktree 的 `.superpowers` 下，worktree 已清理；新会话首次 `dotnet build/test` 会重新还原（需要联网 → 升级权限）。
- GitHub 推送间歇性 TLS 失败：用 `git -c http.version=HTTP/1.1 push ...` 并自动重试。
- gh CLI 已登录（keyring，用户 jczhaang）。
- 子代理通道不可用：deepseek-v4-pro 要 2026-08 初之后才开放；flash 子代理会"只问用户不干活"→ 用内联执行（executing-plans）。

## 关键路径

- 主工作区：`C:\Users\Zhang\workspace\DouyiDownloadUI`（main）
- 设计规格：`docs/superpowers/specs/2026-08-02-douyi-download-ui-design.md`
- 实施计划：`docs/superpowers/plans/2026-08-02-douyi-download-ui.md`
- SDD 台账：`.superpowers\sdd\2026-08-02-douyi-download-ui\progress.md`
- 安装包脚本：`installer\installer.iss`；图标：`src/DouyiDownloadUI/assets/app.ico`
- 引擎版本锁定：`tools\engine-version.json`（yt-dlp 2026.07.04）
