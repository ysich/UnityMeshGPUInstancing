// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI/Default(RGB+A)"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _AlphaTex ("Alpha Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
        
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "UnityUI.cginc"
    // #include "../../Common/UIVertexID.cginc"
    
    struct appdata_t
    {
        float4 vertex   : POSITION;
        float4 color    : COLOR;
        float4 texcoord : TEXCOORD0;
    };

    struct v2f
    {
        float4 vertex   : SV_POSITION;
        fixed4 color    : COLOR;
        float4 texcoord  : TEXCOORD0;
        float4 worldPosition : TEXCOORD2;
    };
    
    fixed4 _Color;

    v2f vert(appdata_t IN)
    {
        v2f OUT;
        OUT.worldPosition = IN.vertex;
        OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
        OUT.texcoord = IN.texcoord;
        #ifdef UNITY_HALF_TEXEL_OFFSET
            OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
        #endif
        // float4 color = GetColorByVertexID(IN.color, IN.texcoord.z, _Color);
        float4 color = IN.color * _Color;
        OUT.color = color;
        return OUT;
    }

    sampler2D _MainTex;
    float4 _MainTex_ST;
    sampler2D _AlphaTex;
    float4 _ClipRect;

    fixed4 frag(v2f IN) : SV_Target
    {
        half4 color;
        color = tex2D(_MainTex, IN.texcoord.xy);
        fixed4 alpha = tex2D(_AlphaTex, IN.texcoord.xy);
        color.a = color.a * alpha.r;
        color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
        // half4 vertexColor = GetColorByVertexID(IN.color, IN.texcoord.z, _Color);
        half4 vertexColor = IN.color;
        if (dot(vertexColor, fixed4(1, 1, 1, 0)) == 0) 
        {
            fixed grey = dot(color.rgb, float3(0.299, 0.587, 0.114));
            color.rgb = fixed3(grey, grey, grey);
            color.a *= vertexColor.a;
        }
        else
        {
            color = color * vertexColor;
        }
        #ifdef UNITY_UI_ALPHACLIP
            clip (color.a - 0.001);
        #endif
        return color;
    }

    ENDCG

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            // "VertexIDType" = "Uv0z"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            ENDCG
        }
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
            // "VertexIDType" = "Uv0z"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            ENDCG
        }
    }
}
