
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

// 函数1b [平台专用]: 水平接缝遮罩（地板用 UV.x，段沿 X 方向排列）
// 用于地板/平台类物体，接缝方向为水平（重力方向垂直于接缝）
void SeamMask_H_float(
    float2 UV,
    float SeamCount,
    float SeamSharpness,
    out float OutMask)
{
    float wave = abs(sin(UV.x * 3.14159 * SeamCount));
    OutMask = pow(wave, SeamSharpness);
}

// 函数1c [通用]: 轴切换接缝遮罩（UseHorizontal=0用UV.y柱体，1用UV.x平台）
void SeamMask_Axis_float(
    float2 UV,
    float SeamCount,
    float SeamSharpness,
    float UseHorizontal,  // 0 = 柱体/竖向分段(UV.y), 1 = 平台/横向分段(UV.x)
    out float OutMask)
{
    float coord = lerp(UV.y, UV.x, saturate(UseHorizontal));
    float wave = abs(sin(coord * 3.14159 * SeamCount));
    OutMask = pow(wave, SeamSharpness);
}

// 函数1d [高级]: 非规则凹陷遮罩（随机酒窝/褶皱）
// 适用于你想打破完美圆柱体，产生类似皮革纽扣凹陷或不规则形变的区域
// Hash函数用于生成伪随机
float2 hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

void IrregularDents_float(
    float2 UV,
    float SeamCount,      // 对应节点面板上的 SeamCount
    float SeamSharpness,  // 对应节点面板上的 SeamSharpness
    out float OutMask)
{
    float2 p = UV * SeamCount;
    float2 i = floor(p);
    float2 f = frac(p);
    
    float minDist = 1.0;
    // 简单的 3x3 邻域 Voronoi 寻找最近的"凹陷点"
    for(int y = -1; y <= 1; y++)
    {
        for(int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 randomPoint = hash22(i + neighbor);
            // 让点在格子里稍微游动一下，产生不规则感
            randomPoint = 0.5 + 0.4 * sin(6.2831 * randomPoint); 
            float2 diff = neighbor + randomPoint - f;
            float dist = length(diff);
            minDist = min(minDist, dist);
        }
    }
    
    // minDist 越小（越靠近随机点），Mask 越接近 1（最凹）
    // 用 pow 调整锐利度
    float dent = 1.0 - saturate(minDist * 1.5); // 1.5是散布控制
    OutMask = pow(abs(dent), SeamSharpness);
}

// 函数2: 顶点位移（接缝/凹陷下凹 + 段中央外鼓）
// ⚠️ 在 Vertex Stage 使用。顶点数不够时效果会有棱角，可配合 GPU Tessellation 改善。
// 不改建模的前提下，建议 SinkAmount 0.04~0.08，BulgeAmount 0.03~0.06
void SeamDisplace_float(
    float3 PositionOS,
    float3 NormalOS,
    float SeamMask,      // 来自 SeamMask/SeamMask_H/SeamMask_Axis 的输出（接缝=1，段中=0）
    float BulgeAmount,   // 段中央向外鼓多少，推荐 0.03~0.06
    float SinkAmount,    // 接缝处向内凹多少，推荐 0.04~0.08
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