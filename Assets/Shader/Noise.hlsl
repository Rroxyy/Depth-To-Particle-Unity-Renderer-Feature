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

//FBM（Fractal Brownian Motion）是一种将多个不同频率的噪声层叠加起来形成“细节丰富”的连续噪声的方法，常用于模拟自然纹理、火焰、烟雾、云彩等。
//不好看
float fbmWorleyNoise3D(float3 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    // 通常叠 4～6 层就足够
    for (int i = 0; i < 5; i++)
    {
        value += worleyNoise3D(p * frequency) * amplitude;
        frequency *= 2.0;   // 每层频率加倍
        amplitude *= 0.5;   // 每层幅度减半
    }

    return value;
}


float3 mod289(float3 x) {
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 mod289(float4 x) {
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 permute(float4 x) {
    return mod289(((x * 34.0) + 1.0) * x);
}


//https://thebookofshaders.com/edit.php#13/turbulence.frag
float snoise3D(float3 v) {
    const float2  C = float2(1.0 / 6.0, 1.0 / 3.0);
    const float4  D = float4(0.0, 0.5, 1.0, 2.0);

    // First corner
    float3 i  = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy;
    float3 x3 = x0 - D.yyy;

    // Permutations
    i = mod289(i);
    float4 p = permute(permute(permute(
                i.z + float4(0.0, i1.z, i2.z, 1.0))
              + i.y + float4(0.0, i1.y, i2.y, 1.0))
              + i.x + float4(0.0, i1.x, i2.x, 1.0));

    // Gradients
    float4 j = p - 49.0 * floor(p / 49.0);
    float4 x_ = floor(j / 7.0);
    float4 y_ = floor(j - 7.0 * x_);
    float4 x = (x_ * 2.0 + 0.5) / 7.0 - 1.0;
    float4 y = (y_ * 2.0 + 0.5) / 7.0 - 1.0;
    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, 0.0);

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 g0 = float3(a0.x, a0.y, h.x);
    float3 g1 = float3(a0.z, a0.w, h.y);
    float3 g2 = float3(a1.x, a1.y, h.z);
    float3 g3 = float3(a1.z, a1.w, h.w);

    // Normalize
    float4 norm = rsqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
    g0 *= norm.x;
    g1 *= norm.y;
    g2 *= norm.z;
    g3 *= norm.w;

    // Mix contributions
    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    m = m * m;

    float4 grad_dot = float4(dot(g0, x0), dot(g1, x1), dot(g2, x2), dot(g3, x3));
    float n= 42.0 * dot(m, grad_dot);
     
    return n;
}

//用来做Force Field上面的闪电噪声
//输出后考虑取反再继续幂出理想效果
//还行
float turbulence3D(float3 p, int octaves = 4) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++) {
        value += amplitude * abs(snoise3D(p * frequency));
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}


