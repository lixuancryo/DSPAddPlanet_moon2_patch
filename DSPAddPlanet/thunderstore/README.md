# DSPAddPlanet-moon^2-patch

这是 Zincon 维护的《戴森球计划》DSPAddPlanet 个人 fork，不是原版 DSPAddPlanet，也不是 GalacticScale。

先说在前面：Zincon 本人是一个纯代码小白，甚至不知道应该用什么软件编辑代码。制作这个 Mod 的初衷非常简单：我纯粹是为了满足自己游戏里的需求，十分希望能做出一个可以自行添加“卫星的卫星”的 Mod。但原作者已经长久不更新，幸好 AI 的发展让我有机会根据原作者开源出来的代码，在 AI 协助下制作一个符合自己需求的 Mod。

我也非常感谢《戴森球计划》的游戏制作组。没有这款游戏本身，就不会有这些折腾星系生成、星球主题和卫星系统的乐趣。

另外，我也很喜欢 GalacticScale 中的一些自制星球主题。因此，这里做了一个简单的个人 Mod：保留 DSPAddPlanet 自定义添加星球的思路，加入卫星的卫星功能，并尝试把一部分 GalacticScale 风格主题带进 DSPAddPlanet，满足自己的游戏需求。

特别感谢：

- LittleSaya / IndexOutOfRangeDSPMod / DSPAddPlanet  
  https://github.com/LittleSaya/IndexOutOfRangeDSPMod/tree/master/DSPAddPlanet
- Touhma / DSP Galactic Scale  
  https://github.com/Touhma/DSP_Galactic_Scale
- 《戴森球计划》游戏制作组
- Codex-GPT 和 AI 工具的发展

使用前请务必备份存档和配置文件。添加或修改星球后，存档可能无法安全恢复。

## 相比原版 DSPAddPlanet 新增了什么

这个 fork 保留原版 DSPAddPlanet 的“通过 XML 自定义添加星球”用法，并额外加入下面这些能力。这里只列最短写法，完整示例和注意事项详见下方。

- **卫星的卫星**：新增星球可以围绕另一颗卫星运行。最短写法：`<OrbitAroundIndex>5</OrbitAroundIndex>`。这里的 `5` 填父星球的 `Index`；`OrbitAround` 仍要保留；`OrbitIndex` 控制轨道序号；`Number` 控制该轨道中心下的编号。

- **GalacticScale 风格星球主题**：不开启 GalacticScale，也能用 Beach、GiganticForest、SulfurSea 等主题。最短写法：`<ThemeName>Beach</ThemeName>`。`ThemeName` 和 `ThemeId` 二选一，推荐用 `ThemeName`；固态星球通常写 `GasGiant=false`。

- **GalacticScale 风格气态巨星**：可以添加 Inferno、OilGiant 这类气态巨星主题。最短写法：`<GasGiant>true</GasGiant><ThemeName>OilGiant</ThemeName>`。气态巨星建议同时写 `GasGiant=true` 和 `DontGenerateVein=true`。

- **控制新主题是否进入随机星区生成**：可以选择普通随机星区是否自然刷出这些新主题。最短写法：`<EnableGalacticScaleThemesInRandomGeneration>false</EnableGalacticScaleThemesInRandomGeneration>`。这一行写在 `<Config>` 下面；`false` 表示只在手动指定 `ThemeName` 时使用；`true` 表示随机生成也可能使用。

- **非 200 半径星球兼容**：使用 `<Radius>100</Radius>` 之类的自定义半径时会自动生效，无需增加配置行。云层和本地大气模糊会跟随真实半径；矿机与矿物范围、传送带网格宽度也会按真实半径计算。半径小于 100 的固态星球还会使用更密的矿脉碰撞采样。

- **移除不稳定小型主题**：避免 DwarfPlanet、Comet 这类极小星球在原版生成器里出问题。本 fork 不提供 `DwarfPlanet`、`Comet`、`BarrenSatellite` 这些新增 `ThemeName`。

## 可设置参数速查

