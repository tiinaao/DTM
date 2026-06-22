#ifndef NEAR_CAMERA_DISSOLVE_INCLUDED
#define NEAR_CAMERA_DISSOLVE_INCLUDED

static const float BAYER_4x4[16] =
{
    0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
    12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
     3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
    15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
};

void NearCameraDissolve_float(
    in float3 PositionWS,
    in float4 ScreenPos,
    in float DissolveStart,
    in float DissolveEnd,
    in float DitherScale,
    in float ExistingAlpha,
    out float Alpha,
    out float Dissolving)
{
    float3 camPos = _WorldSpaceCameraPos;
    float dist = distance(PositionWS, camPos);
    float ramp = smoothstep(DissolveEnd, DissolveStart, dist);
    Dissolving = ramp;

    float2 screenUV = ScreenPos.xy / ScreenPos.w;
    float2 pixelCoord = floor(screenUV * _ScreenParams.xy / 4.5);
    uint px = (uint) abs(pixelCoord.x) % 4;
    uint py = (uint) abs(pixelCoord.y) % 4;
    float threshold = BAYER_4x4[py * 4 + px];

    float ditherAlpha = max(0.15, step(threshold, ramp * 1.5 - 0.25));
    Alpha = ExistingAlpha * ditherAlpha;
}

void NearCameraDissolve_half(
    in half3 PositionWS,
    in half4 ScreenPos,
    in half DissolveStart,
    in half DissolveEnd,
    in half DitherScale,
    in half ExistingAlpha,
    out half Alpha,
    out half Dissolving)
{
    half3 camPos = (half3) _WorldSpaceCameraPos;
    half dist = distance(PositionWS, camPos);
    half ramp = smoothstep(DissolveEnd, DissolveStart, dist);
    Dissolving = ramp;

    half2 screenUV = ScreenPos.xy / ScreenPos.w;
    half2 pixelCoord = floor(screenUV * (half2) _ScreenParams.xy / 4.5);
    uint px = (uint) abs(pixelCoord.x) % 4;
    uint py = (uint) abs(pixelCoord.y) % 4;
    half threshold = (half) BAYER_4x4[py * 4 + px];

    half ditherAlpha = max(0.15, step(threshold, ramp * 1.5 - 0.25));
    Alpha = ExistingAlpha * ditherAlpha;
}
#endif