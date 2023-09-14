#include "Macros.fxh"

// Constant for Fresnel computation
static const float R0 = 0.02037f;

DECLARE_TEXTURE_LINEAR_WRAP(TextureWaveNormalMap0);
DECLARE_TEXTURE_LINEAR_WRAP(TextureWaveNormalMap1);
DECLARE_TEXTURE_LINEAR_CLAMP(TextureRefraction);
DECLARE_TEXTURE_LINEAR_CLAMP(TextureReflection);

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 CameraPosition;

matrix ReflectionMatrix;
Texture2D RefractionTexture;
Texture2D ReflectionTexture;

// Normal maps
Texture2D WaveNormalMap0;
Texture2D WaveNormalMap1;

// Texture coordinate offset vectors for scrolling
// normal maps.
float2 WaveMapOffset0;
float2 WaveMapOffset1;

//scale used on the wave maps
float WaveTextureScale;

bool EnableWaves;
bool EnableRefraction;
bool EnableReflection;
bool EnableFresnel;
bool EnableSpecularLighting;

float3 WaterColor;

// Sun
float3 SunColor;
float3 SunDirection;
float SunFactor;
float SunPower;

float RefractionReflectionMergeTerm;

// Function calculating fresnel term.
float ComputeFresnelTerm(float3 eyeVec, float3 cameraPosition)
{
	// We'll just use the y unit vector for spec reflection.
	float3 up = float3(0, 1, 0);

	// Compute the fresnel term to blend reflection and refraction maps
	float angle = saturate(dot(-eyeVec, up));
	float f = R0 + (1.0f - R0) * pow(1.0f - angle, 5.0);

	//also blend based on distance
	f = min(1.0f, f + 0.007f * cameraPosition.y);
	
	return f;
}

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 UV  		: TEXCOORD0;
};

struct VertexShaderOutput
{
	float4 Position					: POSITION0;
	float3 ToCameraVector			: TEXCOORD0;
	float2 WaveNormalMapPosition0	: TEXCOORD1;
	float2 WaveNormalMapPosition1	: TEXCOORD2;
	float4 ReflectionPosition		: TEXCOORD3;
	float4 RefractionPosition   	: TEXCOORD4;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;
	matrix viewProjectWorld;
	matrix reflectProjectWorld;

	// Change the position vector to be 4 units for proper matrix calculations.
	input.Position.w = 1.0f;

	float4x4 worldViewProj = mul(World, View);
	worldViewProj = mul(worldViewProj, Projection);

	output.Position = mul(input.Position, worldViewProj);

	float4 worldPos = mul(input.Position, World);
	output.ToCameraVector = worldPos.xyz - CameraPosition;

	// Calculate reflection position

	// Create the reflection projection world matrix.
	reflectProjectWorld = mul(ReflectionMatrix, Projection);
	reflectProjectWorld = mul(World, reflectProjectWorld);

	// Calculate the input position against the reflectProjectWorld matrix.
	output.ReflectionPosition = mul(input.Position, reflectProjectWorld);

	// Scroll texture coordinates.
	output.WaveNormalMapPosition0 = (input.UV * WaveTextureScale) + WaveMapOffset0;
	output.WaveNormalMapPosition1 = (input.UV * WaveTextureScale) + WaveMapOffset1;

	output.RefractionPosition = output.Position;

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	// Light vector is the opposite of the light direction
	float3 lightVector = normalize(-SunDirection);
	float4 refractionTexCoord;
	float4 reflectionTexCoord;
	float3 refractionColor;
	float3 reflectionColor;
	float4 color;

	input.ToCameraVector = normalize(input.ToCameraVector);

	float fresnelTerm = ComputeFresnelTerm(input.ToCameraVector, CameraPosition);

	// Compute the reflection from sunlight

	// Get the reflection vector from the eye
	float3 up = float3(0, 1.0f, 0);
	float3 sunLight = 0;

	if (!EnableFresnel)
		fresnelTerm = RefractionReflectionMergeTerm;

	// Transform the projective refraction texcoords to NDC space
	// and scale and offset xy to correctly sample a DX texture
	refractionTexCoord = input.RefractionPosition;
	refractionTexCoord.xyz /= refractionTexCoord.w;
	refractionTexCoord.x = 0.5f * refractionTexCoord.x + 0.5f;
	refractionTexCoord.y = -0.5f * refractionTexCoord.y + 0.5f;
	// refract more based on distance from the camera
	refractionTexCoord.z = .1f / refractionTexCoord.z; 

	// Transform the projective reflection texcoords to NDC space
	// and scale and offset xy to correctly sample a DX texture
	reflectionTexCoord = input.ReflectionPosition;
	reflectionTexCoord.xyz /= reflectionTexCoord.w;
	reflectionTexCoord.x = 0.5f * reflectionTexCoord.x + 0.5f;
	reflectionTexCoord.y = -0.5f * reflectionTexCoord.y + 0.5f;
	// reflect more based on distance from the camera
	reflectionTexCoord.z = .1f / reflectionTexCoord.z;

	// Sample refraction & reflection
	float3 R = 0;
	float2 refractionTex = 0;
	float2 reflectionTex = 0;

#ifdef WAVES
	// Sample wave normal map
	float3 normalT0 = SAMPLE_TEXTURE(TextureWaveNormalMap0, input.WaveNormalMapPosition0).rgb;
	float3 normalT1 = SAMPLE_TEXTURE(TextureWaveNormalMap1, input.WaveNormalMapPosition1).rgb;

	// Unroll the normals retrieved from the normal maps
	normalT0.yz = normalT0.zy;
	normalT1.yz = normalT1.zy;

	normalT0 = 2.0f * normalT0 - 1.0f;
	normalT1 = 2.0f * normalT1 - 1.0f;

	float3 normalT = normalize(0.5f * (normalT0 + normalT1));

	// Sample the texture pixels from the textures using the updated texture coordinates.
	R = normalize(reflect(input.ToCameraVector, normalT));
	refractionTex = refractionTexCoord.xy - refractionTexCoord.z * normalT.xz;
	reflectionTex = reflectionTexCoord.xy + reflectionTexCoord.z * normalT.xz;
#else
	R = normalize(reflect(input.ToCameraVector, up));
	refractionTex = refractionTexCoord.xy;
	reflectionTex = reflectionTexCoord.xy;
#endif

	refractionColor = SAMPLE_TEXTURE(TextureRefraction, refractionTex).rgb;
	reflectionColor = SAMPLE_TEXTURE(TextureReflection, reflectionTex).rgb;

	sunLight = SunFactor * pow(saturate(dot(R, lightVector)), SunPower) * SunColor;

	if (!EnableSpecularLighting)
		sunLight = 0;

	if (!EnableRefraction && !EnableReflection)
	{
		color.rgb = WaterColor + sunLight;
	}
	else
	{
		if (EnableRefraction && EnableReflection)
			color.rgb = WaterColor * lerp(refractionColor, reflectionColor, fresnelTerm) + sunLight;
		else if (EnableRefraction)
			color.rgb = WaterColor * refractionColor + sunLight;
		else
			color.rgb = WaterColor * reflectionColor + sunLight;
	}

	// alpha canal
	color.a = 1;

	return color;
}

TECHNIQUE(Default, VertexShaderFunction, PixelShaderFunction);