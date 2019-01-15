using System;
using System.Numerics;

using OpenWheels;
using OpenWheels.Rendering;
using OpenWheels.Rendering.ImageSharp;
using OpenWheels.Veldrid;

using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

using Rectangle = OpenWheels.Rectangle;

namespace Texture
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the window and the graphics device
            VeldridInit(out var window, out var graphicsDevice);

            // See the Primitives sample for the basic concepts of OpenWheels
            var texStorage = new VeldridTextureStorage(graphicsDevice);
            var renderer = new VeldridRenderer(graphicsDevice, texStorage);
            var batcher = new Batcher(NullBitmapFontRenderer.Instance);
            var checkerBoardTextureId = texStorage.LoadTexture("checkerboard.png");

            // OpenWheels defines a sprite as an image that's part of a texture.
            // To create a sprite, we pass a texture and a region of that texture that contains the image
            // We can define the region either in pixels (using Sprite) or in UV coordinates (using UvSprite).
            // Let's create a sprite that draws 3/4th of the checkerboard using UvSprite.
            // So if our original texture looks like this:
            //         |##  |
            //         |##  |
            //         |  ##|
            //         |  ##|
            // We'll create a sprite that looks like this:
            //         |## |
            //         |## |
            //         |  #|

            var cbSize = texStorage.GetTextureSize(checkerBoardTextureId);
            var subSpriteRect = new RectangleF(0, 0, 0.75f, 0.75f);
            var checkerBoardSubSprite = new UvSprite(checkerBoardTextureId, subSpriteRect);

            var frame = 0;

            // We run the game loop here and do our drawing inside of it.
            VeldridRunLoop(window, graphicsDevice, () =>
            {
                renderer.Clear(Color.CornflowerBlue);

                // Start a new batch
                batcher.Start();

                // we set the texture using the texture id we got back when registering the texture
                // OpenWheels internally only works with sprites
                // If you set a texture on a batcher it will convert it to a sprite with the region being the
                // entire texture bounds
                batcher.SetTexture(checkerBoardTextureId);

                // The Batcher API is stateful. Anything we render now will use the checkerboard texture.
                // By default the UV coordinates 0, 0, 1, 1 are use, so our texture is stretched
                batcher.FillRect(new RectangleF(50, 20, 100, 100), Color.White);
                batcher.FillRect(new RectangleF(200, 20, 100, 200), Color.White);

                // Let's draw our subsprite
                batcher.SetUvSprite(checkerBoardSubSprite);
                batcher.FillRect(new RectangleF(350, 20, 100, 100), Color.White);

                // We can only draw 1 texture in a single draw call, but since our subsprite actually uses the same
                // texture as our full checkerboard the batcher can still combine the calls into a single batch.

                batcher.SetTexture(checkerBoardTextureId);
                // Most of the primitives support UV coordinates one way or another.
                batcher.FillCircle(new Vector2(550, 70), 50, Color.White, .25f);
                batcher.FillRoundedRect(new RectangleF(650, 20, 100, 100), 15, Color.White);

                var v1 = new Vector2(50, 280);
                var v2 = new Vector2(150, 380);
                batcher.DrawLine(v1, v2, Color.White, 6f);
                // Note that the texture rotates with the line
                // This is different from the circle(segment) primitives where we draw a cutout of the active texture
                // There are a lot of ways to UV-map shapes, but OpenWheels currently picks just one for each shape

                // we can set a matrix to transform UV coordinates
                // let's make our texture loop in length while keeping it's aspect ratio and UV across its width.

                // The sampler should wrap to be able to loop the texture (the default sampler state is LinearClamp)
                // Render state sticks across frames, so we could set it before the render loop as well
                batcher.SamplerState = SamplerState.LinearWrap;
                var v3 = new Vector2(200, 280);
                var v4 = new Vector2(300, 380);
                const float lineWidth = 10f;

                // we want our UV aspect ratio to be 1:1, but it's lineWidth:length and we want to use
                // the coordinate system of the width, so we normalize height to get the right aspect ratio
                // (note that height is defined as the forward direction of the line)

                var uvHeight = Vector2.Distance(v3, v4) / lineWidth;
                batcher.UvTransform = Matrix3x2.CreateScale(1f, uvHeight);
                batcher.DrawLine(v3, v4, Color.White, lineWidth);

                // Reset the uv transform
                batcher.UvTransform = Matrix3x2.Identity;

                // The color value we can pass to these methods is multiplied with our texture color at each pixel.
                batcher.FillRect(new RectangleF(350, 280, 100, 100), Color.Red);

                // Finish the batch and let the renderer draw everything to the back buffer.
                batcher.Render(renderer);

                if (frame < 2)
                {
                    // Note that the first frame renders in two batches because we change the sampler state
                    // halfway through.
                    // Every subsequent frame render in a single batch because the sampler state stays at LinearClamp
                    Console.WriteLine("Frame " + frame);
                    Console.WriteLine("Vertices: " + batcher.VerticesSubmitted);
                    Console.WriteLine("Indices: " + batcher.IndicesSubmitted);
                    Console.WriteLine("Batches: " + batcher.BatchCount);
                    Console.WriteLine();
                    frame++;
                }
            });

            renderer.Dispose();
            graphicsDevice.Dispose();
        }

        private static void VeldridInit(out Sdl2Window window, out GraphicsDevice graphicsDevice)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "OpenWheels Texture Sample"
            };

            window = VeldridStartup.CreateWindow(ref windowCI);

            // no debug, no depth buffer and enable v-sync
            var gdo = new GraphicsDeviceOptions(false, null, syncToVerticalBlank: true);
            graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, gdo);
        }

        private static void VeldridRunLoop(Sdl2Window window, GraphicsDevice graphicsDevice, Action action)
        {
            while (window.Exists)
            {
                window.PumpEvents();

                if (window.Exists)
                {
                    action();

                    graphicsDevice.SwapBuffers();
                    graphicsDevice.WaitForIdle();
                }
            }
        }
    }
}
