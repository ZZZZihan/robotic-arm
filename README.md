# Robotic Arm Lab

机械臂控制与 EtherCAT 示例的私有开发仓库。这里保存后续要持续编写、测试和同步的源码、脚本、文档与测试约定。

## 当前仓库边界

首版只同步可维护的工程材料：

- `thinkbook-deploy/payload/source/EtherCATDemo/`：当前 EtherCAT C# 示例的可编辑源码与工程文件。
- `thinkbook-deploy/payload/reference/VCppDemo/`：VC++ 示例参考源码；它是参考材料，不代表已经完成编译或现场运行验收。
- `thinkbook-deploy/prepare.py`：从本地资料准备部署副本的脚本。
- `thinkbook-deploy/scripts/build.ps1`：Windows x64 构建入口。
- `资源索引.md` 与 `已清洗数据/` 下的资源说明/结构化索引：帮助后续 Agent 找到本机资料。
- `tests/`：后续放置单元测试、协议测试、仿真测试和硬件联调记录的约定。

以下内容默认只保留在本机，不进入 GitHub：原始压缩包、完整解包资料、厂商 PDF、DLL/EXE/库文件、编译缓存、部署压缩包，以及包含 SSH/tailnet 连接信息、设备网络配置或现场状态的证据文件。当前示例源文件可能保留供应商 demo 的控制器端点常量；它们不是凭据，后续新代码应配置化，公开发布前仍要重新审查。上述本机资料已经在 `.gitignore` 中列出；如以后确实需要同步某个文件，应先单独确认授权、敏感信息和文件大小。

## 本地开发

当前目录保留了资料清洗结果，源码编辑和后续新代码可以逐步整理到清晰的 `src/`、`tests/`、`docs/` 目录。现有 ThinkBook 示例的本地部署副本仍由 `thinkbook-deploy/prepare.py` 生成，生成的二进制和现场证据不会因为运行脚本而自动进入提交。

提交前建议至少运行：

```sh
git diff --check
git status --short
```

Windows 示例的构建需要目标机器上的 Visual Studio/MSBuild 与厂商运行库；Mac 上的源码检查不等同于控制卡、伺服器或真实机械臂的现场验收。

## 测试分层

测试记录要明确属于哪一层：

1. 离线单元/协议测试：不接控制卡即可重复运行。
2. 仿真或固定夹具测试：输入、期望状态和版本可重放。
3. Windows 应用构建/启动测试：证明程序能构建和启动，不证明运动安全。
4. 真实设备联调：另行记录接线、急停、限位、轴状态和现场批准。

详细约定见 [`tests/README.md`](tests/README.md)。

## 仓库状态

- 默认分支：`main`
- 远端：GitHub 私有仓库
- 当前阶段：建立可同步的工程骨架；控制卡、伺服器和真实运动能力不在首个 Git 提交中宣称已验收。
