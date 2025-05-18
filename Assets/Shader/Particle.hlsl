struct Particle
{
    float3 position;
    float3 direction;
    float velocity;

    float3 als;
    uint active;
};

struct ParticleStatic
{
    uint useGravity;
    float3 gravity;

    uint useVelocityCurve;
    uint velocityCurveCount;

    uint overwriteMode;
    uint renderMode;
};
struct InitializeParameters
{
    float3 dirction;
    uint enableRandomDirection;
    float3 randomDirectionRange;

    float size;
    uint enableRandomSize;
    float randomSizeRange;

    float velocity;
    uint enableRandomVelocity;
    float randomVelocityRange;

    float lifeTime;
    uint enableRandomLifeTime;
    float randomLifeTimeRange;
};