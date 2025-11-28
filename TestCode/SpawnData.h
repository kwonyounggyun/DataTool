#pragma once

namespace GameData
{
	class SpawnData
	{
	public:
		static void Load(std::string jsonDir, std::map<int, SpawnData>&data);
		int Id = 0;
		std::string Name = "";
		Vec3 Pos;
		int State = 0;
	};

	void from_json(const json& j, SpawnData& dataObj)
	{
		dataObj.Id = j.at("id").get<int>();
		dataObj.Name = j.at("name").get<std::string>();;
		dataObj.Pos = j.at("pos").get<Vec3>();
		dataObj.State = j.at("state").get<int>();
	}

	void SpawnData::Load(std::string jsonDir, std::map<int, SpawnData>&data)
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
				data.emplace(item.Id, std::move(item));
			}
		}
	}

}
