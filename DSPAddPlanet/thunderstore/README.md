# DSPAddPlanet-moon^2-patch

这是一个《戴森球计划》Mod，作者/维护者名义为 **Zincon**。

本项目不是原版 DSPAddPlanet。它是在 LittleSaya 的 **DSPAddPlanet** 基础上，参考 Touhma 的 **GalacticScale** 部分思路和主题数据，由 **Codex-GPT** 协助制作出来的个人 fork。

Zincon 本人是一个纯代码小白，甚至不知道应该用什么软件编辑代码。这个项目能做出来，主要依靠原作者们留下的优秀 Mod 代码和 Codex-GPT 的协助。请特别感谢：

- LittleSaya / IndexOutOfRangeDSPMod / DSPAddPlanet  
  https://github.com/LittleSaya/IndexOutOfRangeDSPMod/tree/master/DSPAddPlanet
- Touhma / DSP Galactic Scale  
  https://github.com/Touhma/DSP_Galactic_Scale

## 请先看原版说明

DSPAddPlanet 的基础用法、配置文件结构、添加星球的基本参数等，请优先阅读原版 DSPAddPlanet 的说明。

本 README 只说明这个 fork 相比原版 DSPAddPlanet 改了什么。

使用前请务必备份存档和配置文件。添加或修改星球后，存档可能无法安全恢复。

## 这个 fork 改了什么

### 1. 支持卫星的卫星

原版 DSPAddPlanet 只能让游戏默认生成器认为某个位置多了一个星球，因此默认只能做到恒星行星和普通卫星。

本 fork 增加了 `OrbitAroundIndex`，可以让新星球围绕另一个卫星运行，也就是创建“卫星的卫星”。

简单理解：

- `OrbitAround` 保留原版行为，通常只适合指向直接围绕恒星运行的行星。
- `OrbitAroundIndex` 直接使用目标父星球的 `index`，可以指向卫星。
- 如果一个新卫星要围绕另一个新卫星，父卫星必须已经存在，或者在配置文件中写在更前面。

示例：

```xml
<OrbitAroundIndex>3</OrbitAroundIndex>
```

### 2. 加入部分 GalacticScale 风格主题

本 fork 尝试在不启用 GalacticScale 的完整星系生成系统时，让 DSPAddPlanet 也能使用一部分 GalacticScale 风格星球主题。

可用主题名包括：

- `GiganticForest`
- `RedForest`
- `SulfurSea`
- `MoltenWorld`
- `Beach`
- `Pandora`
- `Obsidian`
- `HotObsidian`
- `Inferno`
- `OilGiant`
- `GiganticForestCold`
- `RedForestCold`
- `BeachCold`
- `PandoraCold`

配置中可以这样写：

```xml
<ThemeName>Beach</ThemeName>
```

不要在同一个星球上同时填写 `ThemeId` 和 `ThemeName`，二选一即可。

### 3. 可控制 GalacticScale 风格主题是否进入随机生成

配置文件中可以加入：

```xml
<EnableGalacticScaleThemesInRandomGeneration>false</EnableGalacticScaleThemesInRandomGeneration>
```

- `false`：这些主题只会在你明确写了 `ThemeName` 时使用。
- `true`：游戏普通随机星区生成时也可能抽到这些主题。

### 4. 删除或避开不稳定主题

`DwarfPlanet` 和 `Comet` 是极小行星主题，在原版生成器中不稳定，因此本 fork 没有提供它们。

`BarrenSatellite` 也没有作为 GalacticScale 主题加入，因为游戏默认生成器中已经有类似主题。

### 5. 部分主题做了兼容调整

为了让这些主题能在未开启 GalacticScale 的情况下工作，本 fork 做了不少近似处理，例如：

- Beach 的地形、植被和矿物生成兼容。
- GiganticForest 保持自然植被分布，不再强行翻倍植被。
- SulfurSea 做了地面材质压暗处理，避免明显过曝。
- MoltenWorld 的随机生成位置接近 Lava，权重约为 Lava 的 20%。
- RedForest、Beach、Pandora、GiganticForest 可以出现在有机星球位置和寒冷星球位置。

这些主题不是完整 GalacticScale 生成器的复制品，而是在 DSPAddPlanet 框架下尽量模拟 GalacticScale 风格。

## 兼容性

- 本 fork 与原版 `IndexOutOfRange.DSPAddPlanet` 不应同时启用。
- 本 fork 与 `GalacticScale 2` 不应同时启用。
- 推荐只保留 `Zincon-DSPAddPlanet-moon^2-patch` 这一份。

## r2modman 导入

使用 r2modman 的 “Import local mod” 导入 thunderstore zip 包即可。

项目中生成过的 zip 位于：

```text
DSPAddPlanet/thunderstore/
```

## 再次致谢

这个项目本质上站在两个优秀 Mod 的肩膀上：

- 没有 DSPAddPlanet，就没有自定义添加星球的基础。
- 没有 GalacticScale，就没有这些复杂星球主题和卫星系统方面的参考。

Zincon 只是提出需求、测试效果、反馈问题的普通玩家。代码实现和整理主要由 Codex-GPT 协助完成。非常感谢两位原作者和社区。