星球写在 `<Planet>...</Planet>` 里。常用参数如下，具体写法详见下方完整示例。

| 参数 | 用途 |
| --- | --- |
| `UniqueStarId` | 指定目标存档、星区字符串和恒星名；在 `GameNameSpecific` 中使用 |
| `IsBirthPoint` | 是否把这颗星球设为出生点 |
| `Index` | 这颗星球在当前恒星里的索引，新增星球不要和已有星球重复 |
| `OrbitAround` | 原版 DSPAddPlanet 的父轨道参数；普通行星写 `0`；本 fork 中仍然保留为必填 |
| `OrbitAroundIndex` | 本 fork 新增参数，直接按父星球 `Index` 指定公转中心，可用于卫星的卫星 |
| `OrbitIndex` | 轨道序号，会影响和父星球的距离 |
| `Number` | 同一轨道中心下的星球编号，不要重复 |
| `GasGiant` | 是否是气态巨星 |
| `ThemeName` | 按名字指定主题，推荐用于新增 GalacticScale 风格主题 |
| `ThemeId` | 按数字指定主题；不推荐用于新增主题，因为 ID 可能受主题表顺序影响 |
| `InfoSeed` / `GenSeed` | 星球信息和地形生成种子 |
| `ForcePlanetRadius` / `Radius` | 是否强制半径，以及星球半径 |
| `OrbitalPeriod` / `RotationPeriod` | 公转周期和自转周期 |
| `IsTidalLocked` | 是否潮汐锁定 |
| `OrbitInclination` / `Obliquity` / `OrbitLongitude` | 轨道倾角、地轴倾角、升交点经度 |
| `DontGenerateVein` | 是否禁止生成矿物；`false` 表示允许生成矿物 |
| `ReplaceAllVeinsTo` | 把矿物统一替换成某一种矿物 |
| `VeinCustom` | 自定义矿物种类、矿脉数量、矿点数量和矿量 |

## 非 200 半径星球兼容

游戏原版只自然生成半径 200 的固态星球，因此很多代码虽然支持读取 `Radius`，实际仍带有半径 200 的默认假设。3.0.12 针对 GalacticScale 中能确认、并且适合独立移植的部分做了以下调整：

- 云层高度：在原版云层初始化前，按真实半径缩放大气材质的内外半径。云图球壳和云粒子会继续使用游戏原版公式，但不再拿固定的 200/270 半径计算。
- 本地大气模糊：大气模糊球壳、启用距离和淡入距离按真实半径同比缩放。
- 矿机与矿物：矿机覆盖矿脉的距离判断不再把星球强制投影到半径 200。
- 传送带建造：传送带在不同纬度计算网格宽度时使用当前星球半径和当前活动网格段数。
- 极小星球矿脉碰撞：半径小于 100 时增加曲面采样点，降低建造时漏检附近矿脉的概率。
- 蓝图放置：地表基准使用 `realRadius + 0.2`。

当前游戏版本的普通建筑碰撞、高度限制和物流调度大多已经直接读取 `planet.realRadius` 或 `AstroData.uRadius`。因此本 fork 没有覆盖这些正常逻辑，也没有照搬 GalacticScale 涉及战斗、导航和黑雾的全局半径替换补丁。

GalacticScale 还会在半径小于 20 时强制忽略分拣器的 `TooSkew`（倾斜过大）建造限制。该做法会绕过游戏的正常建造校验，本 fork 没有移植；半径小于 20 的极端星球仍属于实验范围。

小星球是本次适配重点。大星球会同时受益于云层、矿机范围、传送带网格和蓝图修复，但超大半径仍可能遇到游戏原版未调试的其他边缘问题。修改已有存档中的星球半径前，请先备份存档。

## 下载与安装

推荐在 GitHub Release 下载 Thunderstore/r2modman zip 包，然后在 r2modman 里使用 `Import local mod` 导入。

如果只想手动安装，也可以下载 Release 里的 `DSPAddPlanet.dll`，放到当前配置档案的 BepInEx 插件目录中。

