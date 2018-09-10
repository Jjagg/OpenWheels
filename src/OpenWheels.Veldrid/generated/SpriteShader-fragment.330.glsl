#version 330 core

struct SamplerDummy { int _dummyValue; };

struct OpenWheels_Veldrid_Shaders_SpriteShader_VertexInput
{
    vec3 Position;
    vec4 Color;
    vec2 TextureCoordinates;
};

struct OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput
{
    vec4 Position;
    vec4 Color;
    vec2 TextureCoordinates;
};

uniform sampler2D Input;

const SamplerDummy Sampler = SamplerDummy(0);


vec4 FS( OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput input_)
{
    vec2 texCoords = input_.TextureCoordinates;
    vec4 inputColor = texture(Input, texCoords);
    return inputColor * input_.Color;
}


in vec4 fsin_0;
in vec2 fsin_1;
out vec4 _outputColor_;

void main()
{
    OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput input_;
    input_.Position = gl_FragCoord;
    input_.Color = fsin_0;
    input_.TextureCoordinates = fsin_1;
    vec4 output_ = FS(input_);
    _outputColor_ = output_;
}
