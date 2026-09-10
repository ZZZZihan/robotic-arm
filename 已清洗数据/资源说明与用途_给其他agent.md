# 机械臂项目：资源清洗与用途说明（for Agents）

更新时间：2026-09-10

## 1) 清洗结果

- 已完成的操作：
  - 所有原始压缩包解压到：`已清洗数据/unpacked`
  - 所有原始压缩包归档到：`已清洗数据/raw`
  - 生成结构化索引：`已清洗数据/资源结构化清单.json`

- 处理原则：
  - 不删除任何原始文件。
  - 保留工程源码、配置、可执行文件、动态库、日志与文档。
  - 优先兼容 Windows 与 Linux 可继续调用工具。

## 2) 每个资源是什么（用途说明）

### 2.1 总线控制类（核心）

- `C#例程源代码及库文件.rar` → `已清洗数据/raw/` 与 `已清洗数据/unpacked/MultiCard-Visual C#`
  - 用途：C# 示例、工程源码、MultiCard 相关 DLL/库与测试可执行，偏向总线卡点位/点动相关。
  - 特征：含 `*.csproj`、`*.cs`、`MultiCard*.dll/lib`。

- `VC++例程源代码及库文件.rar` → `已清洗数据/unpacked/VC++例程源代码及库文件`
  - 用途：VC++ 示例源码与 32/64 位动态库。
  - 特征：含 `DemoVC.exe`、`MultiCardCPP.h`、VS2010 工程文件。

- `Demo - VC++总线卡点位和插补运动.rar` → `已清洗数据/unpacked/Demo - VC++总线卡点位和插补运动`
  - 用途：VC++ 点位与插补运动演示。
  - 特征：含 `Demo.cpp/.h/.rc`、`*.vcxproj`，常配合 MultiCard DLL 运行。

- `EtherCAT - 总线初始化-点位-Jog运动(C#).rar` → `已清洗数据/unpacked/EtherCAT - 总线初始化-点位-Jog运动`
  - 用途：EtherCAT 总线初始化流程、点位读取与 Jog 运动示例。
  - 特征：含 `CSharpDemo.csproj`、`CSharpDemo.exe`。

- `测试升级工具-2025-5-5.rar` → `已清洗数据/unpacked/测试升级工具-2025-5-5`
  - 用途：升级/测试工具包。
  - 特征：含 `EthBoardTest_2016.exe`、`sscom5.10.exe`、`CardIPConfig.txt`、`*.dll`、大量 `RunTimeLog`。

### 2.2 技术文档

- `SV630N系列伺服用户手册-CN-B06-网络型.PDF`
  - 用途：SV630N 系列伺服相关参数、配置与网络接入说明。

- `博派科技EthCAT-I32-O16-MPG-GAS2总线运动控制卡用户手册V1.0.pdf`
  - 用途：博派总线运动控制卡手册，含总线/报文/接口与配置说明。

### 2.3 智能码垛项目

- `MOVIFT智能码垛.zip` → `已清洗数据/unpacked/sp`、`已清洗数据/unpacked/智能码垛模板`、`已清洗数据/unpacked/版本文件`
  - 用途：智能码垛场景相关图片、模板、使用说明及版本运行包。
  - `sp/`：流程图示意图（png）。
  - `智能码垛模板/`：模板元数据（json）和模板说明。
  - `版本文件/`：控制程序和部署资源（可执行文件、Qt 运行库、配置、PLC/teach 程序），含 3 个嵌套 zip（`deploy-main-20250630_120147.zip`、`PC-main-20250630_135302.zip`、`smartpallet-main-20250630.zip`）。

### 2.4 图片素材

- `86e063488de381e433ab92168f048469.jpg`
- `88592e2c4d5609abbbaf537ff7bb6dc1.jpg`
- `da01cf719ff1b4dd9e3544876891511c.jpg`
  - 用途：通常用于页面展示/设备标识或资料附图，建议在整理资料包时作为外部展示资源。

## 3) 方便交给其他 Agent 的标准用法

1. 统一入口路径：
   - 清洗后路径：`已清洗数据/unpacked`
   - 原始归档：`已清洗数据/raw`
   - 结构化索引：`已清洗数据/资源结构化清单.json`

2. 直接给 Agent 的「最少上下文」
   - 先发这个文件：`已清洗数据/资源结构化清单.json`
   - 再发这条路径：`已清洗数据/unpacked`
   - 最后补充任务目标（例如：只找 EtherCAT 示例、只找 Windows 可执行样例）。

3. 快速检索命令
   - 看 C# 源码：`rg --files 已清洗数据/unpacked | rg '/.*\.cs$'`
   - 看 DLL 清单：`rg --files 已清洗数据/unpacked | rg '\.(dll|lib|exe)$'`
   - 看文档：`rg --files 已清洗数据/unpacked | rg '\.(pdf|pdf|txt|md|json|yml|ini)$'`
   - 看版本目录：`find '已清洗数据/unpacked/版本文件' -maxdepth 2 -type d`

## 4) 解压方式（给后续重复运行）

- 统一推荐：
  - RAR / ZIP（当前环境推荐）：`bsdtar -xf "文件名" -C 已清洗数据/unpacked`
- 兼容后备：
  - `unrar x "文件名"`
  - `7z x "文件名" -o已清洗数据/unpacked`
- ZIP 在 Windows 中文环境常见更兼容：`7z x ...`

## 5) 异常与约定

- 在当前环境，早期解压动作已出现旧 `unzip` 兼容性提示，但已改用 `bsdtar` 重新整理，最终核心文件已落在上面路径。
- `版本文件` 内的子 zip 目前保留原样，便于在目标环境按需再次拆包；如你要“全量扁平化”，我建议在有 7z 的机器上再做一次单独递归解包。
