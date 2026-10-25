# Contributing

感谢关注。这个项目主要面向 Windows 个人自用的 QQ / 微信通知自动化。

## 构建

需要 Windows 10/11 自带的 .NET Framework 4.8 编译器（`csc.exe`），不需要 Visual Studio / .NET SDK / Node：

```bat
build.cmd
```

## 提交

- 修 bug / 改进 UI / 文档修正都欢迎；
- 提交前请确认 `build.cmd` 能通过；
- 请保持“本地键鼠模拟”的实现路线，不接受用于群发广告、刷屏骚扰或规避平台风控的功能；
- 不要提交 `dist/`、`settings.ini`、`data/`、`logs/` 等构建或运行产物。
