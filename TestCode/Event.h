#pragma once
namespace GameData
{
	class Event
	{
	public:
		static void Load(std::string jsonDir, std::map<int, Event*>&data);
		int ID = 0;
		std::tm time;
		int value = 0;
	};

	void from_json(const json& j, Event& dataObj)
	{
		dataObj.ID = j.at("ID").get<int>();
		{
			auto dateStr = j.at("time").get<std::string>();
			std::stringstream ss(dateStr);
			ss >> std::get_time(&dataObj.time, "%Y-%m-%dT%H:%M:%S");
			dataObj.time.tm_isdst = 0;
		}
		dataObj.value = j.at("value").get<int>();
	}

	void Event::Load(std::string jsonDir, std::map<int, Event*>&data)
	{
		std::ifstream inputFile(jsonDir +"/Event.json");
		if (inputFile.is_open())
		{
			std::stringstream buffer;
			buffer << inputFile.rdbuf();
			json j = json::parse(buffer.str());
			for (const auto& elem : j)
			{
				auto item = elem.get<Event>();
				data.emplace(item.ID, new Event(item));
			}
		}
	}

}
