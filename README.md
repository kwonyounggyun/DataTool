# 📘 Excel → JSON & Code Generator  
**Excel 기반 데이터 → JSON → C++ / C# 코드 자동 생성 툴**

이 프로젝트는 **Excel 데이터를 JSON 형식으로 변환**하고,  
해당 JSON을 로드할 수 있는 **C++ / C# 데이터 클래스 파일을 자동 생성**하는 도구입니다.

데이터 파이프라인을 자동화하여  
**게임 서버·클라이언트의 정적 데이터 구조를 단일 소스에서 관리**할 수 있도록 설계되었습니다.

---

# 📦 Dependencies

### 🔹 공통
- **ClosedXML** – Excel 파일 읽기/쓰기  
- **System.CommandLine** – CLI 기반 실행  

### 🔹 C++ 코드 사용 시 필요 라이브러리
- **nlohmann/json**  
  생성된 `State.h`, `SpawnData.h` 등의 C++ 데이터 클래스는  
  JSON 파싱을 위해 **nlohmann/json** 라이브러리를 사용합니다.

➡ 사용 전 반드시 프로젝트에 `nlohmann/json` 포함 필요.

### 🔹 C# 코드 사용 시 필요 라이브러리
- **Newtonsoft.Json**  
  생성된 C# 코드에서 JSON 파싱을 위해 사용됩니다.

➡ `Newtonsoft.Json` 패키지를 프로젝트에 추가해야 정상 동작합니다.

---

# 🛠 Code Generation

지원 언어:

- **C++ (cpp)**  
- **C# (cs)**  

각 언어별로 JSON 데이터를 로드할 수 있는 **클래스 코드 자동 생성**이 이루어집니다.

---

# 🚀 CLI Command Guide

기본 실행 예시:

```sh
datagen -i <input_directory> [options...]
```

### 옵션 목록

| 옵션 | 설명 | 기본값 |
|------|------|--------|
| `-i, --input <input>` | **(필수)** Excel 데이터 파일들이 있는 디렉토리 | — |
| `-sn, --server_namespace` | 서버 코드 네임스페이스 | `GameData` |
| `-cn, --client_namespace` | 클라이언트 코드 네임스페이스 | `GameData` |
| `-s, --server <cpp\|cs>` | 서버 코드 출력 언어 | — |
| `-c, --client <cpp\|cs>` | 클라이언트 코드 출력 언어 | — |
| `-scp, --server_code_path` | 서버 코드 출력 폴더 | `./ServerCode` |
| `-ccp, --client_code_path` | 클라이언트 코드 출력 폴더 | `./ClientCode` |
| `-sjp, --server_json_path` | 서버 JSON 출력 폴더 | `./ServerJson` |
| `-cjp, --client_json_path` | 클라이언트 JSON 출력 폴더 | `./ClientJson` |
| `-?, -h, --help` | 도움말 및 사용법 표시 | — |
| `--version` | 버전 정보 표시 | — |

---

# 📂 Excel 시트 규칙

### 1️⃣ 데이터 시트(Data Sheet)

- 일반적인 시트 이름은 **데이터 클래스 이름**이 됩니다.
- 예: `State` 시트 → `State` 클래스 생성

### 2️⃣ 스키마 시트(Schema Sheet)

- 데이터 시트 이름 앞에 **언더스코어 `_`** 가 붙은 시트는 해당 데이터의 **스키마(필드 정의)** 를 뜻합니다.
- 예:  
  - `State`  → 데이터 시트  
  - `_State` → `State`의 필드를 정의하는 스키마 시트  

---

# ❗ 스키마 시트에서의 ID 규칙 (중요)

- 모든 데이터에는 **기본 키 필드 `ID`** 가 존재합니다.
- `ID` 필드는 **툴이 자동으로 생성**하며, 스키마 시트에서 직접 정의하면 안 됩니다.
- 즉, `_State`, `_SpawnData` 등의 스키마 시트에 `ID`, `id`, `Id` 등으로 필드를 추가하면 안 됩니다.

자동 생성되는 `ID` 필드의 특성:

- 타입: `int`
- `required = true`
- 서버/클라이언트 JSON 모두에 포함

---

# 📝 스키마 필드 컬럼 설명

스키마 시트는 각 데이터 클래스의 필드를 정의하며, 다음 컬럼을 사용합니다.

| 컬럼명 | 타입 | 설명 |
|--------|------|------|
| `index` | int | 필드 정렬 순서(0보다 큰 값 사용) |
| `name` | string | 필드 이름 / JSON key / 클래스 멤버 변수명 |
| `type` | string | `int`, `float`, `string`, `bool`, `vec3`, `vec2`, `datetime` 지원 |
| `container` | bool | `true`면 List/컨테이너로 생성, 값은 `,`로 구분 |
| `required` | bool | `true`면 빈값 허용하지 않음 |
| `ref` | string | `type = int` 일 때만 사용. 참조하는 시트 이름 |
| `server` | bool | 서버용 JSON에 필드를 포함할지 여부 |
| `client` | bool | 클라이언트용 JSON에 필드를 포함할지 여부 |

### container 사용 불가 타입

- `vec3`
- `vec2`
- `datetime`

---

# 📚 예시 1 — State

(이전 내용 동일 — 생략 없음)

---

# 📚 예시 2 — SpawnData

(이전 내용 동일 — 생략 없음)

---

# 📦 StaticData 로딩 구조

생성된 모든 데이터(`State`, `SpawnData`, …)는 `StaticData` 클래스를 통해 한 번에 로드됩니다.

(이전 내용 동일)

---

# 🎯 요약

이 툴은 다음 파이프라인을 자동화합니다:

```
Excel → JSON → C++/C# 코드 → StaticData 로딩 → 게임/애플리케이션에서 사용
```

### C++ 사용 시
✔ nlohmann/json 필수  
✔ Excel에 스키마만 정의하면 자동으로 파싱 코드 생성  

### C# 사용 시
✔ Newtonsoft.Json 필수  
✔ 동일한 Excel 기반으로 서버/클라이언트 데이터 구조를 자동 통일  

정적 데이터 관리가 매우 단순해지고 유지보수 비용이 크게 감소합니다.

