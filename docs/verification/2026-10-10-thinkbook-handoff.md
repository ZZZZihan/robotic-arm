# ThinkBook 更新与封存交接记录

2026-10-10，经用户明确要求，把已合并、通过离线回归的最新操作台更新到 ThinkBook，并固定本次交接版本。应用源码基线为 [`1d66dd4`](https://github.com/ZZZZihan/robotic-arm/commit/1d66dd4388e1f0e4d5e8f3d73be2c96eedddf80a)，交接标签为 `handoff-2026-10-10`。标签用于固定交付快照；仓库保留可写，后续由接手者通过新分支继续开发。

## 已更新的内容

部署记录时间为 `2026-10-10T05:30:21.4180401Z`（北京时间 13:30:21）。正式桌面入口仍叫“机械臂-四轴操作台”，已回读确认指向：

```text
C:\RobotArmDemo\releases\four-axis-handoff-20261010-1d66dd4\app\CSharpDemo.exe
```

工作目录是同级 `app` 目录，启动参数为空，图标指向新 EXE，原窗口样式和热键保留。其他桌面快捷方式未改动。本次发布目录包括完整 App、Git 基线源码、离线验证结果和部署清单。

发布的 EXE SHA-256：

```text
6E69787008315CBA29233E942B4852F93E08C39C8A31B201DC1BC7983A01012E
```

这个二进制来自本日已验证的独立构建，Windows x64 Release 构建及 20 项运动核心、33 项保险、26 组 UI 检查通过。本次没有重新编译或改动应用代码，而是在复制前后检查 5 个 App 文件（EXE、PDB、三份 DLL）、19 个已测试构建/资源/测试文件，以及 25 个源码与证据包文件。部署前后均未发现 `CSharpDemo.exe` 进程或 UDP 60000 监听。

具体证据见[部署结果](2026-10-10-thinkbook-handoff/deployment.json)，测试范围与逐文件源码哈希见[本轮离线回归记录](2026-10-10-merge-review.md)。部署脚本经独立静态复核，使用新目录、逐项复制、原快捷方式备份及替换失败时恢复原入口的处理。

## 回滚

旧版 EXE 和三份 SDK DLL 在更新前后均核验一致，原发布目录保留：

```text
C:\RobotArmDemo\releases\four-axis-insurance-20261009-180249\built\app\CSharpDemo.exe
```

旧 EXE SHA-256 为 `923A82EFA30DBE292FD17218445FEABC01626AF31767D960F642420C29ECFDB2`。原桌面快捷方式的二进制备份及其哈希保留在：

```text
C:\RobotArmDemo\releases\four-axis-handoff-20261010-1d66dd4\rollback\机械臂-四轴操作台.lnk
```

若需回滚，由现场操作者确认四轴停稳、负载可靠保持并正常关闭控制程序，再把备份快捷方式复制回桌面覆盖同名入口，或从旧发布目录启动完整 App。不要同时运行两个控制程序，不覆盖运行中的文件。

## 封存范围与接手事项

ThinkBook 本机封存包为：

```text
C:\RobotArmDemo\archives\four-axis-handoff-20261010-1d66dd4.zip
```

包内恰好 33 个文件：5 个 App 文件、21 个源码文件、4 个离线验证文件、部署清单、部署结果和交接说明；不包含桌面快捷方式备份或运维脚本。打包后重新打开 ZIP，逐条校验名称、数量和文件哈希，全部通过，详见[封存包校验记录](2026-10-10-thinkbook-handoff/archive.json)。大小为 2,579,035 字节，SHA-256 为 `2C5C526FF3430CF46384E32BA0656DFF7FEF450BC34953B55F25E4CDB7AB1FAE`；同目录 `.zip.sha256` 文件保存校验值。该包是 ThinkBook 本机备份，未发布为 GitHub 二进制 Release。

封存包含当前应用、源码、验证与部署记录，旧版与入口备份继续保留。封存不关闭 GitHub 仓库，也不删除本机其他未纳入交接的原型、课程或现场资料。后续工作以[README](../../README.md)和[HANDOFF](../HANDOFF.md)为入口。

**本次完成文件和桌面入口更新，没有启动操作者 App、连接控制卡、初始化总线、使能或执行硬件动作。** 后续打开新程序会自动连接并检查总线，符合条件时会初始化，因此首次现场使用仍由现场操作者安排。新版实际使用、真实停止效果、机械标定、四轴映射、抱闸、物理急停/STO 与限位尚待验收；这些事项随本次交接移交，不因封存而视为完成。
