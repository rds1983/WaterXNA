#include "Macros.fxh"

DECLARE_TEXTURE_LINEAR_WRAP(Texture);

float4x4 World;
float4x4 View;
float4x4 Projection;

// General lighting
bool EnableLighting;
// Ambient lighting
float4 AmbientColor;
float AmbientIntensity;
// Diffuse lighting
float3 DiffuseLightDirection;
float4 DiffuseColor;
float DiffuseIntensity;

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 UV  		: TEXCOORD0;
	float3 Normal 	: NORMAL;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 UV  		: TEXCOORD0;
	float3 Normal   : TEXCOORD1;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

	// Change the position vector to be 4 units for proper matrix calculations.
	input.Position.w = 1.0f;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

	output.UV = input.UV;
	output.Normal = input.Normal;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	// Get the texture color.
	float4 textureColor = SAMPLE_TEXTURE(Texture, input.UV);

	if (EnableLighting)
	{
		// Interpolated normals can become unnormal--so normalize.
		float3 normal = normalize(input.Normal);

		// Light vector is opposite the direction of the light.
		float3 lightVector = -DiffuseLightDirection;

		// Determine the diffuse light intensity that strikes the vertex.
		float s = saturate(dot(lightVector, normal));

		// Compute the ambient, diffuse and specular terms separatly. 
		float3 diffuse = s * (DiffuseColor.rgb * DiffuseIntensity);

		// Combine the color from lighting with the texture color.
		float3 color = (diffuse + (AmbientColor.rgb * AmbientIntensity)) * textureColor.rgb;

		// Sum all the terms together and copy over the diffuse alpha.
		return float4(color, textureColor.a);
	}
	else
	{
		return textureColor;
	}
}

TECHNIQUE(Default, VertexShaderFunction, PixelShaderFunction);