# OpenWheels [![Gitter chat](https://badges.gitter.im/Jjagg/OpenWheels.svg)](https://gitter.im/Jjagg/OpenWheels) [![Build status](https://travis-ci.org/Jjagg/OpenWheels.svg?branch=master)](https://travis-ci.org/Jjagg/OpenWheels)

OpenWheels is a set of libraries written mostly for use in game development. Its goal is to provide platform-agnostic and engine-agnostic solutions to common problems.

Note that the project is still in an early stage and the API can change at any time. Feedback is welcomed, please open an issue if you have any.

OpenWheels is split into multiple projects:

- [OpenWheels](src/OpenWheels): core classes for basic geometry and colors.
- [OpenWheels.BinPack](src/OpenWheels.BinPack): bin packing using MaxRects.
- [OpenWheels.Fonts](src/OpenWheels.Fonts): create font atlases (does not handle rasterizing glyphs).
- [OpenWheels.Fonts.ImageSharp](src/OpenWheels.Fonts.ImageSharp): render font atlases to images.
- [OpenWheels.Rendering](src/OpenWheels.Rendering): platform-agnostic 2D rendering.
- [OpenWheels.Rendering.ImageSharp](src/OpenWheels.Rendering.ImageSharp): load images and fonts for OpenWheels.Rendering using ImageSharp.
- [OpenWheels.Veldrid](src/OpenWheels.Veldrid): the reference backend for OpenWheels.Rendering using [Veldrid](https://github.com/mellinoe/veldrid).
- [OpenWheels.Game](src/OpenWheels.Game): Components typically used in game development: FPS counter, tweening, coroutines...
- [OpenWheels.Plotting](src/OpenWheels.Plotting): 2D plotting using OpenWheels.Rendering.
