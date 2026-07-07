# DSPAddPlanet-moon^2-patch

这是 Zincon 维护的《戴森球计划》DSPAddPlanet 个人 fork。

本项目不是原版 DSPAddPlanet。它基于 LittleSaya 的 **DSPAddPlanet**，并参考 Touhma 的 **GalacticScale** 部分星球主题和生成思路，由 **Codex-GPT** 协助制作。Zincon 本人是纯代码小白，甚至不知道应该用什么软件编辑代码；这个项目能做出来，主要依靠两位原作者留下的优秀 Mod 代码、Zincon 的测试反馈，以及 Codex-GPT 的协助。

特别感谢：

- LittleSaya / IndexOutOfRangeDSPMod / DSPAddPlanet  
  https://github.com/LittleSaya/IndexOutOfRangeDSPMod/tree/master/DSPAddPlanet
- Touhma / DSP Galactic Scale  
  https://github.com/Touhma/DSP_Galactic_Scale

使用前请务必备份存档和配置文件。添加或修改星球后，存档可能无法安全恢复。

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
