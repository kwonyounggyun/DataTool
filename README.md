# 📘 Excel → JSON & Code Generator  
**Excel 기반 데이터 → JSON → C++ / C# 코드 자동 생성 툴**

이 프로젝트는 **Excel 데이터를 JSON 형식으로 변환**하고,  
해당 JSON을 로드하는 **C++ / C# 데이터 클래스 파일을 자동 생성**하는 도구입니다.

데이터 파이프라인을 자동화하여  
**서버·클라이언트 정적 데이터 구조를 단일 소스(Excel)에서 통합 관리**할 수 있도록 설계되었습니다.

---

# 📦 Dependencies

## 🔹 공통
- **ClosedXML** – Excel 파일 읽기/쓰기  
- **System.CommandLine** – CLI 기반 실행  

## 🔹 C++ 코드 사용 시
- **nlohmann/json** (필수)  
  생성된 `.h` 파일들은 JSON 파싱을 위해 `nlohmann/json` 라이브러리를 사용합니다.

## 🔹 C# 코드 사용 시
- **Newtonsoft.Json** (필수)  
  C#으로 생성된 데이터 클래스는 Newtonsoft.Json 기반으로 파싱을 수행합니다.

---

# 🛠 Code Generation

지원 언어:

- **C++ (cpp)**  
- **C# (cs)**  

각 언어별 JSON 파싱 및 데이터 로드가 가능한 구조의 코드가 자동으로 생성됩니다.

---

# 🚀 CLI Command Guide

기본 실행:

```sh
datagen -i <input_directory> [options...]
```

### 옵션 목록

| 옵션 | 설명 | 기본값 |
|------|------|--------|
| `-i, --input <input>` | **(필수)** Excel 데이터 파일이 있는 디렉토리 | — |
| `-sn, --server_namespace` | 서버 코드 네임스페이스 | `GameData` |
| `-cn, --client_namespace` | 클라이언트 코드 네임스페이스 | `GameData` |
| `-s, --server <cpp\|cs>` | 서버 코드 출력 언어 | — |
| `-c, --client <cpp\|cs>` | 클라이언트 코드 출력 언어 | — |
| `-scp, --server_code_path` | 서버 코드 출력 폴더 | `./ServerCode` |
| `-ccp, --client_code_path` | 클라이언트 코드 출력 폴더 | `./ClientCode` |
| `-sjp, --server_json_path` | 서버 JSON 출력 폴더 | `./ServerJson` |
| `-cjp, --client_json_path` | 클라이언트 JSON 출력 폴더 | `./ClientJson` |
| `-?, -h, --help` | 도움말 표시 | — |
| `--version` | 버전 표시 | — |

---

# 📂 Excel 시트 규칙

## 1️⃣ 데이터 시트 (Data Sheet)
- 일반 시트 이름 = 생성될 **데이터 클래스**의 이름  
  예: `State` → `State` 클래스 생성

## 2️⃣ 스키마 시트 (Schema Sheet)
- 데이터 시트명 앞에 `_` 붙은 시트  
  예: `_State` → State 클래스의 필드 정의

---

# ❗ 스키마 시트 ID 규칙 (중요)

- **ID 필드는 스키마 시트에서 정의하지 않습니다.**
- 툴이 자동으로 생성합니다.
- 자동 생성된 ID 특성:
  - 타입: `int`
  - required: `true`
  - server/client JSON 모두 포함

스키마 시트에 ID를 정의하면 오류가 발생합니다.

---

# 📝 스키마 필드 컬럼 설명

| 컬럼명 | 타입 | 설명 |
|--------|------|-------|
| `index` | int | 정렬 순서(0보다 큰 값) |
| `name` | string | 필드 이름, JSON key, 클래스 멤버 변수명 |
| `type` | string | int, float, string, bool, vec3, vec2, datetime |
| `container` | bool | true면 List/Vector 생성, 값은 `,`로 구분 |
| `required` | bool | 빈 값 허용 여부 |
| `ref` | string | type=int 일 때만 사용, 참조 시트 이름 |
| `server` | bool | 서버 JSON 포함 여부 |
| `client` | bool | 클라이언트 JSON 포함 여부 |

### container 불가 타입
- vec3  
- vec2  
- datetime  

---

# 📚 예시 1 — State

## 🧾 `_State` (스키마 시트)

```
index  name   type  container  required  ref   server  client
1      Type   int   FALSE      TRUE             TRUE    TRUE
2      Value  int   FALSE      TRUE             TRUE    TRUE
```

## 📄 `State` (데이터 시트)

```
ID  Type  Value
1   1     12
2   2     12
3   3     12
...
14  14    12
```

## 📦 결과: `State.json`

```json
[
  { "ID": 1, "Type": 1, "Value": 12 },
  { "ID": 2, "Type": 2, "Value": 12 },
  { "ID": 3, "Type": 3, "Value": 12 },
  { "ID": 4, "Type": 4, "Value": 12 },
  { "ID": 5, "Type": 5, "Value": 12 },
  { "ID": 6, "Type": 6, "Value": 12 },
  { "ID": 7, "Type": 7, "Value": 12 },
  { "ID": 8, "Type": 8, "Value": 12 },
  { "ID": 9, "Type": 9, "Value": 12 },
  { "ID": 10, "Type": 10, "Value": 12 },
  { "ID": 11, "Type": 11, "Value": 12 },
  { "ID": 12, "Type": 12, "Value": 12 },
  { "ID": 13, "Type": 13, "Value": 12 },
  { "ID": 14, "Type": 14, "Value": 12 }
]
```