不要同时启用这些 Mod：

- 原版 `IndexOutOfRange.DSPAddPlanet`
- `GalacticScale 2`

推荐只保留 `Zincon-DSPAddPlanet-moon^2-patch` 这一份。

## 配置文件在哪里

本 Mod 仍然沿用 DSPAddPlanet 的配置方式。第一次启动游戏后，如果没有配置文件，会自动创建：

```text
<戴森球计划存档目录>/modData/IndexOutOfRange.DSPAddPlanet/config.xml
```

如果你不知道这个目录在哪里，可以先启动一次游戏，然后在磁盘里搜索 `IndexOutOfRange.DSPAddPlanet` 或 `config.xml`。

本项目附带了一个完整示例：

```text
DSPAddPlanet/thunderstore/gs-theme-example-config.xml
```

它不会自动生效，只是给你复制、改写用的参考。

## XML 基本结构

不需要写 C# 代码。玩家实际要改的是 XML 配置。

推荐把只针对某个存档生效的星球写进 `GameNameSpecific`。`GameName`、`ClusterString`、`Star` 可以从游戏内 DSPAddPlanet 的 Add Planet 面板复制。

```xml
<?xml version="1.0" encoding="utf-8"?>
<Config>
    <EnableGalacticScaleThemesInRandomGeneration>false</EnableGalacticScaleThemesInRandomGeneration>

    <Global>
        <Planets>
        </Planets>
    </Global>

    <GameNameSpecific>
        <Planets>
            <Planet>
                <UniqueStarId>
                    <GameName>你的存档名</GameName>
                    <ClusterString>你的星区字符串</ClusterString>
                    <Star>目标恒星名</Star>
                </UniqueStarId>

                <!-- 星球参数写在这里 -->
            </Planet>
        </Planets>
    </GameNameSpecific>
</Config>
```

## 添加普通星球

普通行星直接围绕恒星运行，`OrbitAround` 写 `0`。

```xml
<Planet>
    <UniqueStarId>
        <GameName>你的存档名</GameName>
        <ClusterString>你的星区字符串</ClusterString>
        <Star>目标恒星名</Star>
    </UniqueStarId>

    <IsBirthPoint>false</IsBirthPoint>
    <Index>4</Index>
    <OrbitAround>0</OrbitAround>
    <OrbitIndex>3</OrbitIndex>
    <Number>4</Number>

    <GasGiant>false</GasGiant>
    <InfoSeed>3001</InfoSeed>
    <GenSeed>4001</GenSeed>
    <Radius>200</Radius>
    <OrbitalPeriod>3600</OrbitalPeriod>
    <RotationPeriod>3600</RotationPeriod>
    <IsTidalLocked>true</IsTidalLocked>
    <OrbitInclination>5</OrbitInclination>
    <Obliquity>10</Obliquity>
    <OrbitLongitude>30</OrbitLongitude>

    <ThemeName>GiganticForest</ThemeName>
    <DontGenerateVein>false</DontGenerateVein>
</Planet>
```

几个容易填错的点：

- `Index` 是这个星球在当前恒星里的索引。不要和已有星球重复，新增星球之间也不要跳号。
- `OrbitIndex` 是轨道序号，会影响距离。
- `Number` 是游戏显示和内部轨道关系会用到的编号，同一个轨道中心下不要重复。
- `DontGenerateVein=false` 表示允许生成矿物；如果写 `true`，这个星球不生成矿物。

## 添加卫星

原版 DSPAddPlanet 只靠 `OrbitAround` 找父星球。这个 fork 新增了 `OrbitAroundIndex`，可以直接按父星球的 `Index` 查找，推荐添加卫星时也写上它。

下面这个星球围绕 `Index=4` 的星球运行：

```xml
<Index>5</Index>
<OrbitAround>4</OrbitAround>
<OrbitAroundIndex>4</OrbitAroundIndex>
<OrbitIndex>1</OrbitIndex>
<Number>1</Number>
```

说明：

