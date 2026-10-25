# 预编译版本

- 文件：`通知助手.exe`
- 版本：`v1.6.0`
- 适用：Windows 10 / 11（.NET Framework 4.8.1，系统自带）
- 校验：见同目录 `SHA256.txt`

## 使用

1. 双击 `通知助手.exe`；
2. 导入 TXT / 打开三字经模式；
3. 点「选择窗口」，5 秒内切到 QQ 或微信聊天窗口；
4. 设置间隔，点「开始发送」或按 F8。

数据保存在：

```text
%LOCALAPPDATA%\通知助手\
```

不会在 exe 旁边生成文件。

## 自行构建

```bat
build.cmd
```

产物在 `dist\QQNotify.exe`。
