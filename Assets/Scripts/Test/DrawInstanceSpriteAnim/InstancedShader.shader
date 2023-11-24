Shader "Instanced/InstancedShader" {
    Properties {
        _Texture ("Albedo (RGB)", 2D) = "white" {}
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            half2 _Flip;
            Texture2D _Texture;
            SamplerState sampler_Texture;
            
            struct SpriteInfoData
            {
                float4 uv;
                float4 pivot;
            };
            
            StructuredBuffer<SpriteInfoData> _spriteInfoBuffer;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float3 normal  : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float3 TransformObjectToWorld(float3 positionOS)
            {
                #if defined(SHADER_STAGE_RAY_TRACING)
                return mul(ObjectToWorld3x4(), float4(positionOS, 1.0)).xyz;
                #else
                return mul(UNITY_MATRIX_M, float4(positionOS, 1.0)).xyz;
                #endif
            }

            float4 TransformWorldToHClip(float3 positionWS)
            {
                return mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
            }

            inline float4 UnityFlipSprite(in float3 pos, in half2 flip)
            {
                return float4(pos.xy * flip, pos.z, 1.0);
            }
            
            v2f vert (appdata_t IN, uint instanceID : SV_InstanceID)
            {
                v2f OUT;
                // UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
 
                UNITY_SETUP_INSTANCE_ID (IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                
                SpriteInfoData sprite_info_data = _spriteInfoBuffer[instanceID];
                float4 pivot = sprite_info_data.pivot;
                half4x4 m;
                m._11 = pivot.x; m._12 = 0; m._13 = 0; m._14 = pivot.z;
                m._21 = 0; m._22 = pivot.y; m._23 = 0; m._24 = pivot.w;
                m._31 = 0; m._32 = 0; m._33 = 1; m._34 = 0;
                m._41 = 0; m._42 = 0; m._43 = 0; m._44 = 1;

                OUT.vertex = UnityFlipSprite(IN.vertex.xyz, _Flip);

                // uv coordinate transform matrix
                half3x3 uvm;
                half4 newUV = sprite_info_data.uv;
                uvm._11 = newUV.x; uvm._12 = 0; uvm._13 = newUV.z;
                uvm._21 = 0; uvm._22 = newUV.y; uvm._23 = newUV.w;
                uvm._31 = 0; uvm._32 = 0; uvm._33 = 1;

                // sample quad's original uv
                half3 uv = half3(IN.texcoord.x, IN.texcoord.y, 1);

                // apply uv transform
                uv = mul(uvm, uv);

                // transform quad's original mesh to sprite's mesh
                OUT.vertex = mul(m, OUT.vertex);
                OUT.vertex = TransformWorldToHClip(TransformObjectToWorld(OUT.vertex.xyz));

                // instance Pos
                uint instanceLineIndex = instanceID / 40;
                float4 offsetPos = float4(instanceLineIndex * 0.5f, 1 + 0.3f * (instanceID % 40),0,0);
                OUT.vertex.xyz += offsetPos.xyz;
                
                OUT.texcoord.x = uv.x;
                OUT.texcoord.y = uv.y;

                float3 normalWS = TransformObjectToWorld(IN.normal);
                OUT.color = float4(max(half3(0, 0, 0), normalWS), 1.0f);
                return OUT;
            }

            half4 frag (v2f IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 c = _Texture.Sample(sampler_Texture, IN.texcoord);
                 // c = c * float4(IN.color.xyz, 1.0);
                return c;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}