- `OrbitAround` 仍然是必填项，因为旧版 DSPAddPlanet 的配置结构要求它存在。
- 如果写了 `OrbitAroundIndex`，本 fork 会优先使用 `OrbitAroundIndex`。
- `OrbitAroundIndex` 指向父星球的 `Index`，可以指向普通行星，也可以指向卫星。

## 添加卫星的卫星

这是本 fork 最重要的改动之一。假设：

- `Index=4` 是一颗普通行星。
- `Index=5` 是围绕它运行的卫星。
- 你想让 `Index=6` 成为 `Index=5` 的卫星。

那么 `Index=6` 这样写：

```xml
<Planet>
    <UniqueStarId>
        <GameName>你的存档名</GameName>
        <ClusterString>你的星区字符串</ClusterString>
        <Star>目标恒星名</Star>
    </UniqueStarId>

    <IsBirthPoint>false</IsBirthPoint>
    <Index>6</Index>

    <!-- 这一行仍然必须存在；真正的父星球由 OrbitAroundIndex 指定。 -->
    <OrbitAround>0</OrbitAround>

    <!-- 这里填父卫星的 Index。 -->
    <OrbitAroundIndex>5</OrbitAroundIndex>

    <OrbitIndex>1</OrbitIndex>
    <Number>1</Number>

    <GasGiant>false</GasGiant>
    <InfoSeed>3003</InfoSeed>
    <GenSeed>4003</GenSeed>
    <Radius>100</Radius>
    <OrbitalPeriod>600</OrbitalPeriod>
    <RotationPeriod>600</RotationPeriod>
    <IsTidalLocked>true</IsTidalLocked>
    <OrbitInclination>15</OrbitInclination>
    <Obliquity>0</Obliquity>
    <OrbitLongitude>180</OrbitLongitude>

    <ThemeName>SulfurSea</ThemeName>
    <DontGenerateVein>false</DontGenerateVein>
</Planet>
```

注意：

- 父星球必须已经存在，或者在同一个 XML 文件里写在更前面。
- `OrbitAroundIndex` 不能指向自己。
- 不要制造循环，例如 A 绕 B、B 又绕 A。

## 使用 GalacticScale 风格主题

这个 fork 可以在不开启 GalacticScale 的情况下，让 DSPAddPlanet 使用一部分 GalacticScale 风格星球主题。

推荐使用 `ThemeName`，不要优先使用 `ThemeId`：

```xml
<ThemeName>Beach</ThemeName>
```

不要在同一个星球上同时写 `ThemeName` 和 `ThemeId`，二选一即可。

可用主题名：

| ThemeName | 类型 | 备注 |
| --- | --- | --- |
| `GiganticForest` | 固态星球 | 巨型森林风格 |
| `RedForest` | 固态星球 | 红色森林风格 |
| `SulfurSea` | 固态星球 | 硫海风格 |
| `MoltenWorld` | 固态星球 | 熔融世界风格，随机生成位置接近 Lava |
| `Beach` | 固态星球 | 海滩风格，带专门地形和矿物生成 |
| `Pandora` | 固态星球 | Pandora 风格 |
| `Obsidian` | 固态星球 | 黑曜石风格 |
| `HotObsidian` | 固态星球 | 高温黑曜石风格 |
| `GiganticForestCold` | 固态星球 | 寒冷版巨型森林 |
| `RedForestCold` | 固态星球 | 寒冷版红色森林 |
| `BeachCold` | 固态星球 | 寒冷版海滩 |
| `PandoraCold` | 固态星球 | 寒冷版 Pandora |
| `Inferno` | 气态巨星 | 使用时请写 `GasGiant=true` |
| `OilGiant` | 气态巨星 | 使用时请写 `GasGiant=true` |

主题名解析会忽略大小写、空格、横线、下划线和点号。例如 `GiganticForest` 和 `Gigantic Forest` 都能匹配，但为了减少歧义，建议统一使用表格里的写法。

## 主题 ID 说明

