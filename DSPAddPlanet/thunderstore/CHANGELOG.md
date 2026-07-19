# DSPAddPlanet 3.0.14

- Added explicit `Star ID` and `<StarId>...</StarId>` lines to the starmap Add Planet panel.
- Changed `Copy` to `Copy XML`; it now copies a complete `UniqueStarId` XML block for the selected star.
- Displayed star IDs in the left star list and enabled best-fit text for long star names.
- Rebuilt the planet list as a parent-child tree, with recursive indentation for moons and deeper submoons.
- Sorted every sibling group by `orbitIndex`, then `number`, then `index`.
- Added a compact summary showing planet count, maximum used `Index`, and the next suggested `Index`.
- Enabled wrapped text, dynamic right-panel content height, and vertical-only scrolling to prevent long lines from escaping the panel.

# DSPAddPlanet 3.0.13

- Added the preferred `<StarId>` XML selector so custom planets keep targeting a fixed one-based star slot after the star is renamed or modified.
- Applied star-ID-first matching consistently during planet creation and all custom vein generation paths.
- Kept the legacy `<Star>` name selector as a fallback for existing XML configurations.
- Changed the in-game Add Planet panel's copied unique-star identifier to use the star ID.
- Updated the generated and packaged XML examples to use `<StarId>`.

# DSPAddPlanet 3.0.12

- Scaled solid-planet atmosphere material radii before cloud initialization, so nephogram and cloud-particle heights follow custom planet radius.
- Scaled the local atmosphere blur shell, activation distance, and fade distance for non-200-radius solid planets.
- Changed miner-to-vein range checks and belt grid-width calculations to use the current planet radius instead of a fixed radius of 200.
- Added denser curved-surface vein collider sampling for solid planets smaller than radius 100.
- Corrected the blueprint paste surface offset to use `realRadius + 0.2`.
- Verified that current DSP logistics dispatch already uses each planet's `AstroData.uRadius`; no extra ship-distance override was added.

# DSPAddPlanet 3.0.11

- Reworked SulfurSea ground material into a darker matte sulfur-rock surface to stop the washed-out land appearance.
- Reduced SulfurSea terrain multiplier, ambient lift and specular response while leaving the sulfur ocean and custom terrain generation path intact.

# DSPAddPlanet 3.0.10

- Fixed the DSP 0.10.34 `UniverseSimulator.VirtualMapping` runtime error by disabling the incompatible per-frame GalacticScale-style atmosphere refresh path.
- SulfurSea now applies GalacticScale's Gobi-derived terrain lighting keys, including `_LightColorScreen` and `_HeightEmissionColor`, instead of relying on the earlier partial terrain tint.
- SulfurSea now writes the VolcanicAsh ocean color set directly, matching GalacticScale's generated SulfurSea theme more closely.

# DSPAddPlanet 3.0.9

- Restored Beach/BeachCold to the GalacticScale-style OceanWorld terrain material source instead of the earlier Mediterranean workaround.
- GalacticScale-style themes now use the first material/ambient set like GalacticScale's generated ThemeProto, avoiding random vanilla style material variants.
- SulfurSea terrain and atmosphere tinting now follows GalacticScale's tint process more closely.
- Removed the broad runtime brightness clamp and added one-time terrain material diagnostics in the log for registered GalacticScale-style themes.

# DSPAddPlanet 3.0.8

- Fixed GalacticScale-style vein generation so Beach/BeachCold use DSPAddPlanet's local GS2 vein generator even when the game enters the normal vanilla algorithm path.
- Beach/BeachCold terrain materials now use a safer land-material source and reduced ground brightness instead of directly inheriting OceanWorld terrain brightness.
- Added a clearer startup/build tag and a one-time log line when a GalacticScale-style vein generator is used.

# DSPAddPlanet 3.0.7

- Beach and BeachCold now use a DSPAddPlanet-local GS2-style vein generator instead of vanilla OceanWorld/Prairie vein generation.
- Added GalacticScale Beach vein composition: Silicium, Bamboo, Fractal and Grat.
- Wrote the same Beach vein composition into the registered ThemeProto as a fallback for scan/UI paths.

# DSPAddPlanet 3.0.6

- Reapplied GalacticScale-style theme materials before planet modeling creates renderer materials.
- Updated loaded planets in place instead of replacing material objects after renderers have already captured them.
- Added a runtime display refresh for registered GalacticScale-style themes so ocean and atmosphere radius/intensity values follow the actual planet instance.

# DSPAddPlanet 3.0.5

- Added a small GalacticScale-style terrain path for DSPAddPlanet's registered themes without enabling GalacticScale galaxy generation.
- Beach and SulfurSea now use their GalacticScale terrain settings instead of relying on vanilla planet terrain plus material edits.
- Reduced high-emission material tinting that could make GalacticScale-style themes appear overexposed.
- Fixed the StationComponent startup transpiler crash on DSP 0.10.34 by reading integer IL operands safely.
- Declared incompatibility with the original IndexOutOfRange.DSPAddPlanet plugin to avoid double-loading the old mod and this fork.

# DSPAddPlanet 3.0.4

- Reworked Beach and BeachCold to use a dedicated GalacticScale-style generation path.
- Applied GalacticScale's OceanWorld material colors and shader parameters to Beach so it no longer looks like a vanilla prairie/Mediterranean mix.
- Updated internal plugin identity and assembly version to Zincon / DSPAddPlanet-moon^2-patch 3.0.4.

# DSPAddPlanet 3.0.3

- Removed the extra bright Beach and BeachCold material tint so Beach follows GalacticScale's original material behavior more closely.

# DSPAddPlanet 3.0.2

- Package renamed to Zincon-DSPAddPlanet-moon^2-patch for this custom fork.
- Added GalacticScale-style theme material reapply after theme preload to reduce washed-out/overbright planets.
- Fixed ocean material binding in the planet simulator so water ambient lighting updates correctly.
- Added a startup build tag for easier log verification.

# DSPAddPlanet 3.0.1

- Added GalacticScale-style themes that can be used without enabling GalacticScale.
- Added support for moon-of-moon orbit configuration.
- Removed small-planet themes that are unsafe in the vanilla generator.
