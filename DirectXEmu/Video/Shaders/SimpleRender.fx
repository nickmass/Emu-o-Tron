Texture2D tex2d;

SamplerState pointSampler
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

SamplerState linearSampler
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};


struct VS_IN {
	float4 pos : POSITION;
	float2 tex : TEXCOORD0;
};

struct PS_IN {
	float4 pos : SV_POSITION;
	float2 tex : TEXCOORD0;
};

PS_IN VS( VS_IN input ) {
	PS_IN output = (PS_IN)0;
	output.pos = input.pos;
	output.tex = input.tex;
	return output;
}


float4 PSPoint( PS_IN input ) : SV_Target {
	return tex2d.Sample( pointSampler, input.tex ).zyxw; //Have to swizzle to account for DX10s texture format not mirroring the format my scalers and NES PPU assume
}
float4 PSSmooth( PS_IN input ) : SV_Target {
	return tex2d.Sample( linearSampler, input.tex ).zyxw;
}

technique10 Render {
	pass P0 {
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PSPoint() ) );
	}
}

technique10 RenderSmooth {
	pass P0 {
		SetGeometryShader( 0 );
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PSSmooth() ) );
	}
}
