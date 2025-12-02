#pragma once
namespace GameData
{
	class State;
	class SpawnData
	{
	public:
		static void LinkState(std::map<int, GameData::SpawnData*>& mapSpawnData, std::map<int, GameData::State*>& mapState)
		{
			for (auto& [key, value] : mapSpawnData)
				for (auto& [key2, value2] : value->State)
					if (auto find = mapState.find(key2); find != mapState.end())
						value2 = find->second;
		}

		static void Load(std::string jsonDir, std::map<int, SpawnData*>&data);
		int ID = 0;
		std::string Name = "";
		Vec3 Pos;
		std::map<int, const State*> State;
		std::vector<bool> Switch;
		std::vector<std::string> Childs;
		std::vector<float> Values;
	};

	void from_json(const json& j, SpawnData& dataObj)
	{
		dataObj.ID = j.at("ID").get<int>();
		dataObj.Name = j.at("Name").get<std::string>();
		dataObj.Pos = j.at("Pos").get<Vec3>();
		{
			auto ids = j.at("State").get<std::vector<int>>();
			for(auto id : ids) dataObj.State[id] = nullptr;
		}
		dataObj.Switch = j.at("Switch").get<std::vector<bool>>();
		dataObj.Childs = j.at("Childs").get<std::vector<std::string>>();
		dataObj.Values = j.at("Values").get<std::vector<float>>();
	}

	void SpawnData::Load(std::string jsonDir, std::map<int, SpawnData*>&data)
	{
		std::ifstream inputFile(jsonDir +"/SpawnData.json");
		if (inputFile.is_open())
		{
			std::stringstream buffer;
			buffer << inputFile.rdbuf();
			json j = json::parse(buffer.str());
			for (const auto& elem : j)
			{
				auto item = elem.get<SpawnData>();
				data.emplace(item.ID, new SpawnData(item));
			}
		}
	}

}
