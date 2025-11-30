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
#include "SpawnData.h"
#include "State.h"
#include "MonsterState.h"
namespace GameData
{
	class StaticData
	{
	public:
		void Load(std::string jsonDir)
		{
			std::map <int, GameData::SpawnData*> _SpawnData;
			std::map <int, GameData::State*> _State;
			std::map <int, GameData::MonsterState*> _MonsterState;
			SpawnData::Load(jsonDir, _SpawnData);
			State::Load(jsonDir, _State);
			MonsterState::Load(jsonDir, _MonsterState);
			std::list<std::function<void()>> tasks;
			tasks.push_back([&](){
				for (auto& [key, value] : _SpawnData)
					value->State = _State[value->_State];
			});
			tasks.push_back([&](){
				for (auto& [key, value] : _MonsterState)
				{
					for (auto& [key2, value2] : value->Params)
						value2 = _State[key2];
				}

			});
			while(!tasks.empty()) { auto task = tasks.front(); tasks.pop_front(); task(); }
			SpawnData.insert(_SpawnData.begin(), _SpawnData.end());
			State.insert(_State.begin(), _State.end());
			MonsterState.insert(_MonsterState.begin(), _MonsterState.end());
		}

		std::map<int, const GameData::SpawnData*> SpawnData;
		std::map<int, const GameData::State*> State;
		std::map<int, const GameData::MonsterState*> MonsterState;
	};

}
