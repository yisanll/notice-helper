# 通知助手 · 方案 v1.6（已实现）

> 日期：2026-10-24 ｜ 状态：v1.6 数据目录迁移完成 ｜ 产物：`通知助手.exe`（单文件约 75 KB）
>
> 修复：把 exe 放桌面后，程序不再在桌面生成 `settings.ini` / `logs` / `data` 等文件。

---

## 0. 问题原因

旧版 `AppSettings` 使用：

```csharp
BaseDir = Application.StartupPath;
SettingsFile = BaseDir\settings.ini;
LogDir = BaseDir\logs;
DataDir = BaseDir\data;
```

`Application.StartupPath` 就是 exe 所在目录。用户把 exe 移到桌面后：

- 关闭程序时写 `settings.ini` → 桌面出现 settings.ini；
- 发送时写日志 → 桌面出现 logs 文件夹；
- 换头像 → 桌面出现 data 文件夹。

---

## 1. 修复方式

### 1.1 新数据目录

```text
%LOCALAPPDATA%\通知助手\
├─ settings.ini
├─ data\avatar.png
└─ logs\yyyy-MM-dd.log
```

- 使用 `Environment.SpecialFolder.LocalApplicationData` + `"通知助手"`；
- `SettingsFile`、`LogDir`、`DataDir`、`ResolveAvatarPath` 全部指向该目录；
- `Save()` 自动 `Directory.CreateDirectory(BaseDir)`；
- exe 放桌面/任意目录，都不会在 exe 旁写文件。

### 1.2 旧数据迁移

- 首次启动时，如果用户数据目录还没有 `settings.ini`：
  - 查找 exe 同目录的旧 `settings.ini`、`data`、`logs`；
  - 优先 `File.Move` / `Directory.Move` 移到新目录；失败则复制；
  - 迁移后旧文件从 exe 目录消失（桌面变干净）；
  - 之后加载新目录中的设置。
- 如果新目录已有设置，则不再迁移，避免覆盖。

### 1.3 界面入口

- 左下角新增「打开数据目录」链接；
- 点击后确保目录存在并打开资源管理器，方便用户备份/清理。

---

## 2. 验证记录

| 项目 | 结果 |
| --- | --- |
| 从临时目录启动 exe | ✅ exe 同目录只有 exe；设置写入 `%LOCALAPPDATA%\通知助手` |
| 旧数据迁移 | ✅ exe 旁的 `settings.ini`、`data`、`logs` 被移到新目录，原目录清空 |
| 迁移后设置 | ✅ `interval=7`、`sanzijing=1` 等保留 |
| 旧日志 | ✅ 一起迁移到新目录 `logs\` |
| 打开数据目录 | ✅ 左下角入口可用 |

---

## 3. 其他功能（累计）

- 间隔 0.01~3600 秒、两位小数、点击输入；
- 低于 1 秒自动压缩等待，1 秒以上普通流程；
- QQ / 微信目标窗口；
- 三字经模式；
- 暖白 UI、动画、迷你窗、头像；
- 前台校验、失焦暂停、200 条上限、@ 拦截、日志。

---

## 4. 构建

```bat
build.cmd
:: 产物 dist\QQNotify.exe（约 75 KB），交付复制为 通知助手.exe
```

## 5. 限制

- 旧版如果已经在新目录生成过设置，迁移不会覆盖；残留旧文件需手动删除；
- 用户数据目录随 Windows 用户配置，换 Windows 账号后不会共享；
- 其他发送限制不变（前台、单窗口、无回执、风控风险）。
