float random(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

//return [0,1)
float random(float x)
{
    return frac(sin(dot(x, 1.21313214)) * 43758.5453);
}

//[a,b)
float random(float seed, float a, float b)
{
    return lerp(a, b, random(seed)); // [0,1) → [a,b)
}


float random(float3 p) {
    return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453);
}

float3 random3(float3 p) {
    return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * float3(43758.5453, 28001.8384, 15731.7431));
}


// 平滑插值函数
float2 fade(float2 t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

// 计算梯度噪声
float grad(float2 p, float2 offset) {
    float2 gradDir = normalize(frac(sin(float2(dot(p + offset, float2(127.1, 311.7)),
                                               dot(p + offset, float2(269.5, 183.3)))) * 43758.5453) * 2.0 - 1.0);
    return dot(gradDir, offset);
}

// 2D Perlin Noise 实现
float perlinNoise(float2 uv) {
    float2 i = floor(uv);
    float2 f = frac(uv);

    float2 u = fade(f);
    
    return lerp(lerp(grad(i, float2(0, 0)), grad(i, float2(1, 0)), u.x),
                lerp(grad(i, float2(0, 1)), grad(i, float2(1, 1)), u.x), u.y);
}

// 生成 2D Value Noise
float valueNoise(float2 uv) {
    float2 i = floor(uv);
    float2 f = frac(uv);

    // 取得四个栅格点的随机值
    float a = random(i);
    float b = random(i + float2(1, 0));
    float c = random(i + float2(0, 1));
    float d = random(i + float2(1, 1));

    // 插值平滑
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}


float worleyNoise(float2 uv)
{
    float2 i_st = floor(uv);
    float2 f_st = frac(uv);

    float min_dist = 1.0;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 point1 = random(i_st + neighbor);
            float d = length(neighbor + point1 - f_st);
            min_dist = min(min_dist, d);
        }
    }

    return min_dist;
}


float worleyNoise3D(float3 p) {
    float3 i_st = floor(p);  // 取整得到网格坐标
    float3 f_st = frac(p);   // 获取小数部分（网格内偏移）

    float min_dist = 1.0;

    // 遍历相邻 3x3x3 立方体范围内的网格
    for (int z = -1; z <= 1; z++) {
        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                float3 neighbor = float3(x, y, z);
                
                // 计算该网格内的随机特征点
                float3 point1 = random3(i_st + neighbor);

                // 计算当前点到该特征点的距离
                float d = length(neighbor + point1 - f_st);
                
                // 记录最小距离
                min_dist = min(min_dist, d);
            }
        }
    }

    return min_dist;
}