## 🧩 결과: `State.h`

```cpp
#pragma once
namespace bugat::GameDB
{
    class State
    {
    public:
        static void Load(std::string jsonDir, std::map<int, State*>& data);
        int ID = 0;
        int Type = 0;
        int Value = 0;
    };

    void from_json(const json& j, State& obj)
    {
        obj.ID = j.at("ID").get<int>();
        obj.Type = j.at("Type").get<int>();
        obj.Value = j.at("Value").get<int>();
    }

    void State::Load(std::string jsonDir, std::map<int, State*>& data)
    {
        std::ifstream inputFile(jsonDir + "/State.json");
        if (inputFile.is_open())
        {
            std::stringstream buffer;
            buffer << inputFile.rdbuf();
            json j = json::parse(buffer.str());
            for (auto& elem : j)
            {
                auto item = elem.get<State>();
                data.emplace(item.ID, new State(item));
            }
        }
    }
}
```

---

# 📚 예시 2 — SpawnData

## 🧾 `_SpawnData` (스키마 시트)

```
index  name      type    container  required  ref    server  client
1      Name      string  FALSE      TRUE              TRUE    TRUE
2      Pos       vec3    FALSE      FALSE             TRUE    TRUE
3      Resource  string  TRUE       TRUE              FALSE   TRUE
4      State     int     TRUE       TRUE      State   TRUE    TRUE
5      Switch    bool    TRUE       TRUE              TRUE    TRUE
6      Childs    string  TRUE       TRUE              TRUE    TRUE
7      Values    float   TRUE       TRUE              TRUE    TRUE
```

## 📄 `SpawnData` (데이터 시트)

```
ID  Name   Pos     Resource  State    Switch             Childs                 Values
1   Test1  1,2,3   Test1     1,2,3    true,false,true    test1,test2,test3      111,111,11
2   Test2  1,1,1   Test2     3,2,1    true,false,true    test1,test2,test3      111,111,11
3   Test3  1,1,1   Test3     3,1,4    true,false,true    test1,test2,test3      111,111,11
...
```

## 📦 결과: `SpawnData.json` (일부)

```json
{
  "ID": 1,
  "Name": "Test1",
  "Pos": { "x": 1.0, "y": 2.0, "z": 3.0 },
  "State": [1, 2, 3],
  "Switch": [true, false, true],
  "Childs": ["test1", "test2", "test3"],
  "Values": [111.0, 111.0, 11.0]
}
```

## 🧩 결과: `SpawnData.h`

```cpp
#pragma once
namespace bugat::GameDB
{
    class State;

    class SpawnData
    {
    public:
        static void LinkState(std::map<int, SpawnData*>& mapSpawn,
                              std::map<int, State*>& mapState)
        {
            for (auto& [key, value] : mapSpawn)
                for (auto& [sid, sptr] : value->State)
                    if (auto f = mapState.find(sid); f != mapState.end())
                        sptr = f->second;
        }

        static void Load(std::string jsonDir, std::map<int, SpawnData*>& data);

        int ID = 0;
        std::string Name = "";
        Vec3 Pos;
        std::map<int, const State*> State;
        std::vector<bool> Switch;
        std::vector<std::string> Childs;
        std::vector<float> Values;
    };

    void from_json(const json& j, SpawnData& obj)
    {
        obj.ID = j.at("ID").get<int>();
        obj.Name = j.at("Name").get<std::string>();
        obj.Pos = j.at("Pos").get<Vec3>();

        for (auto id : j.at("State").get<std::vector<int>>())
            obj.State[id] = nullptr;

        obj.Switch = j.at("Switch").get<std::vector<bool>>();
        obj.Childs = j.at("Childs").get<std::vector<std::string>>();
        obj.Values = j.at("Values").get<std::vector<float>>();
    }
}
```

---

# 📦 StaticData 로딩 구조

모든 데이터는 **StaticData** 클래스의 `Load()` 함수에서 한 번에 로딩됩니다.

## StaticData.h

```cpp
#include "SpawnData.h"
#include "State.h"

namespace bugat::GameDB
{
    class StaticData
    {
    public:
        void Load(std::string jsonDir)
        {
            std::map<int, SpawnData*> _Spawn;
            std::map<int, State*> _State;

            SpawnData::Load(jsonDir, _Spawn);
            State::Load(jsonDir, _State);

            SpawnData::LinkState(_Spawn, _State);

            SpawnData.insert(_Spawn.begin(), _Spawn.end());
            State.insert(_State.begin(), _State.end());
        }

        std::map<int, const SpawnData*> SpawnData;
        std::map<int, const State*> State;
    };
}
```

---

# 🎯 최종 요약

이 툴은 아래 파이프라인을 자동화합니다:

```
Excel → JSON → C++/C# 코드 생성 → StaticData 로딩 → 게임/앱에서 사용
```

### C++ 사용 시
- **nlohmann/json 필요**
- Excel 기반 스키마 → 자동 cpp 코드 생성

### C# 사용 시
- **Newtonsoft.Json 필요**
- 서버/클라 일관된 데이터 구조 유지

정적 데이터 파이프라인을 단일화하여 유지보수 효율을 크게 향상시킵니다.

