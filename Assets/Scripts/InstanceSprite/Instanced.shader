Shader "Custom/Instanced"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Texture ("Texture", 2D) = "white" {}
        
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
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

       Pass
       {
            HLSLPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma require 2darray
            #pragma multi_compile_instancing
            #pragma shader_feature USE_LIGHT_PROBE
            #include <UnityShaderVariables.cginc>
            #include <UnityShaderUtilities.cginc>
            #include <HLSLSupport.cginc>
            #include <UnityInstancing.cginc>

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _TextureIndex)
            UNITY_DEFINE_INSTANCED_PROP(half4, _Pivot)
            UNITY_DEFINE_INSTANCED_PROP(half4, _NewUV)
            UNITY_INSTANCING_BUFFER_END(Props)

            half2 _Flip;
            Texture2D _Texture;
            SamplerState sampler_Texture;
            
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
            
            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                UNITY_SETUP_INSTANCE_ID (IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                // vertices transform matrix
                half4 pivot = UNITY_ACCESS_INSTANCED_PROP(Props, _Pivot);
                half4x4 m;
                m._11 = pivot.x; m._12 = 0; m._13 = 0; m._14 = pivot.z;
                m._21 = 0; m._22 = pivot.y; m._23 = 0; m._24 = pivot.w;
                m._31 = 0; m._32 = 0; m._33 = 1; m._34 = 0;
                m._41 = 0; m._42 = 0; m._43 = 0; m._44 = 1;

                OUT.vertex = UnityFlipSprite(IN.vertex.xyz, _Flip);

                // uv coordinate transform matrix
                half3x3 uvm;
                half4 newUV = UNITY_ACCESS_INSTANCED_PROP(Props, _NewUV);
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
                
                OUT.texcoord.x = uv.x;
                OUT.texcoord.y = uv.y;

                float3 normlWS = TransformObjectToWorld(IN.normal);
                OUT.color = float4(max(half3(0, 0, 0), normlWS), 1.0f);
                return OUT;
            }

            half4 SpriteFrag(v2f IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                half4 c = _Texture.Sample(sampler_Texture,IN.texcoord);

#if USE_LIGHT_PROBE
                c = c * float4(IN.color.xyz, 1.0);
#endif
                return c;
            }
            
            ENDHLSL
       }
    }
    FallBack "Diffuse"
}
