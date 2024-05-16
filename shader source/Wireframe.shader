Shader "Unlit/Wireframe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color",color) = (1,1,1,0.3)
        _WireframeFrontColor("Wire front color", color) = (0,0,0,1)
        _WireframeBackColor("Wire back color", color) = (0, 0, 0, 1)
        _WireframeWidth("Wireframe width threshold", float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            fixed4 _WireframeFrontColor;
            fixed4 _WireframeBackColor;
            float _WireframeWidth;
            fixed4 _Color;

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // Ensures the wires aren't based off of tri size
                float3 unitWidth = fwidth(i.bary);
                float3 edge = step(unitWidth * _WireframeWidth, i.bary);
                float alpha = 1 - min(edge.x, min(edge.y, edge.z));
                return fixed4(_WireframeFrontColor.rgb, alpha);
            }

            [maxvertexcount(3)]
            void geom(triangle  v2f IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                o.pos = IN[0].vertex;
                o.bary = float3(1,0,0);
                triStream.Append(o);
                o.pos = IN[1].vertex;
                o.bary = float3(0,1,0);
                triStream.Append(o);
                o.pos = IN[2].vertex;
                o.bary = float3(0,0,1);
                triStream.Append(o);
            }
            ENDCG
        }

        Pass
        {
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom

            fixed4 _WireframeFrontColor;
            fixed4 _WireframeBackColor;
            float _WireframeWidth;
            fixed4 _Color;

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // Ensures the wires aren't based off of tri size
                float3 unitWidth = fwidth(i.bary);
                float3 edge = step(unitWidth * _WireframeWidth, i.bary);
                float alpha = 1 - min(edge.x, min(edge.y, edge.z));

                fixed4 color = fixed4(_WireframeBackColor.rgb, alpha);
                if(alpha < 1)
                {
                    color = _Color;
                }
                
                return fixed4(color);
            }

            [maxvertexcount(3)]
            void geom(triangle  v2f IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                o.pos = IN[0].vertex;
                o.bary = float3(1,0,0);
                triStream.Append(o);
                o.pos = IN[1].vertex;
                o.bary = float3(0,1,0);
                triStream.Append(o);
                o.pos = IN[2].vertex;
                o.bary = float3(0,0,1);
                triStream.Append(o);
            }
            ENDCG
        }
    }
}