不推荐在配置里填写新增主题的 `ThemeId`。原因是这些 GalacticScale 风格主题是在游戏启动时追加注册到主题表末尾的，具体数字可能受游戏版本或其他主题表修改影响。

请优先使用：

```xml
<ThemeName>GiganticForest</ThemeName>
```

如果你确实需要看 ID，可以打开 BepInEx 日志，搜索类似内容：

```text
Registered ... GalacticScale-style themes
```

或在报错里查看 `Known themes` 列表。普通玩家不需要手动填写这些 ID。

## 控制新主题是否进入随机星区生成

在 `<Config>` 下面直接写这一行：

```xml
<EnableGalacticScaleThemesInRandomGeneration>false</EnableGalacticScaleThemesInRandomGeneration>
```

含义：

- `false`：默认值。只有你在某个星球里明确写了 `ThemeName`，才会使用这些 GalacticScale 风格主题。
- `true`：游戏普通随机生成星区时，也可能把这些主题用于自然生成的星球。

如果你只想用 DSPAddPlanet 手动添加星球，建议保持 `false`。

## 已删除或避开的主题

这些主题没有作为可用 GalacticScale 风格主题提供：

- `DwarfPlanet`
- `Comet`
- `BarrenSatellite`

原因：

- `DwarfPlanet` 和 `Comet` 是极小星球主题，在原版生成器中不稳定。
- `BarrenSatellite` 在游戏默认生成器中已经有类似主题，而且移植后出现过异常外壳问题。

## 这个 fork 做过的兼容调整

为了让 GalacticScale 风格主题能在未开启 GalacticScale 的情况下工作，本 fork 做了这些处理：

- 新增 `OrbitAroundIndex`，支持卫星围绕卫星运行。
- 注册 GalacticScale 风格主题，但不启用 GalacticScale 的完整星系生成器。
- Beach 使用本 fork 内置的 GalacticScale 风格地形、植被和矿物生成路径。
- Beach/BeachCold 使用 GS2 风格矿物组合：硅石、刺笋、水晶和有机晶体。
- SulfurSea 使用单独地形材质和颜色处理，尽量减少地面过曝。
- GiganticForest 保持自然植被分布，不再强行把树木数量翻倍。
- MoltenWorld 的随机生成位置接近 Lava，权重大约是 Lava 的 20%。
- RedForest、Beach、Pandora、GiganticForest 可以出现在有机星球位置和寒冷星球位置。
- 移除了会导致不稳定的小型星球主题。

这些主题不是完整 GalacticScale 生成器的复制品，而是在 DSPAddPlanet 框架下尽量模拟 GalacticScale 风格。外观、矿物、地形和 GalacticScale 原版可能仍有差异。

## 气态巨星写法

`Inferno` 和 `OilGiant` 是气态巨星主题，需要同时写：

```xml
<GasGiant>true</GasGiant>
<ThemeName>OilGiant</ThemeName>
<DontGenerateVein>true</DontGenerateVein>
```

固态星球则通常写：

```xml
<GasGiant>false</GasGiant>
<ThemeName>Beach</ThemeName>
<DontGenerateVein>false</DontGenerateVein>
```

## 完整示例

仓库里提供了一个较完整的示例文件：

```text
DSPAddPlanet/thunderstore/gs-theme-example-config.xml
```

它包含：

- 普通 GalacticScale 风格行星。
- 普通卫星。
- 卫星的卫星。
- 气态巨星主题。
- 随机生成开关。

建议先复制这个示例，再改 `GameName`、`ClusterString`、`Star`、`Index`、`OrbitIndex` 和 `Number`。

## 再次致谢

这个项目本质上站在两个优秀 Mod 的肩膀上：

- 没有 DSPAddPlanet，就没有自定义添加星球的基础。
- 没有 GalacticScale，就没有这些复杂星球主题和卫星系统方面的参考。

Zincon 只是提出需求、测试效果、反馈问题的普通玩家。代码实现和整理主要由 Codex-GPT 协助完成。非常感谢两位原作者和社区。
