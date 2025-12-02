#pragma once
namespace GameData
{
	class State
	{
	public:
		static void Load(std::string jsonDir, std::map<int, State*>&data);
		int ID = 0;
		int Type = 0;
		int Value = 0;
	};

	void from_json(const json& j, State& dataObj)
	{
		dataObj.ID = j.at("ID").get<int>();
		dataObj.Type = j.at("Type").get<int>();
		dataObj.Value = j.at("Value").get<int>();
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
				data.emplace(item.ID, new State(item));
			}
		}
	}

}
