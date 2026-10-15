# 2026-10-15 笛卡尔坐标基础验证

新增内容是右手坐标约定、毫米点与无量纲方向、刚体变换和显式单轴标定换算。未接入控制器反馈、操作台实际 XYZ 显示或运动入口；未假定机械臂结构、臂长、传动比或零位。

## 已实际执行

- 在 macOS arm64 使用微软官方 SHA512 校验后的 .NET SDK 8.0.426 / Runtime 8.0.32，以 C# 4 语法编译维护版源码与同一份测试，27 组检查全部通过。
- 检查覆盖纯平移、正负/多圈角度、通用三维旋转、正反换算、非交换组合、坐标系名称、右手正交轴基、非有限数、溢出，以及每轴显式比例、零点、符号、导程、Int32 计数差和未知值清空。
- 主 Agent 在集成后的仓库再次运行独立测试入口。完整输出见 [cartesian-results.txt](2026-10-15-cartesian-coordinates/cartesian-results.txt)，源码与测试 SHA256、环境及结果见 [verification.json](2026-10-15-cartesian-coordinates/verification.json)。
- 两个子 Agent 分别核对机械证据/文档/构建集成，以及编写测试/独立复核核心算法；未发现阻塞问题。
- 校验待测 JSON、SVG 与工程 XML；检查文档相对链接与差异格式。实际标定字段保持未知，示意图完成渲染检查。

复现纯数学检查：

```sh
dotnet run --project tests/cartesian-portable/CartesianCoordinatesTests.csproj --configuration Release
```

需要 .NET 8 SDK，不依赖第三方 NuGet 包。此入口不运行 WinForms，不加载厂商 SDK。

## Windows 检查入口与证据边界

`build-dashboard.ps1` 已接入相同数学测试，并输出 `cartesian-results.txt` 和 `cartesianChecks`。新增 GitHub Actions 工作流在 Windows 上使用系统 .NET Framework C# 编译器执行同一份纯数学测试；工作流运行结果以对应 PR 的实际检查状态为准。

本次本机运行不是完整 Windows/.NET Framework 4.0 Client Profile 操作台构建证据。原运动/保险/UI源文件没有修改，本轮未重跑其 Windows 检查，也未更新 ThinkBook 发布物。

`controllerConnected=false`、`operatorAppStarted=false`。尚未完成实体原点标记、机械参数标定、TCP正运动学、XYZ运动或实机验收；不能据27组数学检查推断这些事项完成。
