using System.Numerics;
using ShaderGen;

[assembly: ShaderSet("SpriteShader", "OpenWheels.Veldrid.Shaders.SpriteShader.VS", "OpenWheels.Veldrid.Shaders.SpriteShader.FS")]

namespace OpenWheels.Veldrid.Shaders
{
    public class SpriteShader
    {
        public Matrix4x4 Wvp;
        public Texture2DResource Input;
        public SamplerResource Sampler;

        [VertexShader]
        public FragmentInput VS(VertexInput input)
        {
            FragmentInput output;
            output.Position = ShaderBuiltins.Mul(Wvp, new Vector4(input.Position, 1));
            output.Color = input.Color;
            output.TextureCoordinates = input.TextureCoordinates;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            Vector2 texCoords = input.TextureCoordinates;
            Vector4 inputColor = ShaderBuiltins.Sample(Input, Sampler, texCoords);
            return inputColor * input.Color;
        }

        public struct VertexInput
        {
            [PositionSemantic]
            public Vector3 Position;
            [ColorSemantic]
            public Vector4 Color;
            [TextureCoordinateSemantic]
            public Vector2 TextureCoordinates;
        }

        public struct FragmentInput
        {
            [SystemPositionSemantic]
            public Vector4 Position;
            [ColorSemantic]
            public Vector4 Color;
            [TextureCoordinateSemantic]
            public Vector2 TextureCoordinates;
        }
    }
}
