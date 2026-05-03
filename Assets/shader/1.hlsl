
#ifndef INFLATABLE_VERTEX_EXTRUSION_INCLUDED
#define INFLATABLE_VERTEX_EXTRUSION_INCLUDED

void ExtrudeNormal_float(
    float3 PositionOS,
    float3 NormalOS,
    float BaseInflate,
    float BreathAmount,
    float BreathSpeed,
    float PhaseOffset,
    float HitPulse,
    float Mask,
    out float3 OutPositionOS)
{
    float breath = sin(_Time.y * BreathSpeed + PhaseOffset) * BreathAmount;
    float amount = (BaseInflate + breath + HitPulse) * Mask;
    OutPositionOS = PositionOS + normalize(NormalOS) * amount;
}

void ExtrudeRadial_float(
    float3 PositionOS,
    float3 CenterOS,
    float BaseInflate,
    float RadialFalloff,
    float BreathAmount,
    float BreathSpeed,
    float PhaseOffset,
    float HitPulse,
    float Mask,
    out float3 OutPositionOS)
{
    float3 fromCenter = PositionOS - CenterOS;
    float dist = max(length(fromCenter), 1e-4);
    float3 dir = fromCenter / dist;

    float breath = sin(_Time.y * BreathSpeed + PhaseOffset) * BreathAmount;
    float falloff = saturate(1.0 - dist * RadialFalloff);
    float amount = (BaseInflate + breath + HitPulse) * falloff * Mask;

    OutPositionOS = PositionOS + dir * amount;
}

void ExtrudeHybrid_float(
    float3 PositionOS,
    float3 NormalOS,
    float3 CenterOS,
    float BaseInflate,
    float RadialFalloff,
    float BreathAmount,
    float BreathSpeed,
    float PhaseOffset,
    float HitPulse,
    float Mask,
    float Blend,
    out float3 OutPositionOS)
{
    float3 normalPos;
    ExtrudeNormal_float(
        PositionOS,
        NormalOS,
        BaseInflate,
        BreathAmount,
        BreathSpeed,
        PhaseOffset,
        HitPulse,
        Mask,
        normalPos);

    float3 radialPos;
    ExtrudeRadial_float(
        PositionOS,
        CenterOS,
        BaseInflate,
        RadialFalloff,
        BreathAmount,
        BreathSpeed,
        PhaseOffset,
        HitPulse,
        Mask,
        radialPos);

    OutPositionOS = lerp(normalPos, radialPos, saturate(Blend));
}

// ============================================================
// 充气接缝系统
// ============================================================

// 函数1: 计算接缝遮罩
// 输出：0.0 = 段中央（最鼓），1.0 = 接缝位置（最凹）
void SeamMask_float(
    float2 UV,
    float SeamCount,
    float SeamSharpness,
    out float OutMask)
{
    float wave = abs(sin(UV.y * 3.14159 * SeamCount));
    OutMask = pow(wave, SeamSharpness);
}

// 函数1b: 顶点位移（接缝下凹 + 段中央外鼓）
// 在 Vertex Stage 使用，需要网格有足够的环形细分
void SeamDisplace_float(
    float3 PositionOS,
    float3 NormalOS,
    float SeamMask,      // 来自 SeamMask 函数的输出（接缝=1，段中=0）
    float BulgeAmount,   // 段中央向外鼓多少，推荐 0.02~0.06
    float SinkAmount,    // 接缝处向内凹多少，推荐 0.02~0.05
    out float3 OutPositionOS)
{
    // 接缝处 SeamMask=1 → 内凹；段中央 SeamMask=0 → 外鼓
    float bulgeFactor = 1.0 - SeamMask;  // 段中央=1，接缝=0
    float displacement = bulgeFactor * BulgeAmount - SeamMask * SinkAmount;
    OutPositionOS = PositionOS + normalize(NormalOS) * displacement;
}

// 函数2: 接缝法线偏移（产生凹陷感）
void SeamNormal_float(
    float SeamMask,
    float SeamDepth,
    out float3 OutNormalTS)
{
    OutNormalTS = float3(0.0, SeamMask * SeamDepth, 1.0);
}

// 函数3: 接缝处暗化（模拟阴影/AO）
void SeamAO_float(
    float4 BaseColor,
    float SeamMask,
    float SeamDarkness,
    out float4 OutColor)
{
    float aoFactor = lerp(1.0, 1.0 - SeamDarkness, SeamMask);
    OutColor = float4(BaseColor.rgb * aoFactor, BaseColor.a);
}

#endif