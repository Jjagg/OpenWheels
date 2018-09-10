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
constant float4x4& Wvp;

ShaderContainer(
constant float4x4& Wvp_param
)
:
Wvp(Wvp_param)
{}
OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput VS( OpenWheels_Veldrid_Shaders_SpriteShader_VertexInput input)
{
    OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput output;
    output.Position = Wvp * float4(float4(input.Position, 1));
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}


};

vertex OpenWheels_Veldrid_Shaders_SpriteShader_FragmentInput VS(OpenWheels_Veldrid_Shaders_SpriteShader_VertexInput input [[ stage_in ]], constant float4x4 &Wvp [[ buffer(0) ]])
{
return ShaderContainer(Wvp).VS(input);
}
