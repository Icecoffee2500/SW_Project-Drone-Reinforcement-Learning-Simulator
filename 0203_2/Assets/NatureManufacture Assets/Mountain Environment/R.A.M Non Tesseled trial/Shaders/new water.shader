Shader "Custom/SWater"
{
    Properties
    {
        _BumpMap("BumpMap", 2D) = "Bump"{}
        _WaveSpeed("Wave Speed", float) = 0.05
        _WavePower("Wave Power", float) = 0.2
        _WaveTilling("Wave Tilling", float) = 25

        _CubeMap("CubeMap", Cube) = ""{}

        _SpacPow("Spacular Power", float) = 2
        _WaveNormalMap("NormalMap", 2D) = "bump" {}
        _DistortionMap("DistortionMap", 2D) = "black" {}

		

    }
       
        
        SubShader
    {
        Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent" } //"Geometry+999" }
        //Tags { "RenderType" = "Opaque" }
 
        CGPROGRAM
       
        #pragma surface surf _CatDarkLight  vertex:vert noshadow
 
 
        void vert(inout appdata_full v)
        {
            v.vertex.y += sin((abs(v.texcoord.x * 2.0f - 1.0f)*10.0f) + _Time.y*0.8f) * 0.12f +
                sin((abs(v.texcoord.y * 2.0f - 1.0f)*10.0f) + _Time.y*0.8f) * 0.12f;
        }
 
 
        sampler2D _WaveNormalMap;
        sampler2D _DistortionMap;
        sampler2D _GrabTexture;        
        
		
		
        struct Input
        {
			
            float2 uv_WaveNormalMap;
            float2 uv_DistortionMap;
            float3 viewDir;
 
            float4 screenPos;    
 
            float3 worldRefl;
            INTERNAL_DATA
        };
		

        fixed4 _Color;

		
 
        void surf (Input i, inout SurfaceOutput o)
        {  
            //o.Albedo = 1.0f;
            
			
            float fNormalWaveSpeed = 0.025f;
            float3 fNormal_L = UnpackNormal(tex2D(_WaveNormalMap, i.uv_WaveNormalMap - float2(0, _Time.y*fNormalWaveSpeed)));
            float3 fNormal_R = UnpackNormal(tex2D(_WaveNormalMap, i.uv_WaveNormalMap + float2(0, _Time.y*fNormalWaveSpeed)));
            float3 fNormal_T = UnpackNormal(tex2D(_WaveNormalMap, i.uv_WaveNormalMap - float2(_Time.y*fNormalWaveSpeed, 0)));
            float3 fNormal_B = UnpackNormal(tex2D(_WaveNormalMap, i.uv_WaveNormalMap + float2(_Time.y*fNormalWaveSpeed, 0)));
            
            o.Normal = (fNormal_L + fNormal_R + fNormal_T + fNormal_B) / 4.0f;
 
 
            float3 fWorldReflectionVector = WorldReflectionVector(i, o.Normal).xyz;
            float3 fReflection = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, fWorldReflectionVector).rgb * unity_SpecCube0_HDR.r;
 
 
            float4 fDistortion = tex2D(_DistortionMap, i.uv_DistortionMap + _Time.y*0.05f);    
 
            float3 scrPos = i.screenPos.xyz / (i.screenPos.w + 0.00001f);    
            float4 fGrab = tex2D(_GrabTexture, scrPos.xy + (fDistortion.r * 0.2f));        
 
            float fNDotV = dot(o.Normal, i.viewDir);
            float fRim = saturate(pow(1 - fNDotV + 0.1f, 1));
 
            o.Emission = lerp(fGrab.rgb, fReflection, fRim);
            o.Alpha = 1;
			
        }
 
        float4 Lighting_CatDarkLight(SurfaceOutput s, float3 lightDir, float3 viewDir, float atten)
        {
            float4 fSpec = float4(0.0f, 0.0f, 0.0f, 0.0f);
            float3 fHalfVector = normalize(lightDir + viewDir);
            float fSpecHDotN = saturate(dot(s.Normal, fHalfVector));
            fSpecHDotN = pow(fSpecHDotN, 100.0f);
 
            float4 fFinalColor = 0.0f;
            fFinalColor = fSpecHDotN;
 
            return fFinalColor;
        }
        ENDCG
    }
        /*
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            GrabPass{}

            CGPROGRAM
            #pragma surface surf WLight vertex:vert noambient noshadow 

            #pragma target 3.0

            sampler2D _BumpMap;
            float _WaveSpeed;
            float _WavePower;
            float _WaveTilling;

            samplerCUBE _CubeMap;

            sampler2D _GrabTexture;
            float _SpacPow;

            float dotData;

            struct Input
            {
                float2 uv_BumpMap;
                float3 worldRefl;
                float4 screenPos;
                float3 viewDir;
                INTERNAL_DATA
            };
            

           

            void vert(inout appdata_full v)
            {
                v.vertex.y = sin(abs(v.texcoord.x * 2 - 1) * _WaveTilling + _Time.y) * _WavePower;
            }

            void surf(Input IN, inout SurfaceOutput o)
            {
                float4 nor1 = tex2D(_BumpMap, IN.uv_BumpMap + float2(_Time.y * _WaveSpeed, 0));
                float4 nor2 = tex2D(_BumpMap, IN.uv_BumpMap - float2(_Time.y * _WaveSpeed, 0));
                o.Normal = UnpackNormal((nor1 + nor2) * 0.5);

                //
             //   float3 scrPos = IN.screenPos.xyz / (IN.screenPos.w + 0.00001f);
               // float4 fGrab = tex2D(_GrabTexture, scrPos.xy);
                //

                float4 sky = texCUBE(_CubeMap, WorldReflectionVector(IN, o.Normal));
                float4 refrection = tex2D(_GrabTexture, (IN.screenPos / IN.screenPos.a).xy + o.Normal.xy * 0.03);

                dotData = pow(saturate(1 - dot(o.Normal, IN.viewDir)), 0.6);
                float3 water = lerp(refrection, sky, dotData).rgb;
                //
               // o.Emission = lerp(fGrab.rgb, reflection, 0.8f);
                //
                o.Albedo = water;
            }

            float4 LightingWLight(SurfaceOutput s, float3 lightDIr, float3 viewDir, float atten)
            {
                float3 refVec = s.Normal * dot(s.Normal, viewDir) * 2 - viewDir;
                refVec = normalize(refVec);

                float spcr = lerp(0, pow(saturate(dot(refVec, lightDIr)),256), dotData) * _SpacPow;

                return float4(s.Albedo + spcr.rrr,1);
            }
            ENDCG
        }*/
            FallBack "Diffuse"
}