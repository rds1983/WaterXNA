//-----------------------------------------------------------------------------
// Macros.fxh
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

#ifdef SM4

// Macros for targetting shader model 4.0 (DX11)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_4_0 vsname (); PixelShader = compile ps_4_0 psname(); } }

#define SAMPLE_TEXTURE(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)
#define SAMPLE_CUBEMAP(Name, texCoord)  Name.Sample(Name##Sampler, texCoord)

#else


// Macros for targetting shader model 3.0 (mojoshader)

#define TECHNIQUE(name, vsname, psname ) \
	technique name { pass { VertexShader = compile vs_3_0 vsname (); PixelShader = compile ps_3_0 psname(); } }

#define SAMPLE_TEXTURE(Name, texCoord)  tex2D(Name##Sampler, texCoord)
#define SAMPLE_CUBEMAP(Name, texCoord)  texCUBE(Name##Sampler, texCoord)

#endif

#define DECLARE_TEXTURE_LINEAR_WRAP(Name) \
	texture2D Name; \
	sampler Name##Sampler = sampler_state { Texture = (Name); MipFilter = LINEAR; MinFilter = LINEAR; MagFilter = LINEAR; AddressU = Wrap; AddressV = Wrap; };
	
#define DECLARE_TEXTURE_LINEAR_MIRROR(Name) \
	texture2D Name; \
	sampler Name##Sampler = sampler_state { Texture = (Name); MipFilter = LINEAR; MinFilter = LINEAR; MagFilter = LINEAR; AddressU = Mirror; AddressV = Mirror; };

	
#define DECLARE_TEXTURE_LINEAR_CLAMP(Name) \
	texture2D Name; \
	sampler Name##Sampler = sampler_state { Texture = (Name); MipFilter = LINEAR; MinFilter = LINEAR; MagFilter = LINEAR; AddressU = Clamp; AddressV = Clamp; };
	
#define DECLARE_CUBEMAP_LINEAR_CLAMP(Name) \
	textureCUBE Name; \
	sampler Name##Sampler = sampler_state { Texture = (Name); MipFilter = LINEAR; MinFilter = LINEAR; MagFilter = LINEAR; AddressU = Clamp; AddressV = Clamp; };

#define DECLARE_CUBEMAP_LINEAR_MIRROR(Name) \
	textureCUBE Name; \
	sampler Name##Sampler = sampler_state { Texture = (Name); MipFilter = LINEAR; MinFilter = LINEAR; MagFilter = LINEAR; AddressU = Mirror; AddressV = Mirror; };
