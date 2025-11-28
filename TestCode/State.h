#pragma once

namespace GameData
{
	class State
	{
	public:
		static void Load(std::string jsonDir, std::map<int, State*>&data);
		int Id = 0;
		int Type = 0;
		int Value = 0;
	};

	void from_json(const json& j, State& dataObj)
	{
		dataObj.Id = j.at("id").get<int>();
		dataObj.Type = j.at("type").get<int>();
		dataObj.Value = j.at("value").get<int>();
	}

	void State::Load(std::string jsonDir, std::map<int, State*>&data)
	{
		std::ifstream inputFile(jsonDir +"/State.json");
		if (inputFile.is_open())
		{
			std::stringstream buffer;
			buffer << inputFile.rdbuf();
			json j = json::parse(buffer.str());
			for (const auto& elem : j)
			{
				auto item = elem.get<State>();
				data.emplace(item.Id, new State(item));
			}
		}
	}

}
