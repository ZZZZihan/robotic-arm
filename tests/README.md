# 测试入口与证据范围

当前交接版本的可重复测试位于 `thinkbook-deploy/payload/source/EtherCATDemo/tests/`，统一由[操作台构建脚本](../thinkbook-deploy/scripts/build-dashboard.ps1)执行：

```powershell
powershell.exe -NoProfile -File .\thinkbook-deploy\scripts\build-dashboard.ps1
```

| 检查 | 当前数量 | 环境与作用 |
| --- | --- | --- |
| `Axis2MotionTests.cs` | 20 项 | Windows x64 C# 编译器；假适配器，不加载 SDK，不连接控制卡 |
| `OperationSafetyTests.cs` | 33 项 | Windows x64 C# 编译器；假适配器，检查初始化、保险与停止确认 |
| `CartesianCoordinatesTests.cs` | 27 组 | 纯坐标变换与显式标定换算；不加载 SDK，不读取实际机械参数 |
| `UiSmoke.cs` | 26 组 | Windows WinForms 和固定版本 SDK；不打开控制卡，检查布局、选择、状态及输入行为 |

每次输出保存行为测试文本、UI 测试文本、界面截图和 `verification-result.json`。构建脚本会使用新目录，既不启动操作者 App，也不覆盖已运行的版本。新增加功能时应检查真实用户关心的行为，例如文本框逐步输入和停止后再次启动，而不只给控件属性直接赋值。

坐标检查另存为 `cartesian-results.txt`，结果 JSON 中计数为 `cartesianChecks`。

## 坐标数学独立检查

Mac/Linux/Windows 安装 .NET 8 SDK 后，在仓库根目录执行：

```sh
dotnet run --project tests/cartesian-portable/CartesianCoordinatesTests.csproj --configuration Release
```

该项目直接编译维护版的 `CartesianCoordinates.cs` 和同一份 C# 测试，固定 C# 4 语法，无外部 NuGet 包。覆盖正反变换、三维轴基、右手性、变换顺序、缺失标定及计数/毫米/角度的边界；示例全部是合成数据，不对应真实机械参数。未知标定的结果为 `null`，不会退化成零坐标。

这项检查验证纯 C# 源码在 .NET 8 上的数学行为，不能代替 Windows/.NET Framework 4.0 Client Profile 操作台构建与 UI 检查。完整 Windows 构建仍使用上面的 `build-dashboard.ps1`。坐标模块没有连接现有运动入口，不执行初始化、使能、清零或任何运动命令。

已执行结果见 [2026-10-15 坐标验证记录](../docs/verification/2026-10-15-cartesian-coordinates.md)。PR 中的 `Windows .NET Framework coordinate tests` 只运行同一套数学测试，不能替代完整操作台构建。

## 记录要求

至少记录源码版本、环境、输入、预期/实际结果、命令与时间。区分四类证据：

1. 纯逻辑/假适配器检查；
2. Windows 程序构建与离线界面检查；
3. 真实控制卡只读状态；
4. 操作者参与的实机动作与机构验收。

前一类通过不能替代后一类。SDK返回0、编码器反馈变化或规划到位，不等于负载、抱闸与机构已经完成物理动作。硬件联调需另行确认现场条件和授权。

## 本机另有的历史原型

本机可能保留 `src/robot_arm_mvp` 以及 `tests/unit`、`tests/simulation`、`tests/fixtures` 等 Python 原型目录；它们未在本次交接提交中自动纳入。那些合成夹具的 `confirmed` 仅指相对于夹具定义固定，不表示真实机械臂已标定。不要在仅克隆本仓库时照旧说明期待这些目录完整存在。
