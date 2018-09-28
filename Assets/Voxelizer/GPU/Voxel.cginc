#ifndef __INCLUDE_VOXEL__
#define __INCLUDE_VOXEL__

struct Voxel
{
	float3 position;
	bool fill;
	bool front;
};

struct AABB
{
	float3 min, max, center;
};

struct Plane
{
	float3 normal;
	float distance;
};

#endif