using System;
using System.Numerics;

using OpenWheels;
using OpenWheels.Rendering;
using OpenWheels.Veldrid;

using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Primitives
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the window and the graphics device
            VeldridInit(out var window, out var graphicsDevice);

            // Create a texture storage that manages textures.
            // Textures in OpenWheels are represented with integer values.
            // A platform-specific ITextureStorageImplementation handles texture creation,
            // destruction and modification.
            var texStorage = new VeldridTextureStorage(graphicsDevice);

            // Create a renderer that implements the OpenWheels.Rendering.IRenderer interface
            // this guy actually draws everything to the backbuffer
            var renderer = new VeldridRenderer(graphicsDevice, texStorage);

            // OpenWheels always requires a texture to render, so renderer implementations only need a single shader
            // Even for untextured primitives we need to have a texture set. So we create a white 1x1 texture for those.
            ReadOnlySpan<Color> blankSpan = stackalloc Color[] { Color.White };
            var blank = texStorage.CreateTexture(1, 1, TextureFormat.Rgba32);
            texStorage.SetData(blank, blankSpan);

            // Our batcher lets use make calls to render lots of different primitive shapes and text.
            // When we're done the batcher can export draw calls so the renderer can use them do the drawing.
            // We won't use text rendering in this sample so we use the dummy text renderer.
            var batcher = new Batcher(texStorage, NullBitmapFontRenderer.Instance);

            var first = true;

            // We run the game loop here and do our drawing inside of it.
            VeldridRunLoop(window, graphicsDevice, () =>
            {
                renderer.Clear(Color.CornflowerBlue);

                // Start a new batch
                batcher.Start();

                // set the texture to the blank one we registered
                batcher.SetTexture(blank);

                // Let's draw some primitives. The API is very obvious, you can use IntelliSense to find supported shapes.

                batcher.FillRect(new RectangleF(10, 10, 100, 100), Color.LimeGreen);

                // Note that subsequent line segments are connected at their corners
                Span<Vector2> points = stackalloc Vector2[] { new Vector2(140, 20), new Vector2(320, 20), new Vector2(320, 120), new Vector2(420, 120) };
                batcher.DrawLineStrip(points, Color.Red, 20);

                batcher.FillTriangle(new Vector2(500, 20), new Vector2(600, 70), new Vector2(500, 120), Color.White);

                // The tessellation of the circle and corners for the rounded rectangle can be adjusted with the maxError parameter
                batcher.DrawCircle(new Vector2(700, 70), 50, Color.BlueViolet, 2);

                batcher.FillRoundedRect(new RectangleF(790, 10, 100, 100), 10, Color.SandyBrown);

                var pa = new Vector2(50, 220);
                var pb = new Vector2(150, 120);
                var pc = new Vector2(250, 220);
                var curve = new QuadraticBezier(pa, pb, pc);
                // The segmentation for curves can be adjusted with the segmentsPerLength parameter
                // Using that parameter and an (over)estimate of the length of the curve the number of segments
                // is computed
                batcher.DrawCurve(curve, Color.DarkGoldenrod, 2);

                var o = new Vector2(0, 100);
                var pd = new Vector2(200, 420);
                var curve2 = new CubicBezier(pa + o, pb + o, pd, pc + o);
                batcher.DrawCurve(curve2, Color.DarkOrchid, 2);

                // Finish the batch and let the renderer draw everything to the back buffer.
                batcher.Render(renderer);

                if (first)
                {
                    Console.WriteLine("Vertices: " + batcher.VerticesSubmitted);
                    Console.WriteLine("Indices: " + batcher.IndicesSubmitted);
                    Console.WriteLine("Batches: " + batcher.BatchCount);
                    first = false;
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
                WindowTitle = "OpenWheels Batcher Primitives"
            };

            window = VeldridStartup.CreateWindow(ref windowCI);
            graphicsDevice = VeldridStartup.CreateGraphicsDevice(window);
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
