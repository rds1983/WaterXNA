#include "Macros.fxh"

DECLARE_CUBEMAP_LINEAR_MIRROR(SkyboxTexture);

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 CameraPosition;

struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float3 TextureCoordinate : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	float4 VertexPosition = mul(input.Position, World);
	output.TextureCoordinate = VertexPosition.xyz - CameraPosition;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	return SAMPLE_CUBEMAP(SkyboxTexture, normalize(input.TextureCoordinate));
}

TECHNIQUE(Default, VertexShaderFunction, PixelShaderFunction);