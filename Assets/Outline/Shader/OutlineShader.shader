Shader "Hidden/OutlineShader"
{
	Properties
	{
		_Color ("Outline Color", Color) = (1,1,1,1)
		_Outline ("Outline Width", Range (0, 0.1)) = .01
		_IsOrthogonal ("Is Orthogonal", Range (0, 1)) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Front
	    Blend SrcAlpha OneMinusSrcAlpha
	        
		Pass
        {
	        CGPROGRAM
	        #include "UnityCG.cginc"
	        #pragma vertex vert
	        #pragma fragment frag
	       
	        uniform float _IsOrthogonal;
	        uniform float _Outline;
	        uniform float4 _Color;

	        struct v2f
	        {
	            float4 pos : POSITION;
	            float4 color : COLOR;
	        };
	       
	        v2f vert(appdata_base v)
	        {
	            v2f o;
	            o.pos = UnityObjectToClipPos (v.vertex);
	            float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
	            norm = normalize(norm);
	            float2 offset = (1.0-_IsOrthogonal)*TransformViewToProjection(norm.xy)+_IsOrthogonal*norm.xy;
	            o.pos.xy += offset  * _Outline;
	            o.color = _Color;
	            return o;
	        }
	       
	        half4 frag(v2f i) :COLOR
	        {
	            return i.color;
	        }
	               
	        ENDCG
	    }
	}
}
