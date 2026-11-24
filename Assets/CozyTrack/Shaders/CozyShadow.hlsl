float3 _LightDirection;
float3 _LightPosition;
struct A { float4 pos : POSITION; float3 n : NORMAL; };
float4 vert (A a) : SV_POSITION
{
    float3 ws = TransformObjectToWorld(a.pos.xyz);
    float3 nw = TransformObjectToWorldNormal(a.n);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 ld = normalize(_LightPosition - ws);
#else
    float3 ld = _LightDirection;
#endif
    float4 cs = TransformWorldToHClip(ApplyShadowBias(ws, nw, ld));
#if UNITY_REVERSED_Z
    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
#else
    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return cs;
}
half4 frag () : SV_Target { return 0; }
