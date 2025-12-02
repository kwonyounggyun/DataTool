#pragma once
#include <map>
#include <list>
#include <string>
#include <fstream>
#include <sstream>
#include <chrono>
#include <nlohmann/json.hpp>
using json = nlohmann::json;
namespace GameData
{
	struct Vec3
	{
	public:
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
	public:
		float x;
		float y;
	};

	void from_json(const json& j, Vec2& dataObj)
	{
		dataObj.x = j.at("x").get<float>();
		dataObj.y = j.at("y").get<float>();
	}

}
#include "Event.h"
#include "SpawnData.h"
#include "MonsterState.h"
#include "State.h"
namespace GameData
{
	class StaticData
	{
	public:
		void Load(std::string jsonDir)
		{
			std::map <int, GameData::Event*> _Event;
			std::map <int, GameData::SpawnData*> _SpawnData;
			std::map <int, GameData::MonsterState*> _MonsterState;
			std::map <int, GameData::State*> _State;
			Event::Load(jsonDir, _Event);
			SpawnData::Load(jsonDir, _SpawnData);
			MonsterState::Load(jsonDir, _MonsterState);
			State::Load(jsonDir, _State);
			SpawnData::LinkState(_SpawnData, _State);
			MonsterState::Linkparams(_MonsterState, _State);
			Event.insert(_Event.begin(), _Event.end());
			SpawnData.insert(_SpawnData.begin(), _SpawnData.end());
			MonsterState.insert(_MonsterState.begin(), _MonsterState.end());
			State.insert(_State.begin(), _State.end());
		}

		std::map<int, const GameData::Event*> Event;
		std::map<int, const GameData::SpawnData*> SpawnData;
		std::map<int, const GameData::MonsterState*> MonsterState;
		std::map<int, const GameData::State*> State;
	};

}
