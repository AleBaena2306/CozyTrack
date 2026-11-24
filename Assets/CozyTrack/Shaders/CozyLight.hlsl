struct A { float4 pos : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; float4 col : COLOR; };
struct V { float4 cs : SV_POSITION; float3 ws : TEXCOORD0; float3 n : TEXCOORD1; float2 uv : TEXCOORD2; float fog : TEXCOORD3; float4 col : COLOR; };

V vert (A a)
{
    V o;
    VertexPositionInputs p = GetVertexPositionInputs(a.pos.xyz);
    o.cs = p.positionCS; o.ws = p.positionWS;
    o.n = TransformObjectToWorldNormal(a.n);
    o.uv = TRANSFORM_TEX(a.uv, _BaseMap);
    o.fog = ComputeFogFactor(p.positionCS.z);
    o.col = a.col;
    return o;
}

half4 frag (V i, bool front : SV_IsFrontFace) : SV_Target
{
    float3 n = normalize(i.n); if (!front) n = -n;
    half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
    Light L = GetMainLight(TransformWorldToShadowCoord(i.ws));
    float ndl = saturate(dot(n, L.direction)) * L.shadowAttenuation;
    float3 amb = lerp(_CozyGround.rgb, _CozySky.rgb, n.y * 0.5 + 0.5);
    float3 lit;
    if (_CozyToon > 0.5)
    {
        float band = step(0.28, ndl);
        float3 shade = amb * 0.62 * lerp(float3(1, 1, 1), _CozyShadowTint.rgb, 0.55);
        lit = lerp(shade, amb * 0.55 + L.color * 0.8, band);
    }
    else lit = amb * 0.6 + L.color * ndl * 0.85;
    float3 c = tex.rgb * lit + _EmissionColor.rgb;
    if (_CozyToon > 0.5 && _Rim > 0.5)
    {
        float3 v = normalize(GetWorldSpaceViewDir(i.ws));
        float r = pow(1 - saturate(dot(n, v)), 3);
        c += r * (0.45 * tex.rgb + 0.12);
    }
    c = MixFog(c, i.fog);
    return half4(c, tex.a);
}
