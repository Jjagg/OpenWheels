#include <metal_stdlib>
using namespace metal;
struct OpenWheels_Veldrid_Shaders_SpriteShader_VertexInput
{
    float3 Position [[ attribute(0) ]];
    float4 Color [[ attribute(1) ]];
    float2 TextureCoordinates [[ attribute(2) ]];
};

struct OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput
{
    float4 Position [[ position ]];
    float4 Color [[ attribute(0) ]];
    float2 TextureCoordinates [[ attribute(1) ]];
};

struct ShaderContainer {
thread texture2d<float> Input;
thread sampler Sampler;

ShaderContainer(
thread texture2d<float> Input_param, thread sampler Sampler_param
)
:
Input(Input_param), Sampler(Sampler_param)
{}
float4 FS( OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput input)
{
    float2 texCoords = input.TextureCoordinates;
    float4 inputColor = Input.sample(Sampler, texCoords);
    return inputColor * input.Color;
}


};

fragment float4 FS(OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput input [[ stage_in ]], texture2d<float> Input [[ texture(0) ]], sampler Sampler [[ sampler(0) ]])
{
return ShaderContainer(Input, Sampler).FS(input);
}
