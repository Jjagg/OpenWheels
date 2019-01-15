using System;
using System.Numerics;

using OpenWheels;
using OpenWheels.Rendering;
using OpenWheels.Rendering.ImageSharp;
using OpenWheels.Veldrid;

using SixLabors.Fonts;
using SixLabors.ImageSharp;

using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Font
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

            // Create a renderer that implements the OpenWheels.Rendering.IRenderer interface.
            // This guy actually draws everything to the backbuffer.
            var renderer = new VeldridRenderer(graphicsDevice, texStorage);

            // To render text Batcher needs an IBitmapFontRenderer implementation.
            // Our batcher lets use make calls to render lots of different primitive shapes and text.
            // When we're done the batcher sends the draw calls to the renderer which will actually do the drawing.
            // textures using a string identifier. Internally in OpenWheels textures are identified by an integer.
            var batcher = new Batcher();

            // OpenWheels.Rendering.ImageSharp contains several extension methods to easily load 
            // images and fonts into an ITextureStorage implementation.
            // Using this library is the easiest way to handle font and texture loading, but it's a separate lib so you can
            // use another solution if you prefer.

            // The following call creates a font atlas and the corresponding image for the glyphs.
            // By default it includes only the basic Latin characters in the atlas.
            // The created image is registered with the renderer and the font can be
            // set using the Batcher by calling `SetFont(fontId)`.

            // m6x11 font by Daniel Linssen: https://managore.itch.io
            // Note that this is a pixel perfect font and so it will look good even though OpenWheels has pretty bad text rendering.
            var font = texStorage.LoadFont("Resources/m6x11.ttf", 32, (int) '?');

            // Google's Roboto font
            //var font = texStorage.LoadFont("Resources/Roboto-Medium.ttf", 32, (int) '?');

            var first = true;

            // We run the game loop here and do our drawing inside of it.
            VeldridRunLoop(window, graphicsDevice, () => 
            {
                renderer.Clear(Color.CornflowerBlue);

                // Start a new batch
                batcher.Start();

                // Note that drawing text changes the active texture to the font atlas texture.
                // So if you're rendering other stuff, make sure you set a texture before drawing anything else
                batcher.DrawText(font, "Hello, World!", new Vector2(100f, 80f), Color.Black);
                batcher.DrawText(font, "This is rendered from a font atlas!", new Vector2(100f, 130f), .5f,  Color.DarkRed);

                // Reset the transformation matrix
                batcher.PositionTransform = Matrix3x2.Identity;

                // Let's also render the font atlas so we can see how this works
                batcher.SetTexture(font.Texture);
                var atlasSize = texStorage.GetTextureSize(font.Texture);
                batcher.FillRect(new RectangleF(100, 170, atlasSize.Width, atlasSize.Height), Color.Black);

                // Finish the frame and let the renderer draw everything to the back buffer.
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
                WindowTitle = "OpenWheels Text Rendering"
            };

            window = VeldridStartup.CreateWindow(ref windowCI);

            // no debug, no depth buffer and enable v-sync
            var gdo = new GraphicsDeviceOptions(false, null, syncToVerticalBlank: true);
            graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, gdo, GraphicsBackend.OpenGL);
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
