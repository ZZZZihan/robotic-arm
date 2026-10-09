# 随仓库交接的资料

这是一套围绕当前四轴操作台整理的资料副本。共 15 个原始文件，约 76 MiB；不需要原电脑上的 `已清洗数据/unpacked` 目录即可阅读核心手册和构建 App。完整原始压缩包、升级工具、旧程序运行日志和多个重复版本继续保留在本机。

## 应该用哪一份

| 文件/目录 | 来源与用途 | 注意 |
| --- | --- | --- |
| [博派控制卡手册 V1.0](manuals/Bopai-EtherCAT-Controller-V1.0.pdf) | 原文件名 `博派科技EthCAT-I32-O16-MPG-GAS2总线运动控制卡用户手册V1.0.pdf`，100 页；查硬件接口、API 返回值、运动与总线接口 | 本地已使用版本；不是声称厂商当前最新版 |
| [SV630N 用户手册 CN-B06](manuals/Inovance-SV630N-CN-B06.pdf) | 原文件名 `SV630N系列伺服用户手册-CN-B06-网络型.PDF`，595 页；前言版本记录为 2022-09 B06 | 区分 PDF 页码与印刷页码；按具体驱动型号和参数适用范围阅读 |
| [SDK 三份 DLL](sdk/bopai-ethercat-x64/) | 来自 EtherCAT C# 示例的 `bin/x64/Debug`，与当前 Windows 验证版本哈希一致 | 必须整组使用；不是旧通用 C# 示例的 DLL |
| [原始 C# 示例](vendor-reference/EtherCATDemo/) | 厂商 `EtherCAT - 总线初始化-点位-Jog运动/CSharpDemo` 的 10 个源码/工程文件 | 未修改，默认配置及控制方式与维护版不同；不要拿它当日常操作入口 |
| [逐文件清单](manifest.json) | 15 个文件的原始名称、来源路径、大小、SHA-256 | 可用于传输、换机和依赖版本核验 |

DLL 对应关系：`MultiCardCS.dll` 是 C# 调用接口，`MultiCardCLR.dll` 是所需 CLR 库，`MultiCard.dll` 是本组运行库。不要仅按文件名认版本；不同资料包有同名但哈希不同的文件。

## 手册快速定位

以下页码均指 PDF 阅读器从 1 开始计数的页码；关键词可在 PDF 内搜索。导航帮助找到材料，不代表应在未核对现场条件时改参数。

| 问题 | 查阅入口 |
| --- | --- |
| 控制卡连接、API 返回值 | 博派手册 PDF 第 14～16 页 |
| 点位、JOG、状态与停止相关接口 | 博派手册 PDF 第 22～35 页 |
| EtherCAT 总线相关接口 | 博派手册 PDF 第 71 页起 |
| SV630N 版本与适用产品说明 | SV630N PDF 第 2 页 |
| 电机型号代码 | 搜索 `H00.00`、`14101`；相关说明见 PDF 第 358 页 |
| EtherCAT 站号 | 搜索 `H0E.20`；相关参数页见 PDF 第 436 页 |
| 电子齿轮与实际位置 | 搜索 `6091h`、`6064h`；对象字典中有详细说明 |
| 使能状态、错误码和抱闸 | 搜索 `6041h`、`603Fh`、“抱闸接线”；结合实际型号、接线和当前读数确认 |

维护版使用 `GA_` 前缀的 C# SDK 调用，手册中的接口命名不一定逐字相同；实际函数签名以本组库和维护源码为准。

## 校验

在仓库根目录运行 Windows PowerShell：

```powershell
$manifest = Get-Content .\materials\manifest.json -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($item in $manifest.artifacts) {
    $actual = (Get-FileHash -LiteralPath $item.path -Algorithm SHA256).Hash
    if ($actual -ne $item.sha256) { throw "校验失败：$($item.path)" }
}
"15 个资料文件校验通过"
```

构建入口 [`build-dashboard.ps1`](../thinkbook-deploy/scripts/build-dashboard.ps1) 会自动校验三份 DLL。源文件换行可能按 Git 的文本规则转换；清单中的第三方原始副本已设置为按字节保存。

来源和使用范围见 [NOTICE.md](NOTICE.md)。旧完整资源索引见 [资源索引](../资源索引.md)，其中标注“本机”的项目不随此仓库提供。
