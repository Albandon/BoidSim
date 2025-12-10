float3 safe_normalize(float3 v)
{
    return normalize(v + 1e-12f);
}

float3 clamp_length(float3 v, float max_length)
{
    float len = length(v);
    float scale = min(1.0, max_length / max(len, 1e-6));
    return v * scale;
}