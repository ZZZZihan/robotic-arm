# 测试入口与证据范围

当前交接版本的可重复测试位于 `thinkbook-deploy/payload/source/EtherCATDemo/tests/`，统一由[操作台构建脚本](../thinkbook-deploy/scripts/build-dashboard.ps1)执行：

```powershell
powershell.exe -NoProfile -File .\thinkbook-deploy\scripts\build-dashboard.ps1
```

| 检查 | 当前数量 | 环境与作用 |
| --- | --- | --- |
| `Axis2MotionTests.cs` | 19 项 | Windows x64 C# 编译器；假适配器，不加载 SDK，不连接控制卡 |
| `UiSmoke.cs` | 15 组 | Windows WinForms 和固定版本 SDK；不打开控制卡，检查布局、选择、状态及输入行为 |

每次输出保存行为测试文本、UI 测试文本、界面截图和 `verification-result.json`。构建脚本会使用新目录，既不启动操作者 App，也不覆盖已运行的版本。新增加功能时应检查真实用户关心的行为，例如文本框逐步输入和停止后再次启动，而不只给控件属性直接赋值。

## 记录要求

至少记录源码版本、环境、输入、预期/实际结果、命令与时间。区分四类证据：

1. 纯逻辑/假适配器检查；
2. Windows 程序构建与离线界面检查；
3. 真实控制卡只读状态；
4. 操作者参与的实机动作与机构验收。

前一类通过不能替代后一类。SDK返回0、编码器反馈变化或规划到位，不等于负载、抱闸与机构已经完成物理动作。硬件联调需另行确认现场条件和授权。

## 本机另有的历史原型

本机可能保留 `src/robot_arm_mvp` 以及 `tests/unit`、`tests/simulation`、`tests/fixtures` 等 Python 原型目录；它们未在本次交接提交中自动纳入。那些合成夹具的 `confirmed` 仅指相对于夹具定义固定，不表示真实机械臂已标定。不要在仅克隆本仓库时照旧说明期待这些目录完整存在。
