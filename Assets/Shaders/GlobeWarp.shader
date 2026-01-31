Shader "Custom/GlobeWarp"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        // Globe curvature settings
        _CurvatureX ("Curvature X", Float) = 0.001
        _CurvatureY ("Curvature Y", Float) = 0.001
        _GlobeRadius ("Globe Radius", Float) = 50.0
        _GlobeCenterY ("Globe Center Y", Float) = -20.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model with vertex modification
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _CurvatureX;
        float _CurvatureY;
        float _GlobeRadius;
        float _GlobeCenterY;

        // Add instancing support for this shader
        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        // Vertex shader - apply spherical curvature
        void vert(inout appdata_full v)
        {
            // Get world position
            float4 worldPos = mul(unity_ObjectToWorld, v.vertex);

            // Calculate distance from globe center (along XZ plane)
            float2 distXZ = worldPos.xz;
            float horizontalDist = length(distXZ);

            // Apply spherical curvature based on distance from center
            // This creates the illusion of wrapping around a globe
            float curvatureAmount = horizontalDist * horizontalDist;

            // Drop the terrain down based on distance (creates sphere curve)
            float yOffset = -curvatureAmount * _CurvatureY;

            // Also curve inward slightly on X and Z for more sphere-like appearance
            float3 direction = normalize(float3(distXZ.x, 0, distXZ.y));
            float inwardCurve = curvatureAmount * _CurvatureX * 0.5;

            worldPos.y += yOffset + _GlobeCenterY * (curvatureAmount * 0.01);
            worldPos.xz -= direction.xz * inwardCurve;

            // Convert back to object space
            v.vertex = mul(unity_WorldToObject, worldPos);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Standard texture and color application
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
