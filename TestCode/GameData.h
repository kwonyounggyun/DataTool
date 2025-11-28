#pragma once
#include <map>
#include <list>
#include <string>
#include <fstream>
#include <sstream>
#include <nlohmann/json.hpp>
using json = nlohmann::json;

namespace GameData
{
	struct Vec3
	{
		float x;
		float y;
		float z;
	};

	void from_json(const json& j, Vec3& dataObj)
	{
		dataObj.x = j.at("x").get<float>();
		dataObj.y = j.at("y").get<float>();
		dataObj.z = j.at("z").get<float>();
	}

	struct Vec2
	{
		float x;
		float y;
	};

	void from_json(const json& j, Vec2& dataObj)
	{
		dataObj.x = j.at("x").get<float>();
		dataObj.y = j.at("y").get<float>();
	}

}
#include "SpawnData.h"
#include "State.h"
#include "MonsterState.h"
