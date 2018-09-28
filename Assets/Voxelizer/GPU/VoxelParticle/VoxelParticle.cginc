#ifndef __INCLUDE_VOXEL_PARTICLE__
#define __INCLUDE_VOXEL_PARTICLE__

struct VoxelParticle
{
	float3 position;
	float3 scale;
	float3 velocity;
	float lifeTime;
};

#endif