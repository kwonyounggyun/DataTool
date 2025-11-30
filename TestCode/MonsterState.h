#pragma once
namespace GameData
{
	class State;
	class MonsterState
	{
	public:
		static void Load(std::string jsonDir, std::map<int, MonsterState*>&data);
		int Id = 0;
		std::map<int, State*> Params;
	};

	void from_json(const json& j, MonsterState& dataObj)
	{
		dataObj.Id = j.at("id").get<int>();
		{
			auto ids = j.at("params").get<std::list<int>>();
			for(auto id : ids) dataObj.Params[id] = nullptr;
		}
	}

	void MonsterState::Load(std::string jsonDir, std::map<int, MonsterState*>&data)
	{
		std::ifstream inputFile(jsonDir +"/MonsterState.json");
		if (inputFile.is_open())
		{
			std::stringstream buffer;
			buffer << inputFile.rdbuf();
			json j = json::parse(buffer.str());
			for (const auto& elem : j)
			{
				auto item = elem.get <MonsterState>();
				data.emplace(item.Id, new MonsterState(item));
			}
		}
	}

}
